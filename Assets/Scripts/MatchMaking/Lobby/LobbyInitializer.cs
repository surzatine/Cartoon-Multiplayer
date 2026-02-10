using UnityEngine;
using FishNet.Object;
using System.Collections;

/// <summary>
/// Ensures lobby components are properly initialized before use
/// Attach this to the same GameObject as LobbyUI
/// </summary>
public class LobbyInitializer : MonoBehaviour
{
    [SerializeField] private LobbyUI lobbyUI;
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private float maxWaitTime = 5f;

    private bool isInitialized = false;

    private void Start()
    {
        // Disable LobbyUI until everything is ready
        if (lobbyUI != null)
            lobbyUI.enabled = false;

        StartCoroutine(WaitForInitialization());
    }

    private IEnumerator WaitForInitialization()
    {
        float waitTime = 0f;

        Debug.Log("[X] InitializeLobbyManager...");

        // Wait for lobby manager to be ready
        while (lobbyManager == null || !lobbyManager.IsClientInitialized)
        {
            // Try to find lobby manager if not assigned
            if (lobbyManager == null)
            {
                lobbyManager = FindAnyObjectByType<LobbyManager>();
            }

            waitTime += Time.deltaTime;
            
            if (waitTime >= maxWaitTime)
            {
                Debug.LogError("[LobbyInitializer] Timeout waiting for LobbyManager initialization!");
                break;
            }

            yield return null;
        }

        Debug.Log("[X] Initialize LobbyUI...");

        // Wait for client connection
        while (lobbyManager != null && lobbyManager.ClientManager != null && 
               lobbyManager.ClientManager.Connection == null)
        {
            waitTime += Time.deltaTime;
            
            if (waitTime >= maxWaitTime)
            {
                Debug.LogError("[LobbyInitializer] Timeout waiting for client connection!");
                break;
            }

            yield return null;
        }


        Debug.Log("[X] Finalize LobbyUI...");

        // Wait one more frame to ensure everything is settled
        yield return new WaitForEndOfFrame();

        // Enable LobbyUI
        if (lobbyUI != null)
        {
            lobbyUI.enabled = true;
            Debug.Log("[LobbyInitializer] Lobby UI initialized successfully!");
        }

        isInitialized = true;

        Debug.Log("[X] Complete LobbyUI...");
    }

    public bool IsInitialized => isInitialized;
}
