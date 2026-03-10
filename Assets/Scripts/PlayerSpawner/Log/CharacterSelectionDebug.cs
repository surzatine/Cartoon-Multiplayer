using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

/// <summary>
/// Test script to verify character selection is working correctly
/// Attach to NetworkManager and check console logs
/// </summary>
public class CharacterSelectionDebug : NetworkBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private KeyCode testKey = KeyCode.T;

    //private CharacterSelectionManager selectionManager;
    private CharacterSelectionSync selectionSync;

    private void Awake()
    {
        //selectionManager = FindObjectOfType<CharacterSelectionManager>();
        selectionSync = FindObjectOfType<CharacterSelectionSync>();
    }

    private void Update()
    {
        // Press T to print debug info
        if (Input.GetKeyDown(testKey))
        {
            PrintDebugInfo();
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!enableDebugLogs) return;

        // Log what character the CLIENT selected
        //int localCharacterId = CharacterSelectionManager.GetLocalCharacterId();
        //Debug.Log($"<color=cyan>[CharacterDebug] CLIENT STARTED - My selected character ID: {localCharacterId}</color>");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (!enableDebugLogs) return;

        Debug.Log($"<color=yellow>[CharacterDebug] SERVER STARTED</color>");

        // After 2 seconds, print all player selections on server
        Invoke(nameof(PrintServerSelections), 2f);
    }

    [Server]
    private void PrintServerSelections()
    {
        if (!enableDebugLogs) return;

        Debug.Log($"<color=yellow>========== SERVER CHARACTER SELECTIONS ==========</color>");

        foreach (var conn in ServerManager.Clients.Values)
        {
            if (conn != null && conn.IsActive)
            {
                //int characterId = selectionManager != null
                //    ? selectionManager.GetPlayerCharacter(conn.ClientId)
                //    : -1;

                string playerName = PlayerPrefs.GetString($"Player_{conn.ClientId}_Name", $"Player {conn.ClientId}");

                //Debug.Log($"<color=yellow>[Server] Client {conn.ClientId} ({playerName}) → Character ID: {characterId}</color>");
            }
        }

        Debug.Log($"<color=yellow>================================================</color>");
    }

    private void PrintDebugInfo()
    {
        Debug.Log($"<color=green>========== CHARACTER SELECTION DEBUG ==========</color>");

        // Client info
        //int myCharacterId = CharacterSelectionManager.GetLocalCharacterId();
        //Debug.Log($"<color=green>[Me] My selected character ID: {myCharacterId}</color>");

        // Server info (if server)
        if (IsServerInitialized)
        {
            Debug.Log($"<color=green>[Server] All player selections:</color>");

            foreach (var conn in ServerManager.Clients.Values)
            {
                if (conn != null && conn.IsActive)
                {
                    //int characterId = selectionManager != null
                    //    ? selectionManager.GetPlayerCharacter(conn.ClientId)
                    //    : -1;

                    //Debug.Log($"<color=green>  Client {conn.ClientId} → Character {characterId}</color>");
                }
            }
        }

        Debug.Log($"<color=green>===============================================</color>");
    }

    /// <summary>
    /// Call this from your UI to test character selection
    /// </summary>
    public static void TestSetCharacter(int characterId)
    {
        //CharacterSelectionManager.SetLocalCharacterId(characterId);
        Debug.Log($"<color=magenta>[TEST] Set local character to: {characterId}</color>");
    }
}