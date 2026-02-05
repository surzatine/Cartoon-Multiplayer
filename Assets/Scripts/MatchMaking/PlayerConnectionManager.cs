using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using UnityEngine;

public class PlayerConnectionManager : NetworkBehaviour
{
    [SerializeField] private LANNetworkManager lanNetworkManager;
    [SerializeField] private NetworkManager networkManager;

    private void Awake()
    {
        if (networkManager == null)
            networkManager = FindObjectOfType<NetworkManager>();

        if (lanNetworkManager == null)
            lanNetworkManager = FindObjectOfType<LANNetworkManager>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Subscribe to server events
        networkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        // Unsubscribe from server events
        if (networkManager != null && networkManager.ServerManager != null)
        {
            networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }
    }

    private void OnRemoteConnectionState(NetworkConnection conn, FishNet.Transporting.RemoteConnectionStateArgs args)
    {
        if (!IsServerInitialized) return;

        // Update player count when players connect/disconnect
        if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Started)
        {
            Debug.Log($"Player connected: {conn.ClientId}");
            UpdatePlayerCount();
            
            // Notify all clients about new player
            NotifyPlayerJoined(conn);
        }
        else if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Stopped)
        {
            Debug.Log($"Player disconnected: {conn.ClientId}");
            UpdatePlayerCount();
            
            // Notify all clients about player leaving
            NotifyPlayerLeft(conn);
        }
    }

    private void UpdatePlayerCount()
    {
        if (lanNetworkManager != null && lanNetworkManager.IsHost)
        {
            int playerCount = networkManager.ServerManager.Clients.Count;
            lanNetworkManager.UpdatePlayerCount(playerCount);
            Debug.Log($"Updated player count: {playerCount}");
        }
    }

    [ObserversRpc]
    private void NotifyPlayerJoined(NetworkConnection conn)
    {
        Debug.Log($"[Client] Player {conn.ClientId} joined the game");
        // You can add UI notifications here
    }

    [ObserversRpc]
    private void NotifyPlayerLeft(NetworkConnection conn)
    {
        Debug.Log($"[Client] Player {conn.ClientId} left the game");
        // You can add UI notifications here
    }

    #region Player Data Management

    /// <summary>
    /// Sync player data across network (call this when player customization changes)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerDataServerRpc(NetworkConnection conn, string playerName, int teamId)
    {
        // Broadcast to all clients
        UpdatePlayerDataObserversRpc(conn, playerName, teamId);
    }

    [ObserversRpc]
    private void UpdatePlayerDataObserversRpc(NetworkConnection conn, string playerName, int teamId)
    {
        Debug.Log($"Player {conn.ClientId} updated: Name={playerName}, Team={teamId}");
        // Update your player list UI or game logic here
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Get the total number of players currently connected
    /// </summary>
    public int GetConnectedPlayerCount()
    {
        if (networkManager.ServerManager.Started)
        {
            return networkManager.ServerManager.Clients.Count;
        }
        return 0;
    }

    /// <summary>
    /// Kick a player from the server (Server only)
    /// </summary>
    public void KickPlayer(NetworkConnection conn)
    {
        if (!IsServerInitialized) return;
        
        networkManager.ServerManager.Kick(conn, FishNet.Managing.Server.KickReason.Unset);
        Debug.Log($"Kicked player: {conn.ClientId}");
    }

    /// <summary>
    /// Check if the server is full
    /// </summary>
    public bool IsServerFull()
    {
        if (lanNetworkManager == null || lanNetworkManager.CurrentRoom == null)
            return false;

        int currentPlayers = GetConnectedPlayerCount();
        return currentPlayers >= lanNetworkManager.CurrentRoom.maxPlayers;
    }

    #endregion
}
