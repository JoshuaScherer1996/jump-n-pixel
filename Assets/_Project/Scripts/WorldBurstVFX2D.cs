using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldBurstVFX2D : MonoBehaviour
{
    [SerializeField] private ParticleSystem burst;

    private Collectible2D collectible;
    private EnemyInteraction2D enemy;

    private void Awake()
    {
        // These sources deactivate before raising their events. Keep the handler
        // for the lifetime of the source, including that final deactivation.
        if (TryGetComponent(out collectible))
            collectible.Collected += OnCollected;
        else if (TryGetComponent(out enemy))
            enemy.Defeated += OnDefeated;
    }

    private void OnDestroy()
    {
        if (collectible != null)
            collectible.Collected -= OnCollected;
        if (enemy != null)
            enemy.Defeated -= OnDefeated;
    }

    private void OnCollected(Collectible2D item, PlayerHealth2D player)
    {
        if (!enabled)
            return;

        Color? tint = item.GrantsShield && player.TryGetComponent<PlayerFeedback2D>(out var feedback)
            ? feedback.ShieldTint
            : null;
        GameplayVFX2D.Spawn(burst, item.transform.position, item.gameObject.scene, tint);
    }

    private void OnDefeated(EnemyInteraction2D defeated, PlayerHealth2D player)
    {
        if (enabled)
            GameplayVFX2D.Spawn(burst, defeated.transform.position, defeated.gameObject.scene);
    }
}
