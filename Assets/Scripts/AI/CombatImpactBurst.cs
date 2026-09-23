using System.Collections;
using UnityEngine;

public sealed class CombatImpactBurst : MonoBehaviour
{
    private static Sprite pixelSprite;

    public static void Spawn(Vector3 position, Color color)
    {
        var root = new GameObject("Combat Impact Burst");
        root.transform.position = position;
        var burst = root.AddComponent<CombatImpactBurst>();
        burst.StartCoroutine(burst.Animate(color));
    }

    private IEnumerator Animate(Color color)
    {
        const int shardCount = 4;
        const float duration = 0.16f;
        Transform[] shards = new Transform[shardCount];
        Vector2[] directions =
        {
            new(-1f, 0.35f), new(1f, 0.35f), new(-0.35f, 1f), new(0.35f, -1f)
        };

        for (int i = 0; i < shardCount; i++)
        {
            var shard = new GameObject($"Shard {i + 1}");
            shard.transform.SetParent(transform, false);
            shard.transform.localScale = new Vector3(0.12f, 0.12f, 1f);
            var renderer = shard.AddComponent<SpriteRenderer>();
            renderer.sprite = GetPixelSprite();
            renderer.color = color;
            renderer.sortingOrder = 200;
            shards[i] = shard.transform;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < shards.Length; i++)
            {
                if (shards[i] == null) continue;
                shards[i].localPosition = directions[i] * (0.08f + 0.34f * t);
                shards[i].localScale = Vector3.one * Mathf.Lerp(0.12f, 0.02f, t);
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    private static Sprite GetPixelSprite()
    {
        if (pixelSprite != null) return pixelSprite;
        Texture2D texture = new(1, 1, TextureFormat.RGBA32, false)
        {
            name = "Combat Impact Pixel",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        pixelSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 32f);
        pixelSprite.name = "Combat Impact Pixel";
        return pixelSprite;
    }
}
