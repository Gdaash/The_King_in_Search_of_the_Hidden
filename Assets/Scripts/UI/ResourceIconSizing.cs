using UnityEngine;
using UnityEngine.UI;

public static class ResourceIconSizing
{
    public static void Apply(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        var palette = DamagePalette.IconMaterial(sprite);
        if (palette != null || (image.material != null && image.material.shader.name == "ThoseUnderHex/UI Damage Palette"))
            image.material = palette;
        if (sprite == null)
            return;

        image.preserveAspect = true;
        image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sprite.rect.width * 2f);
        image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sprite.rect.height * 2f);
    }
}
