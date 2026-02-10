using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet.Object;

/// <summary>
/// UI Manager for the lobby waiting room
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private LANNetworkManager networkManager;
    [SerializeField] private MenuUIManager menuUIManager;
    [SerializeField] private LobbyInitializer lobbyInitializer;

    [Header("UI - Room Info")]
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text mapNameText;
    [SerializeField] private TMP_Text gameModeText;

    [Header("UI - Player List")]
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerListItemPrefab;

    [Header("UI - Buttons")]
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private TMP_Text readyButtonText;

    [Header("UI - Status")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private GameObject countdownPanel;

    private Dictionary<int, GameObject> playerListItems = new Dictionary<int, GameObject>();

    private void Start()
    {
        Debug.Log($"[Lobby UI] Start");

        if (lobbyManager == null)
            lobbyManager = FindAnyObjectByType<LobbyManager>();

        if (networkManager == null)
            networkManager = FindAnyObjectByType<LANNetworkManager>();

        SetupUI();
        SubscribeToEvents();
        UpdateRoomInfo();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SetupUI()
    {
        Debug.Log($"[Lobby UI] Setup UI {leaveButton == null}");

        // Setup buttons
        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyButtonClicked);

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameButtonClicked);
            startGameButton.gameObject.SetActive(false); // Only show for host
        }

        if (leaveButton != null)
        {
            Debug.Log("[Lobby UI] Setup leave button");
            leaveButton.onClick.AddListener(OnLeaveButtonClicked);
        }

        if (countdownPanel != null)
            countdownPanel.SetActive(false);
    }

    private void SubscribeToEvents()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnPlayerJoinedLobby += OnPlayerJoined;
            lobbyManager.OnPlayerLeftLobby += OnPlayerLeft;
            lobbyManager.OnPlayerReadyChanged += OnPlayerReadyChanged;
            lobbyManager.OnCountdownTick += OnCountdownTick;
            lobbyManager.OnGameStarting += OnGameStarting;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnPlayerJoinedLobby -= OnPlayerJoined;
            lobbyManager.OnPlayerLeftLobby -= OnPlayerLeft;
            lobbyManager.OnPlayerReadyChanged -= OnPlayerReadyChanged;
            lobbyManager.OnCountdownTick -= OnCountdownTick;
            lobbyManager.OnGameStarting -= OnGameStarting;
        }
    }

    private void Update()
    {
        //if(!LobbyManager.IsLobbyActive) return;
        if(!MenuUIManager.IsLobbyMenuActive) return;
        // Wait for lobby manager to be initialized
        if (lobbyManager == null || !lobbyManager.IsClientInitialized)
            return;

        UpdatePlayerList();
        UpdateButtonStates();
    }

    #region UI Updates

    private void UpdateRoomInfo()
    {
        if (networkManager == null || networkManager.CurrentRoom == null)
            return;

        var room = networkManager.CurrentRoom;

        if (roomNameText != null)
            roomNameText.text = room.roomName;

        if (mapNameText != null)
            mapNameText.text = $"Map: {room.mapName}";

        if (gameModeText != null)
            gameModeText.text = room.gameMode;

        UpdatePlayerCount();
    }

    private void UpdatePlayerCount()
    {
        if (playerCountText != null && lobbyManager != null)
        {
            int current = lobbyManager.LobbyPlayers.Count;
            int max = networkManager?.CurrentRoom?.maxPlayers ?? 8;
            playerCountText.text = $"Players: {current}/{max}";
        }
    }

    private void UpdatePlayerList()
    {
        if (lobbyManager == null || playerListContainer == null) return;

        // Get current player IDs
        HashSet<int> currentPlayerIds = new HashSet<int>();
        foreach (var player in lobbyManager.LobbyPlayers)
        {
            currentPlayerIds.Add(player.clientId);

            // Create or update player list item
            if (!playerListItems.ContainsKey(player.clientId))
            {
                CreatePlayerListItem(player);
            }
            else
            {
                UpdatePlayerListItem(player);
            }
        }

        // Remove players that left
        List<int> playersToRemove = new List<int>();
        foreach (var kvp in playerListItems)
        {
            if (!currentPlayerIds.Contains(kvp.Key))
            {
                playersToRemove.Add(kvp.Key);
            }
        }

        foreach (int playerId in playersToRemove)
        {
            if (playerListItems.ContainsKey(playerId))
            {
                Destroy(playerListItems[playerId]);
                playerListItems.Remove(playerId);
            }
        }

        UpdatePlayerCount();
    }

    private void CreatePlayerListItem(LobbyPlayer player)
    {
        if (playerListItemPrefab == null || playerListContainer == null)
            return;

        GameObject item = Instantiate(playerListItemPrefab, playerListContainer);
        playerListItems[player.clientId] = item;

        UpdatePlayerListItemContent(item, player);
    }

    private void UpdatePlayerListItem(LobbyPlayer player)
    {
        if (!playerListItems.ContainsKey(player.clientId))
            return;

        GameObject item = playerListItems[player.clientId];
        UpdatePlayerListItemContent(item, player);
    }

    private void UpdatePlayerListItemContent(GameObject item, LobbyPlayer player)
    {
        // Update player name
        TMP_Text nameText = item.transform.Find("PlayerName")?.GetComponent<TMP_Text>();
        if (nameText != null)
        {
            string displayName = player.playerName;
            if (player.isHost)
                displayName += " (Host)";
            nameText.text = displayName;
        }

        // Update ready status
        TMP_Text statusText = item.transform.Find("ReadyStatus")?.GetComponent<TMP_Text>();
        Image statusIcon = item.transform.Find("ReadyIcon")?.GetComponent<Image>();

        if (statusText != null)
        {
            if (player.isHost)
                statusText.text = "HOST";
            else
                statusText.text = player.isReady ? "READY" : "NOT READY";

            statusText.color = player.isReady || player.isHost ? Color.green : Color.yellow;
        }

        if (statusIcon != null)
        {
            statusIcon.color = player.isReady || player.isHost ? Color.green : Color.gray;
        }
    }

    private void UpdateButtonStates()
    {
        if (lobbyManager == null) return;

        // Get local player state safely
        bool isHost = false;
        bool isReady = false;

        try
        {
            isHost = lobbyManager.IsLocalPlayerHost();
            isReady = lobbyManager.IsLocalPlayerReady();
        }
        catch
        {
            // Client not fully initialized yet, skip this frame
            return;
        }

        // Update ready button
        if (readyButton != null && readyButtonText != null)
        {
            if (isHost)
            {
                readyButton.gameObject.SetActive(false);
            }
            else
            {
                readyButton.gameObject.SetActive(true);
                readyButtonText.text = isReady ? "UNREADY" : "READY";
                readyButton.interactable = true;
            }
        }

        // Update start game button (host only)
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(isHost);
            int playerCount = lobbyManager.LobbyPlayers.Count;
            startGameButton.interactable = playerCount >= 2; // Minimum 2 players
        }

        // Update status text
        if (statusText != null && !lobbyManager.IsCountingDown)
        {
            int readyCount = lobbyManager.GetReadyPlayerCount();
            int totalCount = lobbyManager.LobbyPlayers.Count;

            if (isHost)
            {
                statusText.text = $"Waiting for players... ({readyCount}/{totalCount} ready)";
            }
            else
            {
                statusText.text = isReady ? "Waiting for host to start..." : "Click Ready when you're prepared!";
            }
        }
    }

    #endregion

    #region Event Handlers

    private void OnPlayerJoined(LobbyPlayer player)
    {
        Debug.Log($"[Lobby UI] Player joined: {player.playerName}");
        UpdatePlayerList();
    }

    private void OnPlayerLeft(LobbyPlayer player)
    {
        Debug.Log($"[Lobby UI] Player left: {player.playerName}");
        UpdatePlayerList();
    }

    private void OnPlayerReadyChanged(LobbyPlayer player)
    {
        Debug.Log($"[Lobby UI] Player ready changed: {player.playerName} = {player.isReady}");
        UpdatePlayerListItem(player);
    }

    private void OnCountdownTick(float timeRemaining)
    {
        if (countdownPanel != null)
            countdownPanel.SetActive(true);

        if (countdownText != null)
        {
            countdownText.text = $"Starting in {Mathf.CeilToInt(timeRemaining)}...";
        }

        if (statusText != null)
        {
            statusText.text = "Game starting soon!";
        }
    }

    private void OnGameStarting()
    {
        if (statusText != null)
            statusText.text = "Loading game...";

        // Disable all buttons
        if (readyButton != null)
            readyButton.interactable = false;
        if (startGameButton != null)
            startGameButton.interactable = false;
        if (leaveButton != null)
            leaveButton.interactable = false;
    }

    #endregion

    #region Button Handlers

    private void OnReadyButtonClicked()
    {
        if (lobbyManager != null)
        {
            lobbyManager.ToggleReadyServerRpc();
        }
    }

    private void OnStartGameButtonClicked()
    {
        if (lobbyManager != null)
        {
            lobbyManager.ForceStartGameServerRpc();
        }
    }

    private void OnLeaveButtonClicked()
    {
        Debug.Log("[Lobby UI] Leaving lobby...");
        // Disconnect and return to main menu
        if (networkManager != null)
        {
            networkManager.Disconnect();
        }

        // Load main menu scene
        menuUIManager.ShowMainMenu();
        //UnityEngine.SceneManagement.SceneManager.LoadScene("MultiplayerMenu");
    }

    #endregion
}