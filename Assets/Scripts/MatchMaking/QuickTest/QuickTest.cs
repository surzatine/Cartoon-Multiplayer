using UnityEngine;

public class QuickTest : MonoBehaviour
{
    public LANNetworkManager networkManager;

    void Update()
    {
        // Press H to host
        if (Input.GetKeyDown(KeyCode.H))
        {
            networkManager.HostGame("Test Room", "Host", 4, "Test Map", "Deathmatch");
            Debug.Log("Hosting...");
        }

        // Press J to join (will need to implement room discovery)
        if (Input.GetKeyDown(KeyCode.J))
        {
            // This requires setting up LANDiscovery and waiting for room data
            Debug.Log("Use the full UI for join functionality");
        }
    }
}