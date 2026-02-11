using FishNet.Managing;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// Keeps NetworkManager GameObject alive across scene/panel changes
/// Attach this to NetworkManager GameObject
/// This is the ONLY script that should call DontDestroyOnLoad
/// </summary>
public class NetworkManagerPersistence : MonoBehaviour
{
    private static NetworkManagerPersistence instance;
    private static GameObject persistentNetworkManager;

    private void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Debug.LogWarning("[NetworkManagerPersistence] Duplicate NetworkManager found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        instance = this;
        persistentNetworkManager = gameObject;

        Destroy(gameObject.GetComponent<NetworkObject>());

        // This is the ONLY place DontDestroyOnLoad should be called
        DontDestroyOnLoad(gameObject);

        Debug.Log("[NetworkManagerPersistence] NetworkManager will persist across all panels/scenes");
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            persistentNetworkManager = null;
            Debug.Log("[NetworkManagerPersistence] NetworkManager destroyed");
        }
    }

    /// <summary>
    /// Force the NetworkManager to stay active
    /// Call this if something accidentally disables it
    /// </summary>
    public static void EnsureActive()
    {
        if (persistentNetworkManager != null && !persistentNetworkManager.activeInHierarchy)
        {
            persistentNetworkManager.SetActive(true);
            Debug.Log("[NetworkManagerPersistence] Re-enabled NetworkManager");
        }
    }

    /// <summary>
    /// Get the persistent NetworkManager GameObject
    /// </summary>
    public static GameObject GetNetworkManager()
    {
        return persistentNetworkManager;
    }

    private void Update()
    {
        // Safety check - ensure NetworkManager stays active
        if (gameObject != null && !gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[NetworkManagerPersistence] NetworkManager was disabled! Re-enabling...");
            gameObject.SetActive(true);
        }
    }
}
