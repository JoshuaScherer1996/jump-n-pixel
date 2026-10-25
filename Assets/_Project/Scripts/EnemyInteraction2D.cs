using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class EnemyInteraction2D : MonoBehaviour
{
    [SerializeField, Min(1)] private int contactDamage = 1;
    [SerializeField, Min(0)] private int points = 50;
    [SerializeField, Min(0.1f)] private float bounceSpeed = 8f;
    [SerializeField, Min(0f)] private float stompTolerance = 0.04f;
    [SerializeField, Range(0f, 0.45f)] private float stompHorizontalInset = 0.1f;

    private Collider2D bodyCollider;
    private readonly Dictionary<Collider2D, PlayerHealth2D> contacts = new Dictionary<Collider2D, PlayerHealth2D>();

    public int Points => points;
    public bool IsDefeated { get; private set; }
    public event Action<EnemyInteraction2D, PlayerHealth2D> Defeated;

    private void Awake()
    {
        bodyCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Interact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Interact(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        contacts.Remove(other);
    }

    private void OnDisable()
    {
        contacts.Clear();
    }

    private void Update()
    {
        if (IsDefeated)
            return;

        foreach (var contact in contacts)
        {
            if (contact.Key != null && contact.Key.isActiveAndEnabled
                && contact.Value != null && contact.Value.isActiveAndEnabled)
                contact.Value.TryTakeDamage(contactDamage);
        }
    }

    private void Interact(Collider2D other)
    {
        if (IsDefeated || other.attachedRigidbody == null
            || !other.attachedRigidbody.TryGetComponent<PlayerHealth2D>(out var player)
            || !player.isActiveAndEnabled)
            return;

        var controller = player.GetComponent<PlayerController2D>();
        contacts[other] = player;
        if (CanStomp(controller))
        {
            IsDefeated = true;
            gameObject.SetActive(false);
            controller.Bounce(bounceSpeed);
            Defeated?.Invoke(this, player);
            return;
        }

        player.TryTakeDamage(contactDamage);
    }

    private bool CanStomp(PlayerController2D controller)
    {
        if (!controller.isActiveAndEnabled || controller.Velocity.y >= -0.1f)
            return false;

        var bounds = bodyCollider.bounds;
        var previousFoot = controller.PreviousFootPosition;
        var foot = controller.FootPosition;
        if (previousFoot.y < bounds.max.y - stompTolerance || foot.y > bounds.max.y + stompTolerance)
            return false;

        var distance = previousFoot.y - foot.y;
        var crossing = distance > 0f ? Mathf.Clamp01((previousFoot.y - bounds.max.y) / distance) : 1f;
        var crossingX = Mathf.Lerp(previousFoot.x, foot.x, crossing);
        var inset = bounds.size.x * stompHorizontalInset;
        return crossingX >= bounds.min.x + inset && crossingX <= bounds.max.x - inset;
    }
}
