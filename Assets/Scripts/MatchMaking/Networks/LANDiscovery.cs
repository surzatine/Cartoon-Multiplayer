using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Linq;

public class LANDiscovery : MonoBehaviour
{
    [Header("Network Settings")]
    [SerializeField] private int broadcastPort = 47777;
    [SerializeField] private float broadcastInterval = 1f;
    [SerializeField] private float roomTimeout = 5f;

    private UdpClient broadcastClient;
    private UdpClient listenerClient;
    private bool isServer = false;
    private float lastBroadcastTime;

    public event Action<RoomData> OnRoomDiscovered;
    public event Action<string> OnRoomLost; // roomId

    // Singleton to prevent multiple instances
    private static LANDiscovery instance;

    // Thread-safe queue for handling broadcasts on main thread
    private readonly System.Collections.Generic.Queue<RoomData> discoveredRoomsQueue = new System.Collections.Generic.Queue<RoomData>();
    private readonly object queueLock = new object();

    private void Awake()
    {
        // Ensure only one LANDiscovery exists
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Multiple LANDiscovery instances detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void OnDestroy()
    {
        StopBroadcasting();
        StopListening();

        if (instance == this)
        {
            instance = null;
        }
    }

    #region Server/Host Methods

    public void StartBroadcasting(RoomData roomData)
    {
        try
        {
            isServer = true;
            broadcastClient = new UdpClient();
            broadcastClient.EnableBroadcast = true;

            PlayerPrefs.SetString("CurrentRoomData", roomData.ToJson());

            Debug.Log($"Started broadcasting room: {roomData.roomName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start broadcasting: {e.Message}");
        }
    }

    public void StopBroadcasting()
    {
        if (broadcastClient != null)
        {
            isServer = false;
            broadcastClient.Close();
            broadcastClient = null;
            Debug.Log("Stopped broadcasting");
        }
    }

    #endregion

    #region Client Methods

    public void StartListening()
    {
        try
        {
            StopListening();

            listenerClient = new UdpClient();
            listenerClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listenerClient.Client.Bind(new IPEndPoint(IPAddress.Any, broadcastPort));
            listenerClient.EnableBroadcast = true;
            listenerClient.BeginReceive(OnBroadcastReceived, null);

            Debug.Log($"Started listening for broadcasts on port {broadcastPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start listening: {e.Message}");
        }
    }

    public void StopListening()
    {
        if (listenerClient != null)
        {
            listenerClient.Close();
            listenerClient = null;
            Debug.Log("Stopped listening");
        }
    }

    private void OnBroadcastReceived(IAsyncResult result)
    {
        try
        {
            if (listenerClient == null) return;

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, broadcastPort);
            byte[] data = listenerClient.EndReceive(result, ref endPoint);
            string message = Encoding.UTF8.GetString(data);

            RoomData roomData = RoomData.FromJson(message);
            roomData.ipAddress = endPoint.Address.ToString();

            lock (queueLock)
            {
                discoveredRoomsQueue.Enqueue(roomData);
            }

            listenerClient.BeginReceive(OnBroadcastReceived, null);
        }
        catch (ObjectDisposedException)
        {
            // Normal when stopping
        }
        catch (Exception e)
        {
            Debug.LogError($"Error receiving broadcast: {e.Message}");

            if (listenerClient != null)
            {
                try
                {
                    listenerClient.BeginReceive(OnBroadcastReceived, null);
                }
                catch { }
            }
        }
    }

    #endregion

    private void Update()
    {
        lock (queueLock)
        {
            while (discoveredRoomsQueue.Count > 0)
            {
                RoomData roomData = discoveredRoomsQueue.Dequeue();
                OnRoomDiscovered?.Invoke(roomData);
            }
        }

        if (isServer && broadcastClient != null)
        {
            if (Time.time - lastBroadcastTime >= broadcastInterval)
            {
                BroadcastRoomData();
                lastBroadcastTime = Time.time;
            }
        }
    }

    private void BroadcastRoomData()
    {
        try
        {
            string roomJson = PlayerPrefs.GetString("CurrentRoomData", "");
            if (string.IsNullOrEmpty(roomJson)) return;

            byte[] data = Encoding.UTF8.GetBytes(roomJson);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
            broadcastClient.Send(data, data.Length, endPoint);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to broadcast: {e.Message}");
        }
    }

    /// <summary>
    /// Get local IP address - IMPROVED to avoid virtual adapters
    /// </summary>
    public static string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var validIPs = new System.Collections.Generic.List<IPAddress>();

            // Collect all IPv4 addresses
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    validIPs.Add(ip);
                    Debug.Log($"[LANDiscovery] Found IP: {ip}");
                }
            }

            if (validIPs.Count == 0)
            {
                throw new Exception("No network adapters with an IPv4 address found!");
            }

            // Filter out known virtual adapter IPs
            var nonVirtualIPs = validIPs.Where(ip => !IsVirtualAdapter(ip)).ToList();

            if (nonVirtualIPs.Count > 0)
            {
                // Priority 1: 192.168.x.x (but not .56.x which is VirtualBox)
                var preferred = nonVirtualIPs.FirstOrDefault(ip =>
                    ip.ToString().StartsWith("192.168.") &&
                    !ip.ToString().StartsWith("192.168.56."));

                if (preferred != null)
                {
                    Debug.Log($"[LANDiscovery] ✓ Selected LAN IP: {preferred}");
                    return preferred.ToString();
                }

                // Priority 2: 10.x.x.x
                preferred = nonVirtualIPs.FirstOrDefault(ip => ip.ToString().StartsWith("10."));
                if (preferred != null)
                {
                    Debug.Log($"[LANDiscovery] ✓ Selected IP: {preferred}");
                    return preferred.ToString();
                }

                // Return first non-virtual IP
                Debug.Log($"[LANDiscovery] ✓ Using first available: {nonVirtualIPs[0]}");
                return nonVirtualIPs[0].ToString();
            }

            // Fallback
            Debug.LogWarning($"[LANDiscovery] ⚠ All IPs appear virtual, using: {validIPs[0]}");
            return validIPs[0].ToString();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting local IP: {e.Message}");
            return "127.0.0.1";
        }
    }

    /// <summary>
    /// Check if IP is from a virtual adapter
    /// </summary>
    private static bool IsVirtualAdapter(IPAddress ip)
    {
        string ipString = ip.ToString();

        // VirtualBox Host-Only: 192.168.56.x
        if (ipString.StartsWith("192.168.56.")) return true;

        // VMware: 192.168.x.1 where x > 50
        if (ipString.StartsWith("192.168.") && ipString.EndsWith(".1"))
        {
            var parts = ipString.Split('.');
            int thirdOctet = int.Parse(parts[2]);
            if (thirdOctet > 50) return true;
        }

        // Hyper-V: 172.24-31.x.x
        if (ipString.StartsWith("172."))
        {
            var parts = ipString.Split('.');
            int secondOctet = int.Parse(parts[1]);
            if (secondOctet >= 24 && secondOctet <= 31) return true;
        }

        return false;
    }

    /// <summary>
    /// Get all IPs for debugging
    /// </summary>
    public static System.Collections.Generic.List<string> GetAllIPAddresses()
    {
        var ipList = new System.Collections.Generic.List<string>();

        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    bool isVirtual = IsVirtualAdapter(ip);
                    ipList.Add($"{ip} {(isVirtual ? "(Virtual)" : "(Physical)")}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting IP list: {e.Message}");
        }

        return ipList;
    }
}