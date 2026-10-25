using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class LevelRun : MonoBehaviour
{
    [SerializeField, Min(1f)] private float timeLimit = 180f;
    [SerializeField] private PlayerController2D playerController;
    [SerializeField] private PlayerHealth2D playerHealth;

    private Rigidbody2D playerBody;
    private Animator playerAnimator;
    private bool restartArmed;
    private bool isRestarting;
    private int expiredFrame;
    private bool wasHealthEnabled;
    private bool wasBodySimulated;
    private float previousAnimatorSpeed;
    private int displayedSeconds;

    public float RemainingSeconds { get; private set; }
    public int DisplayedSeconds => Mathf.CeilToInt(RemainingSeconds);
    public bool IsExpired { get; private set; }
    public event Action<int> TimeChanged;
    public event Action Expired;

    private void Awake()
    {
        RemainingSeconds = timeLimit;
        displayedSeconds = DisplayedSeconds;
        playerBody = playerController.GetComponent<Rigidbody2D>();
        playerAnimator = playerController.GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        playerController.JumpAction.performed += OnJump;
    }

    private void OnDisable()
    {
        if (playerController != null)
            playerController.JumpAction.performed -= OnJump;
        if (!IsExpired)
            return;

        if (playerController != null)
            playerController.SetGameplayPaused(false);
        if (playerHealth != null)
            playerHealth.enabled = wasHealthEnabled;
        if (playerBody != null)
            playerBody.simulated = wasBodySimulated;
        if (playerAnimator != null)
            playerAnimator.speed = previousAnimatorSpeed;
    }

    private void Update()
    {
        if (IsExpired)
        {
            if (!IsJumpHeld())
                restartArmed = true;
            return;
        }

        RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
        if (displayedSeconds != DisplayedSeconds)
        {
            displayedSeconds = DisplayedSeconds;
            TimeChanged?.Invoke(displayedSeconds);
        }

        if (RemainingSeconds <= 0f)
            Expire();
    }

    private void Expire()
    {
        IsExpired = true;
        expiredFrame = Time.frameCount;
        restartArmed = !IsJumpHeld();
        wasHealthEnabled = playerHealth.enabled;
        wasBodySimulated = playerBody.simulated;
        playerController.SetGameplayPaused(true);
        playerHealth.enabled = false;
        playerBody.linearVelocity = Vector2.zero;
        playerBody.angularVelocity = 0f;
        playerBody.simulated = false;
        if (playerAnimator != null)
        {
            previousAnimatorSpeed = playerAnimator.speed;
            playerAnimator.speed = 0f;
        }
        Expired?.Invoke();
    }

    private bool IsJumpHeld()
    {
        foreach (var control in playerController.JumpAction.controls)
            if (control is ButtonControl button && button.isPressed)
                return true;
        return false;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (!IsExpired || !restartArmed || isRestarting || Time.frameCount <= expiredFrame)
            return;

        isRestarting = true;
        SceneManager.LoadScene(gameObject.scene.buildIndex);
    }
}
