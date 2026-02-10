using FishNet.Object;
using FishNet.Connection;
using UnityEngine;

/// <summary>
/// Syncs player data (like names) across the network
/// Attach to NetworkManager GameObject
/// </summary>
public class PlayerDataSync : NetworkBehaviour
{
    private static PlayerDataSync instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Send player name to server when client connects
        if (IsOwner)
        {
            string playerName = PlayerPrefs.GetString("CurrentPlayerName", "Player");
            SendPlayerNameServerRpc(playerName);
        }
    }

    /// <summary>
    /// Client sends their name to server
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SendPlayerNameServerRpc(string playerName, NetworkConnection sender = null)
    {
        if (sender == null) return;

        // Store player name on server
        PlayerPrefs.SetString($"Player_{sender.ClientId}_Name", playerName);
        
        Debug.Log($"[PlayerDataSync] Received player name from {sender.ClientId}: {playerName}");

        // Broadcast to all clients so they know this player's name
        BroadcastPlayerNameObserversRpc(sender.ClientId, playerName);
    }

    /// <summary>
    /// Server broadcasts player name to all clients
    /// </summary>
    [ObserversRpc]
    private void BroadcastPlayerNameObserversRpc(int clientId, string playerName)
    {
        // Store on all clients
        PlayerPrefs.SetString($"Player_{clientId}_Name", playerName);
        
        Debug.Log($"[PlayerDataSync] Player {clientId} name set to: {playerName}");
    }

    /// <summary>
    /// Get player name by client ID
    /// </summary>
    public static string GetPlayerName(int clientId)
    {
        return PlayerPrefs.GetString($"Player_{clientId}_Name", $"Player {clientId}");
    }
}
