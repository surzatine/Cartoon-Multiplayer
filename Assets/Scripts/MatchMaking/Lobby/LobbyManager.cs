using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Complete lobby manager with waiting room and multi-room isolation
/// </summary>
public class LobbyManager : NetworkBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private float autoStartCountdown = 10f;
    [SerializeField] private float waitingTimeBeforeStart = 5f;

    [Header("References")]
    [SerializeField] private LANNetworkManager lanNetworkManager;
    [SerializeField] private MenuUIManager menuUIManager;

    // Room ID for isolation - each lobby instance has unique room
    private string roomId;

    // Synced list of players in THIS lobby
    private readonly SyncList<LobbyPlayer> lobbyPlayers = new SyncList<LobbyPlayer>();

    // Events
    public event Action<LobbyPlayer> OnPlayerJoinedLobby;
    public event Action<LobbyPlayer> OnPlayerLeftLobby;
    public event Action<LobbyPlayer> OnPlayerReadyChanged;
    public event Action<float> OnCountdownTick;
    public event Action OnGameStarting;

    private bool isCountingDown = false;
    private float countdownTimer = 0f;
    private bool gameStarted = false;

    public IReadOnlyList<LobbyPlayer> LobbyPlayers => lobbyPlayers;
    public bool IsCountingDown => isCountingDown;
    public float CountdownTimer => countdownTimer;
    public string RoomId => roomId;

    public static bool IsLobbyActive;

    private void Awake()
    {
        if (lanNetworkManager == null)
            lanNetworkManager = FindAnyObjectByType<LANNetworkManager>();

        if (menuUIManager == null)
            menuUIManager = FindAnyObjectByType<MenuUIManager>();

        // Generate unique room ID
        roomId = System.Guid.NewGuid().ToString();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Get room ID from current room data if available
        if (lanNetworkManager != null && lanNetworkManager.CurrentRoom != null)
        {
            roomId = lanNetworkManager.CurrentRoom.roomId;
        }

        Debug.Log($"[LobbyManager] Started on server for room: {roomId}");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        lobbyPlayers.OnChange += OnLobbyPlayersChanged;
        Debug.Log($"[LobbyManager] Started on client for room: {roomId}");
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        lobbyPlayers.OnChange -= OnLobbyPlayersChanged;
    }

    private void Update()
    {
        try
        {
            if (!IsServerInitialized) return;
        }
        catch
        {
            return;
        }
       
        if (gameStarted) return;

        // Handle countdown
        if (isCountingDown)
        {
            countdownTimer -= Time.deltaTime;

            if (countdownTimer <= 0f)
            {
                StartGame();
            }
            else
            {
                // Update countdown every 0.1 seconds
                if (Time.frameCount % 6 == 0)
                {
                    UpdateCountdownObserversRpc(countdownTimer);
                }
            }
        }
    }

    #region Player Management

    /// <summary>
    /// Add player to lobby (Server only)
    /// </summary>
    [Server]
    public void AddPlayerToLobby(NetworkConnection conn, string playerName)
    {
        // Check if player already in lobby
        foreach (var player in lobbyPlayers)
        {
            if (player.clientId == conn.ClientId)
            {
                Debug.LogWarning($"[LobbyManager {roomId}] Player {conn.ClientId} already in lobby");
                return;
            }
        }

        LobbyPlayer newPlayer = new LobbyPlayer
        {
            clientId = conn.ClientId,
            playerName = playerName,
            isReady = false,
            isHost = lobbyPlayers.Count == 0 // First player is host
        };

        lobbyPlayers.Add(newPlayer);
        Debug.Log($"[LobbyManager {roomId}] Player {playerName} (ID: {conn.ClientId}) joined lobby");

        // Update player count in room
        if (lanNetworkManager != null)
        {
            lanNetworkManager.UpdatePlayerCount(lobbyPlayers.Count);
        }

        // Notify all clients
        PlayerJoinedLobbyObserversRpc(newPlayer);
    }

    /// <summary>
    /// Remove player from lobby (Server only)
    /// </summary>
    [Server]
    public void RemovePlayerFromLobby(NetworkConnection conn)
    {
        LobbyPlayer playerToRemove = default;
        int indexToRemove = -1;

        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].clientId == conn.ClientId)
            {
                playerToRemove = lobbyPlayers[i];
                indexToRemove = i;
                break;
            }
        }

        if (indexToRemove >= 0)
        {
            lobbyPlayers.RemoveAt(indexToRemove);
            Debug.Log($"[LobbyManager {roomId}] Player {playerToRemove.playerName} left lobby");

            // Update player count
            if (lanNetworkManager != null)
            {
                lanNetworkManager.UpdatePlayerCount(lobbyPlayers.Count);
            }

            // Stop countdown if not enough players
            if (lobbyPlayers.Count < minPlayersToStart)
            {
                StopCountdown();
            }

            // Notify all clients
            PlayerLeftLobbyObserversRpc(playerToRemove);

            // If host left, assign new host
            if (playerToRemove.isHost && lobbyPlayers.Count > 0)
            {
                AssignNewHost();
            }
        }
    }

    [Server]
    private void AssignNewHost()
    {
        if (lobbyPlayers.Count == 0) return;

        var firstPlayer = lobbyPlayers[0];
        firstPlayer.isHost = true;
        lobbyPlayers[0] = firstPlayer;

        Debug.Log($"[LobbyManager {roomId}] New host assigned: {firstPlayer.playerName}");
        NotifyHostChangedObserversRpc(firstPlayer.clientId);
    }

    #endregion

    #region Ready System

    /// <summary>
    /// Toggle player ready state
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc(NetworkConnection conn = null)
    {
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].clientId == conn.ClientId)
            {
                var player = lobbyPlayers[i];
                player.isReady = !player.isReady;
                lobbyPlayers[i] = player;

                Debug.Log($"[LobbyManager {roomId}] Player {player.playerName} ready state: {player.isReady}");

                // Notify clients
                PlayerReadyChangedObserversRpc(player);

                // Check if all players are ready
                CheckAllPlayersReady();
                break;
            }
        }
    }

    [Server]
    private void CheckAllPlayersReady()
    {
        if (lobbyPlayers.Count < minPlayersToStart)
        {
            StopCountdown();
            return;
        }

        bool allReady = true;
        foreach (var player in lobbyPlayers)
        {
            if (!player.isReady && !player.isHost)
            {
                allReady = false;
                break;
            }
        }

        if (allReady && !isCountingDown)
        {
            StartCountdown();
        }
        else if (!allReady && isCountingDown)
        {
            StopCountdown();
        }
    }

    #endregion

    #region Game Start

    /// <summary>
    /// Force start game (Host only)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ForceStartGameServerRpc(NetworkConnection conn = null)
    {
        // Verify caller is host
        bool isHost = false;
        foreach (var player in lobbyPlayers)
        {
            if (player.clientId == conn.ClientId && player.isHost)
            {
                isHost = true;
                break;
            }
        }

        if (!isHost)
        {
            Debug.LogWarning($"[LobbyManager {roomId}] Non-host tried to force start game");
            return;
        }

        if (lobbyPlayers.Count < minPlayersToStart)
        {
            Debug.LogWarning($"[LobbyManager {roomId}] Not enough players to start. Need {minPlayersToStart}, have {lobbyPlayers.Count}");
            return;
        }

        StartGame();
    }

    [Server]
    private void StartCountdown()
    {
        if (isCountingDown) return;

        isCountingDown = true;
        countdownTimer = autoStartCountdown;

        Debug.Log($"[LobbyManager {roomId}] Starting countdown: {autoStartCountdown} seconds");
        StartCountdownObserversRpc(autoStartCountdown);
    }

    [Server]
    private void StopCountdown()
    {
        if (!isCountingDown) return;

        isCountingDown = false;
        countdownTimer = 0f;

        Debug.Log($"[LobbyManager {roomId}] Countdown stopped");
        StopCountdownObserversRpc();
    }

    [Server]
    private void StartGame()
    {
        if (lobbyPlayers.Count < minPlayersToStart)
        {
            Debug.LogWarning($"[LobbyManager {roomId}] Cannot start game: not enough players");
            return;
        }

        if (gameStarted)
        {
            Debug.LogWarning($"[LobbyManager {roomId}] Game already started");
            return;
        }

        Debug.Log($"[LobbyManager {roomId}] Starting game with {lobbyPlayers.Count} players!");
        isCountingDown = false;
        gameStarted = true;

        // Notify all clients game is starting
        GameStartingObserversRpc();

        // Show game UI after brief delay
        Invoke(nameof(ShowGameUI), waitingTimeBeforeStart);
    }

    [Server]
    private void ShowGameUI()
    {
        ShowGameUIObserversRpc();
    }

    #endregion

    #region Observer RPCs

    [ObserversRpc]
    private void PlayerJoinedLobbyObserversRpc(LobbyPlayer player)
    {
        OnPlayerJoinedLobby?.Invoke(player);
        Debug.Log($"[LobbyManager {roomId}] [Client] Player joined: {player.playerName}");
    }

    [ObserversRpc]
    private void PlayerLeftLobbyObserversRpc(LobbyPlayer player)
    {
        OnPlayerLeftLobby?.Invoke(player);
        Debug.Log($"[LobbyManager {roomId}] [Client] Player left: {player.playerName}");
    }

    [ObserversRpc]
    private void PlayerReadyChangedObserversRpc(LobbyPlayer player)
    {
        OnPlayerReadyChanged?.Invoke(player);
    }

    [ObserversRpc]
    private void NotifyHostChangedObserversRpc(int newHostId)
    {
        Debug.Log($"[LobbyManager {roomId}] [Client] New host: {newHostId}");
    }

    [ObserversRpc]
    private void StartCountdownObserversRpc(float countdown)
    {
        Debug.Log($"[LobbyManager {roomId}] Countdown started: {countdown} seconds");
        OnCountdownTick?.Invoke(countdown);
    }

    [ObserversRpc]
    private void StopCountdownObserversRpc()
    {
        Debug.Log($"[LobbyManager {roomId}] Countdown stopped");
        OnCountdownTick?.Invoke(0f);
    }

    [ObserversRpc]
    private void UpdateCountdownObserversRpc(float timeRemaining)
    {
        OnCountdownTick?.Invoke(timeRemaining);
    }

    [ObserversRpc]
    private void GameStartingObserversRpc()
    {
        Debug.Log($"[LobbyManager {roomId}] Game is starting!");
        OnGameStarting?.Invoke();
    }

    [ObserversRpc]
    private void ShowGameUIObserversRpc()
    {
        Debug.Log($"[LobbyManager {roomId}] Showing game UI");
        // TODO: Load game scene or show game UI
        // For now, just log - you'll implement your game UI here
    }

    #endregion

    #region SyncList Callbacks

    private void OnLobbyPlayersChanged(SyncListOperation op, int index, LobbyPlayer oldPlayer, LobbyPlayer newPlayer, bool asServer)
    {
        switch (op)
        {
            case SyncListOperation.Add:
                Debug.Log($"[LobbyManager {roomId}] [Client] Player added to lobby: {newPlayer.playerName}");
                break;
            case SyncListOperation.RemoveAt:
                Debug.Log($"[LobbyManager {roomId}] [Client] Player removed from lobby: {oldPlayer.playerName}");
                break;
            case SyncListOperation.Set:
                Debug.Log($"[LobbyManager {roomId}] [Client] Player updated: {newPlayer.playerName}");
                break;
        }
    }

    #endregion

    #region Utility Methods

    public LobbyPlayer? GetLocalPlayer()
    {
        if (!IsClientInitialized || ClientManager == null || ClientManager.Connection == null)
            return null;

        int localClientId = ClientManager.Connection.ClientId;

        foreach (var player in lobbyPlayers)
        {
            if (player.clientId == localClientId)
                return player;
        }

        return null;
    }

    public bool IsLocalPlayerHost()
    {
        var localPlayer = GetLocalPlayer();
        return localPlayer?.isHost ?? false;
    }

    public bool IsLocalPlayerReady()
    {
        var localPlayer = GetLocalPlayer();
        return localPlayer?.isReady ?? false;
    }

    public int GetReadyPlayerCount()
    {
        int count = 0;
        foreach (var player in lobbyPlayers)
        {
            if (player.isReady || player.isHost)
                count++;
        }
        return count;
    }

    #endregion
}

/// <summary>
/// Lobby player data structure
/// </summary>
[System.Serializable]
public struct LobbyPlayer
{
    public int clientId;
    public string playerName;
    public bool isReady;
    public bool isHost;
}