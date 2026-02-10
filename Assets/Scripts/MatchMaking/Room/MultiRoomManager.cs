using FishNet.Connection;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages multiple isolated rooms on LAN
/// Each room has its own lobby and game instance
/// NOTE: This is a regular MonoBehaviour, NOT NetworkBehaviour
/// </summary>
public class MultiRoomManager : MonoBehaviour
{
    private static MultiRoomManager instance;

    // Track which room each connection belongs to
    private Dictionary<int, string> connectionToRoom = new Dictionary<int, string>();

    // Track all active rooms
    private Dictionary<string, RoomInstance> activeRooms = new Dictionary<string, RoomInstance>();

    public static MultiRoomManager Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("[MultiRoomManager] Duplicate instance found, destroying...");
            Destroy(this);
            return;
        }
        instance = this;

        // DON'T use DontDestroyOnLoad - NetworkManagerPersistence handles it
        Debug.Log("[MultiRoomManager] Initialized (regular MonoBehaviour)");
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    #region Room Management

    /// <summary>
    /// Create a new room instance (called from server)
    /// </summary>
    public void CreateRoom(string roomId, RoomData roomData, NetworkConnection host)
    {
        if (activeRooms.ContainsKey(roomId))
        {
            Debug.LogWarning($"[MultiRoomManager] Room {roomId} already exists");
            return;
        }

        RoomInstance room = new RoomInstance
        {
            roomId = roomId,
            roomData = roomData,
            hostConnectionId = host.ClientId,
            players = new List<int> { host.ClientId }
        };

        activeRooms[roomId] = room;
        connectionToRoom[host.ClientId] = roomId;

        Debug.Log($"[MultiRoomManager] Created room: {roomData.roomName} (ID: {roomId})");
    }

    /// <summary>
    /// Add player to existing room (called from server)
    /// </summary>
    public bool JoinRoom(string roomId, NetworkConnection conn)
    {
        if (!activeRooms.ContainsKey(roomId))
        {
            Debug.LogWarning($"[MultiRoomManager] Room {roomId} not found");
            return false;
        }

        var room = activeRooms[roomId];

        // Check if room is full
        if (room.players.Count >= room.roomData.maxPlayers)
        {
            Debug.LogWarning($"[MultiRoomManager] Room {roomId} is full");
            return false;
        }

        room.players.Add(conn.ClientId);
        connectionToRoom[conn.ClientId] = roomId;

        Debug.Log($"[MultiRoomManager] Player {conn.ClientId} joined room {roomId}");
        return true;
    }

    /// <summary>
    /// Remove player from room (called from server)
    /// </summary>
    public void LeaveRoom(NetworkConnection conn)
    {
        if (!connectionToRoom.ContainsKey(conn.ClientId))
            return;

        string roomId = connectionToRoom[conn.ClientId];

        if (activeRooms.ContainsKey(roomId))
        {
            var room = activeRooms[roomId];
            room.players.Remove(conn.ClientId);

            Debug.Log($"[MultiRoomManager] Player {conn.ClientId} left room {roomId}");

            // If room is empty or host left, close room
            if (room.players.Count == 0 || conn.ClientId == room.hostConnectionId)
            {
                CloseRoom(roomId);
            }
        }

        connectionToRoom.Remove(conn.ClientId);
    }

    /// <summary>
    /// Close a room (called from server)
    /// </summary>
    private void CloseRoom(string roomId)
    {
        if (!activeRooms.ContainsKey(roomId))
            return;

        var room = activeRooms[roomId];

        // Remove all players from this room
        foreach (int playerId in room.players)
        {
            connectionToRoom.Remove(playerId);
        }

        activeRooms.Remove(roomId);
        Debug.Log($"[MultiRoomManager] Closed room {roomId}");
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Get room ID for a connection
    /// </summary>
    public string GetPlayerRoom(NetworkConnection conn)
    {
        if (connectionToRoom.ContainsKey(conn.ClientId))
            return connectionToRoom[conn.ClientId];
        return null;
    }

    /// <summary>
    /// Get all connections in a room
    /// </summary>
    public List<int> GetRoomPlayerIds(string roomId)
    {
        if (activeRooms.ContainsKey(roomId))
            return new List<int>(activeRooms[roomId].players);
        return new List<int>();
    }

    /// <summary>
    /// Get room data
    /// </summary>
    public RoomData GetRoomData(string roomId)
    {
        if (activeRooms.ContainsKey(roomId))
            return activeRooms[roomId].roomData;
        return null;
    }

    /// <summary>
    /// Check if connections are in the same room
    /// </summary>
    public bool AreInSameRoom(NetworkConnection conn1, NetworkConnection conn2)
    {
        if (!connectionToRoom.ContainsKey(conn1.ClientId) ||
            !connectionToRoom.ContainsKey(conn2.ClientId))
            return false;

        return connectionToRoom[conn1.ClientId] == connectionToRoom[conn2.ClientId];
    }

    /// <summary>
    /// Get active room count
    /// </summary>
    public int GetActiveRoomCount()
    {
        return activeRooms.Count;
    }

    /// <summary>
    /// Get all active room IDs
    /// </summary>
    public List<string> GetAllRoomIds()
    {
        return new List<string>(activeRooms.Keys);
    }

    #endregion
}

/// <summary>
/// Room instance data
/// </summary>
[System.Serializable]
public class RoomInstance
{
    public string roomId;
    public RoomData roomData;
    public int hostConnectionId;
    public List<int> players;
    public bool gameStarted;
}