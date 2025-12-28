using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
//using FishNet.Scene;
using UnityEngine;

[AddComponentMenu("Game/Networking/Custom Player Spawner")]
public class UserPlayerSpawner : MonoBehaviour
{
    [Header("Player Data")]
    [SerializeField] private SO_Player soPlayer;

    [Header("Spawn Settings")]
    [SerializeField] private bool addToDefaultScene = true;
    [SerializeField] private Transform[] spawnPoints;

    private NetworkManager _networkManager;
    private int _nextSpawnIndex;

    private void Awake()
    {
        _networkManager = GetComponentInParent<NetworkManager>();
        if (_networkManager == null)
        {
            Debug.LogError("NetworkManager not found.");
            return;
        }

        _networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
    }

    private void OnDestroy()
    {
        if (_networkManager != null)
            _networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
    }

    private void OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
    {
        if (!asServer)
            return;

        // Example: playerId could come from authentication, login, or custom handshake
        string playerId = GetPlayerId(conn);

        NetworkObject prefab = soPlayer.GetPrefab(playerId);
        if (prefab == null)
        {
            Debug.LogWarning($"No prefab found for PlayerId: {playerId}");
            return;
        }

        GetSpawnTransform(out Vector3 pos, out Quaternion rot);

        NetworkObject playerObj =
            _networkManager.GetPooledInstantiated(prefab, pos, rot, true);

        _networkManager.ServerManager.Spawn(playerObj, conn);

        if (addToDefaultScene)
            _networkManager.SceneManager.AddOwnerToDefaultScene(playerObj);
    }

    // ----------------------------------------------------
    // Helpers
    // ----------------------------------------------------

    private void GetSpawnTransform(out Vector3 pos, out Quaternion rot)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;
            return;
        }

        Transform t = spawnPoints[_nextSpawnIndex];
        pos = t.position;
        rot = t.rotation;

        _nextSpawnIndex = (_nextSpawnIndex + 1) % spawnPoints.Length;
    }

    private string GetPlayerId(NetworkConnection conn)
    {
        // 🔴 Replace this with your real logic
        // Examples:
        // - conn.ClientId.ToString()
        // - data sent during authentication
        // - lobby selection
        //return conn.ClientId.ToString();

        //Debug.Log("lol");
        Debug.Log("Character Id:" + PlayerStatics.CharacterId);

        return PlayerStatics.CharacterId;
    }
}
