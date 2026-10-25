using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth2D))]
public sealed class PlayerFeedback2D : MonoBehaviour
{
    [SerializeField, Range(0.1f, 1f)] private float minimumAlpha = 0.45f;
    [SerializeField, Min(0.1f)] private float pulseFrequency = 5f;
    [SerializeField] private Color shieldTint = new Color(0.65f, 1f, 0.65f, 1f);

    private PlayerHealth2D health;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private bool wasInvulnerable;
    private float pulseStartedAt;

    private void Awake()
    {
        health = GetComponent<PlayerHealth2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[spriteRenderers.Length];
        for (var i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i].color;
    }

    private void LateUpdate()
    {
        var isInvulnerable = health.IsInvulnerable;
        if (isInvulnerable && !wasInvulnerable)
            pulseStartedAt = Time.time;

        var alpha = isInvulnerable
            ? Mathf.Lerp(minimumAlpha, 1f, (Mathf.Cos((Time.time - pulseStartedAt) * pulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f)
            : 1f;

        for (var i = 0; i < spriteRenderers.Length; i++)
        {
            var color = health.HasShield ? originalColors[i] * shieldTint : originalColors[i];
            color.a = originalColors[i].a * alpha;
            spriteRenderers[i].color = color;
        }

        wasInvulnerable = isInvulnerable;
    }

    private void OnDisable()
    {
        if (spriteRenderers == null)
            return;

        for (var i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = originalColors[i];
        }

        wasInvulnerable = false;
    }
}
