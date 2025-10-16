using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UC;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private Button          continueButton;
    [SerializeField]
    private Button          startButton;
    [SerializeField]
    private BigTextScroll   creditsScroll;
    [SerializeField]
    private Button          quitButton;

    CanvasGroup canvasGroup;

    void Start()
    {
        continueButton.gameObject.SetActive(PlayerPrefs.HasKey("CurrentLevel"));
        canvasGroup = GetComponent<CanvasGroup>();

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
        FullscreenFader.FadeOut(0.25f, Color.black, () =>
        {
            SceneManager.LoadScene("GameScene");
        });
    }

    public void DisplayCredits()
    {
        canvasGroup.FadeOut(0.25f);

        var creditsCanvas = creditsScroll.GetComponent<CanvasGroup>();
        if (creditsCanvas == null) creditsCanvas = creditsScroll.GetComponentInParent<CanvasGroup>();
        creditsCanvas.FadeIn(0.25f);

        creditsScroll.Reset();
        creditsScroll.onEndScroll += CreditsScroll_onEndScroll;
    }

    private void CreditsScroll_onEndScroll()
    {
        canvasGroup.FadeIn(0.25f);

        var creditsCanvas = creditsScroll.GetComponent<CanvasGroup>();
        if (creditsCanvas == null) creditsCanvas = creditsScroll.GetComponentInParent<CanvasGroup>();
        creditsCanvas.FadeOut(0.25f);

        creditsScroll.onEndScroll -= CreditsScroll_onEndScroll;
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
