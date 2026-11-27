using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameOverController : MonoBehaviour
{
    private enum MenuState { WaitingForDeath, Revealing, Ready, Transitioning }

    [SerializeField] private PlayerHealth2D playerHealth;
    [SerializeField] private PauseMenuController pauseMenu;
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private InputSystemUIInputModule inputModule;
    [SerializeField] private GameObject gameOverCanvas;
    [SerializeField] private Image redOverlay;
    [SerializeField] private Color overlayColor = new Color32(153, 0, 0, 255);
    [SerializeField, Range(0f, 1f)] private float overlayOpacity = 0.7f;
    [SerializeField, Min(0f)] private float wipeDuration = 2f;
    [SerializeField] private GameObject title;
    [SerializeField] private MenuButton retryButton;
    [SerializeField] private MenuButton mainMenuButton;
    [SerializeField, Min(0f)] private float revealInterval = 0.5f;
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField, Min(0f)] private float fadeDuration = 2f;
    [SerializeField] private string gameplayScenePath = "Assets/_Project/Scenes/Phase1.unity";
    [SerializeField] private string mainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";

    private MenuState state;
    private ParticleSystem deathBurst;
    private GraphicRaycaster menuRaycaster;
    private InputActionReference submit;
    private InputActionReference click;
    private bool deselectOnBackgroundClick;

    private void Awake()
    {
        menuRaycaster = gameOverCanvas.GetComponent<GraphicRaycaster>();
        submit = inputModule.submit;
        click = inputModule.leftClick;
        deselectOnBackgroundClick = inputModule.deselectOnBackgroundClick;
        retryButton.onClick.AddListener(Retry);
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        gameOverCanvas.SetActive(false);
    }

    private void OnEnable()
    {
        playerHealth.Died += OnDeath;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.Died -= OnDeath;
    }

    private void OnDestroy()
    {
        if (deathBurst != null)
            Destroy(deathBurst.gameObject);

        if (state != MenuState.WaitingForDeath && inputModule != null)
            LockMenuInput();
    }

    private void OnDeath(Vector3 position)
    {
        if (state != MenuState.WaitingForDeath)
            return;

        state = MenuState.Revealing;
        pauseMenu.BeginGameOver();
        LockMenuInput();
        playerHealth.GetComponent<PlayerFeedback2D>().HidePlayer();
        if (playerHealth.TryGetComponent<GameplayVFX2D>(out var vfx))
            deathBurst = vfx.PlayDeath(position);
        else
            Debug.LogError("The player has no GameplayVFX2D for the death burst.", this);

        StartCoroutine(Reveal());
    }

    private IEnumerator Reveal()
    {
        while (deathBurst != null && deathBurst.IsAlive(true))
            yield return null;
        if (deathBurst != null)
            Destroy(deathBurst.gameObject);
        deathBurst = null;

        title.SetActive(false);
        retryButton.gameObject.SetActive(false);
        mainMenuButton.gameObject.SetActive(false);
        retryButton.interactable = false;
        mainMenuButton.interactable = false;
        fadeOverlay.alpha = 0f;
        fadeOverlay.blocksRaycasts = false;

        var color = overlayColor;
        color.a = overlayOpacity;
        redOverlay.color = color;
        SetWipeProgress(0f);
        gameOverCanvas.SetActive(true);

        var startedAt = Time.unscaledTimeAsDouble;
        while (Time.unscaledTimeAsDouble - startedAt < wipeDuration)
        {
            SetWipeProgress((float)((Time.unscaledTimeAsDouble - startedAt) / wipeDuration));
            yield return null;
        }

        SetWipeProgress(1f);
        title.SetActive(true);
        yield return new WaitForSecondsRealtime(revealInterval);
        retryButton.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(revealInterval);
        mainMenuButton.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(revealInterval);
        UnlockMenuInput();
    }

    private void SetWipeProgress(float progress)
    {
        redOverlay.rectTransform.anchorMin = new Vector2(0f, 1f - progress);
    }

    private void UnlockMenuInput()
    {
        state = MenuState.Ready;
        retryButton.interactable = true;
        mainMenuButton.interactable = true;
        menuRaycaster.enabled = true;
        inputModule.deselectOnBackgroundClick = false;
        inputModule.submit = CanArm(submit) ? submit : null;
        inputModule.leftClick = CanArm(click) ? click : null;
        inputModule.enabled = true;
        eventSystem.SetSelectedGameObject(retryButton.gameObject);
        StartCoroutine(ArmConfirmations());
    }

    private IEnumerator ArmConfirmations()
    {
        // A held confirmation must be released before this action can be used.
        // Keep navigation and the other confirmation action available in the meantime.
        while (state == MenuState.Ready && (inputModule.submit == null || inputModule.leftClick == null))
        {
            yield return null;
            if (state != MenuState.Ready)
                yield break;
            if (inputModule.submit == null && CanArm(submit))
                inputModule.submit = submit;
            if (inputModule.leftClick == null && CanArm(click))
                inputModule.leftClick = click;
        }
    }

    private static bool CanArm(InputActionReference reference)
    {
        var action = reference.action;
        if (action.WasPerformedThisFrame())
            return false;

        foreach (var control in action.controls)
            if (control is ButtonControl button && (button.isPressed || button.wasPressedThisFrame))
                return false;

        return true;
    }

    private void LockMenuInput()
    {
        inputModule.enabled = false;
        if (menuRaycaster != null)
            menuRaycaster.enabled = false;
        inputModule.submit = submit;
        inputModule.leftClick = click;
        inputModule.deselectOnBackgroundClick = deselectOnBackgroundClick;
        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(null);
    }

    private void Retry() => StartTransition(gameplayScenePath);
    private void ReturnToMainMenu() => StartTransition(mainMenuScenePath);

    private void StartTransition(string scenePath)
    {
        if (state != MenuState.Ready)
            return;

        if (!Application.CanStreamedLevelBeLoaded(scenePath))
        {
            Debug.LogError($"The Game Over destination is missing from the build: {scenePath}", this);
            return;
        }

        state = MenuState.Transitioning;
        LockMenuInput();
        fadeOverlay.blocksRaycasts = true;
        StartCoroutine(FadeAndLoad(scenePath));
    }

    private IEnumerator FadeAndLoad(string scenePath)
    {
        yield return MenuFade.ToBlack(fadeOverlay, fadeDuration);

        AsyncOperation load = null;
        string error = null;
        try
        {
            load = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
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
        Debug.LogError($"Could not load the Game Over destination: {scenePath}. {error}", this);
        fadeOverlay.alpha = 0f;
        fadeOverlay.blocksRaycasts = false;
        UnlockMenuInput();
    }
}
