using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Shared readable combat numbers; keeps Setup compatibility with the original world-space prefab.</summary>
public class DamageText : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float fadeDuration = .85f;
    private TMP_Text textMesh;
    private Color initialColor;
    private float age;
    private DamageTextLayer layer;
    private Health owner;
    private int ownerVersion;
    private Vector3 anchor;
    private Vector2 lane;
    private RectTransform rect;
    public string DisplayedText => textMesh != null ? textMesh.text : string.Empty;
    public Color NumberColor => initialColor;
    public static readonly Color DamageColor = new(1f, .3f, .3f, 1);
    public static readonly Color HealingColor = new(.42f, 1f, .4f, 1);

    private void Awake() { textMesh = GetComponent<TMP_Text>(); rect = transform as RectTransform; }
    public static void Show(Health target, float signedAmount, int sequence, GameObject legacyPrefab = null)
    {
        float amount = Mathf.Abs(signedAmount);
        if (amount <= 0) return;
        string number = Mathf.Floor(amount).ToString("0", CultureInfo.InvariantCulture);
        Color color = signedAmount > 0 ? DamageColor : HealingColor;
        if (legacyPrefab != null && legacyPrefab.GetComponent<DamageText>() != null)
        {
            var legacy = Instantiate(legacyPrefab, target.DamageNumberPosition, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(legacy, target.gameObject.scene);
            legacy.GetComponent<DamageText>().Setup(number, color);
            return;
        }
        var layer = DamageTextLayer.For(target.gameObject.scene);
        var popup = layer.Rent();
        if (popup == null) return;
        popup.layer = layer; popup.owner = target; popup.ownerVersion = target.SpawnVersion; popup.anchor = target.DamageNumberPosition;
        popup.lane = Vector2.up * (sequence % 3 * 22);
        popup.Setup(number, color); popup.UpdatePosition();
    }
    public void Setup(string text, Color color)
    {
        if (textMesh == null) textMesh = GetComponent<TMP_Text>();
        if (textMesh == null) { Destroy(gameObject); return; }
        textMesh.text = text; textMesh.color = color; initialColor = color; age = 0;
        transform.localScale = Vector3.one;
    }
    private void UpdatePosition()
    {
        if (layer == null || layer.Camera == null) return;
        if (owner != null && !owner.IsDead && owner.SpawnVersion == ownerVersion && age < .1f) anchor = owner.DamageNumberPosition;
        Vector3 screen = layer.Camera.WorldToScreenPoint(anchor);
        // The game's orthographic camera renders the Z=0 plane with a negative near clip.
        // A positive-depth test hides every number in that valid plane.
        textMesh.enabled = screen.z >= layer.Camera.nearClipPlane && screen.z <= layer.Camera.farClipPlane;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer.Rect, screen, null, out var local);
        rect.anchoredPosition = local + lane + Vector2.up * (24 + age * 44);
    }
    private void Update()
    {
        if (Time.timeScale <= 0)
        {
            if (layer != null) layer.Release(this); else Destroy(gameObject);
            return;
        }
        // Presentation remains readable at X6; it still stops when the game is paused.
        age += Time.unscaledDeltaTime;
        if (layer != null)
        {
            UpdatePosition();
            transform.localScale = Vector3.one * Mathf.Lerp(1.16f, 1, Mathf.Clamp01(age / .12f));
        }
        else transform.position += Vector3.up * (moveSpeed * Time.unscaledDeltaTime);
        float alpha = 1 - Mathf.Clamp01((age - fadeDuration * .45f) / (fadeDuration * .55f));
        textMesh.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
        if (age >= fadeDuration)
        {
            if (layer != null) layer.Release(this); else Destroy(gameObject);
        }
    }
}

internal sealed class DamageTextLayer : MonoBehaviour
{
    private static DamageTextLayer current;
    private readonly Stack<DamageText> pool = new();
    private TMP_FontAsset font;
    private Material fontMaterial;
    private int created;
    public RectTransform Rect => (RectTransform)transform;
    public Camera Camera { get; private set; }
    public static DamageTextLayer For(Scene scene)
    {
        if (current != null && current.gameObject.scene == scene) return current;
        var root = new GameObject("Combat Floating Numbers", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(root, scene);
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Combat feedback belongs below every HUD/popup Canvas (World HUD starts at -100).
        canvas.sortingOrder = -101;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        current = root.AddComponent<DamageTextLayer>();
        current.Camera = UnityEngine.Camera.main != null ? UnityEngine.Camera.main : Object.FindFirstObjectByType<Camera>();
        current.font = Resources.Load<TMP_FontAsset>("Fonts/Pixellari Reading UI");
        if (current.font != null)
        {
            current.fontMaterial = new Material(current.font.material) { name = "Combat numbers dark outline" };
            current.fontMaterial.EnableKeyword("OUTLINE_ON");
            current.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, .18f);
            current.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(.035f, .025f, .045f, 1));
        }
        return current;
    }
    public DamageText Rent()
    {
        while (pool.Count > 0)
        {
            var value = pool.Pop(); if (value == null) continue;
            value.gameObject.SetActive(true); return value;
        }
        if (created >= 192) return null;
        var root = new GameObject("Floating combat number", typeof(RectTransform), typeof(TextMeshProUGUI));
        root.transform.SetParent(transform, false); var rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(160, 44);
        var text = root.GetComponent<TextMeshProUGUI>(); text.font = font;
        if (fontMaterial != null) text.fontSharedMaterial = fontMaterial;
        text.fontSize = 26; text.fontStyle = FontStyles.Bold; text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false; text.overflowMode = TextOverflowModes.Overflow; text.raycastTarget = false;
        created++; return root.AddComponent<DamageText>();
    }
    public void Release(DamageText value) { value.gameObject.SetActive(false); pool.Push(value); }
    private void OnDestroy()
    {
        if (fontMaterial != null) Destroy(fontMaterial);
        if (current == this) current = null;
    }
}
