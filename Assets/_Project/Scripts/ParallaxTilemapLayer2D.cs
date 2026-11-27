using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class ParallaxTilemapLayer2D : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField, Range(0f, 1f)] private float horizontalScrollFactor = 0.6f;

    private float initialLayerX;
    private float initialCameraX;

    private void Awake()
    {
        if (viewCamera == null)
            viewCamera = Camera.main;

        if (viewCamera == null)
        {
            enabled = false;
            return;
        }

        // Capture the authored positions before camera follow applies its bounds in Start.
        initialLayerX = transform.position.x;
        initialCameraX = viewCamera.transform.position.x;
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += PositionForCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= PositionForCamera;
    }

    private void LateUpdate()
    {
        if (viewCamera != null)
            PositionLayer();
    }

    private void PositionForCamera(ScriptableRenderContext context, Camera renderedCamera)
    {
        // The camera follow subscribes first and constrains the render position.
        if (renderedCamera == viewCamera)
            PositionLayer();
    }

    private void PositionLayer()
    {
        if (Time.timeScale == 0f)
            return;

        var cameraX = viewCamera.transform.position.x;
        var cameraDelta = cameraX - initialCameraX;
        var desiredX = initialLayerX + cameraDelta * (1f - Mathf.Clamp01(horizontalScrollFactor));
        var pixelsPerUnit = Mathf.Max(1f, viewCamera.pixelHeight / (2f * viewCamera.orthographicSize));
        var position = transform.position;
        position.x = cameraX + Mathf.Round((desiredX - cameraX) * pixelsPerUnit) / pixelsPerUnit;
        transform.position = position;
    }
}
