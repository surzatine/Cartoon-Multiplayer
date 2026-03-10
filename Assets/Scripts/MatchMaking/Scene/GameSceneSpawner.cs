using FishNet.Object;
using FishNet.Connection;
using FishNet.Managing.Scened;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles player spawning in game scenes with room isolation
/// Each client spawns with their own selected character
/// </summary>
public class GameSceneSpawner : NetworkBehaviour
{
    [Header("Player Prefabs")]
    [SerializeField] private SO_Player _soPlayer; // Your ScriptableObject with character prefabs

    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnRadius = 1f;
    [SerializeField] private bool randomizeSpawnPoints = true;
    [SerializeField] private float spawnDelay = 0.5f;

    [Header("Team Spawns (Optional)")]
    [SerializeField] private bool useTeamSpawns = false;
    [SerializeField] private Transform[] teamASpawnPoints;
    [SerializeField] private Transform[] teamBSpawnPoints;

    [Header("Debug")]
    [SerializeField] private bool enableDetailedLogs = true;

    // Track spawned players in this scene instance
    private Dictionary<int, NetworkObject> spawnedPlayers = new Dictionary<int, NetworkObject>();
    private List<int> usedSpawnIndices = new List<int>();

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Subscribe to scene loaded event
        SceneManager.OnLoadEnd += OnSceneLoadEnd;

        Debug.Log($"[GameSceneSpawner] Started on server - waiting for scene load");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        SceneManager.OnLoadEnd -= OnSceneLoadEnd;
    }

    /// <summary>
    /// Called when scene loading completes (Server only)
    /// </summary>
    private void OnSceneLoadEnd(SceneLoadEndEventArgs args)
    {
        // Only process if this is our game scene
        if (args.LoadedScenes == null || args.LoadedScenes.Length == 0)
            return;

        string sceneName = args.LoadedScenes[0].name;

        // Check if this is the LanGameScene
        if (!sceneName.Contains("Game") && sceneName != "LanGameScene")
            return;

        Debug.Log($"<color=green>[GameSceneSpawner] Game scene '{sceneName}' loaded!</color>");

        // Spawn players after a short delay to ensure scene is fully loaded
        Invoke(nameof(SpawnAllPlayers), spawnDelay);
    }

    [Server]
    private void SpawnAllPlayers()
    {
        Debug.Log($"<color=yellow>[GameSceneSpawner] ========== SPAWNING PLAYERS ==========</color>");

        // Get character selection sync
        var characterSync = CharacterSelectionSync.Instance;
        if (characterSync == null)
        {
            Debug.LogWarning("[GameSceneSpawner] CharacterSelectionSync not found! Players will spawn with default character.");
        }

        // Print all selections before spawning (debug)
        if (enableDetailedLogs && characterSync != null)
        {
            characterSync.PrintAllSelections();
        }

        // Spawn each connected player
        foreach (var conn in ServerManager.Clients.Values)
        {
            if (conn != null && conn.IsActive)
            {
                SpawnPlayerForConnection(conn);
            }
        }

        Debug.Log($"<color=green>[GameSceneSpawner] ✓ Finished spawning {spawnedPlayers.Count} players</color>");
        Debug.Log($"<color=yellow>[GameSceneSpawner] ==========================================</color>");
    }

    /// <summary>
    /// Spawn a player for a specific connection with THEIR selected character
    /// </summary>
    [Server]
    public void SpawnPlayerForConnection(NetworkConnection conn)
    {
        if (_soPlayer == null)
        {
            Debug.LogError("[GameSceneSpawner] SO_Player is not assigned!");
            return;
        }

        // Check if player already spawned
        if (spawnedPlayers.ContainsKey(conn.ClientId))
        {
            Debug.LogWarning($"[GameSceneSpawner] Player {conn.ClientId} already spawned");
            return;
        }

        // CRITICAL: Get THIS CLIENT's character ID (not server's!)
        int characterId = GetCharacterIdForClient(conn.ClientId);

        if (enableDetailedLogs)
        {
            Debug.Log($"<color=cyan>[GameSceneSpawner] Client {conn.ClientId} will spawn as character {characterId}</color>");
        }

        // Get spawn position
        Vector3 spawnPosition = GetSpawnPosition(conn);
        Quaternion spawnRotation = Quaternion.identity;

        // Get the character prefab from SO_Player using THIS CLIENT's character ID
        NetworkObject characterPrefab = _soPlayer.GetPlayerPrefab(characterId);

        if (characterPrefab == null)
        {
            Debug.LogError($"[GameSceneSpawner] No character prefab for character ID {characterId}! Check SO_Player.");
            return;
        }

        // Instantiate THE SELECTED CHARACTER for this client
        NetworkObject playerInstance = Instantiate(characterPrefab, spawnPosition, spawnRotation);

        // Spawn for this specific connection (owner)
        ServerManager.Spawn(playerInstance, conn);

        // Track spawned player
        spawnedPlayers[conn.ClientId] = playerInstance;

        string playerName = GetPlayerName(conn.ClientId);
        string prefabName = characterPrefab.name;

        Debug.Log($"<color=lime>[GameSceneSpawner] ✓✓✓ SPAWNED: Client {conn.ClientId} ({playerName}) as '{prefabName}' (Character ID {characterId}) at {spawnPosition}</color>");

        // Initialize player data
        InitializePlayer(playerInstance, conn, characterId);
    }

    /// <summary>
    /// CRITICAL: Get the character ID for a specific client
    /// This ensures each client spawns with THEIR selected character, not the server's
    /// </summary>
    [Server]
    private int GetCharacterIdForClient(int clientId)
    {
        // Try to get from CharacterSelectionSync
        var characterSync = CharacterSelectionSync.Instance;

        if (characterSync != null)
        {
            int characterId = characterSync.GetPlayerCharacterIdServer(clientId);

            if (enableDetailedLogs)
            {
                Debug.Log($"<color=yellow>[GameSceneSpawner] Client {clientId} selected character: {characterId} (from sync)</color>");
            }

            return characterId;
        }

        // Fallback: Try PlayerPrefs (in case sync is missing)
        int fallbackCharId = PlayerPrefs.GetInt($"Player_{clientId}_CharacterId", 0);

        if (fallbackCharId > 0)
        {
            Debug.LogWarning($"[GameSceneSpawner] Using fallback character {fallbackCharId} for client {clientId} (sync missing)");
            return fallbackCharId;
        }

        // Final fallback: Use default character 0
        Debug.LogWarning($"[GameSceneSpawner] No character selection found for client {clientId}, using default (0)");
        return 0;
    }

    /// <summary>
    /// Get spawn position for a player
    /// </summary>
    private Vector3 GetSpawnPosition(NetworkConnection conn)
    {
        Transform[] spawns = spawnPoints;

        // Use team spawns if enabled
        if (useTeamSpawns)
        {
            bool isTeamA = conn.ClientId % 2 == 0;
            spawns = isTeamA ? teamASpawnPoints : teamBSpawnPoints;
        }

        // Fallback to default position if no spawn points
        if (spawns == null || spawns.Length == 0)
        {
            Debug.LogWarning("[GameSceneSpawner] No spawn points defined, using default (0,1,0)");
            return new Vector3(0, 1, 0);
        }

        // Get spawn point index
        int spawnIndex = 0;

        if (randomizeSpawnPoints)
        {
            int attempts = 0;
            do
            {
                spawnIndex = Random.Range(0, spawns.Length);
                attempts++;
            }
            while (usedSpawnIndices.Contains(spawnIndex) && attempts < spawns.Length * 2);
        }
        else
        {
            spawnIndex = spawnedPlayers.Count % spawns.Length;
        }

        usedSpawnIndices.Add(spawnIndex);

        // Get spawn position with random offset
        Vector3 spawnPos = spawns[spawnIndex].position;

        if (spawnRadius > 0)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            spawnPos += new Vector3(randomCircle.x, 0, randomCircle.y);
        }

        return spawnPos;
    }

    /// <summary>
    /// Initialize player with data
    /// </summary>
    [Server]
    private void InitializePlayer(NetworkObject playerInstance, NetworkConnection conn, int characterId)
    {
        string playerName = GetPlayerName(conn.ClientId);

        // Try NetworkedPlayer
        if (playerInstance.TryGetComponent(out NetworkedPlayer player))
        {
            if (enableDetailedLogs)
            {
                Debug.Log($"[GameSceneSpawner] Initialized NetworkedPlayer: {playerName} with character {characterId}");
            }
        }
        // Try NetworkedPlayerEnhanced
        else if (playerInstance.TryGetComponent(out NetworkedPlayerEnhanced playerEnhanced))
        {
            playerEnhanced.SetPlayerNameServer(playerName);

            // Assign team if using team spawns
            if (useTeamSpawns)
            {
                int teamId = conn.ClientId % 2 == 0 ? 1 : 2;
                playerEnhanced.SetTeamServer(teamId);
            }

            if (enableDetailedLogs)
            {
                Debug.Log($"[GameSceneSpawner] Initialized NetworkedPlayerEnhanced: {playerName} with character {characterId}");
            }
        }
        else
        {
            Debug.LogWarning($"[GameSceneSpawner] No player script found on spawned character {characterId}");
        }
    }

    /// <summary>
    /// Get player name from stored data
    /// </summary>
    private string GetPlayerName(int clientId)
    {
        return PlayerPrefs.GetString($"Player_{clientId}_Name", $"Player {clientId}");
    }

    /// <summary>
    /// Handle player disconnection
    /// </summary>
    [Server]
    public void OnPlayerDisconnected(NetworkConnection conn)
    {
        if (spawnedPlayers.ContainsKey(conn.ClientId))
        {
            NetworkObject playerObj = spawnedPlayers[conn.ClientId];

            if (playerObj != null)
            {
                ServerManager.Despawn(playerObj.gameObject);
            }

            spawnedPlayers.Remove(conn.ClientId);
            Debug.Log($"[GameSceneSpawner] Despawned player {conn.ClientId}");
        }
    }

    /// <summary>
    /// Manually spawn player (call from server)
    /// </summary>
    [Server]
    public void SpawnPlayer(NetworkConnection conn)
    {
        SpawnPlayerForConnection(conn);
    }

    /// <summary>
    /// Respawn a player at a spawn point
    /// </summary>
    [Server]
    public void RespawnPlayer(NetworkConnection conn)
    {
        if (!spawnedPlayers.ContainsKey(conn.ClientId))
        {
            Debug.LogWarning($"[GameSceneSpawner] Cannot respawn - player {conn.ClientId} not found");
            return;
        }

        NetworkObject playerObj = spawnedPlayers[conn.ClientId];

        if (playerObj == null)
        {
            Debug.LogWarning($"[GameSceneSpawner] Player object is null for {conn.ClientId}");
            return;
        }

        // Get new spawn position
        Vector3 spawnPos = GetSpawnPosition(conn);

        // Teleport player
        playerObj.transform.position = spawnPos;
        playerObj.transform.rotation = Quaternion.identity;

        // Reset player state if needed
        if (playerObj.TryGetComponent(out NetworkedPlayerEnhanced player))
        {
            player.ResetPlayerState();
        }

        Debug.Log($"[GameSceneSpawner] Respawned player {conn.ClientId} at {spawnPos}");
    }

    #region Utility Methods

    /// <summary>
    /// Get all spawned players in this game instance
    /// </summary>
    public IReadOnlyDictionary<int, NetworkObject> GetSpawnedPlayers()
    {
        return spawnedPlayers;
    }

    /// <summary>
    /// Get player count in this game instance
    /// </summary>
    public int GetPlayerCount()
    {
        return spawnedPlayers.Count;
    }

    #endregion

    #region Editor Helpers

    private void OnDrawGizmos()
    {
        // Draw spawn points in editor
        if (spawnPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (var spawn in spawnPoints)
            {
                if (spawn != null)
                {
                    Gizmos.DrawWireSphere(spawn.position, 0.5f);
                    if (spawnRadius > 0)
                    {
                        Gizmos.DrawWireSphere(spawn.position, spawnRadius);
                    }
                }
            }
        }

        // Draw team spawn points
        if (useTeamSpawns)
        {
            if (teamASpawnPoints != null)
            {
                Gizmos.color = Color.red;
                foreach (var spawn in teamASpawnPoints)
                {
                    if (spawn != null)
                    {
                        Gizmos.DrawWireSphere(spawn.position, 0.5f);
                    }
                }
            }

            if (teamBSpawnPoints != null)
            {
                Gizmos.color = Color.blue;
                foreach (var spawn in teamBSpawnPoints)
                {
                    if (spawn != null)
                    {
                        Gizmos.DrawWireSphere(spawn.position, 0.5f);
                    }
                }
            }
        }
    }

    #endregion
}