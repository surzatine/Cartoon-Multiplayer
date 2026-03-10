using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class CharacterSelection : MonoBehaviour
{
    [SerializeField] private TMP_InputField _usernameInput;
    [SerializeField] private TMP_InputField _characterInput;
    [SerializeField] private Button _startButton;



    //Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _startButton.onClick.AddListener(OnClickStart);
    }

    private void OnClickStart()
    {
        int value;

        if (int.TryParse(_characterInput.text, out value))
        {
            Debug.Log("Value: " + value);
        }
        else
        {
            Debug.Log("Invalid number");
        }
        PlayerStatics.CharacterId = value;
        PlayerStatics.PlayerName = _usernameInput.text;

        SceneManager.LoadScene(1);
    }

}
