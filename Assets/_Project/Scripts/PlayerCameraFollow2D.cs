using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera), typeof(PixelPerfectCamera))]
public sealed class PlayerCameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Tilemap[] levelTilemaps;
    [SerializeField] private float verticalOffset;

    private Camera followingCamera;
    private PixelPerfectCamera pixelPerfectCamera;
    private float fixedDepth;
    private Bounds levelBounds;
    private bool hasLevelBounds;

    private void Awake()
    {
        followingCamera = GetComponent<Camera>();
        pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
        fixedDepth = transform.position.z;
        FindLevelBounds();
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += ConstrainRenderedCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= ConstrainRenderedCamera;
    }

    private void Start()
    {
        FollowTarget();
    }

    private void LateUpdate()
    {
        FollowTarget();
    }

    private void ConstrainRenderedCamera(ScriptableRenderContext context, Camera renderedCamera)
    {
        if (renderedCamera != followingCamera)
            return;

        FollowTarget();
        followingCamera.ResetWorldToCameraMatrix();
    }

    private void FollowTarget()
    {
        if (target == null || !hasLevelBounds)
            return;

        var halfHeight = followingCamera.orthographicSize;
        var halfWidth = followingCamera.orthographicSize * followingCamera.aspect;
        var pixelsPerUnit = Mathf.Max(1f, followingCamera.pixelHeight / (halfHeight * 2f));
        var desiredHeight = target.position.y + verticalOffset;
        transform.position = new Vector3(
            ConstrainAxis(target.position.x, levelBounds.min.x, levelBounds.max.x, halfWidth, pixelsPerUnit),
            ConstrainAxis(desiredHeight, levelBounds.min.y, levelBounds.max.y, halfHeight, pixelsPerUnit),
            fixedDepth);
    }

    private float ConstrainAxis(float position, float lowerBound, float upperBound, float halfExtent, float pixelsPerUnit)
    {
        var assetPixelsPerUnit = pixelPerfectCamera.assetsPPU;
        var minimum = Mathf.Ceil((lowerBound + halfExtent) * assetPixelsPerUnit) / assetPixelsPerUnit;
        var maximum = Mathf.Floor((upperBound - halfExtent) * assetPixelsPerUnit) / assetPixelsPerUnit;
        return minimum <= maximum
            ? Mathf.Clamp(Mathf.Round(position * pixelsPerUnit) / pixelsPerUnit, minimum, maximum)
            : (lowerBound + upperBound) * 0.5f;
    }

    private void FindLevelBounds()
    {
        if (levelTilemaps == null)
            return;

        foreach (var tilemap in levelTilemaps)
        {
            if (tilemap == null || !tilemap.gameObject.activeInHierarchy)
                continue;

            var renderer = tilemap.GetComponent<TilemapRenderer>();
            if (renderer == null || !renderer.enabled)
                continue;

            foreach (var cell in tilemap.cellBounds.allPositionsWithin)
            {
                var sprite = tilemap.GetSprite(cell);
                if (sprite == null)
                    continue;

                var matrix = tilemap.transform.localToWorldMatrix
                    * Matrix4x4.Translate(tilemap.CellToLocalInterpolated((Vector3)cell + tilemap.tileAnchor))
                    * tilemap.orientationMatrix * tilemap.GetTransformMatrix(cell);
                var bounds = sprite.bounds;
                for (var corner = 0; corner < 8; corner++)
                {
                    var direction = new Vector3((corner & 1) == 0 ? -1f : 1f,
                        (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                    var point = matrix.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents, direction));
                    if (!hasLevelBounds)
                    {
                        levelBounds = new Bounds(point, Vector3.zero);
                        hasLevelBounds = true;
                    }
                    else
                    {
                        levelBounds.Encapsulate(point);
                    }
                }
            }
        }
    }
}
