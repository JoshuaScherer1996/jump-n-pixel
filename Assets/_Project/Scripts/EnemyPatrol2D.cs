using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(EnemyInteraction2D))]
public sealed class EnemyPatrol2D : MonoBehaviour
{
    [SerializeField] private float leftBoundary;
    [SerializeField] private float rightBoundary;
    [SerializeField, Min(0.01f)] private float speed = 0.65f;
    [SerializeField] private bool startMovingRight = true;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField, Min(0.001f)] private float edgeProbeDistance = 0.2f;

    private readonly RaycastHit2D[] hits = new RaycastHit2D[8];
    private Rigidbody2D body;
    private BoxCollider2D bodyCollider;
    private EnemyInteraction2D interaction;
    private SpriteRenderer spriteRenderer;
    private ContactFilter2D obstacleFilter;
    private bool movingRight;
    private float fixedY;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        interaction = GetComponent<EnemyInteraction2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        fixedY = body.position.y;
        obstacleFilter = new ContactFilter2D { useTriggers = false };
        obstacleFilter.SetLayerMask(obstacleLayers);
    }

    private void OnEnable()
    {
        movingRight = startMovingRight;
    }

    private void FixedUpdate()
    {
        if (interaction.IsDefeated)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        var bounds = bodyCollider.bounds;
        var minX = leftBoundary + bounds.extents.x - (bounds.center.x - body.position.x);
        var maxX = rightBoundary - bounds.extents.x - (bounds.center.x - body.position.x);
        var direction = movingRight ? Vector2.right : Vector2.left;
        var distance = speed * Time.fixedDeltaTime;
        var nextX = Mathf.MoveTowards(body.position.x, movingRight ? maxX : minX, distance);
        var hitCount = bodyCollider.Cast(direction, obstacleFilter, hits, distance + 0.02f);
        var blocked = false;
        for (var i = 0; i < hitCount; i++)
            if (Vector2.Dot(hits[i].normal, direction) < -0.5f)
                blocked = true;

        var probe = new Vector2(bounds.center.x + direction.x * (bounds.extents.x + distance + 0.02f), bounds.min.y + 0.05f);
        var hasGround = Physics2D.Raycast(probe, Vector2.down, obstacleFilter, hits, edgeProbeDistance + 0.05f) > 0;
        if (blocked || !hasGround)
        {
            movingRight = !movingRight;
            body.linearVelocity = Vector2.zero;
        }
        else
        {
            body.MovePosition(new Vector2(nextX, fixedY));
            if (Mathf.Approximately(nextX, movingRight ? maxX : minX))
                movingRight = !movingRight;
        }

        spriteRenderer.flipX = !movingRight;
    }

    private void OnDisable()
    {
        if (body != null)
            body.linearVelocity = Vector2.zero;
    }

    private void OnValidate()
    {
        rightBoundary = Mathf.Max(leftBoundary, rightBoundary);
    }
}
