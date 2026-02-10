using UnityEngine;

public class MenuUIManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject hostMenu;
    [SerializeField] private GameObject roomMenu;
    [SerializeField] private GameObject lobbyMenu;

    private void DisableMenu()
    {
        mainMenu.SetActive(false);
        hostMenu.SetActive(false);
        roomMenu.SetActive(false);
        lobbyMenu.SetActive(false);
    }
    
    public void ShowMainMenu()
    {
        DisableMenu();
        mainMenu.SetActive(true);
    }

    public void ShowHostMenu()
    {
        DisableMenu();
        hostMenu.SetActive(true);
    }

    public void ShowRoomMenu()
    {
        DisableMenu();
        roomMenu.SetActive(true);
    }

    public void ShowLobbyMenu()
    {
        DisableMenu();
        lobbyMenu.SetActive(true);
    }
}
