using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using System.Collections;
using UnityEngine;

/// <summary>
/// Manages player connections and integrates with panel-based lobby system
/// </summary>
public class ConnectionManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private LANNetworkManager lanNetworkManager;
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private MenuUIManager menuUIManager;
    [SerializeField] private MultiRoomManager multiRoomManager;

    private LobbyManager currentLobbyManager;

    private void Awake()
    {
        if (networkManager == null)
            networkManager = FindObjectOfType<NetworkManager>();

        if (lanNetworkManager == null)
            lanNetworkManager = FindObjectOfType<LANNetworkManager>();

        if (menuUIManager == null)
            menuUIManager = FindObjectOfType<MenuUIManager>();

        if (multiRoomManager == null)
            multiRoomManager = FindObjectOfType<MultiRoomManager>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Subscribe to server events
        networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
        networkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;

        Debug.Log("[CompleteConnectionManager] Server started");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        Debug.Log("OnStartClient CALLED");

        networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;

        // 🔥 IMPORTANT FIX
        if (networkManager.IsClientStarted)
        {
            Debug.Log("Client already started — invoking manually");
            OnClientConnectionState(
                new ClientConnectionStateArgs(LocalConnectionState.Started, 0)
            );
        }
    }

    //private void Update()
    //{
    //    Debug.Log("IsServerStarted: " + networkManager.IsServerStarted);
    //    Debug.Log("IsClientStarted: " + networkManager.IsClientStarted);
    //}
    //private IEnumerator StartClientRoutine()
    //{
    //    yield return new WaitForSeconds(0.5f);

    //    networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
    //}

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (networkManager != null)
        {
            networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
            networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (networkManager != null)
        {
            networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        }
    }

    #region Connection Callbacks

    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        Debug.Log($"[CompleteConnectionManager] Server state: {args.ConnectionState}");

        if (args.ConnectionState == LocalConnectionState.Started)
        {
            // Server started - host is connecting
            if (lanNetworkManager != null && lanNetworkManager.IsHost)
            {
                // Load lobby UI for host
                Invoke(nameof(ShowLobbyForHost), 0.5f);
            }
        }
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        Debug.LogError($"[CompleteConnectionManager] Client state: {args.ConnectionState}");

        if (args.ConnectionState == LocalConnectionState.Started)
        {
            // Successfully connected to server
            Debug.Log($"[CompleteConnectionManager] Connected to server successfully IsOwner: {IsOwner}");
            
            // Send player name to serverIsOwner)
            
            string playerName = PlayerPrefs.GetString("CurrentPlayerName", PlayerStatics.PlayerName);
            SendPlayerInfoServerRpc(playerName);
            

            // Show lobby for client
            if (!lanNetworkManager.IsHost)
            {
                Invoke(nameof(ShowLobbyForClient), 0.5f);
            }
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            // Disconnected from server
            Debug.Log("[CompleteConnectionManager] Disconnected from server");
            
            //if (!Application.isQuitting && menuUIManager != null)
            //{
            //    menuUIManager.ShowMainMenu();
            //}
        }
    }

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (!IsServerInitialized) return;

        Debug.Log($"[CompleteConnectionManager] Remote connection {conn.ClientId} state: {args.ConnectionState}");

        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            // New player connected
            HandlePlayerConnected(conn);
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            // Player disconnected
            HandlePlayerDisconnected(conn);
        }
    }

    #endregion

    #region Player Connection Handling

    [Server]
    private void HandlePlayerConnected(NetworkConnection conn)
    {
        Debug.Log($"[CompleteConnectionManager] Player {conn.ClientId} connected");

        // Update player count
        UpdatePlayerCount();
    }

    [Server]
    private void HandlePlayerDisconnected(NetworkConnection conn)
    {
        Debug.Log($"[CompleteConnectionManager] Player {conn.ClientId} disconnected");

        // Remove from lobby if in one
        if (currentLobbyManager != null)
        {
            currentLobbyManager.RemovePlayerFromLobby(conn);
        }

        // Remove from room
        if (multiRoomManager != null)
        {
            multiRoomManager.LeaveRoom(conn);
        }

        // Update player count
        UpdatePlayerCount();

        // Clean up player data
        PlayerPrefs.DeleteKey($"Player_{conn.ClientId}_Name");
    }

    [Server]
    private void UpdatePlayerCount()
    {
        if (lanNetworkManager != null && lanNetworkManager.IsHost)
        {
            int playerCount = networkManager.ServerManager.Clients.Count;
            lanNetworkManager.UpdatePlayerCount(playerCount);
            Debug.Log($"[CompleteConnectionManager] Updated player count: {playerCount}");
        }
    }

    #endregion

    #region Lobby Integration

    private void ShowLobbyForHost()
    {
        Debug.Log("[CompleteConnectionManager] Showing lobby for host");

        if (menuUIManager != null)
        {
            menuUIManager.ShowLobbyMenu();
        }

        // Find or create lobby manager
        currentLobbyManager = FindObjectOfType<LobbyManager>();
        
        if (currentLobbyManager != null && networkManager.IsServerStarted)
        {
            Debug.LogError("[CompleteConnectionManager] Server is started");
            // Add host to lobby
            string hostName = PlayerPrefs.GetString("CurrentPlayerName", "Host");
            var hostConn = networkManager.ClientManager.Connection;
            
            if (hostConn != null)
            {
                currentLobbyManager.AddPlayerToLobby(hostConn, hostName);
            }
        }
    }

    private void ShowLobbyForClient()
    {
        Debug.Log("[CompleteConnectionManager] Showing lobby for client");

        if (menuUIManager != null)
        {
            menuUIManager.ShowLobbyMenu();
        }
    }

    /// <summary>
    /// Server receives player info and adds them to lobby
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SendPlayerInfoServerRpc(string playerName, NetworkConnection sender = null)
    {
        if (sender == null) return;

        Debug.Log($"[CompleteConnectionManager] Received player info from {sender.ClientId}: {playerName}");

        // Store player name
        PlayerPrefs.SetString($"Player_{sender.ClientId}_Name", playerName);

        // Add to lobby if lobby manager exists
        if (currentLobbyManager == null)
        {
            currentLobbyManager = FindObjectOfType<LobbyManager>();
        }

        if (currentLobbyManager != null)
        {
            currentLobbyManager.AddPlayerToLobby(sender, playerName);
        }

        // Broadcast to all clients
        BroadcastPlayerInfoObserversRpc(sender.ClientId, playerName);
    }

    /// <summary>
    /// Broadcast player info to all clients
    /// </summary>
    [ObserversRpc]
    private void BroadcastPlayerInfoObserversRpc(int clientId, string playerName)
    {
        PlayerPrefs.SetString($"Player_{clientId}_Name", playerName);
        Debug.Log($"[CompleteConnectionManager] Player {clientId} is {playerName}");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Set the current lobby manager
    /// </summary>
    public void SetLobbyManager(LobbyManager lobbyManager)
    {
        currentLobbyManager = lobbyManager;
    }

    /// <summary>
    /// Get current lobby manager
    /// </summary>
    public LobbyManager GetLobbyManager()
    {
        return currentLobbyManager;
    }

    #endregion
}
