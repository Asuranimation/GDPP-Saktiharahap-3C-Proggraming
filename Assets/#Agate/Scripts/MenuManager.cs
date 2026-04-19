using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button exitButton;
    
    void Start()
    {
        playButton.onClick.AddListener(HandlePlayButton);
        exitButton.onClick.AddListener(HandleExitButton);
    }

    private void HandleExitButton()
    {
        SceneController.Instance.ExitGame();
    }

    private void HandlePlayButton()
    {
        SceneController.Instance.LoadSceneByName("Game");
    }

}
