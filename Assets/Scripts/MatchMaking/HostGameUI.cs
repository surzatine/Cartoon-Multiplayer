using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HostGameUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LANNetworkManager networkManager;

    [Header("UI Input Fields")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField hostNameInput;
    [SerializeField] private TMP_Dropdown maxPlayersDropdown;
    [SerializeField] private TMP_Dropdown mapDropdown;
    [SerializeField] private TMP_Dropdown gameModeDropdown;

    [Header("UI Buttons")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button cancelButton;

    [Header("UI Info")]
    [SerializeField] private TMP_Text ipAddressText;
    [SerializeField] private TMP_Text statusText;

    [Header("Default Values")]
    [SerializeField] private string defaultRoomName = "My Room";
    [SerializeField] private string defaultHostName = "Player";
    [SerializeField] private string[] availableMaps = { "Desert", "Urban", "Forest", "Snow" };
    [SerializeField] private string[] availableGameModes = { "Deathmatch", "Team Deathmatch", "Capture The Flag", "Custom" };
    [SerializeField] private int[] playerCountOptions = { 2, 4, 6, 8, 10, 12 };

    private void Start()
    {
        SetupUI();
        
        // Setup buttons
        if (hostButton != null)
            hostButton.onClick.AddListener(OnHostButtonClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelButtonClicked);

        // Show local IP
        UpdateIPAddress();
    }

    private void SetupUI()
    {
        // Setup room name
        if (roomNameInput != null)
            roomNameInput.text = defaultRoomName;

        // Setup host name (could load from PlayerPrefs)
        if (hostNameInput != null)
        {
            string savedName = PlayerPrefs.GetString("PlayerName", defaultHostName);
            hostNameInput.text = savedName;
        }

        // Setup max players dropdown
        if (maxPlayersDropdown != null)
        {
            maxPlayersDropdown.ClearOptions();
            foreach (int count in playerCountOptions)
            {
                maxPlayersDropdown.options.Add(new TMP_Dropdown.OptionData(count.ToString()));
            }
            maxPlayersDropdown.value = 3; // Default to 8 players
            maxPlayersDropdown.RefreshShownValue();
        }

        // Setup map dropdown
        if (mapDropdown != null)
        {
            mapDropdown.ClearOptions();
            foreach (string map in availableMaps)
            {
                mapDropdown.options.Add(new TMP_Dropdown.OptionData(map));
            }
            mapDropdown.value = 0;
            mapDropdown.RefreshShownValue();
        }

        // Setup game mode dropdown
        if (gameModeDropdown != null)
        {
            gameModeDropdown.ClearOptions();
            foreach (string mode in availableGameModes)
            {
                gameModeDropdown.options.Add(new TMP_Dropdown.OptionData(mode));
            }
            gameModeDropdown.value = 0;
            gameModeDropdown.RefreshShownValue();
        }
    }

    private void OnHostButtonClicked()
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(roomNameInput.text))
        {
            UpdateStatus("Please enter a room name!", Color.red);
            return;
        }

        if (string.IsNullOrWhiteSpace(hostNameInput.text))
        {
            UpdateStatus("Please enter your name!", Color.red);
            return;
        }

        // Save player name
        PlayerPrefs.SetString("PlayerName", hostNameInput.text);

        // Get selected values
        string roomName = roomNameInput.text;
        string hostName = hostNameInput.text;
        int maxPlayers = playerCountOptions[maxPlayersDropdown.value];
        string mapName = availableMaps[mapDropdown.value];
        string gameMode = availableGameModes[gameModeDropdown.value];

        // Host the game
        if (networkManager != null)
        {
            networkManager.HostGame(roomName, hostName, maxPlayers, mapName, gameMode);
            UpdateStatus($"Hosting: {roomName}", Color.green);
            
            // Optionally: Switch to lobby/game scene
            // UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
        else
        {
            UpdateStatus("Network Manager not found!", Color.red);
        }
    }

    private void OnCancelButtonClicked()
    {
        // Return to main menu or close panel
        gameObject.SetActive(false);
    }

    private void UpdateIPAddress()
    {
        if (ipAddressText != null)
        {
            string ip = LANDiscovery.GetLocalIPAddress();
            ipAddressText.text = $"Your IP: {ip}";
        }
    }

    private void UpdateStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
    }

    // Public method to show/hide this panel
    public void Show()
    {
        gameObject.SetActive(true);
        UpdateIPAddress();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
