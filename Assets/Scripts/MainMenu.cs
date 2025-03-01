using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private Button      continueButton;
    [SerializeField]
    private Button      startButton;
    [SerializeField]
    private Button      quitButton;

    void Start()
    {
        continueButton.gameObject.SetActive(PlayerPrefs.HasKey("CurrentLevel"));

#if UNITY_WEBGL
        quitButton.gameObject.SetActive(false);
#endif
    }

    public void Continue()
    {
        GameManager.Instance.level = PlayerPrefs.GetInt("CurrentLevel");
        StartGame();
    }

    public void Restart()
    {
        GameManager.Instance.level = 0;
        StartGame();
    }

    void StartGame()
    {
        FullscreenFader.FadeOut(0.5f, Color.black, () =>
        {
            SceneManager.LoadScene("GameScene");
        });
    }

    public void Quit()
    {
        FullscreenFader.FadeOut(0.5f, Color.black, () =>
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif            
        });
    }

    void ResetPlayerData()
    {
        PlayerPrefs.DeleteAll();
    }
}
