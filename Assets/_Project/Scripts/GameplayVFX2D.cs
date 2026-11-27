using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController2D), typeof(PlayerHealth2D), typeof(PlayerFeedback2D))]
public sealed class GameplayVFX2D : MonoBehaviour
{
    [SerializeField] private ParticleSystem jumpDust;
    [SerializeField] private ParticleSystem landingDust;
    [SerializeField] private ParticleSystem damage;
    [SerializeField] private ParticleSystem shieldBreak;
    [SerializeField] private ParticleSystem playerDeath;

    private PlayerController2D controller;
    private PlayerHealth2D health;
    private PlayerFeedback2D feedback;

    private void Awake()
    {
        controller = GetComponent<PlayerController2D>();
        health = GetComponent<PlayerHealth2D>();
        feedback = GetComponent<PlayerFeedback2D>();
    }

    private void OnEnable()
    {
        controller.GroundJumped += PlayJump;
        controller.Landed += PlayLanding;
        health.Damaged += PlayDamage;
        health.ShieldBroken += PlayShieldBreak;
        health.Died += PlayDeath;
    }

    private void OnDisable()
    {
        controller.GroundJumped -= PlayJump;
        controller.Landed -= PlayLanding;
        health.Damaged -= PlayDamage;
        health.ShieldBroken -= PlayShieldBreak;
        health.Died -= PlayDeath;
    }

    private Vector3 AtFeet(Vector2 position) => new Vector3(position.x, position.y, transform.position.z);
    private void PlayJump(Vector2 position) => Spawn(jumpDust, AtFeet(position), gameObject.scene);
    private void PlayLanding(Vector2 position) => Spawn(landingDust, AtFeet(position), gameObject.scene);
    private void PlayDamage(Vector3 position) => Spawn(damage, position, gameObject.scene);
    private void PlayShieldBreak(Vector3 position) => Spawn(shieldBreak, position, gameObject.scene, feedback.ShieldTint);
    private void PlayDeath(Vector3 position) => Spawn(playerDeath, position, gameObject.scene);

    internal static void Spawn(ParticleSystem prefab, Vector3 position, Scene scene, Color? tint = null)
    {
        if (prefab == null)
            return;

        // The source can deactivate or teleport during the gameplay event.
        var burst = Instantiate(prefab, position, Quaternion.identity);
        if (burst.gameObject.scene != scene)
            SceneManager.MoveGameObjectToScene(burst.gameObject, scene);
        if (tint.HasValue)
        {
            var main = burst.main;
            main.startColor = tint.Value;
        }
        burst.Play();
    }
}
