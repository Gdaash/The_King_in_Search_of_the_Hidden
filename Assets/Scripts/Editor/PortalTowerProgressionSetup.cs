#if UNITY_EDITOR
using System;
using System.Linq;
using GameFoundation.Base;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using GameFoundation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class PortalTowerProgressionSetup
{
    public const string BalancePath = "Assets/Resources/World/PortalTowerBalance.asset";
    public const string PrefabPath = "Assets/Prefabs/UI/Portal Tower Progression.prefab";
    static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    static Sprite Icon(string name) => Sprite("Assets/Sprites/Ui/UnitStats/" + name + ".png");
    static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var balance = AssetDatabase.LoadAssetAtPath<PortalTowerBalance>(BalancePath);
        if (balance == null)
        {
            balance = ScriptableObject.CreateInstance<PortalTowerBalance>();
            balance.levelPointIcon = Sprite("Assets/Sprites/Ui/Experience Star.asset");
            void Add(string id, string ru, string en, string desc, string eng, PortalTowerBalance.Effect effect, float value, int max, bool attack, Sprite icon)
                => balance.upgrades.Add(new PortalTowerBalance.Upgrade { id=id, title=ru, englishTitle=en, description=desc, englishDescription=eng,
                    effect=effect, value=value, maximumRank=max, requiresAttack=attack, icon=icon });
            Add("attack", "Магические стрелы", "Magic bolts", "Кристалл башни портала начинает стрелять по врагам.", "The portal tower crystal starts firing at enemies.", PortalTowerBalance.Effect.UnlockAttack, 1, 1, false, Icon("Magic"));
            Add("lights", "Лучи света", "Light beams", "Добавляет один луч света. Максимум — 6 лучей. Новый луч доступен сразу.", "Adds one light beam, available immediately. Maximum: 6 beams.", PortalTowerBalance.Effect.ExtraLight, 1, 5, false, Icon("Magic"));
            Add("damage", "Мощный кристалл", "Powerful crystal", "Каждое улучшение добавляет 25% базового урона башни. Можно улучшать без ограничения.", "Each upgrade adds 25% of the tower's base damage. Unlimited upgrades.", PortalTowerBalance.Effect.Damage, .25f, 0, true, Icon("Sword"));
            Add("speed", "Быстрая стрельба", "Rapid fire", "Каждое улучшение добавляет 20% к скорости атаки. До пяти улучшений.", "Each upgrade adds 20% attack speed. Up to five upgrades.", PortalTowerBalance.Effect.AttackSpeed, .2f, 5, true, Icon("Hourglass"));
            Add("projectiles", "Магический залп", "Magic volley", "Добавляет один снаряд в каждый выстрел. До четырёх снарядов одновременно.", "Adds one projectile to each shot. Up to four projectiles per volley.", PortalTowerBalance.Effect.Projectiles, 1, 3, true, Icon("Magic"));
            Add("range", "Дальний удар", "Long shot", "Каждое улучшение добавляет 15% базовой дальности стрельбы. До трёх улучшений.", "Each upgrade adds 15% of the base attack range. Up to three upgrades.", PortalTowerBalance.Effect.Range, .15f, 3, true, Icon("Eye"));
            AssetDatabase.CreateAsset(balance, BalancePath);
        }
        AddLocalization();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) CreatePrefab(balance);
        ConfigureExperienceFlight();
        var hud = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/Screens/World Screen HUD.prefab");
        try
        {
            if (hud.GetComponentInChildren<PortalTowerProgression>(true) == null)
            {
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), hud.transform);
                PrefabUtility.SaveAsPrefabAsset(hud, "Assets/Prefabs/UI/Screens/World Screen HUD.prefab");
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(hud); }
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/World.unity") throw new InvalidOperationException("Open World to install the scene instance.");
        if (Object.FindFirstObjectByType<PortalTowerProgression>(FindObjectsInactive.Include) == null)
        {
            var canvas = GameObject.Find("Canvas").transform;
            var instance = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), canvas);
            Undo.RegisterCreatedObjectUndo(instance, "Portal tower progression");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        // These effects are now bought exclusively during an expedition.
        var table = Resources.Load<ScientificUpgradeTable>("ScientificUpgradeTable");
        table.entries.RemoveAll(e => e.id == ScientificUpgrades.PortalArrows || ScientificUpgrades.Flashlights.Contains(e.id));
        foreach (var e in table.entries) e.requiredPurchases = Mathf.Min(e.requiredPurchases, table.entries.Count - 1);
        EditorUtility.SetDirty(table);
        LaboratoryListSetup.RefreshPopupCatalog(table);
        AssetDatabase.SaveAssets();
        Selection.activeObject = balance;
        Debug.Log("Portal tower progression installed: XP HUD, mandatory laboratory-style window, six upgrades; permanent laboratory catalog updated.");
    }
    static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 pos)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(parent, false);
        rt.sizeDelta = size; rt.anchoredPosition = pos; return rt;
    }
    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
    static UnityEngine.UI.Image Image(RectTransform rt, Color color, Sprite sprite = null)
    {
        var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.sprite = sprite;
        image.raycastTarget = false; if (sprite != null) image.type = UnityEngine.UI.Image.Type.Sliced; return image;
    }
    static void Localize(Text text, string key, string value)
    {
        text.text = value;
        var component = text.GetComponent<LocalizedText>();
        if (component == null) component = text.gameObject.AddComponent<LocalizedText>();
        SetString(component, "key", key);
    }
    static void SetString(Object target, string field, string value)
    {
        var so = new SerializedObject(target); so.FindProperty(field).stringValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void CreatePrefab(PortalTowerBalance balance)
    {
        var root = Rect("Portal Tower Progression", null, Vector2.zero, Vector2.zero); Stretch(root);
        var progression = root.gameObject.AddComponent<PortalTowerProgression>(); Set(progression, "balance", balance);
        var bar = Rect("Tower Experience", root, new Vector2(454, 30), new Vector2(0, -122));
        bar.anchorMin = bar.anchorMax = new Vector2(.5f, 1); bar.pivot = new Vector2(.5f, 1);
        Image(bar, Color.white, Sprite("Assets/Sprites/UI/Evolution/Popups/Sprites/Shared/PopupPanel9Slice.png"));
        var track = Rect("Track", bar, Vector2.zero, Vector2.zero); Stretch(track); track.offsetMin = new Vector2(5, 5); track.offsetMax = new Vector2(-5, -5);
        Image(track, new Color(.10f, .075f, .13f));
        var fillRect = Rect("Experience Fill", track, Vector2.zero, Vector2.zero); Stretch(fillRect);
        var fill = Image(fillRect, new Color(.47f, .36f, .63f)); fill.type = UnityEngine.UI.Image.Type.Simple; fill.fillAmount = 0;
        var labelRect = Rect("Experience Label", bar, Vector2.zero, Vector2.zero); Stretch(labelRect);
        var label = labelRect.gameObject.AddComponent<Text>(); label.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pixellari Cyrillic/Pixellari-Cyrillic.ttf");
        label.fontSize = 20; label.color = new Color(.94f,.91f,.82f); label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
        label.text = "Башня портала  ур.1   0 / 12";
        var xp = bar.gameObject.AddComponent<PortalTowerExperienceBar>(); xp.progression = progression; xp.fill = fill; xp.label = label;
        var window = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(LaboratoryListSetup.PopupPath), root);
        window.name = "Portal Tower Upgrades";
        Object.DestroyImmediate(window.GetComponent<PopupDimmerLink>());
        var windowRect = (RectTransform)window.transform; Stretch(windowRect);
        var canvas = window.AddComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = 2000;
        window.AddComponent<GraphicRaycaster>();
        window.AddComponent<CanvasGroup>().ignoreParentGroups = true;
        var backing = window.GetComponent<UnityEngine.UI.Image>(); backing.color = new Color(0, 0, 0, .8f); backing.raycastTarget = true;
        var view = window.GetComponent<LaboratoryUpgradeList>(); view.stats = null; view.portalProgression = progression;
        foreach (var row in window.GetComponentsInChildren<LaboratoryUpgradeRow>(true)) Object.DestroyImmediate(row.gameObject);
        foreach (var t in window.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Close") { Object.DestroyImmediate(t.gameObject); continue; }
            var text = t.GetComponent<Text>(); if (text == null) continue;
            if (t.name == "Title") Localize(text, "portal_tower.title", "ПРОКАЧКА БАШНИ ПОРТАЛА");
            if (t.name == "Unlock Hint") Localize(text, "portal_tower.mandatory", "Выберите улучшение, чтобы продолжить забег");
            if (t.name == "Detail Caption") Localize(text, "portal_tower.upgrade", "УЛУЧШЕНИЕ БАШНИ");
        }
        foreach (var upgrade in balance.upgrades)
        {
            var row = Object.Instantiate(view.rowPrefab, view.content); row.name = upgrade.id; row.groupId = upgrade.id;
            foreach (var duplicate in row.GetComponentsInChildren<UnifiedButtonFeedback>(true).GroupBy(c => c.gameObject))
                foreach (var extra in duplicate.Skip(1)) Object.DestroyImmediate(extra);
            foreach (var duplicate in row.GetComponentsInChildren<ActionButtonLabelColor>(true).GroupBy(c => c.gameObject))
                foreach (var extra in duplicate.Skip(1)) Object.DestroyImmediate(extra);
            var icon = row.lockIcon; ResourceIconSizing.Apply(icon, balance.levelPointIcon);
        }
        Set(progression, "upgradeWindow", window);
        view.Refresh(); Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(view.content);
        window.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath); Object.DestroyImmediate(root.gameObject);
    }
    static void AddLocalization()
    {
        var table = Resources.Load<LocalizationTable>("LocalizationTable");
        if (table == null) table = AssetDatabase.FindAssets("t:LocalizationTable").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<LocalizationTable>).First();
        void Add(string key, string ru, string en)
        {
            var e = table.entries.Find(x => x.key == key);
            if (e == null) { e = new LocalizationTable.Entry { key = key }; table.entries.Add(e); }
            while (e.values.Count < table.languages.Count) e.values.Add("");
            e.values[table.languages.IndexOf("ru")] = ru; e.values[table.languages.IndexOf("en")] = en;
        }
        Add("portal_tower.title", "ПРОКАЧКА БАШНИ ПОРТАЛА", "PORTAL TOWER UPGRADES");
        Add("portal_tower.mandatory", "Выберите улучшение, чтобы продолжить забег", "Choose an upgrade to continue the expedition");
        Add("portal_tower.upgrade", "УЛУЧШЕНИЕ БАШНИ", "TOWER UPGRADE");
        Add("portal_tower.experience", "Башня портала", "Portal tower");
        EditorUtility.SetDirty(table);
    }
    public const string CrystalPath = "Assets/Prefabs/Effects/Portal Experience Crystal.prefab";
    public static void ConfigureExperienceFlight()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Effects")) AssetDatabase.CreateFolder("Assets/Prefabs", "Effects");
        var crystal = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalPath);
        if (crystal == null)
        {
            var root = new GameObject("Portal Experience Crystal", typeof(SpriteRenderer), typeof(PortalExperienceCrystal));
            root.GetComponent<SpriteRenderer>().sprite = Sprite("Assets/Sprites/Ui/Icons/Energy Crystal.png");
            crystal = PrefabUtility.SaveAsPrefabAsset(root, CrystalPath); Object.DestroyImmediate(root);
        }
        var progressionRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Set(progressionRoot.GetComponent<PortalTowerProgression>(), "experienceCrystalPrefab", crystal.GetComponent<PortalExperienceCrystal>());
            PrefabUtility.SaveAsPrefabAsset(progressionRoot, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(progressionRoot); }
        AssetDatabase.SaveAssets();
    }
}
#endif
