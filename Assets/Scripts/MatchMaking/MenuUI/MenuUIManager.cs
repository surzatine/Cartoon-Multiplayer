using UnityEngine;

public class MenuUIManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject hostMenu;
    [SerializeField] private GameObject roomMenu;

    private void DisableMenu()
    {
        mainMenu.SetActive(false);
        hostMenu.SetActive(false);
        roomMenu.SetActive(false);
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
}
