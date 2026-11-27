using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    private enum MenuState { Playing, Paused, Options, ReturningToMainMenu, GameOver }

    [SerializeField] private PlayerController2D player;
    [SerializeField] private CameraShake2D cameraShake;
    [SerializeField] private InputActionReference pauseAction;
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private InputSystemUIInputModule inputModule;
    [SerializeField] private GameObject pauseMenuCanvas;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button optionsBackButton;
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField, Min(0f)] private float fadeDuration = 2f;
    [SerializeField] private string mainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";

    private MenuState state;
    private float previousTimeScale;
    private int lastMenuActionFrame = -1;

    private void OnEnable()
    {
        pauseAction.action.performed += OnPause;
        pauseAction.action.Enable();
    }

    private void OnDisable()
    {
        pauseAction.action.performed -= OnPause;
        pauseAction.action.Disable();
        StopAllCoroutines();
        if (state != MenuState.Playing && state != MenuState.GameOver)
            CloseMenu();
    }

    private void OnDestroy()
    {
        if (state == MenuState.GameOver)
            Time.timeScale = previousTimeScale;
    }

    internal void BeginGameOver()
    {
        previousTimeScale = Time.timeScale;
        state = MenuState.GameOver;
        Time.timeScale = 0f;
        player.SetGameplayPaused(true);
        player.SetMenuInputSuspended(true);
        cameraShake.SetMenuPaused(true);
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        if (state == MenuState.Playing)
        {
            if (!AcceptMenuAction(MenuState.Playing) || Time.timeScale <= 0f)
                return;

            previousTimeScale = Time.timeScale;
            player.SetMenuInputSuspended(true);
            cameraShake.SetMenuPaused(true);
            Time.timeScale = 0f;
            state = MenuState.Paused;
            pauseMenuCanvas.SetActive(true);
            inputModule.enabled = true;
            ShowPanel(false, resumeButton);
        }
        else
        {
            BackOrResume();
        }
    }

    public void Resume()
    {
        if (AcceptMenuAction(MenuState.Paused))
            CloseMenu();
    }

    public void OpenOptions()
    {
        if (!AcceptMenuAction(MenuState.Paused))
            return;

        state = MenuState.Options;
        ShowPanel(true, optionsBackButton);
    }

    public void BackOrResume()
    {
        if (state == MenuState.Paused)
        {
            Resume();
        }
        else if (AcceptMenuAction(MenuState.Options))
        {
            state = MenuState.Paused;
            ShowPanel(false, optionsButton);
        }
    }

    public void BackToMainMenu()
    {
        if (!AcceptMenuAction(MenuState.Paused))
            return;

        if (!Application.CanStreamedLevelBeLoaded(mainMenuScenePath))
        {
            Debug.LogError($"The main menu scene is missing from the build: {mainMenuScenePath}", this);
            return;
        }

        state = MenuState.ReturningToMainMenu;
        inputModule.enabled = false;
        fadeOverlay.blocksRaycasts = true;
        StartCoroutine(FadeAndLoad());
    }

    private bool AcceptMenuAction(MenuState expectedState)
    {
        if (state != expectedState || lastMenuActionFrame == Time.frameCount)
            return false;

        lastMenuActionFrame = Time.frameCount;
        return true;
    }

    private void ShowPanel(bool showOptions, Button selection)
    {
        eventSystem.SetSelectedGameObject(null);
        pauseMenuPanel.SetActive(!showOptions);
        optionsPanel.SetActive(showOptions);
        eventSystem.SetSelectedGameObject(selection.gameObject);
    }

    private void CloseMenu()
    {
        state = MenuState.Playing;
        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(null);
        if (inputModule != null)
            inputModule.enabled = false;
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
        }
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);
        if (player != null)
            player.SetMenuInputSuspended(false);
        if (cameraShake != null)
            cameraShake.SetMenuPaused(false);
        Time.timeScale = previousTimeScale;
    }

    private IEnumerator FadeAndLoad()
    {
        yield return MenuFade.ToBlack(fadeOverlay, fadeDuration);

        AsyncOperation load = null;
        string error = null;
        try
        {
            load = SceneManager.LoadSceneAsync(mainMenuScenePath, LoadSceneMode.Single);
        }
        catch (System.Exception exception)
        {
            error = exception.Message;
        }

        if (load != null)
        {
            yield return load;
            yield return null;
        }

        // A successful single-scene load destroys this controller.
        Debug.LogError($"Could not load the main menu scene: {mainMenuScenePath}. {error}", this);
        state = MenuState.Paused;
        fadeOverlay.alpha = 0f;
        fadeOverlay.blocksRaycasts = false;
        inputModule.enabled = true;
        ShowPanel(false, resumeButton);
    }
}
