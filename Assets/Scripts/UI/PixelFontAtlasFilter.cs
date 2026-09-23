using UnityEngine;

namespace GameFoundation.UI
{
    /// <summary>Keeps a rasterized legacy UI font crisp when Unity rebuilds its dynamic atlas.</summary>
    [ExecuteAlways]
    public sealed class PixelFontAtlasFilter : MonoBehaviour
    {
        [SerializeField] private Font font;

        private void OnEnable()
        {
            Font.textureRebuilt += OnFontTextureRebuilt;
            ApplyFilter();
        }

        private void OnDisable()
        {
            Font.textureRebuilt -= OnFontTextureRebuilt;
        }

        private void OnValidate()
        {
            ApplyFilter();
        }

        private void OnFontTextureRebuilt(Font rebuiltFont)
        {
            if (rebuiltFont == font)
                ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (font != null && font.material != null && font.material.mainTexture != null)
                font.material.mainTexture.filterMode = FilterMode.Point;
        }
    }
}
