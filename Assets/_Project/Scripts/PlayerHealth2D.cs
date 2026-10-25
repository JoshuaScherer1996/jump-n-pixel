using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController2D))]
public sealed class PlayerHealth2D : MonoBehaviour
{
    [SerializeField, Min(1)] private int maximumHealth = 3;
    [SerializeField, Min(0.01f)] private float invulnerabilityTime = 0.75f;
    [SerializeField, Min(0.01f)] private float respawnProtectionTime = 1f;

    private PlayerController2D controller;
    private Vector3 spawnPosition;
    private double invulnerableUntil = double.NegativeInfinity;

    public int MaximumHealth => maximumHealth;
    public int CurrentHealth { get; private set; }
    public bool HasShield { get; private set; }
    public bool IsInvulnerable => Time.timeAsDouble < invulnerableUntil;
    public event Action<int, int> HealthChanged;
    public event Action<bool> ShieldChanged;
    public event Action Respawned;

    private void Awake()
    {
        controller = GetComponent<PlayerController2D>();
        spawnPosition = transform.position;
        CurrentHealth = maximumHealth;
    }

    public bool TryTakeDamage(int damage = 1, Vector2 knockback = default)
    {
        if (!isActiveAndEnabled || damage <= 0 || IsInvulnerable || CurrentHealth <= 0)
            return false;

        invulnerableUntil = Time.timeAsDouble + invulnerabilityTime;
        if (HasShield)
        {
            controller.ApplyKnockback(knockback);
            SetShield(false);
            return true;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        if (CurrentHealth > 0)
            controller.ApplyKnockback(knockback);
        HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
        if (CurrentHealth == 0)
            Respawn();

        return true;
    }

    public void GrantShield()
    {
        SetShield(true);
    }

    private void Respawn()
    {
        invulnerableUntil = Time.timeAsDouble + Mathf.Max(invulnerabilityTime, respawnProtectionTime);
        SetShield(false);
        controller.RespawnAt(spawnPosition);
        CurrentHealth = MaximumHealth;
        HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
        Respawned?.Invoke();
    }

    private void SetShield(bool active)
    {
        if (HasShield == active)
            return;

        HasShield = active;
        ShieldChanged?.Invoke(active);
    }
}
