using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public sealed class PlayerController2D : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField, Min(0.1f)] private float movementSpeed = 6f;
    [SerializeField, Min(0.1f)] private float acceleration = 70f;
    [SerializeField, Min(0.1f)] private float deceleration = 100f;
    [SerializeField, Min(0.1f)] private float jumpSpeed = 13.634515f;
    [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField, Min(0.001f)] private float groundCheckDistance = 0.06f;
    [SerializeField, Min(0f)] private float knockbackDuration = 0.15f;

    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private Rigidbody2D body;
    private CapsuleCollider2D bodyCollider;
    private MovingPlatform2D supportingPlatform;
    private ContactFilter2D groundFilter;
    private InputActionAsset runtimeActions;
    private InputAction moveAction;
    private InputAction jumpAction;
    private float horizontalInput;
    private bool jumpRequested;
    private bool jumpAvailable;
    private bool gameplayPaused;
    private double coyoteUntil = double.NegativeInfinity;
    private double knockbackUntil = double.NegativeInfinity;

    public Vector2 Velocity => body.linearVelocity;
    public InputAction JumpAction => jumpAction;
    public Vector2 PrePhysicsVelocity { get; private set; }
    public float JumpSpeed => jumpSpeed;
    public bool IsGrounded { get; private set; }
    public Vector2 PreviousFootPosition { get; private set; }
    public Vector2 FootPosition => new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.min.y);

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<CapsuleCollider2D>();
        PreviousFootPosition = FootPosition;
        groundFilter = new ContactFilter2D { useTriggers = false };
        groundFilter.SetLayerMask(groundLayers);
        runtimeActions = Instantiate(inputActions);
        moveAction = runtimeActions.FindAction("Player/Move", true);
        jumpAction = runtimeActions.FindAction("Player/Jump", true);
    }

    private void OnEnable()
    {
        knockbackUntil = double.NegativeInfinity;
        horizontalInput = 0f;
        jumpRequested = false;
        jumpAvailable = false;
        coyoteUntil = double.NegativeInfinity;
        jumpAction.performed += RequestJump;
        moveAction.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        knockbackUntil = double.NegativeInfinity;
        jumpAction.performed -= RequestJump;
        moveAction.Disable();
        jumpAction.Disable();
        horizontalInput = 0f;
        jumpRequested = false;
        IsGrounded = false;
        jumpAvailable = false;
        coyoteUntil = double.NegativeInfinity;
        body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }

    private void OnDestroy()
    {
        if (runtimeActions != null)
            Destroy(runtimeActions);
    }

    private void Update()
    {
        if (gameplayPaused)
            return;

        horizontalInput = Mathf.Clamp(moveAction.ReadValue<Vector2>().x, -1f, 1f);
    }

    private void FixedUpdate()
    {
        if (gameplayPaused)
            return;

        PreviousFootPosition = FootPosition;
        var wasGrounded = IsGrounded;
        IsGrounded = CheckGrounded();
        if (IsGrounded)
        {
            jumpAvailable = true;
        }
        else if (wasGrounded && jumpAvailable)
        {
            coyoteUntil = Time.fixedTimeAsDouble + coyoteTime;
        }
        var velocity = body.linearVelocity;
        var isKnockedBack = Time.fixedTimeAsDouble < knockbackUntil;
        if (IsGrounded && supportingPlatform != null && !isKnockedBack)
            velocity.y = supportingPlatform.Velocity.y;
        if (!isKnockedBack)
        {
            var targetSpeed = horizontalInput * movementSpeed;
            var speedChange = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, speedChange * Time.fixedDeltaTime);
        }

        if (!isKnockedBack && jumpRequested && jumpAvailable && (IsGrounded || Time.fixedTimeAsDouble < coyoteUntil))
        {
            velocity.y = jumpSpeed;
            IsGrounded = false;
            jumpAvailable = false;
            coyoteUntil = double.NegativeInfinity;
        }

        jumpRequested = false;
        body.linearVelocity = velocity;
        PrePhysicsVelocity = velocity;
    }

    public void Bounce()
    {
        knockbackUntil = double.NegativeInfinity;
        body.linearVelocity = new Vector2(PrePhysicsVelocity.x, jumpSpeed);
        ResetJumpState();
    }

    public void ApplyKnockback(Vector2 velocity)
    {
        if (velocity == Vector2.zero)
            return;

        body.linearVelocity = velocity;
        knockbackUntil = Time.fixedTimeAsDouble + knockbackDuration;
        ResetJumpState();
    }

    public void RespawnAt(Vector3 position)
    {
        knockbackUntil = double.NegativeInfinity;
        PrePhysicsVelocity = Vector2.zero;
        transform.position = position;
        body.position = position;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        horizontalInput = 0f;
        ResetJumpState();
        Physics2D.SyncTransforms();
        PreviousFootPosition = FootPosition;
    }

    private void ResetJumpState()
    {
        jumpRequested = false;
        IsGrounded = false;
        jumpAvailable = false;
        coyoteUntil = double.NegativeInfinity;
        supportingPlatform = null;
    }

    public void SetGameplayPaused(bool paused)
    {
        gameplayPaused = paused;
        horizontalInput = 0f;
        ResetJumpState();
    }

    private void RequestJump(InputAction.CallbackContext context)
    {
        if (!gameplayPaused)
            jumpRequested = true;
    }

    private bool CheckGrounded()
    {
        var previousPlatform = supportingPlatform;
        supportingPlatform = null;
        var count = bodyCollider.Cast(Vector2.down, groundFilter, groundHits, groundCheckDistance);
        var footHeight = bodyCollider.bounds.min.y + groundCheckDistance;

        for (var i = 0; i < count; i++)
        {
            var hit = groundHits[i];
            if (hit.normal.y < 0.7f || hit.point.y > footHeight)
                continue;

            var platform = hit.rigidbody != null ? hit.rigidbody.GetComponent<MovingPlatform2D>() : null;
            var supportVelocity = platform != null ? platform.Velocity.y : 0f;
            if (body.linearVelocity.y - supportVelocity > 0.1f
                && !(IsGrounded && platform != null && platform == previousPlatform))
                continue;

            supportingPlatform = platform;
            return true;
        }

        return false;
    }
}
