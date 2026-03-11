using FishNet.Object;
using FishNet.Connection;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Syncs PlayerStatics.CharacterId from each client to server
/// Ensures each client spawns with their own selected character
/// Attach to NetworkManager GameObject
/// </summary>
public class CharacterSelectionSync : NetworkBehaviour
{
    public static CharacterSelectionSync Instance { get; private set; }

    // Server-side storage: ClientId → CharacterId
    private Dictionary<int, string> serverCharacterSelections = new Dictionary<int, string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[CharacterSelectionSync] ✓ Server started");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Send local character selection to server
        if (IsOwner)
        {
            // Get character ID from PlayerStatics
            string characterId = PlayerStatics.CharacterId;

            Debug.Log($"<color=cyan>[CharacterSelectionSync] ✓ Client started - Sending my character ID: {characterId}</color>");

            // Send to server
            SendCharacterSelectionServerRpc(characterId);
        }
    }

    /// <summary>
    /// Client → Server: Send my selected character ID
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SendCharacterSelectionServerRpc(string characterId, NetworkConnection sender = null)
    {
        if (sender == null)
        {
            Debug.LogError("[CharacterSelectionSync] Sender is null!");
            return;
        }

        // Store on server
        serverCharacterSelections[sender.ClientId] = characterId;

        // Also store in PlayerPrefs as backup
        PlayerPrefs.SetString($"Player_{sender.ClientId}_CharacterId", characterId);
        PlayerPrefs.Save();

        Debug.Log($"<color=yellow>[CharacterSelectionSync] ✓ SERVER received: Client {sender.ClientId} selected character {characterId}</color>");

        // Confirm back to client
        ConfirmCharacterSelectionTargetRpc(sender, characterId);

        // Broadcast to all clients
        BroadcastCharacterSelectionObserversRpc(sender.ClientId, characterId);
    }

    /// <summary>
    /// Server → Client: Confirm character selection received
    /// </summary>
    [TargetRpc]
    private void ConfirmCharacterSelectionTargetRpc(NetworkConnection target, string characterId)
    {
        Debug.Log($"<color=green>[CharacterSelectionSync] ✓ Server confirmed my character: {characterId}</color>");
    }

    /// <summary>
    /// Server → All Clients: Broadcast player's character selection
    /// </summary>
    [ObserversRpc]
    private void BroadcastCharacterSelectionObserversRpc(int clientId, string characterId)
    {
        Debug.Log($"<color=cyan>[CharacterSelectionSync] Player {clientId} selected character {characterId}</color>");

        // Store locally for reference
        PlayerPrefs.SetString($"Player_{clientId}_CharacterId", characterId);
    }

    /// <summary>
    /// SERVER ONLY: Get a specific player's character ID
    /// Called by GameSceneSpawner to determine which character to spawn
    /// </summary>
    [Server]
    public string GetPlayerCharacterIdServer(int clientId)
    {
        // Try dictionary first
        if (serverCharacterSelections.ContainsKey(clientId))
        {
            string charId = serverCharacterSelections[clientId];
            return charId;
        }

        // Try PlayerPrefs backup
        string backupCharId = PlayerPrefs.GetString($"Player_{clientId}_CharacterId", "0");

        if (backupCharId == "")
        {
            Debug.LogWarning($"[CharacterSelectionSync] Using fallback character {backupCharId} for client {clientId}");
            serverCharacterSelections[clientId] = backupCharId; // Update dictionary
            return backupCharId;
        }

        // Final fallback: default to 0
        Debug.LogWarning($"<color=red>[CharacterSelectionSync] No character selection found for client {clientId}, using default (0)</color>");
        return "0";
    }

    /// <summary>
    /// Update character selection (can be called during lobby)
    /// Call this if player changes character after connecting
    /// </summary>
    public void UpdateCharacterSelection(string characterId)
    {
        // Update PlayerStatics
        PlayerStatics.CharacterId = characterId;

        // Send update to server if connected
        if (IsClientInitialized)
        {
            SendCharacterSelectionServerRpc(characterId);
        }

        Debug.Log($"<color=magenta>[CharacterSelectionSync] Updated character to: {characterId}</color>");
    }

    /// <summary>
    /// SERVER: Print all character selections (debug)
    /// </summary>
    [Server]
    public void PrintAllSelections()
    {
        Debug.Log("<color=yellow>========== ALL CHARACTER SELECTIONS ==========</color>");

        if (serverCharacterSelections.Count == 0)
        {
            Debug.Log("<color=yellow>No character selections received yet</color>");
        }
        else
        {
            foreach (var kvp in serverCharacterSelections)
            {
                string playerName = PlayerPrefs.GetString($"Player_{kvp.Key}_Name", $"Player {kvp.Key}");
                Debug.Log($"<color=yellow>Client {kvp.Key} ({playerName}) → Character {kvp.Value}</color>");
            }
        }

        Debug.Log("<color=yellow>============================================</color>");
    }

    /// <summary>
    /// Clear a player's selection when they disconnect
    /// </summary>
    [Server]
    public void ClearPlayerSelection(int clientId)
    {
        if (serverCharacterSelections.ContainsKey(clientId))
        {
            serverCharacterSelections.Remove(clientId);
            Debug.Log($"[CharacterSelectionSync] Cleared selection for client {clientId}");
        }
    }
}