using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerCameraFollow2D))]
public sealed class CameraShake2D : MonoBehaviour
{
    [SerializeField] private PlayerHealth2D player;
    [SerializeField] private bool shakeEnabled = true;
    [SerializeField, Min(0f)] private float amplitudePixels = 1f;
    [SerializeField, Min(0f)] private float damageDuration = 0.1f;
    [SerializeField, Min(0f)] private float shieldBreakDuration = 0.1f;

    private Camera viewCamera;
    private PixelPerfectCamera pixelPerfectCamera;
    private double startedAt;
    private double? pausedAt;
    private float duration;
    private bool offsetApplied;

    private void Awake()
    {
        viewCamera = GetComponent<Camera>();
        pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
    }

    private void OnEnable()
    {
        if (player != null)
        {
            player.Damaged += OnDamage;
            player.ShieldBroken += OnShieldBreak;
        }
        RenderPipelineManager.endCameraRendering += RestoreView;
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.Damaged -= OnDamage;
            player.ShieldBroken -= OnShieldBreak;
        }
        RenderPipelineManager.endCameraRendering -= RestoreView;
        ClearShake();
    }

    private void OnDamage(Vector3 position) => BeginShake(damageDuration);
    private void OnShieldBreak(Vector3 position) => BeginShake(shieldBreakDuration);

    internal void SetMenuPaused(bool paused)
    {
        if (paused)
        {
            pausedAt ??= Time.unscaledTimeAsDouble;
        }
        else if (pausedAt.HasValue)
        {
            startedAt += Time.unscaledTimeAsDouble - pausedAt.Value;
            pausedAt = null;
        }
    }

    private void BeginShake(float seconds)
    {
        if (!shakeEnabled)
            return;

        startedAt = Time.unscaledTimeAsDouble;
        duration = seconds;
    }

    internal void ApplyRenderOffset(Bounds levelBounds)
    {
        if (!isActiveAndEnabled || !shakeEnabled || duration <= 0f)
            return;

        var progress = (float)(((pausedAt ?? Time.unscaledTimeAsDouble) - startedAt) / duration);
        if (progress >= 1f)
        {
            ClearShake();
            return;
        }

        var halfHeight = viewCamera.orthographicSize;
        var halfWidth = halfHeight * viewCamera.aspect;
        var renderPixelsPerUnit = Mathf.Max(1f, viewCamera.pixelHeight / (2f * halfHeight));
        var direction = progress < 0.35f ? 1f : -1f;
        var maxAmplitude = amplitudePixels / pixelPerfectCamera.assetsPPU;
        var cameraX = viewCamera.transform.position.x;
        var available = direction > 0f
            ? levelBounds.max.x - halfWidth - cameraX
            : cameraX - (levelBounds.min.x + halfWidth);
        var availablePixels = Mathf.Floor(Mathf.Min(maxAmplitude, available) * renderPixelsPerUnit);
        var impulsePixels = Mathf.Round(maxAmplitude * (1f - progress) * renderPixelsPerUnit);
        var magnitude = Mathf.Min(impulsePixels, availablePixels) / renderPixelsPerUnit;
        if (magnitude <= 0f)
            return;

        // Translate the view for all world layers after follow has set the camera.
        // Parallax and HUD continue to use the normal transform and overlay canvas.
        viewCamera.worldToCameraMatrix *= Matrix4x4.Translate(Vector3.left * (direction * magnitude));
        offsetApplied = true;
    }

    private void RestoreView(ScriptableRenderContext context, Camera renderedCamera)
    {
        if (renderedCamera == viewCamera)
            ResetViewMatrix();
    }

    private void ClearShake()
    {
        duration = 0f;
        ResetViewMatrix();
    }

    private void ResetViewMatrix()
    {
        if (!offsetApplied)
            return;

        viewCamera.ResetWorldToCameraMatrix();
        offsetApplied = false;
    }
}
