using System;
using UnityEngine;

[Serializable]
public class RoomData
{
    public string roomName;
    public string hostName;
    public int currentPlayers;
    public int maxPlayers;
    public string mapName;
    public string gameMode;
    public string ipAddress;
    public int port;
    public string roomId; // Unique identifier for the room

    public RoomData()
    {
        roomId = Guid.NewGuid().ToString();
    }

    public RoomData(string roomName, string hostName, int maxPlayers, string mapName, string gameMode, string ipAddress, int port)
    {
        this.roomName = roomName;
        this.hostName = hostName;
        this.currentPlayers = 1;
        this.maxPlayers = maxPlayers;
        this.mapName = mapName;
        this.gameMode = gameMode;
        this.ipAddress = ipAddress;
        this.port = port;
        this.roomId = Guid.NewGuid().ToString();
    }

    // Convert to JSON for network transmission
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    // Create from JSON
    public static RoomData FromJson(string json)
    {
        return JsonUtility.FromJson<RoomData>(json);
    }

    public bool IsFull()
    {
        return currentPlayers >= maxPlayers;
    }
}
