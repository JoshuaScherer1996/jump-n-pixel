using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button optionsReturnButton;
    [SerializeField] private Button creditsReturnButton;
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private InputSystemUIInputModule inputModule;
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField, Min(0f)] private float fadeDuration = 2f;
    [SerializeField] private string gameplayScenePath = "Assets/_Project/Scenes/Phase1.unity";

    private Button returnSelection;
    private bool transitioning;

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    private void Start()
    {
        fadeOverlay.alpha = 0f;
        fadeOverlay.blocksRaycasts = false;
        ShowPanel(mainMenuPanel, playButton);
    }

    public void Play()
    {
        if (transitioning || !mainMenuPanel.activeSelf)
            return;

        if (!Application.CanStreamedLevelBeLoaded(gameplayScenePath))
        {
            Debug.LogError($"The gameplay scene is missing from the build: {gameplayScenePath}", this);
            return;
        }

        transitioning = true;
        fadeOverlay.blocksRaycasts = true;
        inputModule.enabled = false;
        StartCoroutine(FadeAndLoad());
    }

    public void OpenOptions()
    {
        OpenSubmenu(optionsPanel, optionsButton, optionsReturnButton);
    }

    public void OpenCredits()
    {
        OpenSubmenu(creditsPanel, creditsButton, creditsReturnButton);
    }

    public void ReturnToMainMenu()
    {
        if (transitioning || mainMenuPanel.activeSelf)
            return;

        ShowPanel(mainMenuPanel, returnSelection);
    }

    public void Exit()
    {
        if (transitioning || !mainMenuPanel.activeSelf)
            return;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OpenSubmenu(GameObject panel, Button opener, Button firstSelection)
    {
        if (transitioning || !mainMenuPanel.activeSelf)
            return;

        returnSelection = opener;
        ShowPanel(panel, firstSelection);
    }

    private void ShowPanel(GameObject panel, Button firstSelection)
    {
        eventSystem.SetSelectedGameObject(null);
        mainMenuPanel.SetActive(panel == mainMenuPanel);
        optionsPanel.SetActive(panel == optionsPanel);
        creditsPanel.SetActive(panel == creditsPanel);
        eventSystem.SetSelectedGameObject(firstSelection.gameObject);
    }

    private IEnumerator FadeAndLoad()
    {
        yield return MenuFade.ToBlack(fadeOverlay, fadeDuration);
        yield return SceneManager.LoadSceneAsync(gameplayScenePath, LoadSceneMode.Single);
    }
}
