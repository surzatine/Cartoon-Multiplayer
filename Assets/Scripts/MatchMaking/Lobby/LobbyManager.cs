using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using FishNet.Managing.Scened;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Manages the lobby waiting room where players wait before game starts
/// </summary>
public class LobbyManager : NetworkBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private float autoStartCountdown = 5f;

    [Header("References")]
    [SerializeField] private LANNetworkManager lanNetworkManager;

    // Synced list of players in lobby
    //[SyncObject]
    private readonly SyncList<LobbyPlayer> lobbyPlayers = new SyncList<LobbyPlayer>();

    // Events
    public event Action<LobbyPlayer> OnPlayerJoinedLobby;
    public event Action<LobbyPlayer> OnPlayerLeftLobby;
    public event Action<LobbyPlayer> OnPlayerReadyChanged;
    public event Action<float> OnCountdownTick;
    public event Action OnGameStarting;

    private bool isCountingDown = false;
    private float countdownTimer = 0f;

    public IReadOnlyList<LobbyPlayer> LobbyPlayers => lobbyPlayers;
    public bool IsCountingDown => isCountingDown;
    public float CountdownTimer => countdownTimer;

    private void Awake()
    {
        if (lanNetworkManager == null)
            lanNetworkManager = FindAnyObjectByType<LANNetworkManager>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Lobby Manager started on server");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Subscribe to sync list changes
        lobbyPlayers.OnChange += OnLobbyPlayersChanged;
        
        Debug.Log("Lobby Manager started on client");
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        lobbyPlayers.OnChange -= OnLobbyPlayersChanged;
    }

    private void Update()
    {
        if (!IsServerInitialized) return;

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
                // Notify clients of countdown tick
                UpdateCountdownObserversRpc(countdownTimer);
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
                Debug.LogWarning($"Player {conn.ClientId} already in lobby");
                return;
            }
        }

        LobbyPlayer newPlayer = new LobbyPlayer
        {
            clientId = conn.ClientId,
            playerName = playerName,
            isReady = false,
            isHost = conn.ClientId == 0 // First player is host
        };

        lobbyPlayers.Add(newPlayer);
        Debug.Log($"Player {playerName} (ID: {conn.ClientId}) joined lobby");

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
            Debug.Log($"Player {playerToRemove.playerName} left lobby");

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

        Debug.Log($"New host assigned: {firstPlayer.playerName}");
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

                Debug.Log($"Player {player.playerName} ready state: {player.isReady}");

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
            if (!player.isReady && !player.isHost) // Host doesn't need to ready
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
            Debug.LogWarning("Non-host tried to force start game");
            return;
        }

        if (lobbyPlayers.Count < minPlayersToStart)
        {
            Debug.LogWarning($"Not enough players to start. Need {minPlayersToStart}, have {lobbyPlayers.Count}");
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
        
        Debug.Log($"Starting countdown: {autoStartCountdown} seconds");
        StartCountdownObserversRpc(autoStartCountdown);
    }

    [Server]
    private void StopCountdown()
    {
        if (!isCountingDown) return;

        isCountingDown = false;
        countdownTimer = 0f;
        
        Debug.Log("Countdown stopped");
        StopCountdownObserversRpc();
    }

    [Server]
    private void StartGame()
    {
        if (lobbyPlayers.Count < minPlayersToStart)
        {
            Debug.LogWarning("Cannot start game: not enough players");
            return;
        }

        Debug.Log("Starting game!");
        isCountingDown = false;

        // Notify all clients game is starting
        GameStartingObserversRpc();

        // Load game scene for all clients
        LoadGameScene();
    }

    [Server]
    private void LoadGameScene()
    {
        // Create scene load data
        SceneLoadData sld = new SceneLoadData(gameSceneName);
        sld.ReplaceScenes = ReplaceOption.All;
        
        // Get all connections in lobby
        NetworkConnection[] connections = new NetworkConnection[lobbyPlayers.Count];
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            connections[i] = ServerManager.Clients[lobbyPlayers[i].clientId];
        }

        sld.Options.AllowStacking = false;

        // Load scene for specific connections only (room isolation)
        SceneManager.LoadConnectionScenes(connections, sld);

        Debug.Log($"Loading game scene '{gameSceneName}' for {connections.Length} players");
    }

    #endregion

    #region Observer RPCs

    [ObserversRpc]
    private void PlayerJoinedLobbyObserversRpc(LobbyPlayer player)
    {
        OnPlayerJoinedLobby?.Invoke(player);
    }

    [ObserversRpc]
    private void PlayerLeftLobbyObserversRpc(LobbyPlayer player)
    {
        OnPlayerLeftLobby?.Invoke(player);
    }

    [ObserversRpc]
    private void PlayerReadyChangedObserversRpc(LobbyPlayer player)
    {
        OnPlayerReadyChanged?.Invoke(player);
    }

    [ObserversRpc]
    private void StartCountdownObserversRpc(float countdown)
    {
        Debug.Log($"Countdown started: {countdown} seconds");
        OnCountdownTick?.Invoke(countdown);
    }

    [ObserversRpc]
    private void StopCountdownObserversRpc()
    {
        Debug.Log("Countdown stopped");
    }

    [ObserversRpc]
    private void UpdateCountdownObserversRpc(float timeRemaining)
    {
        OnCountdownTick?.Invoke(timeRemaining);
    }

    [ObserversRpc]
    private void GameStartingObserversRpc()
    {
        Debug.Log("Game is starting!");
        OnGameStarting?.Invoke();
    }

    #endregion

    #region SyncList Callbacks

    private void OnLobbyPlayersChanged(SyncListOperation op, int index, LobbyPlayer oldPlayer, LobbyPlayer newPlayer, bool asServer)
    {
        switch (op)
        {
            case SyncListOperation.Add:
                Debug.Log($"[Client] Player added to lobby: {newPlayer.playerName}");
                break;
            case SyncListOperation.RemoveAt:
                Debug.Log($"[Client] Player removed from lobby: {oldPlayer.playerName}");
                break;
            case SyncListOperation.Set:
                Debug.Log($"[Client] Player updated: {newPlayer.playerName}");
                break;
        }
    }

    #endregion

    #region Utility Methods

    public LobbyPlayer? GetLocalPlayer()
    {
        if (!IsClientInitialized) return null;

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
