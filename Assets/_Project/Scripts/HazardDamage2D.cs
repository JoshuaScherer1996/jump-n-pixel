using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class HazardDamage2D : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 1;
    private readonly Dictionary<Collider2D, PlayerHealth2D> contacts = new Dictionary<Collider2D, PlayerHealth2D>();

    private void Update()
    {
        foreach (var contact in contacts)
        {
            if (contact.Key != null && contact.Key.isActiveAndEnabled
                && contact.Value != null && contact.Value.isActiveAndEnabled)
                contact.Value.TryTakeDamage(damage);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        contacts.Remove(other);
    }

    private void OnDisable()
    {
        contacts.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ApplyDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        ApplyDamage(other);
    }

    private void ApplyDamage(Collider2D other)
    {
        if (other.attachedRigidbody != null
            && other.attachedRigidbody.TryGetComponent<PlayerHealth2D>(out var player))
        {
            contacts[other] = player;
            player.TryTakeDamage(damage);
        }
    }
}
