using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class MonsterSpawnWarningView : MonoBehaviour
{
    [Header("Отображение")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color visibleColor = Color.white;

    [Header("Появление")]
    [SerializeField] private Vector3 initialScale = Vector3.one * 0.5f;
    [SerializeField] private Vector3 finalScale = Vector3.one;
    [SerializeField, Min(0.01f)] private float appearDuration = 1f;

    [Header("Подготовка спавна")]
    [SerializeField, Min(0f)] private float spawnDelayAfterAppearance = 1.5f;
    [SerializeField] private Light2D spawnLight;
    [SerializeField, Min(0f)] private float visibleLightIntensity = 1f;
    [SerializeField, Min(0.01f)] private float lightFadeDuration = 0.5f;

    [Header("Бамп при появлении монстра")]
    [SerializeField, Min(1f)] private float bumpScale = 1.2f;
    [SerializeField, Min(0.01f)] private float bumpDuration = 0.2f;

    [Header("Исчезновение")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 1f;

    public SpriteRenderer Renderer => spriteRenderer;
    public Color VisibleColor => visibleColor;
    public Vector3 InitialScale => initialScale;
    public Vector3 FinalScale => finalScale;
    public float AppearDuration => appearDuration;
    public float SpawnDelayAfterAppearance => spawnDelayAfterAppearance;
    public float LightFadeDuration => lightFadeDuration;
    public float BumpScale => bumpScale;
    public float BumpDuration => bumpDuration;
    public float FadeDuration => fadeDuration;

    public void Prepare()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        transform.localScale = initialScale;
        transform.localRotation = Quaternion.identity;
        HideLightImmediate();
        if (spriteRenderer != null)
        {
            var color = visibleColor;
            color.a = 0f;
            spriteRenderer.color = color;
        }
    }

    public void SetLightVisibility(float value)
    {
        if (spawnLight == null) return;
        if (!spawnLight.gameObject.activeSelf) spawnLight.gameObject.SetActive(true);
        spawnLight.intensity = visibleLightIntensity * Mathf.Clamp01(value);
    }

    public void HideLightImmediate()
    {
        if (spawnLight == null) return;
        spawnLight.intensity = 0f;
        spawnLight.gameObject.SetActive(false);
    }

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spawnLight = GetComponentInChildren<Light2D>(true);
        if (spawnLight != null) visibleLightIntensity = spawnLight.intensity;
    }
}
