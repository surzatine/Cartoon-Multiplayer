using UnityEngine;

/// <summary>
/// Helper script to validate LAN matchmaking setup
/// Attach to NetworkManager GameObject to check if everything is configured correctly
/// </summary>
public class SetupValidator : MonoBehaviour
{
    [Header("Required Components")]
    public LANNetworkManager lanNetworkManager;
    public LANDiscovery lanDiscovery;
    public PlayerConnectionManager playerConnectionManager;

    [Header("UI References")]
    public MultiplayerMenuUI multiplayerMenu;
    public HostGameUI hostGameUI;
    public RoomBrowserUI roomBrowserUI;

    [Header("Validation Results")]
    public bool isValid = false;
    public string validationMessage = "";

    [ContextMenu("Validate Setup")]
    public void ValidateSetup()
    {
        validationMessage = "";
        isValid = true;

        // Check NetworkManager components
        if (lanNetworkManager == null)
        {
            LogError("LANNetworkManager component is missing!");
            isValid = false;
        }

        if (lanDiscovery == null)
        {
            LogError("LANDiscovery component is missing!");
            isValid = false;
        }

        if (playerConnectionManager == null)
        {
            LogError("PlayerConnectionManager component is missing!");
            isValid = false;
        }

        // Check Fishnet NetworkManager
        var fishnetManager = GetComponent<FishNet.Managing.NetworkManager>();
        if (fishnetManager == null)
        {
            LogError("Fishnet NetworkManager component is missing!");
            isValid = false;
        }
        else
        {
            if (fishnetManager.TransportManager == null || fishnetManager.TransportManager.Transport == null)
            {
                LogError("Fishnet Transport is not configured!");
                isValid = false;
            }
        }

        // Check UI components
        if (multiplayerMenu == null)
        {
            LogWarning("MultiplayerMenuUI reference is missing (optional but recommended)");
        }

        if (hostGameUI == null)
        {
            LogWarning("HostGameUI reference is missing (optional but recommended)");
        }

        if (roomBrowserUI == null)
        {
            LogWarning("RoomBrowserUI reference is missing (optional but recommended)");
        }

        if (isValid)
        {
            validationMessage = "✓ All required components are present!\n";
            validationMessage += "Setup is ready for LAN matchmaking.";
            Debug.Log($"<color=green>{validationMessage}</color>");
        }
        else
        {
            Debug.LogError($"<color=red>Setup validation failed! Check the console for errors.</color>");
        }
    }

    private void LogError(string message)
    {
        validationMessage += $"✗ {message}\n";
        Debug.LogError(message);
    }

    private void LogWarning(string message)
    {
        validationMessage += $"⚠ {message}\n";
        Debug.LogWarning(message);
    }

    private void Reset()
    {
        // Auto-assign components when added
        lanNetworkManager = GetComponent<LANNetworkManager>();
        lanDiscovery = GetComponent<LANDiscovery>();
        playerConnectionManager = GetComponent<PlayerConnectionManager>();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-find components if not assigned
        if (lanNetworkManager == null)
            lanNetworkManager = GetComponent<LANNetworkManager>();
        
        if (lanDiscovery == null)
            lanDiscovery = GetComponent<LANDiscovery>();
        
        if (playerConnectionManager == null)
            playerConnectionManager = GetComponent<PlayerConnectionManager>();

        // Try to find UI components in scene
        if (multiplayerMenu == null)
            multiplayerMenu = FindObjectOfType<MultiplayerMenuUI>();
        
        if (hostGameUI == null)
            hostGameUI = FindObjectOfType<HostGameUI>();
        
        if (roomBrowserUI == null)
            roomBrowserUI = FindObjectOfType<RoomBrowserUI>();
    }
#endif
}
