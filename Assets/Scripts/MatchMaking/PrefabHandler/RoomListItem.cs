using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class RoomListItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text hostNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text mapNameText;
    [SerializeField] private TMP_Text gameModeText;
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private Button joinButton;
    [SerializeField] private Image backgroundImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color fullColor = new Color(0.4f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color highlightColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    private RoomData roomData;
    private Action<RoomData> onJoinCallback;

    public RoomData RoomData => roomData;

    public void Initialize(RoomData data, Action<RoomData> joinCallback)
    {
        roomData = data;
        onJoinCallback = joinCallback;

        UpdateRoomData(data);

        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnJoinButtonClicked);
        }
    }

    public void UpdateRoomData(RoomData data)
    {
        roomData = data;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (roomData == null) return;

        // Update texts
        if (roomNameText != null)
            roomNameText.text = roomData.roomName;

        if (hostNameText != null)
            hostNameText.text = $"Host: {roomData.hostName}";

        if (playerCountText != null)
        {
            playerCountText.text = $"{roomData.currentPlayers}/{roomData.maxPlayers}";
            
            // Change color if full
            if (roomData.IsFull())
                playerCountText.color = Color.red;
            else if (roomData.currentPlayers > roomData.maxPlayers / 2)
                playerCountText.color = Color.yellow;
            else
                playerCountText.color = Color.green;
        }

        if (mapNameText != null)
            mapNameText.text = $"Map: {roomData.mapName}";

        if (gameModeText != null)
            gameModeText.text = roomData.gameMode;

        if (pingText != null)
            pingText.text = "< 10ms"; // LAN is always low ping

        // Update join button
        if (joinButton != null)
        {
            joinButton.interactable = !roomData.IsFull();
            
            TMP_Text buttonText = joinButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = roomData.IsFull() ? "FULL" : "JOIN";
            }
        }

        // Update background color
        if (backgroundImage != null)
        {
            backgroundImage.color = roomData.IsFull() ? fullColor : normalColor;
        }
    }

    private void OnJoinButtonClicked()
    {
        if (roomData != null && !roomData.IsFull())
        {
            onJoinCallback?.Invoke(roomData);
        }
    }

    // Optional: Add hover effects
    public void OnPointerEnter()
    {
        if (backgroundImage != null && !roomData.IsFull())
        {
            backgroundImage.color = highlightColor;
        }
    }

    public void OnPointerExit()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = roomData.IsFull() ? fullColor : normalColor;
        }
    }
}
