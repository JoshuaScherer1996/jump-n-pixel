using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class HazardDamage2D : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Range(0.35f, 0.5f)] private float knockbackJumpFraction = 0.5f;
    private readonly List<Collider2D> contacts = new List<Collider2D>();
    private readonly HashSet<PlayerHealth2D> touchingPlayers = new HashSet<PlayerHealth2D>();
    private readonly HashSet<PlayerHealth2D> resolvedContacts = new HashSet<PlayerHealth2D>();
    private Collider2D hazardCollider;

    private void Awake()
    {
        hazardCollider = GetComponent<Collider2D>();
    }

    private void FixedUpdate()
    {
        touchingPlayers.Clear();
        hazardCollider.GetContacts(contacts);
        foreach (var contact in contacts)
        {
            if (contact.attachedRigidbody == null
                || !contact.attachedRigidbody.TryGetComponent<PlayerHealth2D>(out var player)
                || !player.isActiveAndEnabled
                || hazardCollider.Distance(contact).distance > Physics2D.defaultContactOffset * 2f)
                continue;

            touchingPlayers.Add(player);
            if (!resolvedContacts.Add(player))
                continue;

            var controller = player.GetComponent<PlayerController2D>();
            var knockback = new Vector2(controller.Velocity.x, controller.JumpSpeed * knockbackJumpFraction);
            player.TryTakeSpikeDamage(damage, knockback);
            if (hazardCollider.Distance(contact).distance > Physics2D.defaultContactOffset * 2f)
                touchingPlayers.Remove(player);
        }

        resolvedContacts.RemoveWhere(player => !touchingPlayers.Contains(player));
    }

    private void OnDisable()
    {
        contacts.Clear();
        touchingPlayers.Clear();
        resolvedContacts.Clear();
    }
}
