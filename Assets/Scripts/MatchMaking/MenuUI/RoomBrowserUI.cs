using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class RoomBrowserUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LANDiscovery lanDiscovery;
    [SerializeField] private LANNetworkManager networkManager;
    [SerializeField] private MenuUIManager menuUIManager;

    [Header("UI References")]
    [SerializeField] private Transform roomListContainer;
    [SerializeField] private GameObject roomItemPrefab;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject noRoomsText;

    [Header("Settings")]
    [SerializeField] private float roomExpiryTime = 5f;

    private Dictionary<string, RoomListItem> activeRoomItems = new Dictionary<string, RoomListItem>();
    private Dictionary<string, float> roomLastSeen = new Dictionary<string, float>();

    private void Start()
    {
        // Setup buttons
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshRoomList);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);

        // Subscribe to discovery events
        if (lanDiscovery != null)
        {
            lanDiscovery.OnRoomDiscovered += OnRoomDiscovered;
        }

        // Start listening for rooms
        StartBrowsing();
    }

    private void OnDestroy()
    {
        if (lanDiscovery != null)
        {
            lanDiscovery.OnRoomDiscovered -= OnRoomDiscovered;
            lanDiscovery.StopListening();
        }
    }

    private void Update()
    {
        // Remove expired rooms
        List<string> expiredRooms = new List<string>();
        
        foreach (var kvp in roomLastSeen)
        {
            if (Time.time - kvp.Value > roomExpiryTime)
            {
                expiredRooms.Add(kvp.Key);
            }
        }

        foreach (var roomId in expiredRooms)
        {
            RemoveRoom(roomId);
        }

        // Update "No Rooms" text visibility
        if (noRoomsText != null)
        {
            noRoomsText.SetActive(activeRoomItems.Count == 0);
        }
    }

    public void StartBrowsing()
    {
        ClearRoomList();
        
        if (lanDiscovery != null)
        {
            lanDiscovery.StartListening();
            UpdateStatusText("Searching for rooms...");
        }
    }

    public void StopBrowsing()
    {
        if (lanDiscovery != null)
        {
            lanDiscovery.StopListening();
        }
        ClearRoomList();
    }

    private void OnRoomDiscovered(RoomData room)
    {
        // Update last seen time
        roomLastSeen[room.roomId] = Time.time;

        // Update or create room item
        if (activeRoomItems.ContainsKey(room.roomId))
        {
            // Update existing room
            activeRoomItems[room.roomId].UpdateRoomData(room);
        }
        else
        {
            // Create new room item
            CreateRoomItem(room);
        }

        UpdateStatusText($"Found {activeRoomItems.Count} room(s)");
    }

    private void CreateRoomItem(RoomData room)
    {
        if (roomItemPrefab == null || roomListContainer == null) return;

        GameObject itemObj = Instantiate(roomItemPrefab, roomListContainer);
        RoomListItem item = itemObj.GetComponent<RoomListItem>();
        
        if (item != null)
        {
            item.Initialize(room, OnJoinRoomClicked);
            activeRoomItems[room.roomId] = item;
        }
    }

    private void RemoveRoom(string roomId)
    {
        if (activeRoomItems.ContainsKey(roomId))
        {
            Destroy(activeRoomItems[roomId].gameObject);
            activeRoomItems.Remove(roomId);
        }
        
        if (roomLastSeen.ContainsKey(roomId))
        {
            roomLastSeen.Remove(roomId);
        }

        UpdateStatusText($"Found {activeRoomItems.Count} room(s)");
    }

    private void ClearRoomList()
    {
        foreach (var item in activeRoomItems.Values)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        
        activeRoomItems.Clear();
        roomLastSeen.Clear();
        UpdateStatusText("No rooms found");
    }

    private void RefreshRoomList()
    {
        ClearRoomList();
        UpdateStatusText("Refreshing...");
    }

    private void OnJoinRoomClicked(RoomData room)
    {
        if (networkManager != null)
        {
            networkManager.JoinRoom(room);
            UpdateStatusText($"Joining {room.roomName}...");
        }
    }

    public void QuickJoin()
    {
        var availableRooms = activeRoomItems.Values
            .Select(item => item.RoomData)
            .Where(room => !room.IsFull())
            .ToList();

        if (networkManager != null)
        {
            networkManager.QuickJoin(availableRooms);
        }
    }

    private void UpdateStatusText(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }

    private void OnBackButtonClicked()
    {
        StopBrowsing();

        networkManager.Disconnect();

        // Load main menu or previous scene
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        menuUIManager.ShowMainMenu();
    }
}
