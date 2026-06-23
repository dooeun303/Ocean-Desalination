using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManagerr : MonoBehaviour
{
    public Button startButton;

    private void Awake()
    {
        startButton.onClick.AddListener(OnStartButton);
    }

    public void OnStartButton()
    {
        SceneManager.LoadScene("GameScene");
    }



}
