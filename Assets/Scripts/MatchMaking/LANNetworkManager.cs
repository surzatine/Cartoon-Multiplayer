using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
using System.Collections.Generic;
using System;

public class LANNetworkManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private LANDiscovery lanDiscovery;

    [Header("Server Settings")]
    [SerializeField] private int serverPort = 7770;
    [SerializeField] private int maxPlayers = 8;

    private RoomData currentRoom;
    private bool isHost = false;

    public bool IsHost => isHost;
    public RoomData CurrentRoom => currentRoom;

    private void Awake()
    {
        if (networkManager == null)
            networkManager = FindObjectOfType<NetworkManager>();

        if (lanDiscovery == null)
            lanDiscovery = GetComponent<LANDiscovery>();
    }

    private void Start()
    {
        // Subscribe to Fishnet events
        if (networkManager != null)
        {
            networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
            networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        }
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
            networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        }
    }

    #region Host Game

    /// <summary>
    /// Host a new game room
    /// </summary>
    public void HostGame(string roomName, string hostName, int maxPlayers, string mapName, string gameMode)
    {
        try
        {
            // Create room data
            string ipAddress = LANDiscovery.GetLocalIPAddress();
            currentRoom = new RoomData(roomName, hostName, maxPlayers, mapName, gameMode, ipAddress, serverPort);
            
            this.maxPlayers = maxPlayers;
            isHost = true;

            // Start Fishnet server
            networkManager.ServerManager.StartConnection();
            
            // Start broadcasting room
            lanDiscovery.StartBroadcasting(currentRoom);

            Debug.Log($"Hosting game: {roomName} on {ipAddress}:{serverPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to host game: {e.Message}");
            isHost = false;
        }
    }

    /// <summary>
    /// Stop hosting
    /// </summary>
    public void StopHosting()
    {
        lanDiscovery.StopBroadcasting();
        
        if (networkManager.ServerManager.Started)
        {
            networkManager.ServerManager.StopConnection(true);
        }
        
        if (networkManager.ClientManager.Started)
        {
            networkManager.ClientManager.StopConnection();
        }

        isHost = false;
        currentRoom = null;
        
        Debug.Log("Stopped hosting");
    }

    #endregion

    #region Join Game

    /// <summary>
    /// Join a specific room
    /// </summary>
    public void JoinRoom(RoomData room)
    {
        try
        {
            currentRoom = room;
            
            // Set the server address and port
            networkManager.TransportManager.Transport.SetClientAddress(room.ipAddress);
            networkManager.TransportManager.Transport.SetPort((ushort)room.port);
            
            // Start Fishnet client
            networkManager.ClientManager.StartConnection();
            
            Debug.Log($"Joining room: {room.roomName} at {room.ipAddress}:{room.port}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join room: {e.Message}");
        }
    }

    /// <summary>
    /// Quick join first available room
    /// </summary>
    public void QuickJoin(List<RoomData> availableRooms)
    {
        if (availableRooms == null || availableRooms.Count == 0)
        {
            Debug.LogWarning("No rooms available for quick join");
            return;
        }

        // Find first non-full room
        RoomData roomToJoin = null;
        foreach (var room in availableRooms)
        {
            if (!room.IsFull())
            {
                roomToJoin = room;
                break;
            }
        }

        if (roomToJoin != null)
        {
            JoinRoom(roomToJoin);
        }
        else
        {
            Debug.LogWarning("All rooms are full");
        }
    }

    /// <summary>
    /// Disconnect from current game
    /// </summary>
    public void Disconnect()
    {
        if (isHost)
        {
            StopHosting();
        }
        else
        {
            if (networkManager.ClientManager.Started)
            {
                networkManager.ClientManager.StopConnection();
            }
            currentRoom = null;
        }
        
        Debug.Log("Disconnected");
    }

    #endregion

    #region Fishnet Callbacks

    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        Debug.Log($"Server state changed to: {args.ConnectionState}");
        
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            // Server started successfully
            // Also start client if hosting (to play on the same machine)
            if (isHost && !networkManager.ClientManager.Started)
            {
                networkManager.ClientManager.StartConnection();
            }
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            // Server stopped
            if (isHost)
            {
                lanDiscovery.StopBroadcasting();
            }
        }
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        Debug.Log($"Client state changed to: {args.ConnectionState}");
        
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            // Successfully connected to server
            Debug.Log("Connected to server successfully");
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            // Disconnected from server
            Debug.Log("Disconnected from server");
            if (!isHost)
            {
                currentRoom = null;
            }
        }
    }

    #endregion

    #region Player Count Updates

    /// <summary>
    /// Update current player count (call this when players join/leave)
    /// </summary>
    public void UpdatePlayerCount(int playerCount)
    {
        if (currentRoom != null && isHost)
        {
            currentRoom.currentPlayers = playerCount;
            PlayerPrefs.SetString("CurrentRoomData", currentRoom.ToJson());
        }
    }

    /// <summary>
    /// Get current number of connected players
    /// </summary>
    public int GetPlayerCount()
    {
        if (networkManager.ServerManager.Started)
        {
            return networkManager.ServerManager.Clients.Count;
        }
        return 0;
    }

    #endregion
}
