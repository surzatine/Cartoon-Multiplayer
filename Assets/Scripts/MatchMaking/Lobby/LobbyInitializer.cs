using System;
using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class LobbyInitializer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LobbyUI lobbyUI;
    [SerializeField] private LobbyManager lobbyManager;

    [Header("Settings")]
    [SerializeField] private float maxWaitTime = 5f;

    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        if (lobbyUI == null)
            lobbyUI = GetComponent<LobbyUI>();

        if (lobbyUI != null)
            lobbyUI.enabled = false;
    }

    private void Start()
    {
        StartCoroutine(InitializeRoutine());
    }

    private IEnumerator InitializeRoutine()
    {
        Debug.Log("[LobbyInitializer] Starting initialization...");

        // Wait for LobbyManager
        yield return WaitUntilCondition(
            condition: () =>
            {
                if (lobbyManager == null)
                    lobbyManager = FindAnyObjectByType<LobbyManager>();

                return lobbyManager != null && lobbyManager.IsClientInitialized;
            },
            timeoutMessage: "Timeout waiting for LobbyManager initialization."
        );

        if (lobbyManager == null)
            yield break;

        Debug.Log("[LobbyInitializer] LobbyManager ready.");

        // Wait for client connection
        yield return WaitUntilCondition(
            condition: () => lobbyManager?.ClientManager?.Connection != null,
            timeoutMessage: "Timeout waiting for client connection."
        );

        Debug.Log("[LobbyInitializer] Client connected.");

        // Let one frame pass to stabilize
        yield return new WaitForEndOfFrame();

        if (lobbyUI != null)
            lobbyUI.enabled = true;

        IsInitialized = true;

        Debug.Log("[LobbyInitializer] Initialization complete.");
    }

    /// <summary>
    /// Waits until condition returns true or timeout occurs.
    /// </summary>
    private IEnumerator WaitUntilCondition(Func<bool> condition, string timeoutMessage)
    {
        float timer = 0f;

        while (!condition())
        {
            timer += Time.deltaTime;

            if (timer >= maxWaitTime)
            {
                Debug.LogError($"[LobbyInitializer] {timeoutMessage}");
                yield break;
            }

            yield return null;
        }
    }
}
