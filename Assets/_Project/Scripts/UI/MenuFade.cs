using System.Collections;
using UnityEngine;

internal static class MenuFade
{
    internal static IEnumerator ToBlack(CanvasGroup overlay, float duration)
    {
        double start = Time.unscaledTimeAsDouble;
        while (Time.unscaledTimeAsDouble - start < duration)
        {
            float t = Mathf.Clamp01((float)((Time.unscaledTimeAsDouble - start) / duration));
            overlay.alpha = t * t * (3f - 2f * t);
            yield return null;
        }

        overlay.alpha = 1f;
        // Render a black frame before loading the next scene.
        yield return null;
    }
}
