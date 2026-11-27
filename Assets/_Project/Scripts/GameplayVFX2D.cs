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
    }

    private void OnDisable()
    {
        controller.GroundJumped -= PlayJump;
        controller.Landed -= PlayLanding;
        health.Damaged -= PlayDamage;
        health.ShieldBroken -= PlayShieldBreak;
    }

    private Vector3 AtFeet(Vector2 position) => new Vector3(position.x, position.y, transform.position.z);
    private void PlayJump(Vector2 position) => Spawn(jumpDust, AtFeet(position), gameObject.scene);
    private void PlayLanding(Vector2 position) => Spawn(landingDust, AtFeet(position), gameObject.scene);
    private void PlayDamage(Vector3 position) => Spawn(damage, position, gameObject.scene);
    private void PlayShieldBreak(Vector3 position) => Spawn(shieldBreak, position, gameObject.scene, feedback.ShieldTint);

    internal ParticleSystem PlayDeath(Vector3 position)
    {
        if (playerDeath == null || !playerDeath.gameObject.activeSelf)
        {
            Debug.LogError("The player death burst needs an active particle prefab.", this);
            return null;
        }

        var burst = CreateBurst(playerDeath, position, gameObject.scene);
        foreach (var system in burst.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = system.main;
            if (main.loop || main.simulationSpeed <= 0f || main.ringBufferMode != ParticleSystemRingBufferMode.Disabled)
            {
                Debug.LogError("The player death burst needs finite particle systems that can finish playing.", this);
                Destroy(burst.gameObject);
                return null;
            }

            main.useUnscaledTime = true;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            // Keep the hierarchy alive until every child and sub-emitter has finished.
            main.stopAction = ParticleSystemStopAction.None;
        }

        burst.Play();
        return burst;
    }

    internal static void Spawn(ParticleSystem prefab, Vector3 position, Scene scene, Color? tint = null)
    {
        if (prefab == null)
            return;

        var burst = CreateBurst(prefab, position, scene);
        if (tint.HasValue)
        {
            var main = burst.main;
            main.startColor = tint.Value;
        }
        burst.Play();
    }

    private static ParticleSystem CreateBurst(ParticleSystem prefab, Vector3 position, Scene scene)
    {
        var burst = Instantiate(prefab, position, Quaternion.identity);
        if (burst.gameObject.scene != scene)
            SceneManager.MoveGameObjectToScene(burst.gameObject, scene);
        return burst;
    }
}
