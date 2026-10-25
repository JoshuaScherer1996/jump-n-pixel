using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class Collectible2D : MonoBehaviour
{
    [SerializeField, Min(0)] private int points = 10;
    [SerializeField] private bool grantsShield;

    public int Points => points;
    public bool GrantsShield => grantsShield;
    public bool IsCollected { get; private set; }
    public event Action<Collectible2D, PlayerHealth2D> Collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider2D other)
    {
        if (IsCollected || other.attachedRigidbody == null
            || !other.attachedRigidbody.TryGetComponent<PlayerHealth2D>(out var player)
            || !player.isActiveAndEnabled)
            return;

        IsCollected = true;
        if (grantsShield)
            player.GrantShield();

        gameObject.SetActive(false);
        Collected?.Invoke(this, player);
    }
}
