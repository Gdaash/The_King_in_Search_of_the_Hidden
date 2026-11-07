#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using GameFoundation.Bestiary;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using GameFoundation.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Scene-local, editor-only additions to the next prepared enemy wave.</summary>
public sealed class EditorWaveCheatPopup : MonoBehaviour
{
    private sealed class Row { public GameObject prefab; public TMP_Text amount; }
    private readonly List<Row> rows = new();
    private AlarmSystem alarm;
    private GameObject window;
    private TMP_Text status;
    private float previousSpeed;
    private Sprite buttonSprite;
    private TMP_FontAsset font;
    private string builtLanguage;
    private static EditorWaveCheatPopup opened;
    public static bool IsOpen => opened != null && opened.window != null && opened.window.activeSelf;
    public static int ClosedFrame { get; private set; } = -1;
    public bool IsAvailable => alarm != null && alarm.isActiveAndEnabled && alarm.DifficultyTable != null;
    private static readonly Color Ink = new(.94f, .91f, .82f);
    private static string T(string suffix, string fallback)
    {
        string key = "cheat.waves." + suffix;
        string value = LocalizationService.Instance != null ? LocalizationService.Instance.Get(key) : key;
        return value == key ? fallback : value;
    }
    private void Awake() => alarm = GetComponent<AlarmSystem>();
    private void Update()
    {
        if (window != null && window.activeSelf && (!IsAvailable || PortalCloudTransition.IsLoading || WorldEscapeController.ResultsActive)) Close();
        if (!IsAvailable || PortalCloudTransition.IsLoading || WorldEscapeController.ResultsActive) return;
        if (Input.GetKeyDown(KeyCode.J) && !EditingText())
        {
            if (IsOpen && opened == this) Close();
            else if (!IsOpen && !PortalTowerProgression.IsChoosingUpgrade && FindAnyObjectByType<GameFoundation.Settings.GameMenuController>() == null) Open();
        }
        else if (IsOpen && opened == this && Input.GetKeyDown(KeyCode.Escape)) Close();
    }
    private static bool EditingText()
    {
        var selection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selection != null && (selection.GetComponent<InputField>() != null || selection.GetComponent<TMP_InputField>() != null);
    }
    public void Open()
    {
        if (!IsAvailable || IsOpen || PortalArsenalPopup.IsOpen || PortalCloudTransition.IsLoading || WorldEscapeController.ResultsActive || PortalTowerProgression.IsChoosingUpgrade) return;
        string language = LocalizationService.Instance != null ? LocalizationService.Instance.Language : "ru";
        if (window != null && builtLanguage != language) { Destroy(window); window = null; rows.Clear(); }
        if (window == null) Build();
        previousSpeed = GameSpeedControls.SimulationSpeed;
        opened = this;
        GameSpeedControls.SetSimulationSpeed(0);
        window.SetActive(true);
        alarm.WaveChanged += Refresh;
        Refresh();
    }
    public void Close()
    {
        if (window == null || !window.activeSelf) return;
        alarm.WaveChanged -= Refresh;
        window.SetActive(false);
        ClosedFrame = Time.frameCount;
        if (opened == this)
        {
            opened = null;
            if (!PortalCloudTransition.IsLoading && gameObject.scene.isLoaded) GameSpeedControls.SetSimulationSpeed(previousSpeed);
        }
        EventSystem.current?.SetSelectedGameObject(null);
    }
    private void OnDisable() => Close();
    private void OnDestroy() { Close(); if (window != null) Destroy(window); }
    private void Refresh()
    {
        if (status == null) return;
        status.text = alarm.EditorEditingCountdown
            ? T("countdown", "Добавление в волну текущего отсчёта")
            : T("next", "Добавление в следующую волну");
        foreach (var row in rows) row.amount.text = "+" + alarm.EditorAddedCount(row.prefab);
    }
    private RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position;
        return rect;
    }
    private TMP_Text Label(string name, Transform parent, string value, Vector2 size, Vector2 position, float fontSize, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var rect = Rect(name, parent, size, position);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.fontSize = fontSize; label.color = Ink; label.text = value;
        label.alignment = alignment; label.raycastTarget = false;
        label.enableAutoSizing = true; label.fontSizeMin = fontSize - 4; label.fontSizeMax = fontSize;
        return label;
    }
    private Button Button(Transform parent, string value, Vector2 size, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var rect = Rect(value, parent, size, position);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = buttonSprite; image.type = UnityEngine.UI.Image.Type.Sliced;
        image.color = buttonSprite != null ? Color.white : new Color(.3f, .25f, .36f);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.onClick.AddListener(action);
        Label("Label", rect, value, size - new Vector2(12, 8), Vector2.zero, 24);
        return button;
    }
    private void Build()
    {
        builtLanguage = LocalizationService.Instance != null ? LocalizationService.Instance.Language : "ru";
        font = Resources.Load<TMP_FontAsset>("Fonts/Pixellari Reading UI");
        buttonSprite = Resources.Load<ButtonVisualTheme>("UI/ButtonVisualTheme")?.textButtonSprite;
        window = new GameObject("Editor next wave cheat", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(window, gameObject.scene);
        var canvas = window.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 31000;
        var scaler = window.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        var shade = Rect("Dimmer", window.transform, Vector2.zero, Vector2.zero);
        shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one; shade.offsetMin = shade.offsetMax = Vector2.zero;
        shade.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, .72f);
        var panel = Rect("Wave panel", window.transform, new Vector2(1060, 760), Vector2.zero);
        panel.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.075f, .055f, .095f);
        var outline = panel.gameObject.AddComponent<UnityEngine.UI.Outline>(); outline.effectColor = new Color(.43f, .36f, .5f); outline.effectDistance = new Vector2(2, -2);
        var title = Label("Title", panel, T("title", "Следующая волна – тестирование [J]"), new Vector2(950, 55), new Vector2(0, 330), 32); title.color = new Color(.96f, .82f, .45f);
        status = Label("Status", panel, "", new Vector2(960, 40), new Vector2(0, 280), 24);
        Label("Hint", panel, T("hint", "Добавленные враги появятся вместе с обычным составом волны."), new Vector2(960, 50), new Vector2(0, -275), 22);
        Button(panel, T("clear", "Очистить добавления"), new Vector2(350, 56), new Vector2(-235, -335), () => alarm.EditorClearNextWaveAdditions());
        Button(panel, T("close", "Закрыть"), new Vector2(220, 56), new Vector2(260, -335), Close);
        var scrollRoot = Rect("Enemy scroll", panel, new Vector2(990, 475), new Vector2(0, 5));
        var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
        var viewport = Rect("Viewport", scrollRoot, Vector2.zero, Vector2.zero); viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one; viewport.offsetMin = Vector2.zero; viewport.offsetMax = new Vector2(-22, 0);
        viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = Color.white;
        var mask = viewport.gameObject.AddComponent<UnityEngine.UI.Mask>(); mask.showMaskGraphic = false;
        var content = Rect("Content", viewport, Vector2.zero, Vector2.zero); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 8; layout.padding = new RectOffset(6, 6, 6, 6); layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false;
        var fit = content.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport; scroll.content = content;
        var barRect = Rect("Scrollbar", scrollRoot, Vector2.zero, Vector2.zero);
        barRect.anchorMin = new Vector2(1, 0); barRect.anchorMax = Vector2.one;
        barRect.offsetMin = new Vector2(-14, 0); barRect.offsetMax = Vector2.zero;
        barRect.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.16f, .12f, .2f);
        var handle = Rect("Handle", barRect, Vector2.zero, Vector2.zero); handle.anchorMin = Vector2.zero; handle.anchorMax = Vector2.one; handle.offsetMin = handle.offsetMax = Vector2.zero;
        var handleImage = handle.gameObject.AddComponent<UnityEngine.UI.Image>(); handleImage.color = new Color(.48f, .4f, .58f);
        var scrollbar = barRect.gameObject.AddComponent<Scrollbar>(); scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handle; scrollbar.targetGraphic = handleImage; scroll.verticalScrollbar = scrollbar;
        var descriptions = Resources.LoadAll<UnitDescriptionDefinition>("UI/Unit Details");
        var enemies = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Units" })
            .Select(id => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(id)))
            .Where(p => p != null && p.CompareTag("Enemy1") && p.GetComponent<Health>() != null && p.GetComponent<EnemyIdentity>() != null)
            .OrderBy(p => p.GetComponent<EnemyIdentity>().DangerRank).ThenBy(p => p.name);
        foreach (var prefab in enemies)
        {
            var definition = descriptions.FirstOrDefault(d => d.unitPrefab == prefab);
            var row = Rect(prefab.name, content, new Vector2(960, 92), Vector2.zero);
            var element = row.gameObject.AddComponent<LayoutElement>(); element.preferredHeight = 92;
            row.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.16f, .125f, .2f);
            var sprite = definition != null ? definition.Portrait : prefab.GetComponentInChildren<SpriteRenderer>()?.sprite;
            if (sprite != null)
            {
                var icon = Rect("Portrait", row, sprite.rect.size * 2, new Vector2(-430, 0)).gameObject.AddComponent<UnityEngine.UI.Image>();
                icon.sprite = sprite; icon.preserveAspect = true; icon.raycastTarget = false;
            }
            Label("Enemy", row, definition != null ? definition.Title : prefab.name, new Vector2(320, 80), new Vector2(-215, 0), 24, TextAlignmentOptions.MidlineLeft);
            var count = Label("Added", row, "+0", new Vector2(90, 60), new Vector2(40, 0), 28); count.color = new Color(.6f, .84f, .45f);
            int[] amounts = { -1, 1, 5, 10 };
            for (int i = 0; i < amounts.Length; i++)
            {
                int delta = amounts[i]; var enemy = prefab;
                Button(row, delta > 0 ? "+" + delta : delta.ToString(), new Vector2(78, 56), new Vector2(135 + i * 86, 0), () => alarm.EditorAdjustNextWave(enemy, delta));
            }
            rows.Add(new Row { prefab = prefab, amount = count });
        }
        window.SetActive(false);
    }
}
#endif
