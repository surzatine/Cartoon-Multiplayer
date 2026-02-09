# Complete Lobby & Game Scene Setup Guide

## Overview
This system creates a complete flow: **Main Menu → Lobby Waiting Room → Game Scene** with room isolation (each room gets its own game instance).

---

## 🎮 Flow Diagram

```
Main Menu
    ↓ Host/Join
Lobby Scene (Waiting Room)
    ↓ Ready + Start
Game Scene (Isolated per Room)
    ↓ Disconnect
Main Menu
```

---

## 📦 New Scripts Added

1. **LobbyManager.cs** - Manages lobby waiting room
2. **LobbyUI.cs** - Lobby UI controller
3. **GameSceneSpawner.cs** - Spawns players in game scene
4. **NetworkedPlayerEnhanced.cs** - Enhanced player with combat
5. **SceneFlowManager.cs** - Manages scene transitions

---

## 🔧 SETUP INSTRUCTIONS

### Part 1: Update NetworkManager GameObject

**GameObject: NetworkManager (already exists)**

Add these NEW components:
- `SceneFlowManager`
- Configure in Inspector:
  - Main Menu Scene: "MultiplayerMenu"
  - Lobby Scene: "LobbyScene"
  - Game Scene: "GameScene"

**Existing components should have:**
- NetworkManager (Fishnet)
- LANDiscovery
- LANNetworkManager
- PlayerConnectionManager
- SceneFlowManager ← NEW

---

### Part 2: Create Lobby Scene

**1. Create new scene: `LobbyScene`**
   - File → New Scene
   - Save as: `Assets/Scenes/LobbyScene.unity`

**2. Scene Hierarchy:**

```
LobbyScene
├── NetworkManager (from Prefab or DontDestroyOnLoad)
│   └── LobbyManager (Add Component)
│
├── Canvas (Screen Space - Overlay)
│   ├── Header Panel
│   │   ├── RoomNameText (TMP)
│   │   ├── PlayerCountText (TMP)
│   │   ├── MapNameText (TMP)
│   │   └── GameModeText (TMP)
│   │
│   ├── Player List Panel
│   │   ├── Title: "Players" (TMP)
│   │   └── PlayerListContainer (Vertical Layout Group)
│   │
│   ├── Controls Panel
│   │   ├── ReadyButton
│   │   │   └── Text: "READY"
│   │   ├── StartGameButton
│   │   │   └── Text: "START GAME"
│   │   └── LeaveButton
│   │       └── Text: "LEAVE"
│   │
│   └── Status Panel
│       ├── StatusText (TMP)
│       └── CountdownPanel (Initially disabled)
│           └── CountdownText (TMP)
│
└── LobbyUIManager (Empty GameObject)
    └── LobbyUI Component
```

**3. Create Player List Item Prefab:**

Create prefab: `PlayerListItem.prefab`

```
PlayerListItem (with Image background)
├── PlayerName (TMP) - Left aligned
├── ReadyStatus (TMP) - Right aligned
└── ReadyIcon (Image) - Small circle indicator
```

**4. Configure LobbyManager:**
- Game Scene Name: "GameScene"
- Min Players To Start: 2
- Auto Start Countdown: 5

**5. Configure LobbyUI:**
- Lobby Manager: Drag LobbyManager from NetworkManager
- Network Manager: Drag LANNetworkManager
- Assign all UI references
- Player List Item Prefab: Drag PlayerListItem prefab

---

### Part 3: Create Game Scene

**1. Create new scene: `GameScene`**
   - File → New Scene
   - Save as: `Assets/Scenes/GameScene.unity`

**2. Scene Hierarchy:**

```
GameScene
├── GameManager (Empty GameObject)
│   └── GameSceneSpawner Component
│
├── Spawn Points (Empty GameObject)
│   ├── SpawnPoint_1 (Empty GameObject)
│   ├── SpawnPoint_2 (Empty GameObject)
│   ├── SpawnPoint_3 (Empty GameObject)
│   └── SpawnPoint_4 (Empty GameObject)
│
├── Team A Spawns (Optional - Empty GameObject)
│   ├── TeamA_Spawn_1
│   └── TeamA_Spawn_2
│
├── Team B Spawns (Optional - Empty GameObject)
│   ├── TeamB_Spawn_1
│   └── TeamB_Spawn_2
│
├── Map
│   ├── Ground (Sprite/Tilemap)
│   ├── Platforms
│   └── Obstacles
│
└── Main Camera
    └── CameraFollow Component (auto-added)
```

**3. Configure GameSceneSpawner:**
- Player Prefab: Drag your player prefab (see below)
- Spawn Points: Drag all spawn point transforms
- Spawn Radius: 1.0
- Randomize Spawn Points: ✓
- Use Team Spawns: ☐ (check if using teams)
- Team A/B Spawn Points: Drag if using teams

---

### Part 4: Create Player Prefab

**1. Create Player GameObject:**

```
Player (NetworkObject)
├── Visuals
│   ├── Sprite (SpriteRenderer)
│   └── WeaponPivot (Empty GameObject)
│       └── Weapon (Sprite)
│
├── NameTag (Canvas - World Space)
│   └── PlayerNameText (TextMeshPro)
│
└── Colliders
    ├── Body (CircleCollider2D)
    └── GroundCheck (Empty at feet)
```

**2. Add Components to Player:**
- NetworkObject (Fishnet) - Check "Is Spawnable"
- Rigidbody2D - Gravity Scale: 3, Freeze Rotation Z
- CircleCollider2D
- NetworkedPlayerEnhanced script

**3. Configure NetworkedPlayerEnhanced:**
- Movement Settings: Adjust as needed
- Combat Settings: Max Health: 100
- Assign all references (RB, Sprite, Weapon Pivot, etc.)

**4. Save as Prefab:**
- Drag to `Assets/Prefabs/Player.prefab`

**5. Register in Fishnet:**
- NetworkManager → Object Pooling → Spawnable Prefabs
- Add Player prefab to list

---

### Part 5: Build Settings

Add scenes in this order:
1. MainMenu (or MultiplayerMenu)
2. LobbyScene
3. GameScene

File → Build Settings → Add Open Scenes

---

## 🎯 TESTING

### Test 1: Host Flow
1. Run game
2. Click "Host Game"
3. Should load → LobbyScene
4. Should see yourself in player list
5. Click "Start Game" (as host)
6. Should load → GameScene
7. Should spawn as player

### Test 2: Join Flow
1. Device 1: Host game
2. Device 2: Join game
3. Both should be in LobbyScene
4. Device 2: Click "Ready"
5. Device 1: Click "Start Game"
6. Both load → GameScene
7. Both spawn at different positions

### Test 3: Room Isolation
1. Device 1 & 2: Create "Room A"
2. Device 3 & 4: Create "Room B"
3. Room A players should NOT see Room B players
4. Each room has its own GameScene instance ✓

---

## ⚙️ How It Works

### Connection Flow:
```
1. Player clicks "Host/Join"
   ↓
2. LANNetworkManager connects to server
   ↓
3. SceneFlowManager detects connection
   ↓
4. Server loads LobbyScene for player
   ↓
5. LobbyManager adds player to lobby
   ↓
6. Player sees lobby UI
```

### Game Start Flow:
```
1. Host clicks "Start Game" OR all ready
   ↓
2. LobbyManager.StartGame()
   ↓
3. Loads GameScene for SPECIFIC connections only
   ↓
4. GameSceneSpawner spawns players
   ↓
5. Each player controls their character
```

### Room Isolation:
```
Room A Players: [Conn 0, Conn 1]
Room B Players: [Conn 2, Conn 3]
    ↓
SceneManager.LoadConnectionScenes([Conn 0, 1], GameScene)
SceneManager.LoadConnectionScenes([Conn 2, 3], GameScene)
    ↓
Result: Two separate GameScene instances
```

---

## 🔑 Key Concepts

### 1. DontDestroyOnLoad
NetworkManager persists across scenes via SceneFlowManager

### 2. Connection-Based Scene Loading
```csharp
// Load scene for specific connections only
NetworkConnection[] connections = { conn1, conn2 };
SceneManager.LoadConnectionScenes(connections, sceneData);
```

### 3. SyncVar & SyncList
- LobbyManager uses SyncList for player list
- NetworkedPlayer uses SyncVar for health, name, etc.

### 4. Server Authority
- Only server can spawn players
- Only server can start game
- Clients send RPCs to request actions

---

## 🎨 UI Customization

### Lobby UI Colors:
```csharp
// In LobbyUI.cs, find UpdatePlayerListItemContent
Color readyColor = Color.green;
Color notReadyColor = Color.yellow;
Color hostColor = Color.cyan;
```

### Countdown Timer:
```csharp
// In LobbyManager.cs
[SerializeField] private float autoStartCountdown = 5f;
```

### Min Players:
```csharp
// In LobbyManager.cs
[SerializeField] private int minPlayersToStart = 2;
```

---

## 🐛 Troubleshooting

### Issue: Lobby scene doesn't load
**Fix:** 
- Ensure scene name matches exactly: "LobbyScene"
- Check Build Settings - scene must be added
- Verify SceneFlowManager is on NetworkManager

### Issue: Players don't spawn in game
**Fix:**
- Check Player prefab is in Fishnet's Spawnable Prefabs list
- Verify GameSceneSpawner has spawn points assigned
- Make sure NetworkObject is on player prefab

### Issue: Can't see other players
**Fix:**
- This is INTENTIONAL for room isolation
- Players in different rooms shouldn't see each other
- Players in same room should see each other

### Issue: "Start Game" button doesn't work
**Fix:**
- Must be host to see this button
- Need minimum 2 players
- Check LobbyManager reference in LobbyUI

### Issue: Multiple players spawn at same position
**Fix:**
- Add more spawn points in GameScene
- Enable "Randomize Spawn Points" in GameSceneSpawner
- Increase Spawn Radius

---

## 📝 Important Notes

1. **NetworkManager MUST persist** - SceneFlowManager handles this
2. **Each room is isolated** - Uses LoadConnectionScenes
3. **Scene names must match** - Check spelling
4. **Build Settings order matters** - Add all scenes
5. **Player prefab MUST be registered** - In Fishnet spawnable list

---

## 🚀 Next Steps

After basic setup works:

1. **Add game modes** - Deathmatch, Team DM, CTF
2. **Add weapons** - Create weapon system
3. **Add HUD** - Health, kills, deaths, minimap
4. **Add chat** - Text chat in lobby & game
5. **Add scoreboard** - End game results
6. **Add game timer** - Match duration
7. **Add spectator mode** - Watch after death

---

## 💡 Pro Tips

1. **Test in Editor first** - Use "ParrelSync" to test multiplayer locally
2. **Use Debug.Log** - Track scene transitions
3. **Check Console** - Look for "[SceneFlow]", "[LobbyManager]" logs
4. **Start simple** - Get lobby working before adding complex game logic
5. **Room isolation is automatic** - Fishnet handles it via LoadConnectionScenes

---

**Ready to implement? Start with Part 1 and work through each section!** 🎮
