using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class MovingPlatform2D : MonoBehaviour
{
    [SerializeField] private float lowerY;
    [SerializeField] private float upperY;
    [SerializeField, Min(0.01f)] private float speed = 0.65f;
    [SerializeField] private bool startMovingUp = true;
    [SerializeField, Min(0f)] private float endpointPause = 0.35f;

    private Rigidbody2D body;
    private float fixedX;
    private bool movingUp;
    private float pauseRemaining;

    public Vector2 Velocity { get; private set; }
    public float LowerY => lowerY;
    public float UpperY => upperY;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        fixedX = body.position.x;
    }

    private void OnEnable()
    {
        movingUp = startMovingUp;
        pauseRemaining = 0f;
        Velocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        Velocity = Vector2.zero;
        if (pauseRemaining > 0f)
        {
            pauseRemaining = Mathf.Max(0f, pauseRemaining - Time.fixedDeltaTime);
            body.linearVelocity = Vector2.zero;
            return;
        }

        var targetY = movingUp ? upperY : lowerY;
        var nextY = Mathf.MoveTowards(body.position.y, targetY, speed * Time.fixedDeltaTime);
        var nextPosition = new Vector2(fixedX, nextY);
        Velocity = (nextPosition - body.position) / Time.fixedDeltaTime;
        body.MovePosition(nextPosition);
        if (Mathf.Approximately(nextY, targetY))
        {
            movingUp = !movingUp;
            pauseRemaining = endpointPause;
        }
    }

    private void OnDisable()
    {
        Velocity = Vector2.zero;
        if (body != null)
            body.linearVelocity = Vector2.zero;
    }

    private void OnValidate()
    {
        upperY = Mathf.Max(lowerY, upperY);
    }
}
