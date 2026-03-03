using FishNet.Object;
using FishNet.Connection;
using FishNet.Managing.Scened;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles player spawning in game scenes with room isolation
/// Automatically spawns players when they enter the game scene
/// </summary>
public class GameSceneSpawner : NetworkBehaviour
{
    [Header("Player Prefab")]
    [SerializeField] private NetworkObject playerPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnRadius = 1f;
    [SerializeField] private bool randomizeSpawnPoints = true;
    [SerializeField] private float spawnDelay = 0.5f; // Delay before spawning players

    [Header("Team Spawns (Optional)")]
    [SerializeField] private bool useTeamSpawns = false;
    [SerializeField] private Transform[] teamASpawnPoints;
    [SerializeField] private Transform[] teamBSpawnPoints;

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

        // Check if this is the LanGameScene (or whatever your game scene is called)
        if (!sceneName.Contains("Game") && sceneName != "LanGameScene")
            return;

        Debug.Log($"[GameSceneSpawner] Game scene '{sceneName}' loaded!");

        // Spawn players after a short delay to ensure scene is fully loaded
        Invoke(nameof(SpawnAllPlayers), spawnDelay);
    }

    [Server]
    private void SpawnAllPlayers()
    {
        Debug.Log($"[GameSceneSpawner] Spawning players...");

        // Get all connected clients
        foreach (var conn in ServerManager.Clients.Values)
        {
            if (conn != null && conn.IsActive)
            {
                SpawnPlayerForConnection(conn);
            }
        }

        Debug.Log($"[GameSceneSpawner] Finished spawning {spawnedPlayers.Count} players");
    }

    /// <summary>
    /// Spawn a player for a specific connection
    /// </summary>
    [Server]
    public void SpawnPlayerForConnection(NetworkConnection conn)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[GameSceneSpawner] Player prefab is not assigned!");
            return;
        }

        // Check if player already spawned
        if (spawnedPlayers.ContainsKey(conn.ClientId))
        {
            Debug.LogWarning($"[GameSceneSpawner] Player {conn.ClientId} already spawned");
            return;
        }

        // Get spawn position
        Vector3 spawnPosition = GetSpawnPosition(conn);
        Quaternion spawnRotation = Quaternion.identity;

        // Instantiate player
        NetworkObject playerInstance = Instantiate(playerPrefab, spawnPosition, spawnRotation);

        // Spawn for this specific connection (owner)
        ServerManager.Spawn(playerInstance, conn);

        // Track spawned player
        spawnedPlayers[conn.ClientId] = playerInstance;

        Debug.Log($"[GameSceneSpawner] ✓ Spawned player for client {conn.ClientId} ({GetPlayerName(conn.ClientId)}) at {spawnPosition}");

        // Initialize player data
        if (playerInstance.TryGetComponent(out NetworkedPlayer player))
        {
            InitializePlayer(player, conn);
        }
        else if (playerInstance.TryGetComponent(out NetworkedPlayerEnhanced playerEnhanced))
        {
            InitializePlayerEnhanced(playerEnhanced, conn);
        }
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
            // Determine team (simple alternating for now)
            bool isTeamA = conn.ClientId % 2 == 0;
            spawns = isTeamA ? teamASpawnPoints : teamBSpawnPoints;
        }

        // Fallback to default position if no spawn points
        if (spawns == null || spawns.Length == 0)
        {
            Debug.LogWarning("[GameSceneSpawner] No spawn points defined, using default (0,0,0)");
            return Vector3.zero;
        }

        // Get spawn point index
        int spawnIndex = 0;

        if (randomizeSpawnPoints)
        {
            // Try to find unused spawn point
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
            // Sequential spawning
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
    /// Initialize basic player
    /// </summary>
    [Server]
    private void InitializePlayer(NetworkedPlayer player, NetworkConnection conn)
    {
        string playerName = GetPlayerName(conn.ClientId);
        Debug.Log($"[GameSceneSpawner] Initialized player {playerName}");
    }

    /// <summary>
    /// Initialize enhanced player
    /// </summary>
    [Server]
    private void InitializePlayerEnhanced(NetworkedPlayerEnhanced player, NetworkConnection conn)
    {
        string playerName = GetPlayerName(conn.ClientId);

        // Set player properties
        player.SetPlayerNameServer(playerName);

        // Assign team if using team spawns
        if (useTeamSpawns)
        {
            int teamId = conn.ClientId % 2 == 0 ? 1 : 2; // Team A = 1, Team B = 2
            player.SetTeamServer(teamId);
        }

        Debug.Log($"[GameSceneSpawner] Initialized enhanced player {playerName} (Team: {(useTeamSpawns ? (conn.ClientId % 2 == 0 ? "A" : "B") : "FFA")})");
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