# Quick Start Guide - LAN Matchmaking Setup

## 5-Minute Setup (Minimum Viable Product)

### Step 1: Install Fishnet (2 minutes)
1. Open Unity Package Manager
2. Search for "Fish-Net" or import from GitHub
3. Import the package

### Step 2: Create Network Scene (1 minute)
1. Create new scene "MultiplayerMenu"
2. Create GameObject: `NetworkManager`
3. Add these components to NetworkManager:
   - Fishnet's `NetworkManager`
   - `LANDiscovery`
   - `LANNetworkManager`
   - `PlayerConnectionManager`

### Step 3: Basic UI Setup (2 minutes)
1. Create Canvas (Screen Space - Overlay)
2. Add 3 buttons:
   - "Host Game" → calls `LANNetworkManager.HostGame()`
   - "Join Game" → enables room browser
   - "Quick Join" → calls `QuickJoin()`
3. Add empty GameObject under Canvas named "RoomList"

That's it! You now have basic LAN matchmaking working.

## Test Your Setup

### Test 1: Single Device
1. Press Play
2. Click "Host Game"
3. Check Console for "Hosting game: ..." message

### Test 2: Multiple Devices (Android)
1. Build APK for Android
2. Install on 2+ devices on same WiFi
3. Device 1: Click "Host Game"
4. Device 2: Click "Join Game" (wait 1-2 seconds for room to appear)

## Common First-Time Issues

### Issue: "NetworkManager not found"
**Fix:** Assign the NetworkManager component reference in LANNetworkManager inspector

### Issue: "Rooms not appearing"
**Fix:** Make sure both devices are on the same WiFi network (not mobile data!)

### Issue: "Can't join room"
**Fix:** Check firewall isn't blocking ports 7770 (TCP) and 47777 (UDP)

## Next Steps

Once basic setup works:

1. **Add Full UI** - Use the complete UI scripts provided (MultiplayerMenuUI, HostGameUI, RoomBrowserUI)
2. **Create Player Prefab** - Use NetworkedPlayer.cs as reference
3. **Add Game Logic** - Create your game modes, spawning, etc.
4. **Test on LAN** - Test with multiple devices

## Minimal Code Example

If you want to test with just code (no UI):

```csharp
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
```

## Architecture Overview

```
User Action → UI → LANNetworkManager → Fishnet → Network

Example flow:
1. User clicks "Host Game"
2. HostGameUI gathers settings
3. Calls LANNetworkManager.HostGame()
4. LANNetworkManager starts Fishnet server
5. LANDiscovery broadcasts room info
6. Other clients see the room in RoomBrowserUI
```

## Resource Checklist

✅ Fishnet installed
✅ NetworkManager GameObject created
✅ LANNetworkManager + LANDiscovery components added
✅ Canvas with buttons created
✅ References assigned in inspector

## Building for Android

### Build Settings:
1. File → Build Settings
2. Switch to Android
3. Player Settings:
   - Minimum API Level: Android 7.0 (API 24)
   - Internet Access: Required
   - Write Permission: Internal

### Permissions (Add to AndroidManifest.xml if needed):
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
<uses-permission android:name="android.permission.ACCESS_WIFI_STATE" />
```

## Performance Tips for Mobile

1. **Reduce broadcast frequency** (LANDiscovery.broadcastInterval = 2f)
2. **Limit max players** (8 players recommended for LAN)
3. **Use LOD** for distant players
4. **Optimize physics** (Fixed Timestep = 0.02 or higher)

## Debugging Commands

Add this to any MonoBehaviour for quick debug info:

```csharp
void OnGUI()
{
    GUILayout.Label($"Is Host: {networkManager.IsHost}");
    GUILayout.Label($"Players: {networkManager.GetPlayerCount()}");
    GUILayout.Label($"IP: {LANDiscovery.GetLocalIPAddress()}");
}
```

## Help & Support

**Fishnet Documentation:** https://fish-networking.gitbook.io/
**Common Issues:** Check README.md → Troubleshooting section
**Discord:** Join Fishnet Discord for community support

---

**Ready to build your game?** Start with the minimal setup above, then gradually add features as needed!
