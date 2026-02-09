using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Diagnostic tool for network port issues
/// Attach to NetworkManager to debug port conflicts
/// </summary>
public class NetworkPortDiagnostics : MonoBehaviour
{
    [Header("Ports to Check")]
    [SerializeField] private int broadcastPort = 47777;
    [SerializeField] private int serverPort = 7770;

    [Header("Diagnostics")]
    [SerializeField] private bool runOnStart = false;

    private void Start()
    {
        if (runOnStart)
        {
            RunDiagnostics();
        }
    }

    [ContextMenu("Run Network Diagnostics")]
    public void RunDiagnostics()
    {
        Debug.Log("===== NETWORK DIAGNOSTICS =====");
        
        CheckPortAvailability(broadcastPort, "Broadcast Port (UDP)");
        CheckPortAvailability(serverPort, "Server Port (TCP)");
        
        Debug.Log($"Local IP Address: {GetLocalIPAddress()}");
        Debug.Log("==============================");
    }

    private void CheckPortAvailability(int port, string portName)
    {
        // Check UDP
        try
        {
            using (UdpClient udp = new UdpClient())
            {
                udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));
                Debug.Log($"✓ {portName} ({port}) is AVAILABLE (UDP)");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"✗ {portName} ({port}) is BLOCKED (UDP): {e.Message}");
        }

        // Check TCP
        try
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.Stop();
            Debug.Log($"✓ {portName} ({port}) is AVAILABLE (TCP)");
        }
        catch (Exception e)
        {
            Debug.LogError($"✗ {portName} ({port}) is BLOCKED (TCP): {e.Message}");
        }
    }

    [ContextMenu("Force Release All Ports")]
    public void ForceReleaseAllPorts()
    {
        Debug.Log("Attempting to force release ports...");
        
        // Find all LANDiscovery instances
        LANDiscovery[] discoveries = Resources.FindObjectsOfTypeAll<LANDiscovery>();
        Debug.Log($"Found {discoveries.Length} LANDiscovery instances");
        
        foreach (var discovery in discoveries)
        {
            // Try to stop them
            try
            {
                discovery.StopListening();
                discovery.StopBroadcasting();
                Debug.Log("Stopped LANDiscovery instance");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error stopping LANDiscovery: {e.Message}");
            }
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        
        Debug.Log("Port release completed. Wait 2 seconds before restarting.");
    }

    private string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "No IPv4 address found";
        }
        catch (Exception e)
        {
            return $"Error: {e.Message}";
        }
    }

    [ContextMenu("List All Network Objects")]
    public void ListNetworkObjects()
    {
        Debug.Log("===== NETWORK OBJECTS IN SCENE =====");
        
        var discoveries = Resources.FindObjectsOfTypeAll<LANDiscovery>();
        Debug.Log($"LANDiscovery instances: {discoveries.Length}");
        foreach (var d in discoveries)
        {
            Debug.Log($"  - {d.gameObject.name} (Active: {d.gameObject.activeInHierarchy})");
        }

        var networkManagers = Resources.FindObjectsOfTypeAll<LANNetworkManager>();
        Debug.Log($"LANNetworkManager instances: {networkManagers.Length}");
        foreach (var nm in networkManagers)
        {
            Debug.Log($"  - {nm.gameObject.name} (IsHost: {nm.IsHost})");
        }

        var roomBrowsers = Resources.FindObjectsOfTypeAll<RoomBrowserUI>();
        Debug.Log($"RoomBrowserUI instances: {roomBrowsers.Length}");
        foreach (var rb in roomBrowsers)
        {
            Debug.Log($"  - {rb.gameObject.name} (Active: {rb.gameObject.activeInHierarchy})");
        }
        
        Debug.Log("====================================");
    }
}
