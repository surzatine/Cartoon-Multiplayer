using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using System.Collections.Generic;
using UnityEngine;
using System;
using FishNet;

/// <summary>
/// Manages game state, scoring, and match flow in the game scene
/// Isolated per room - each room has its own GameManager instance
/// </summary>
public class GameManager : NetworkBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private GameMode gameMode = GameMode.Deathmatch;
    [SerializeField] private int scoreLimit = 30;
    [SerializeField] private float timeLimit = 600f; // 10 minutes
    [SerializeField] private bool enableFriendlyFire = false;

    [Header("References")]
    [SerializeField] private GameSceneSpawner spawner;

    // Game state
    //[SyncVar(OnChange = nameof(OnGameStateChanged))]
    private GameState currentGameState = GameState.WaitingForPlayers;

    //[SyncVar]
    private float matchTimeRemaining = 0f;

    //[SyncVar]
    private int teamAScore = 0;

    //[SyncVar]
    private int teamBScore = 0;

    // Player scores (for FFA)
    //[SyncObject]
    private readonly SyncDictionary<int, PlayerScore> playerScores = new SyncDictionary<int, PlayerScore>();

    // Events
    public event Action<GameState> OnGameStateChangedEvent;
    public event Action<int, int> OnScoreUpdated; // teamA, teamB or playerId, score
    public event Action<float> OnTimeUpdated;
    public event Action<string> OnGameEnded; // Winner info

    public GameState CurrentGameState => currentGameState;
    public float TimeRemaining => matchTimeRemaining;
    public int TeamAScore => teamAScore;
    public int TeamBScore => teamBScore;
    public IReadOnlyDictionary<int, PlayerScore> PlayerScores => playerScores;

    private void Awake()
    {
        if (spawner == null)
            spawner = GetComponent<GameSceneSpawner>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        matchTimeRemaining = timeLimit;
        currentGameState = GameState.WaitingForPlayers;

        Debug.Log("[GameManager] Game started on server");

        // Start game when all players are spawned
        Invoke(nameof(CheckStartGame), 2f);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        playerScores.OnChange += OnPlayerScoresChanged;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        playerScores.OnChange -= OnPlayerScoresChanged;
    }

    private void Update()
    {
        if (!IsServerInitialized) return;

        if (currentGameState == GameState.InProgress)
        {
            UpdateGameTimer();
            CheckWinConditions();
        }
    }

    #region Game State Management

    [Server]
    private void CheckStartGame()
    {
        if (spawner == null) return;

        int playerCount = spawner.GetPlayerCount();
        
        if (playerCount >= 2)
        {
            StartGame();
        }
        else
        {
            // Check again in a bit
            Invoke(nameof(CheckStartGame), 1f);
        }
    }

    [Server]
    private void StartGame()
    {
        Debug.Log("[GameManager] Starting game!");
        
        currentGameState = GameState.InProgress;
        matchTimeRemaining = timeLimit;

        // Initialize player scores
        if (spawner != null)
        {
            foreach (var kvp in spawner.GetSpawnedPlayers())
            {
                playerScores[kvp.Key] = new PlayerScore
                {
                    clientId = kvp.Key,
                    kills = 0,
                    deaths = 0
                };
            }
        }

        GameStartedObserversRpc();
    }

    [Server]
    private void UpdateGameTimer()
    {
        matchTimeRemaining -= Time.deltaTime;

        if (matchTimeRemaining <= 0f)
        {
            matchTimeRemaining = 0f;
            EndGame("Time's up!");
        }
    }

    [Server]
    private void CheckWinConditions()
    {
        switch (gameMode)
        {
            case GameMode.Deathmatch:
                CheckDeathmatchWin();
                break;
            case GameMode.TeamDeathmatch:
                CheckTeamDeathmatchWin();
                break;
        }
    }

    [Server]
    private void CheckDeathmatchWin()
    {
        foreach (var kvp in playerScores)
        {
            if (kvp.Value.kills >= scoreLimit)
            {
                EndGame($"Player {kvp.Key} wins!");
                break;
            }
        }
    }

    [Server]
    private void CheckTeamDeathmatchWin()
    {
        if (teamAScore >= scoreLimit)
        {
            EndGame("Team A wins!");
        }
        else if (teamBScore >= scoreLimit)
        {
            EndGame("Team B wins!");
        }
    }

    [Server]
    private void EndGame(string winMessage)
    {
        if (currentGameState == GameState.Ended) return;

        Debug.Log($"[GameManager] Game ended: {winMessage}");
        
        currentGameState = GameState.Ended;
        GameEndedObserversRpc(winMessage);

        // Return to lobby after delay
        Invoke(nameof(ReturnToLobby), 10f);
    }

    [Server]
    private void ReturnToLobby()
    {
        // Load lobby scene for all players in this game
        var sceneManager = InstanceFinder.SceneManager;
        if (sceneManager != null)
        {
            FishNet.Managing.Scened.SceneLoadData sld = new FishNet.Managing.Scened.SceneLoadData("LobbyScene");
            sld.ReplaceScenes = FishNet.Managing.Scened.ReplaceOption.All;
            
            // Get all connections in this game
            List<NetworkConnection> connections = new List<NetworkConnection>();
            if (spawner != null)
            {
                foreach (var kvp in spawner.GetSpawnedPlayers())
                {
                    var conn = ServerManager.Clients[kvp.Key];
                    if (conn != null && conn.IsActive)
                    {
                        connections.Add(conn);
                    }
                }
            }

            sceneManager.LoadConnectionScenes(connections.ToArray(), sld);
        }
    }

    #endregion

    #region Scoring

    /// <summary>
    /// Register a kill (Server only)
    /// </summary>
    [Server]
    public void RegisterKill(int killerId, int victimId)
    {
        // Update killer score
        if (playerScores.ContainsKey(killerId))
        {
            var score = playerScores[killerId];
            score.kills++;
            playerScores[killerId] = score;
        }

        // Update victim deaths
        if (playerScores.ContainsKey(victimId))
        {
            var score = playerScores[victimId];
            score.deaths++;
            playerScores[victimId] = score;
        }

        // Update team scores if in team mode
        if (gameMode == GameMode.TeamDeathmatch)
        {
            // Get killer's team
            var killerObj = spawner.GetSpawnedPlayers()[killerId];
            if (killerObj.TryGetComponent(out NetworkedPlayerEnhanced killer))
            {
                int killerTeam = killer.GetTeamId();

                if (killerTeam == 1)
                    teamAScore++;
                else if (killerTeam == 2)
                    teamBScore++;

                UpdateTeamScoresObserversRpc(teamAScore, teamBScore);
            }
        }

        KillRegisteredObserversRpc(killerId, victimId);
    }

    /// <summary>
    /// Get leaderboard (sorted by kills)
    /// </summary>
    public List<PlayerScore> GetLeaderboard()
    {
        List<PlayerScore> leaderboard = new List<PlayerScore>(playerScores.Values);
        leaderboard.Sort((a, b) => b.kills.CompareTo(a.kills));
        return leaderboard;
    }

    #endregion

    #region Observer RPCs

    [ObserversRpc]
    private void GameStartedObserversRpc()
    {
        Debug.Log("[GameManager] Game started!");
        OnGameStateChangedEvent?.Invoke(GameState.InProgress);
    }

    [ObserversRpc]
    private void GameEndedObserversRpc(string winMessage)
    {
        Debug.Log($"[GameManager] Game ended: {winMessage}");
        OnGameStateChangedEvent?.Invoke(GameState.Ended);
        OnGameEnded?.Invoke(winMessage);
    }

    [ObserversRpc]
    private void KillRegisteredObserversRpc(int killerId, int victimId)
    {
        Debug.Log($"[GameManager] Player {killerId} killed Player {victimId}");
    }

    [ObserversRpc]
    private void UpdateTeamScoresObserversRpc(int scoreA, int scoreB)
    {
        OnScoreUpdated?.Invoke(scoreA, scoreB);
    }

    #endregion

    #region SyncVar Callbacks

    private void OnGameStateChanged(GameState oldState, GameState newState, bool asServer)
    {
        Debug.Log($"[GameManager] Game state changed: {oldState} → {newState}");
        OnGameStateChangedEvent?.Invoke(newState);
    }

    private void OnPlayerScoresChanged(SyncDictionaryOperation op, int key, PlayerScore value, bool asServer)
    {
        if (op == SyncDictionaryOperation.Add || op == SyncDictionaryOperation.Set)
        {
            OnScoreUpdated?.Invoke(key, value.kills);
        }
    }

    #endregion

    #region Public Methods

    public PlayerScore? GetPlayerScore(int clientId)
    {
        if (playerScores.ContainsKey(clientId))
            return playerScores[clientId];
        return null;
    }

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(matchTimeRemaining / 60f);
        int seconds = Mathf.FloorToInt(matchTimeRemaining % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    #endregion
}

#region Enums and Structs

public enum GameState
{
    WaitingForPlayers,
    InProgress,
    Ended
}

public enum GameMode
{
    Deathmatch,
    TeamDeathmatch,
    CaptureTheFlag
}

[System.Serializable]
public struct PlayerScore
{
    public int clientId;
    public int kills;
    public int deaths;

    public float KDRatio => deaths > 0 ? (float)kills / deaths : kills;
}

#endregion
