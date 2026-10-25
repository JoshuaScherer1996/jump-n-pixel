using UnityEngine;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera), typeof(PixelPerfectCamera))]
public sealed class PlayerCameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float levelLeft;
    [SerializeField] private float levelRight;

    private Camera followingCamera;
    private PixelPerfectCamera pixelPerfectCamera;
    private float fixedHeight;
    private float fixedDepth;

    private void Awake()
    {
        followingCamera = GetComponent<Camera>();
        pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
        fixedHeight = transform.position.y;
        fixedDepth = transform.position.z;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        var halfWidth = followingCamera.orthographicSize * followingCamera.aspect;
        var pixelsPerUnit = pixelPerfectCamera.assetsPPU;
        var minimum = Mathf.Ceil((levelLeft + halfWidth) * pixelsPerUnit) / pixelsPerUnit;
        var maximum = Mathf.Floor((levelRight - halfWidth) * pixelsPerUnit) / pixelsPerUnit;
        var horizontalPosition = minimum <= maximum
            ? Mathf.Clamp(Mathf.Round(target.position.x * pixelsPerUnit) / pixelsPerUnit, minimum, maximum)
            : (levelLeft + levelRight) * 0.5f;

        transform.position = new Vector3(horizontalPosition, fixedHeight, fixedDepth);
    }
}
