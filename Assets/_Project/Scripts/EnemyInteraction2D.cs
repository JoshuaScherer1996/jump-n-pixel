using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class EnemyInteraction2D : MonoBehaviour
{
    [SerializeField, Min(1)] private int contactDamage = 1;
    [SerializeField, Min(0)] private int points = 50;
    [SerializeField, Min(0f)] private float horizontalKnockback = 4f;
    [SerializeField, Min(0f)] private float verticalKnockback = 3.5f;
    [SerializeField, Min(0f)] private float stompTolerance = 0.08f;
    [SerializeField, Min(0f)] private float minimumStompOverlap = 0.04f;
    [SerializeField, Range(0f, 1f)] private float minimumStompNormal = 0.35f;

    private readonly Dictionary<Collider2D, PlayerHealth2D> contacts = new Dictionary<Collider2D, PlayerHealth2D>();
    private readonly HashSet<Collider2D> resolvedContacts = new HashSet<Collider2D>();
    private Collider2D bodyCollider;

    public int Points => points;
    public bool IsDefeated { get; private set; }
    public event Action<EnemyInteraction2D, PlayerHealth2D> Defeated;

    private void Awake()
    {
        bodyCollider = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Interact(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        Interact(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        contacts.Remove(collision.collider);
        resolvedContacts.Remove(collision.collider);
    }

    private void OnDisable()
    {
        contacts.Clear();
        resolvedContacts.Clear();
    }

    private void Update()
    {
        if (IsDefeated)
            return;

        foreach (var contact in contacts)
        {
            if (contact.Key != null && contact.Key.isActiveAndEnabled
                && contact.Value != null && contact.Value.isActiveAndEnabled
                && !resolvedContacts.Contains(contact.Key) && bodyCollider.IsTouching(contact.Key))
                TryDamage(contact.Key, contact.Value);
        }
    }

    private void Interact(Collision2D collision)
    {
        var other = collision.collider;
        if (IsDefeated || resolvedContacts.Contains(other) || other.attachedRigidbody == null
            || !other.attachedRigidbody.TryGetComponent<PlayerHealth2D>(out var player)
            || !player.isActiveAndEnabled)
            return;

        var controller = player.GetComponent<PlayerController2D>();
        if (CanStomp(collision, controller))
        {
            IsDefeated = true;
            bodyCollider.enabled = false;
            gameObject.SetActive(false);
            controller.Bounce();
            Defeated?.Invoke(this, player);
            return;
        }

        contacts[other] = player;
        TryDamage(other, player);
    }

    private bool CanStomp(Collision2D collision, PlayerController2D controller)
    {
        if (!controller.isActiveAndEnabled || controller.PrePhysicsVelocity.y >= -0.1f)
            return false;

        var bounds = bodyCollider.bounds;
        var playerBounds = collision.collider.bounds;
        var overlap = Mathf.Min(bounds.max.x, playerBounds.max.x) - Mathf.Max(bounds.min.x, playerBounds.min.x);
        if (overlap < minimumStompOverlap)
            return false;

        var previousFoot = controller.PreviousFootPosition;
        var radius = playerBounds.extents.x;
        var horizontalDistance = Mathf.Abs(previousFoot.x - Mathf.Clamp(previousFoot.x, bounds.min.x, bounds.max.x));
        if (horizontalDistance > radius + stompTolerance)
            return false;

        var roundedFootOffset = radius - Mathf.Sqrt(Mathf.Max(0f, radius * radius - horizontalDistance * horizontalDistance));
        if (previousFoot.y + roundedFootOffset < bounds.max.y - stompTolerance)
            return false;

        for (var i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            if (contact.point.y >= bounds.max.y - stompTolerance && Mathf.Abs(contact.normal.y) >= minimumStompNormal)
                return true;
        }

        return false;
    }

    private void TryDamage(Collider2D other, PlayerHealth2D player)
    {
        var direction = other.bounds.center.x < bodyCollider.bounds.center.x ? -1f : 1f;
        var knockback = new Vector2(direction * horizontalKnockback, verticalKnockback);
        if (player.TryTakeDamage(contactDamage, knockback))
            resolvedContacts.Add(other);
    }
}
