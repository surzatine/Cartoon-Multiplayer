using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListItem : MonoBehaviour
{
    public TMP_Text PlayerNameText;
    public TMP_Text PlayerStatusText;
    public Image PlayerProfileImage;
    public Image PlayerStatusImage;

    public TMP_Text GetPlayerName()
    {
        return PlayerNameText;
        //PlayerNameText.text = playerName;

    }

    public TMP_Text GetPlayerStatus()
    {
        //PlayerStatusText.text = playerStatus;
        return PlayerStatusText;
    }

    public Image GetPlayerStatusImage()
    {
        return PlayerStatusImage;
    }
}
