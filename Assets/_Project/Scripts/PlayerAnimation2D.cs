using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public sealed class PlayerAnimation2D : MonoBehaviour
{
    [SerializeField] private PlayerController2D controller;

    private static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
    private static readonly int Run = Animator.StringToHash("Base Layer.Run");
    private static readonly int Jump = Animator.StringToHash("Base Layer.Jump");
    private static readonly int Fall = Animator.StringToHash("Base Layer.Fall");
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private int currentState;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        currentState = 0;
    }

    private void Update()
    {
        var velocity = controller.Velocity;
        if (Mathf.Abs(velocity.x) > 0.05f)
            spriteRenderer.flipX = velocity.x < 0f;

        var state = controller.IsGrounded
            ? Mathf.Abs(velocity.x) > 0.1f ? Run : Idle
            : velocity.y > 0.1f ? Jump : Fall;

        if (state == currentState)
            return;

        animator.Play(state, 0, 0f);
        currentState = state;
    }
}
