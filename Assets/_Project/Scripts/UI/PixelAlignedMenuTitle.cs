using TMPro;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public sealed class PixelAlignedMenuTitle : MonoBehaviour
{
    private const float AtlasSamplingBias = 1f / 64f;
    private TextMeshProUGUI title;

    private void OnEnable()
    {
        title = GetComponent<TextMeshProUGUI>();
        title.OnPreRenderText += AlignAtlasSample;
        title.SetVerticesDirty();
    }

    private void OnDisable()
    {
        if (title == null)
            return;

        title.OnPreRenderText -= AlignAtlasSample;
        title.SetVerticesDirty();
    }

    private void AlignAtlasSample(TMP_TextInfo textInfo)
    {
        for (var i = 0; i < textInfo.materialCount; i++)
        {
            var mesh = textInfo.meshInfo[i];
            var atlas = mesh.material.mainTexture;
            if (atlas == null || atlas.filterMode != FilterMode.Point)
                continue;

            // Resolve samples on texel boundaries consistently across the two glyph triangles.
            var offset = new Vector4(AtlasSamplingBias / atlas.width, AtlasSamplingBias / atlas.height, 0f, 0f);
            for (var vertex = 0; vertex < mesh.vertexCount; vertex++)
                mesh.uvs0[vertex] += offset;
        }
    }
}
