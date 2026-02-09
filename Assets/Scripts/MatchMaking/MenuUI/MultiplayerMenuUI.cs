using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MultiplayerMenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LANNetworkManager networkManager;
    [SerializeField] private GameObject hostGamePanel;
    [SerializeField] private GameObject roomBrowserPanel;
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button hostGameButton;
    [SerializeField] private Button joinGameButton;
    [SerializeField] private Button quickJoinButton;
    [SerializeField] private Button backButton;

    [Header("Status")]
    [SerializeField] private TMP_Text connectionStatusText;
    [SerializeField] private GameObject connectedIndicator;

    private void Start()
    {
        // Setup buttons
        if (hostGameButton != null)
            hostGameButton.onClick.AddListener(OnHostGameClicked);

        if (joinGameButton != null)
            joinGameButton.onClick.AddListener(OnJoinGameClicked);

        if (quickJoinButton != null)
            quickJoinButton.onClick.AddListener(OnQuickJoinClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        // Show main menu by default
        ShowMainMenu();
    }

    private void Update()
    {
        UpdateConnectionStatus();
    }

    #region Button Handlers

    private void OnHostGameClicked()
    {
        ShowHostPanel();
    }

    private void OnJoinGameClicked()
    {
        ShowBrowserPanel();
    }

    private void OnQuickJoinClicked()
    {
        // Quick join implementation
        var roomBrowser = roomBrowserPanel?.GetComponent<RoomBrowserUI>();
        if (roomBrowser != null)
        {
            ShowBrowserPanel();
            roomBrowser.QuickJoin();
        }
    }

    private void OnBackClicked()
    {
        // Return to main game menu
         SceneManager.LoadScene(SceneConstant.MENUSCENE);
        //Application.Quit();
    }

    #endregion

    #region Panel Management

    private void ShowMainMenu()
    {
        SetPanelActive(mainMenuPanel, true);
        SetPanelActive(hostGamePanel, false);
        SetPanelActive(roomBrowserPanel, false);
    }

    private void ShowHostPanel()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(hostGamePanel, true);
        SetPanelActive(roomBrowserPanel, false);

        var hostUI = hostGamePanel?.GetComponent<HostGameUI>();
        if (hostUI != null)
            hostUI.Show();
    }

    private void ShowBrowserPanel()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(hostGamePanel, false);
        SetPanelActive(roomBrowserPanel, true);

        var browserUI = roomBrowserPanel?.GetComponent<RoomBrowserUI>();
        if (browserUI != null)
            browserUI.StartBrowsing();
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    #endregion

    #region Status Updates

    private void UpdateConnectionStatus()
    {
        if (networkManager == null) return;

        bool isConnected = networkManager.IsHost || networkManager.CurrentRoom != null;

        if (connectedIndicator != null)
            connectedIndicator.SetActive(isConnected);

        if (connectionStatusText != null)
        {
            if (networkManager.IsHost)
            {
                connectionStatusText.text = $"Hosting: {networkManager.CurrentRoom?.roomName} ({networkManager.GetPlayerCount()} players)";
                connectionStatusText.color = Color.green;
            }
            else if (networkManager.CurrentRoom != null)
            {
                connectionStatusText.text = $"Connected to: {networkManager.CurrentRoom.roomName}";
                connectionStatusText.color = Color.cyan;
            }
            else
            {
                connectionStatusText.text = "Not Connected";
                connectionStatusText.color = Color.gray;
            }
        }
    }

    #endregion

    #region Public Methods

    public void Disconnect()
    {
        if (networkManager != null)
        {
            networkManager.Disconnect();
            ShowMainMenu();
        }
    }

    #endregion
}
