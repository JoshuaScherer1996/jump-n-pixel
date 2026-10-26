using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class ParallaxLayer2D : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField, Range(0f, 1f)] private float horizontalScrollFactor = 0.1f;
    [SerializeField] private bool alternateFlipX;
    [SerializeField] private SpriteRenderer[] tiles;

    private Vector3 initialPosition;
    private float initialCameraX;
    private float localTileWidth;
    private float worldTileWidth;
    private bool initialized;

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += PositionForCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= PositionForCamera;
    }

    private void Start()
    {
        if (viewCamera == null)
            viewCamera = Camera.main;

        if (viewCamera == null || tiles == null || tiles.Length == 0
            || tiles[0] == null || tiles[0].sprite == null)
        {
            enabled = false;
            return;
        }

        initialPosition = transform.position;
        initialCameraX = viewCamera.transform.position.x;
        localTileWidth = tiles[0].sprite.bounds.size.x;
        worldTileWidth = localTileWidth * Mathf.Abs(transform.lossyScale.x);
        if (worldTileWidth <= 0f)
        {
            enabled = false;
            return;
        }

        initialized = true;
        PositionTiles();
    }

    private void LateUpdate()
    {
        PositionTiles();
    }

    private void PositionForCamera(ScriptableRenderContext context, Camera renderedCamera)
    {
        if (renderedCamera == viewCamera)
            PositionTiles();
    }

    private void PositionTiles()
    {
        if (!initialized)
            return;

        var halfWidth = viewCamera.orthographicSize * viewCamera.aspect;
        EnsureCoverage(halfWidth * 2f);

        var cameraX = viewCamera.transform.position.x;
        var pixelsPerUnit = Mathf.Max(1f, viewCamera.pixelHeight / (viewCamera.orthographicSize * 2f));
        var desiredX = initialPosition.x
            + (cameraX - initialCameraX) * (1f - Mathf.Clamp01(horizontalScrollFactor));
        var position = initialPosition;
        position.x = cameraX + Mathf.Round((desiredX - cameraX) * pixelsPerUnit) / pixelsPerUnit;
        transform.position = position;

        var firstTile = Mathf.FloorToInt((cameraX - halfWidth - position.x) / worldTileWidth) - 1;
        for (var i = 0; i < tiles.Length; i++)
        {
            var tileIndex = firstTile + i;
            tiles[i].transform.localPosition = new Vector3((tileIndex + 0.5f) * localTileWidth, 0f, 0f);
            tiles[i].flipX = alternateFlipX && (tileIndex & 1) != 0;
        }
    }

    private void EnsureCoverage(float viewWidth)
    {
        var required = Mathf.CeilToInt(viewWidth / worldTileWidth) + 2;
        if (required <= tiles.Length)
            return;

        var previousCount = tiles.Length;
        System.Array.Resize(ref tiles, required);
        for (var i = previousCount; i < required; i++)
        {
            tiles[i] = Instantiate(tiles[0], transform);
            tiles[i].name = "Tile " + i;
        }
    }
}
