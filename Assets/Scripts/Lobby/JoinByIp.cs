using UnityEngine;
using TMPro; // If you're using TextMeshPro
using FishNet.Managing;

public class JoinByIP : MonoBehaviour
{
    public TMP_InputField ipInputField;

    public void JoinGame()
    {
        string ip = ipInputField.text;

        // Fix: Cast NetworkManager.Singleton to NetworkManager to access TransportManager
        var networkManager = NetworkManager.Singleton as NetworkManager;
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager.Singleton is not a NetworkManager instance.");
            return;
        }

        var transport = networkManager.TransportManager.Transport;
        transport.SetClientAddress(ip);  // Example: "192.168.1.5"
        transport.SetPort(7777);         // Match the server port

        networkManager.ClientManager.StartConnection();
        Debug.Log("Connecting to: " + ip);
    }
}
