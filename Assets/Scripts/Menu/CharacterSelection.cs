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
        PlayerStatics.CharacterId = _characterInput.text;
        PlayerStatics.PlayerName = _usernameInput.text;

        SceneManager.LoadScene(1);
    }

}
