using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class HazardDamage2D : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Range(0.35f, 0.5f)] private float knockbackJumpFraction = 0.5f;
    private readonly Dictionary<Collider2D, PlayerHealth2D> contacts = new Dictionary<Collider2D, PlayerHealth2D>();
    private readonly HashSet<Collider2D> resolvedContacts = new HashSet<Collider2D>();
    private Collider2D hazardCollider;

    private void Awake()
    {
        hazardCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        foreach (var contact in contacts)
        {
            if (contact.Key != null && contact.Key.isActiveAndEnabled
                && contact.Value != null && contact.Value.isActiveAndEnabled
                && !resolvedContacts.Contains(contact.Key) && hazardCollider.IsTouching(contact.Key))
                TryDamage(contact.Key, contact.Value);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        contacts.Remove(other);
        resolvedContacts.Remove(other);
    }

    private void OnDisable()
    {
        contacts.Clear();
        resolvedContacts.Clear();
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
            && !resolvedContacts.Contains(other)
            && other.attachedRigidbody.TryGetComponent<PlayerHealth2D>(out var player)
            && player.isActiveAndEnabled)
        {
            contacts[other] = player;
            TryDamage(other, player);
        }
    }

    private void TryDamage(Collider2D other, PlayerHealth2D player)
    {
        var controller = player.GetComponent<PlayerController2D>();
        var knockback = new Vector2(controller.Velocity.x, controller.JumpSpeed * knockbackJumpFraction);
        if (player.TryTakeDamage(damage, knockback))
            resolvedContacts.Add(other);
    }
}
