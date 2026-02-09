# Mini Militia Style LAN Matchmaking for Unity + Fishnet

A complete LAN multiplayer matchmaking system inspired by Mini Militia, built for Unity with Fishnet Networking.

## Features

✅ **UDP Broadcast Discovery** - Automatic room discovery on local network
✅ **Room Browser** - See all available games with details (players, map, mode)
✅ **Host Game** - Create custom rooms with settings
✅ **Quick Join** - Instantly join available games
✅ **Player Tracking** - Automatic player count updates
✅ **Room Timeout** - Expired rooms automatically removed
✅ **Full UI System** - Complete menu system ready to use

## Requirements

- Unity 2021.3 or higher [ Unity 6000.3.2f1 ]
- Fishnet Networking (install from Asset Store or GitHub)
- TextMeshPro (auto-installed with Unity)

## Installation

### 1. Install Fishnet
Download and import Fishnet from:
- Asset Store: https://assetstore.unity.com/packages/tools/network/fish-net-networking-evolved-207815
- GitHub: https://github.com/FirstGearGames/FishNet

### 2. Import Scripts
Copy all provided scripts into your Unity project's `Scripts` folder:
- `RoomData.cs`
- `LANDiscovery.cs`
- `LANNetworkManager.cs`
- `RoomBrowserUI.cs`
- `RoomListItem.cs`
- `HostGameUI.cs`
- `MultiplayerMenuUI.cs`
- `PlayerConnectionManager.cs`
- `NetworkedPlayer.cs`

## Setup Instructions

### Scene Setup

#### 1. Create Network Manager GameObject
- Create empty GameObject named "NetworkManager"
- Add Fishnet's `NetworkManager` component
- Add `LANDiscovery` component
- Add `LANNetworkManager` component
- Add `PlayerConnectionManager` component

#### 2. Configure Network Manager
In the NetworkManager component:
- Set Transport to `Tugboat` (Fishnet's default)
- Server Manager Settings:
  - Max Connections: 8 (or your desired max)
  - Start Port: 7770
- Client Manager Settings:
  - Start Port: 7770

#### 3. Create UI Hierarchy

Create a Canvas with the following structure:

```
Canvas (Screen Space - Overlay)
├── MainMenuPanel
│   ├── Title (TextMeshPro)
│   ├── HostGameButton
│   ├── JoinGameButton
│   ├── QuickJoinButton
│   ├── BackButton
│   └── ConnectionStatus (TextMeshPro)
│
├── HostGamePanel (Initially disabled)
│   ├── RoomNameInput (TMP_InputField)
│   ├── HostNameInput (TMP_InputField)
│   ├── MaxPlayersDropdown (TMP_Dropdown)
│   ├── MapDropdown (TMP_Dropdown)
│   ├── GameModeDropdown (TMP_Dropdown)
│   ├── IPAddressText (TextMeshPro)
│   ├── StatusText (TextMeshPro)
│   ├── HostButton
│   └── CancelButton
│
└── RoomBrowserPanel (Initially disabled)
    ├── RoomListContainer (Vertical Layout Group)
    ├── RefreshButton
    ├── BackButton
    ├── StatusText (TextMeshPro)
    └── NoRoomsText (TextMeshPro)
```

#### 4. Create Room List Item Prefab

Create a prefab named "RoomListItem" with:

```
RoomListItem (with Image as background)
├── RoomNameText (TextMeshPro)
├── HostNameText (TextMeshPro)
├── PlayerCountText (TextMeshPro)
├── MapNameText (TextMeshPro)
├── GameModeText (TextMeshPro)
├── PingText (TextMeshPro)
└── JoinButton
    └── ButtonText (TextMeshPro)
```

Add `RoomListItem` script to the prefab and assign all references.

### 5. Assign References

**NetworkManager GameObject:**
- LANNetworkManager:
  - Network Manager: Drag NetworkManager component
  - LAN Discovery: Drag LANDiscovery component
  - Server Port: 7770
  - Max Players: 8

**MultiplayerMenuUI (on Canvas):**
- Network Manager: Drag LANNetworkManager from NetworkManager GameObject
- Host Game Panel: Drag HostGamePanel
- Room Browser Panel: Drag RoomBrowserPanel
- Main Menu Panel: Drag MainMenuPanel
- Assign all buttons

**HostGameUI (on HostGamePanel):**
- Network Manager: Drag LANNetworkManager
- Assign all input fields and dropdowns
- Configure available maps and game modes in inspector

**RoomBrowserUI (on RoomBrowserPanel):**
- LAN Discovery: Drag LANDiscovery from NetworkManager GameObject
- Network Manager: Drag LANNetworkManager
- Room List Container: Drag the container with Vertical Layout
- Room Item Prefab: Drag your RoomListItem prefab
- Assign buttons and text fields

## Usage

### For Players

#### Host a Game:
1. Click "Host Game"
2. Enter room name and your name
3. Select max players, map, and game mode
4. Click "Host"
5. Game will start and begin broadcasting

#### Join a Game:
1. Click "Join Game"
2. Wait for rooms to appear (1-2 seconds)
3. Click "Join" on desired room
4. Connected!

#### Quick Join:
1. Click "Quick Join"
2. Automatically connects to first available room

### For Developers

#### Creating Player Prefab:
```csharp
// Your player prefab needs:
1. NetworkObject component (Fishnet)
2. NetworkedPlayer script (or your custom player script)
3. Rigidbody2D
4. Collider2D
5. SpriteRenderer

// Configure NetworkObject:
- Check "Is Spawnable"
- Check "Is NetworkObject"
```

#### Spawning Players:
```csharp
public class GameManager : NetworkBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    public override void OnStartServer()
    {
        NetworkManager.ServerManager.OnRemoteConnectionState += OnPlayerConnection;
    }

    private void OnPlayerConnection(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            // Spawn player for this connection
            Vector3 spawnPos = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
            NetworkObject nob = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            ServerManager.Spawn(nob, conn);
        }
    }
}
```

#### Custom Room Settings:
Modify `RoomData.cs` to add custom fields:
```csharp
public class RoomData
{
    // Add custom fields
    public bool friendlyFire;
    public int timeLimit;
    public string password; // For private rooms
    // etc...
}
```

## Network Architecture

```
Host Device                    Client Devices
┌──────────────┐              ┌──────────────┐
│              │              │              │
│  Fishnet     │◄────────────►│  Fishnet     │
│  Server      │   TCP/UDP    │  Client      │
│              │              │              │
└──────┬───────┘              └──────────────┘
       │                              ▲
       │ UDP Broadcast                │
       │ (Room Info)                  │
       └──────────────────────────────┘
```

## Troubleshooting

### Rooms Not Appearing
- Check firewall settings (allow UDP port 47777)
- Ensure devices are on same network
- Check router AP isolation isn't enabled
- Verify broadcast port in LANDiscovery (default: 47777)

### Can't Connect to Room
- Check server port matches (default: 7770)
- Ensure Fishnet Transport is configured correctly
- Verify max players not exceeded
- Check if room timed out (default 5 seconds)

### Players Not Syncing
- Ensure NetworkObject is on player prefab
- Check player prefab is registered in Fishnet's Spawnable Prefabs
- Verify NetworkBehaviour is inherited correctly

## Performance Tips

1. **Broadcast Interval**: Adjust in LANDiscovery (default: 1 second)
2. **Room Timeout**: Adjust in RoomBrowserUI (default: 5 seconds)
3. **Tick Rate**: Configure in Fishnet's NetworkManager (recommended: 30-60)
4. **Player Count**: Keep below 12 for optimal LAN performance

## Customization Examples

### Add Password Protection:
```csharp
// In RoomData.cs
public string password;

// In HostGameUI.cs
[SerializeField] private TMP_InputField passwordInput;

// In LANNetworkManager.cs
public void JoinRoom(RoomData room, string password)
{
    if (!string.IsNullOrEmpty(room.password) && room.password != password)
    {
        Debug.Log("Wrong password!");
        return;
    }
    // Continue with join...
}
```

### Add Ping Display:
```csharp
// In RoomListItem.cs
private void UpdatePing(float pingMs)
{
    if (pingText != null)
    {
        pingText.text = $"{pingMs:F0}ms";
        pingText.color = pingMs < 50 ? Color.green : Color.yellow;
    }
}
```

## API Reference

### LANNetworkManager
```csharp
// Host a game
public void HostGame(string roomName, string hostName, int maxPlayers, string mapName, string gameMode)

// Join specific room
public void JoinRoom(RoomData room)

// Quick join
public void QuickJoin(List<RoomData> availableRooms)

// Disconnect
public void Disconnect()

// Update player count (call when players join/leave)
public void UpdatePlayerCount(int playerCount)
```

### LANDiscovery
```csharp
// Start broadcasting (host)
public void StartBroadcasting(RoomData roomData)

// Stop broadcasting
public void StopBroadcasting()

// Start listening (client)
public void StartListening()

// Stop listening
public void StopListening()

// Get local IP
public static string GetLocalIPAddress()

// Events
public event Action<RoomData> OnRoomDiscovered;
```

## License

Free to use for personal and commercial projects.

## Credits

Inspired by Mini Militia (Doodle Army 2: Mini Militia)
Built with Fishnet Networking by FirstGearGames

## Support

For issues or questions:
1. Check Fishnet documentation: https://fish-networking.gitbook.io/
2. Verify all references are assigned in inspector
3. Check Unity console for error messages
