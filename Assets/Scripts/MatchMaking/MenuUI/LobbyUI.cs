using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Complete lobby UI with waiting room countdown and player ready system
/// Works with panel-based MenuUIManager
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private LANNetworkManager networkManager;
    [SerializeField] private MenuUIManager menuUIManager;

    [Header("UI - Room Info")]
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text mapNameText;
    [SerializeField] private TMP_Text gameModeText;
    [SerializeField] private TMP_Text roomIdText;

    [Header("UI - Player List")]
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerListItemPrefab;

    [Header("UI - Buttons")]
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private TMP_Text readyButtonText;
    [SerializeField] private TMP_Text startButtonText;

    [Header("UI - Status")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private Image countdownProgressBar;

    [Header("UI - Waiting Indicator")]
    [SerializeField] private GameObject waitingIndicator;
    [SerializeField] private TMP_Text waitingText;

    private Dictionary<int, GameObject> playerListItems = new Dictionary<int, GameObject>();
    private float initialCountdown = 0f;

    private void OnEnable()
    {
        Debug.Log("[CompleteLobbyUI] Lobby UI enabled");

        if (lobbyManager == null)
            lobbyManager = FindAnyObjectByType<LobbyManager>();

        if (networkManager == null)
            networkManager = FindAnyObjectByType<LANNetworkManager>();

        if (menuUIManager == null)
            menuUIManager = FindAnyObjectByType<MenuUIManager>();

        SetupUI();
        SubscribeToEvents();
        UpdateRoomInfo();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        ClearPlayerList();
    }

    private void SetupUI()
    {
        // Setup buttons
        if (readyButton != null)
        {
            readyButton.onClick.RemoveAllListeners();
            readyButton.onClick.AddListener(OnReadyButtonClicked);
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(OnStartGameButtonClicked);
            startGameButton.gameObject.SetActive(false);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(OnLeaveButtonClicked);
        }

        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        if (waitingIndicator != null)
            waitingIndicator.SetActive(false);
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
        if (!LobbyManager.IsLobbyActive) return;
        if (lobbyManager == null || !lobbyManager.IsClientInitialized) return;

        UpdatePlayerList();
        UpdateButtonStates();
        UpdateWaitingIndicator();
    }

    #region UI Updates

    private void UpdateRoomInfo()
    {
        if (networkManager == null || networkManager.CurrentRoom == null)
            return;

        var room = networkManager.CurrentRoom;

        if (roomNameText != null)
            roomNameText.text = $"Room: {room.roomName}";

        if (mapNameText != null)
            mapNameText.text = $"Map: {room.mapName}";

        if (gameModeText != null)
            gameModeText.text = $"Mode: {room.gameMode}";

        if (roomIdText != null)
            roomIdText.text = $"Room ID: {room.roomId.Substring(0, 8)}...";

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

        HashSet<int> currentPlayerIds = new HashSet<int>();
        foreach (var player in lobbyManager.LobbyPlayers)
        {
            currentPlayerIds.Add(player.clientId);

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
        PlayerListItem playerListItem = item.GetComponent<PlayerListItem>();
        if (playerListItem == null) return;
        // Update player name
        TMP_Text nameText = playerListItem.GetPlayerName();
        if (nameText != null)
        {
            string displayName = player.playerName;
            if (player.isHost)
                displayName += " ★"; // Star for host
            nameText.text = displayName;
        }

        // Update ready status
        TMP_Text statusText = playerListItem.GetPlayerStatus();
        Image statusIcon = playerListItem.GetPlayerStatusImage();

        if (statusText != null)
        {
            if (player.isHost)
            {
                statusText.text = "HOST";
                statusText.color = new Color(1f, 0.84f, 0f); // Gold
            }
            else
            {
                statusText.text = player.isReady ? "READY" : "NOT READY";
                statusText.color = player.isReady ? Color.green : Color.yellow;
            }
        }

        if (statusIcon != null)
        {
            statusIcon.color = player.isReady || player.isHost ? Color.green : Color.gray;
        }
    }

    private void UpdateButtonStates()
    {
        if (lobbyManager == null) return;

        bool isHost = false;
        bool isReady = false;

        try
        {
            isHost = lobbyManager.IsLocalPlayerHost();
            isReady = lobbyManager.IsLocalPlayerReady();
        }
        catch
        {
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
                readyButtonText.text = isReady ? "CANCEL READY" : "READY";
                readyButton.interactable = !lobbyManager.IsCountingDown;

                // Change button color based on state
                var colors = readyButton.colors;
                colors.normalColor = isReady ? new Color(1f, 0.5f, 0f) : Color.green;
                readyButton.colors = colors;
            }
        }

        // Update start game button (host only)
        if (startGameButton != null && startButtonText != null)
        {
            startGameButton.gameObject.SetActive(isHost);
            int playerCount = lobbyManager.LobbyPlayers.Count;
            startGameButton.interactable = playerCount >= 2 && !lobbyManager.IsCountingDown;
            startButtonText.text = playerCount >= 2 ? "START GAME" : $"NEED {2 - playerCount} MORE";
        }

        // Update status text
        if (statusText != null && !lobbyManager.IsCountingDown)
        {
            int readyCount = lobbyManager.GetReadyPlayerCount();
            int totalCount = lobbyManager.LobbyPlayers.Count;

            if (isHost)
            {
                if (totalCount < 2)
                {
                    statusText.text = "Waiting for more players to join...";
                    statusText.color = Color.yellow;
                }
                else
                {
                    statusText.text = $"Ready: {readyCount}/{totalCount} players";
                    statusText.color = Color.white;
                }
            }
            else
            {
                if (isReady)
                {
                    statusText.text = "Waiting for host to start the game...";
                    statusText.color = Color.cyan;
                }
                else
                {
                    statusText.text = "Click READY when you're prepared!";
                    statusText.color = Color.yellow;
                }
            }
        }
    }

    private void UpdateWaitingIndicator()
    {
        if (waitingIndicator == null) return;

        bool shouldShow = lobbyManager != null &&
                         lobbyManager.LobbyPlayers.Count < 2;

        waitingIndicator.SetActive(shouldShow);

        if (shouldShow && waitingText != null)
        {
            waitingText.text = "Waiting for players to join...\n" +
                             $"({lobbyManager.LobbyPlayers.Count}/2 minimum)";
        }
    }

    private void ClearPlayerList()
    {
        foreach (var item in playerListItems.Values)
        {
            if (item != null)
                Destroy(item);
        }
        playerListItems.Clear();
    }

    #endregion

    #region Event Handlers

    private void OnPlayerJoined(LobbyPlayer player)
    {
        Debug.Log($"[CompleteLobbyUI] Player joined: {player.playerName}");
        UpdatePlayerList();

        //Play join sound / animation here if desired
    }

    private void OnPlayerLeft(LobbyPlayer player)
    {
        Debug.Log($"[CompleteLobbyUI] Player left: {player.playerName}");
        UpdatePlayerList();
    }

    private void OnPlayerReadyChanged(LobbyPlayer player)
    {
        Debug.Log($"[CompleteLobbyUI] Player ready changed: {player.playerName} = {player.isReady}");
        UpdatePlayerListItem(player);
    }

    private void OnCountdownTick(float timeRemaining)
    {
        if (countdownPanel != null)
            countdownPanel.SetActive(timeRemaining > 0);

        if (countdownText != null && timeRemaining > 0)
        {
            countdownText.text = $"Starting in {Mathf.CeilToInt(timeRemaining)}...";
        }

        // Update progress bar
        if (countdownProgressBar != null && timeRemaining > 0)
        {
            if (initialCountdown == 0)
                initialCountdown = timeRemaining;

            float progress = timeRemaining / initialCountdown;
            countdownProgressBar.fillAmount = progress;
        }
        else if (timeRemaining <= 0)
        {
            initialCountdown = 0;
        }

        // Update status text
        if (statusText != null && timeRemaining > 0)
        {
            statusText.text = "Game starting soon!";
            statusText.color = Color.green;
        }
    }

    private void OnGameStarting()
    {
        Debug.Log("[CompleteLobbyUI] Game is starting!");

        if (statusText != null)
        {
            statusText.text = "Loading game...";
            statusText.color = Color.green;
        }

        // Disable all buttons
        if (readyButton != null)
            readyButton.interactable = false;
        if (startGameButton != null)
            startGameButton.interactable = false;
        if (leaveButton != null)
            leaveButton.interactable = false;

        // Show loading indicator
        if (waitingIndicator != null)
            waitingIndicator.SetActive(true);

        if (waitingText != null)
            waitingText.text = "Loading game...";
    }

    #endregion

    #region Button Handlers

    private void OnReadyButtonClicked()
    {
        if (lobbyManager != null)
        {
            lobbyManager.ToggleReadyServerRpc();
            Debug.Log("[CompleteLobbyUI] Ready button clicked");
        }
    }

    private void OnStartGameButtonClicked()
    {
        if (lobbyManager != null)
        {
            lobbyManager.ForceStartGameServerRpc();
            Debug.Log("[CompleteLobbyUI] Start game button clicked");
        }
    }

    private void OnLeaveButtonClicked()
    {
        Debug.Log("[CompleteLobbyUI] Leaving lobby...");

        // Disconnect from network
        if (networkManager != null)
        {
            networkManager.Disconnect();
        }

        // Return to main menu
        if (menuUIManager != null)
        {
            menuUIManager.ShowMainMenu();
        }
    }

    #endregion
}