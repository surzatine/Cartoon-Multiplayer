using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Connection;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Manages the flow between scenes and handles player connections
/// Attach to NetworkManager and persists across scenes
/// </summary>
public class SceneFlowManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuScene = "MultiplayerMenu";
    [SerializeField] private string lobbyScene = "LobbyScene";
    [SerializeField] private string gameScene = "GameScene";

    [Header("References")]
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private LANNetworkManager lanNetworkManager;

    private LobbyManager lobbyManager;
    private bool isTransitioningToLobby = false;

    private void Awake()
    {
        if (networkManager == null)
            networkManager = GetComponent<NetworkManager>();

        if (lanNetworkManager == null)
            lanNetworkManager = GetComponent<LANNetworkManager>();

        // Don't destroy this manager when loading scenes
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Subscribe to connection events
        if (networkManager != null)
        {
            networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
            networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
            networkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
            networkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;
        }
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
            networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
            networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            networkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;
        }
    }

    #region Connection Callbacks

    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        Debug.Log($"[SceneFlow] Server state: {args.ConnectionState}");

        if (args.ConnectionState == LocalConnectionState.Started)
        {
            // Server started - if hosting, also load lobby for host
            if (lanNetworkManager != null && lanNetworkManager.IsHost && !isTransitioningToLobby)
            {
                LoadLobbyScene();
            }
        }
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        Debug.Log($"[SceneFlow] Client state: {args.ConnectionState}");

        if (args.ConnectionState == LocalConnectionState.Started)
        {
            // Connected to server - server will load lobby scene for us
            Debug.Log("[SceneFlow] Connected to server, waiting for lobby scene...");
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            // Disconnected - return to main menu
            //if (!Application.isQuitting)
            //{
            //    ReturnToMainMenu();
            //}
        }
    }

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        Debug.Log($"[SceneFlow] Remote connection {conn.ClientId} state: {args.ConnectionState}");

        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            // New player connected
            if (!isTransitioningToLobby)
            {
                // Load lobby scene for this player
                LoadLobbySceneForConnection(conn);
            }
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            // Player disconnected
            HandlePlayerDisconnect(conn);
        }
    }

    private void OnSceneLoadEnd(SceneLoadEndEventArgs args)
    {
        string sceneName = args.LoadedScenes.Length > 0 ? args.LoadedScenes[0].name : "Unknown";
        Debug.Log($"[SceneFlow] Scene loaded: {sceneName}");

        // Find lobby manager in lobby scene
        if (sceneName == lobbyScene)
        {
            StartCoroutine(InitializeLobby(args));
        }
    }

    private System.Collections.IEnumerator InitializeLobby(SceneLoadEndEventArgs args)
    {
        // Wait a frame for scene to fully load
        yield return null;

        lobbyManager = FindObjectOfType<LobbyManager>();

        if (lobbyManager != null)
        {
            // Wait for lobby manager to be fully initialized
            while (!lobbyManager.IsServerInitialized && !lobbyManager.IsClientInitialized)
            {
                yield return null;
            }

            // Add players to lobby (server only)
            if (networkManager.IsServerStarted)
            {
                // Add all connected players to lobby
                foreach (var conn in networkManager.ServerManager.Clients.Values)
                {
                    if (conn.IsActive)
                    {
                        AddPlayerToLobby(conn);
                    }
                }
            }

            Debug.Log("[SceneFlow] Lobby initialized successfully");
        }
        else
        {
            Debug.LogWarning("[SceneFlow] LobbyManager not found in lobby scene!");
        }
    }

    #endregion

    #region Scene Loading

    /// <summary>
    /// Load lobby scene (Server only)
    /// </summary>
    private void LoadLobbyScene()
    {
        if (!networkManager.IsServerStarted) return;

        isTransitioningToLobby = true;

        // Create scene load data
        SceneLoadData sld = new SceneLoadData(lobbyScene);
        sld.ReplaceScenes = ReplaceOption.All;

        // Load for all connected clients
        networkManager.SceneManager.LoadGlobalScenes(sld);

        Debug.Log($"[SceneFlow] Loading lobby scene for all players");

        isTransitioningToLobby = false;
    }

    /// <summary>
    /// Load lobby scene for specific connection
    /// </summary>
    private void LoadLobbySceneForConnection(NetworkConnection conn)
    {
        if (!networkManager.IsServerStarted) return;

        SceneLoadData sld = new SceneLoadData(lobbyScene);
        sld.ReplaceScenes = ReplaceOption.All;

        NetworkConnection[] connections = new NetworkConnection[] { conn };
        networkManager.SceneManager.LoadConnectionScenes(connections, sld);

        Debug.Log($"[SceneFlow] Loading lobby scene for player {conn.ClientId}");
    }

    /// <summary>
    /// Return to main menu
    /// </summary>
    private void ReturnToMainMenu()
    {
        Debug.Log("[SceneFlow] Returning to main menu");

        // Stop networking
        if (lanNetworkManager != null)
        {
            lanNetworkManager.Disconnect();
        }

        // Load main menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuScene);
    }

    #endregion

    #region Lobby Management

    private void AddPlayerToLobby(NetworkConnection conn)
    {
        if (lobbyManager == null)
        {
            lobbyManager = FindObjectOfType<LobbyManager>();
        }

        if (lobbyManager != null)
        {
            // Get player name from PlayerPrefs
            string playerName = "Player";

            // For the local connection (client), use stored name
            if (conn.IsLocalClient)
            {
                playerName = PlayerPrefs.GetString("CurrentPlayerName", "Player");
            }
            else
            {
                // For remote connections, try to get from stored data or use default
                playerName = PlayerPrefs.GetString($"Player_{conn.ClientId}_Name", $"Player {conn.ClientId}");
            }

            // Store for later use in game scene
            PlayerPrefs.SetString($"Player_{conn.ClientId}_Name", playerName);

            lobbyManager.AddPlayerToLobby(conn, playerName);

            Debug.Log($"[SceneFlow] Added player '{playerName}' (ID: {conn.ClientId}) to lobby");
        }
        else
        {
            Debug.LogWarning("[SceneFlow] Lobby manager not found!");
        }
    }

    private void HandlePlayerDisconnect(NetworkConnection conn)
    {
        // Remove from lobby if in lobby
        if (lobbyManager != null)
        {
            lobbyManager.RemovePlayerFromLobby(conn);
        }

        // Clean up player data
        PlayerPrefs.DeleteKey($"Player_{conn.ClientId}_Name");

        Debug.Log($"[SceneFlow] Player {conn.ClientId} disconnected");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Get current scene type
    /// </summary>
    public SceneType GetCurrentSceneType()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (currentScene == mainMenuScene)
            return SceneType.MainMenu;
        else if (currentScene == lobbyScene)
            return SceneType.Lobby;
        else if (currentScene == gameScene)
            return SceneType.Game;
        else
            return SceneType.Unknown;
    }

    #endregion
}

public enum SceneType
{
    Unknown,
    MainMenu,
    Lobby,
    Game
}