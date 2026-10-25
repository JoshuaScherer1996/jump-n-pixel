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
    [SerializeField, Min(0.1f)] private float jumpSpeed = 13f;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField, Min(0.001f)] private float groundCheckDistance = 0.06f;

    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private Rigidbody2D body;
    private CapsuleCollider2D bodyCollider;
    private ContactFilter2D groundFilter;
    private InputActionAsset runtimeActions;
    private InputAction moveAction;
    private InputAction jumpAction;
    private float horizontalInput;
    private bool jumpRequested;

    public Vector2 Velocity => body.linearVelocity;
    public bool IsGrounded { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<CapsuleCollider2D>();
        groundFilter = new ContactFilter2D { useTriggers = false };
        groundFilter.SetLayerMask(groundLayers);
        runtimeActions = Instantiate(inputActions);
        moveAction = runtimeActions.FindAction("Player/Move", true);
        jumpAction = runtimeActions.FindAction("Player/Jump", true);
    }

    private void OnEnable()
    {
        horizontalInput = 0f;
        jumpRequested = false;
        jumpAction.performed += RequestJump;
        moveAction.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        jumpAction.performed -= RequestJump;
        moveAction.Disable();
        jumpAction.Disable();
        horizontalInput = 0f;
        jumpRequested = false;
        IsGrounded = false;
        body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }

    private void OnDestroy()
    {
        if (runtimeActions != null)
            Destroy(runtimeActions);
    }

    private void Update()
    {
        horizontalInput = Mathf.Clamp(moveAction.ReadValue<Vector2>().x, -1f, 1f);
    }

    private void FixedUpdate()
    {
        IsGrounded = CheckGrounded();
        var velocity = body.linearVelocity;
        var targetSpeed = horizontalInput * movementSpeed;
        var speedChange = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, speedChange * Time.fixedDeltaTime);

        if (jumpRequested && IsGrounded)
        {
            velocity.y = jumpSpeed;
            IsGrounded = false;
        }

        jumpRequested = false;
        body.linearVelocity = velocity;
    }

    private void RequestJump(InputAction.CallbackContext context)
    {
        jumpRequested = true;
    }

    private bool CheckGrounded()
    {
        if (body.linearVelocity.y > 0.1f)
            return false;

        var count = bodyCollider.Cast(Vector2.down, groundFilter, groundHits, groundCheckDistance);
        var footHeight = bodyCollider.bounds.min.y + groundCheckDistance;

        for (var i = 0; i < count; i++)
        {
            var hit = groundHits[i];
            if (hit.normal.y >= 0.7f && hit.point.y <= footHeight)
                return true;
        }

        return false;
    }
}
