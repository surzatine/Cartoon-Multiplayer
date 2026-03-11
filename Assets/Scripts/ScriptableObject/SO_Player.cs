using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_Player", menuName = "Player/SO_Player")]
public class SO_Player : ScriptableObject
{
    public List<PlayerData> PlayerData = new List<PlayerData>();

    public NetworkObject GetPlayerPrefab(string characterId)
    {
        foreach (var p in PlayerData)
        {
            if (p.CharacterId == characterId)
                return p.PlayerPrefab;
        }
        return null;
    }
}
[System.Serializable]
public class PlayerData
{
    public string CharacterId;
    public string CharacterName;
    public NetworkObject PlayerPrefab;
}