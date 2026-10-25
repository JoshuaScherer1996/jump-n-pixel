using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LevelScore : MonoBehaviour
{
    [SerializeField] private LevelRun levelRun;
    [SerializeField] private PlayerHealth2D player;

    private readonly HashSet<UnityEngine.Object> awardedSources = new HashSet<UnityEngine.Object>();
    private Collectible2D[] collectibles;
    private EnemyInteraction2D[] enemies;

    public int Score { get; private set; }
    public event Action<int> ScoreChanged;

    private void OnEnable()
    {
        collectibles = FindObjectsByType<Collectible2D>(FindObjectsInactive.Include);
        enemies = FindObjectsByType<EnemyInteraction2D>(FindObjectsInactive.Include);
        foreach (var collectible in collectibles)
            collectible.Collected += OnCollected;
        foreach (var enemy in enemies)
            enemy.Defeated += OnDefeated;
    }

    private void OnDisable()
    {
        foreach (var collectible in collectibles)
            if (collectible != null)
                collectible.Collected -= OnCollected;
        foreach (var enemy in enemies)
            if (enemy != null)
                enemy.Defeated -= OnDefeated;
    }

    private void OnCollected(Collectible2D collectible, PlayerHealth2D collector)
    {
        if (collectible.IsCollected && !collectible.GrantsShield)
            Award(collectible, collector, collectible.Points);
    }

    private void OnDefeated(EnemyInteraction2D enemy, PlayerHealth2D attacker)
    {
        if (enemy.IsDefeated)
            Award(enemy, attacker, enemy.Points);
    }

    private void Award(UnityEngine.Object source, PlayerHealth2D recipient, int points)
    {
        if (levelRun.IsExpired || recipient != player || !player.isActiveAndEnabled
            || points <= 0 || !awardedSources.Add(source))
            return;

        Score += Math.Min(points, int.MaxValue - Score);
        ScoreChanged?.Invoke(Score);
    }
}
