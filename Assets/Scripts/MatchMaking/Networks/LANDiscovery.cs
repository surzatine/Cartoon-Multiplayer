using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

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

    /// <summary>
    /// Start broadcasting this server's room data
    /// </summary>
    public void StartBroadcasting(RoomData roomData)
    {
        try
        {
            isServer = true;
            broadcastClient = new UdpClient();
            broadcastClient.EnableBroadcast = true;

            // Store room data for broadcasting
            PlayerPrefs.SetString("CurrentRoomData", roomData.ToJson());

            Debug.Log($"Started broadcasting room: {roomData.roomName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start broadcasting: {e.Message}");
        }
    }

    /// <summary>
    /// Stop broadcasting
    /// </summary>
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

    /// <summary>
    /// Start listening for server broadcasts
    /// </summary>
    public void StartListening()
    {
        try
        {
            // Stop any existing listener first
            StopListening();

            // Create UDP client with socket reuse enabled
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

    /// <summary>
    /// Stop listening for broadcasts
    /// </summary>
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

            // Parse the room data
            RoomData roomData = RoomData.FromJson(message);
            roomData.ipAddress = endPoint.Address.ToString();

            // Add to queue for processing on main thread
            lock (queueLock)
            {
                discoveredRoomsQueue.Enqueue(roomData);
            }

            // Continue listening
            listenerClient.BeginReceive(OnBroadcastReceived, null);
        }
        catch (ObjectDisposedException)
        {
            // Client was disposed, this is normal when stopping
        }
        catch (Exception e)
        {
            Debug.LogError($"Error receiving broadcast: {e.Message}");

            // Try to restart listening
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
        // Process discovered rooms on main thread
        lock (queueLock)
        {
            while (discoveredRoomsQueue.Count > 0)
            {
                RoomData roomData = discoveredRoomsQueue.Dequeue();
                OnRoomDiscovered?.Invoke(roomData);
            }
        }

        // Broadcast room data at intervals if we're a server
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
    /// Get local IP address
    /// </summary>
    public static string GetLocalIPAddress()
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
            throw new Exception("No network adapters with an IPv4 address found!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting local IP: {e.Message}");
            return "127.0.0.1";
        }
    }
}