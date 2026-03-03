using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Connection;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Manages the flow between scenes and handles player connections
/// Attach to NetworkManager and persists across scenes
/// </summary>
public class LanSceneFlowManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private LANNetworkManager lanNetworkManager;
    [SerializeField] private LobbyManager lobbyManager;

    private string mainMenuScene = SceneConstant.MENUSCENE;
    private string lanLobbyScene = SceneConstant.LAN_LOBBYSCENE;
    private string gameScene = SceneConstant.LAN_GAMESCENE;

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

    //private void Start()
    //{
    //    // Subscribe to connection events
    //    if (networkManager != null)
    //    {
    //        networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
    //        networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
    //        networkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
    //        networkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;
    //    }
    //}

    //private void OnDestroy()
    //{
    //    if (networkManager != null)
    //    {
    //        networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
    //        networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
    //        networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
    //        networkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;
    //    }
    //}

    #region Connection Callbacks




  
    #endregion

    #region Scene Loading

  

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

    #endregion
}
