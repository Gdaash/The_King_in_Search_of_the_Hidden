#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using GameFoundation.Localization;

public static class DirectedWavesSetup
{
    [MenuItem("Tools/Game Foundation/World/Configure Directed Waves")]
    public static void Apply()
    {
        var table = AssetDatabase.LoadAssetAtPath<AlarmDifficultyTable>("Assets/Resources/World/AlarmDifficultyTable.asset");
        foreach (var difficulty in table.difficulties)
        foreach (var threshold in difficulty.thresholds)
        {
            threshold.waveCountdown = 35;
            if (threshold.waves.Count > 0) continue;
            var enemies = threshold.enemies.Where(e => e.prefab != null && e.weight > 0).ToArray();
            if (enemies.Length == 0) continue;
            for (int variant = 0; variant < 3; variant++)
            {
                int total = Mathf.Max(2, variant == 0 ? threshold.minEnemiesPerWave : variant == 1
                    ? (threshold.minEnemiesPerWave + threshold.maxEnemiesPerWave) / 2 : threshold.maxEnemiesPerWave);
                if (enemies.Length == 1) total = threshold.minEnemiesPerWave + variant;
                var wave = new AlarmWaveComposition { name = "Danger " + threshold.alarmValue + " / " + (variant + 1), groups = threshold.alarmValue >= 100 ? 2 : 1 };
                var counts = new int[enemies.Length];
                // Deliberate compositions: alternate the main threat rather than rolling every enemy.
                int focus = variant % enemies.Length;
                for (int i = 0; i < total; i++)
                    counts[i % 3 == 0 ? (i / 3 + variant) % enemies.Length : focus]++;
                for (int i = 0; i < counts.Length; i++)
                    if (counts[i] > 0) wave.units.Add(new AlarmWaveUnit { prefab = enemies[i].prefab, count = counts[i] });
                threshold.waves.Add(wave);
            }
        }
        EditorUtility.SetDirty(table);
        var localization = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        Add(localization, "world.wave.countdown", "До волны: ", "Next wave: ");
        Add(localization, "world.wave.attacking", "Атака", "Attack");
        EditorUtility.SetDirty(localization);
        AssetDatabase.SaveAssets();
    }
    private static void Add(LocalizationTable table, string key, string ru, string en)
    {
        var entry = table.entries.FirstOrDefault(e => e.key == key);
        if (entry == null) { entry = new LocalizationTable.Entry { key = key }; table.entries.Add(entry); }
        while (entry.values.Count < table.languages.Count) entry.values.Add("");
        entry.values[table.languages.IndexOf("ru")] = ru;
        entry.values[table.languages.IndexOf("en")] = en;
    }

    [MenuItem("Tools/Game Foundation/World/Setup Provoke Wave Button")]
    public static void SetupProvokeButton()
    {
        const string path = "Assets/Prefabs/UI/HUD/Enemy Roster.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponent<GameFoundation.UI.EnemyRosterView>();
            var title = root.GetComponentsInChildren<UnityEngine.UI.Text>(true).First(t => t.name == "Title");
            var existing = root.transform.Find("Provoke Wave");
            var go = existing != null ? existing.gameObject : new GameObject("Provoke Wave", typeof(RectTransform));
            go.transform.SetParent(root.transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -100); rect.sizeDelta = new Vector2(-24, 72);
            var image = go.GetComponent<UnityEngine.UI.Image>() ?? go.AddComponent<UnityEngine.UI.Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Evolution/Popups/Sprites/Shared/PopupButton9Slice.png");
            image.type = UnityEngine.UI.Image.Type.Sliced;
            var button = go.GetComponent<UnityEngine.UI.Button>() ?? go.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(.75f, .75f, .75f); colors.disabledColor = new Color(.5f, .5f, .5f, .8f); button.colors = colors;
            var label = ProvokeText(go.transform, "Label", title.font, 20);
            label.text = "Спровоцировать нападение"; label.color = new Color(.65f, .85f, .47f);
            label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(10, 6); label.rectTransform.offsetMax = new Vector2(-70, -6);
            var reward = ProvokeText(go.transform, "Reward", title.font, 20);
            reward.text = "+0"; reward.color = new Color(1, .85f, .45f); reward.alignment = TextAnchor.MiddleRight;
            var rr = reward.rectTransform; rr.anchorMin = rr.anchorMax = new Vector2(1, .5f);
            rr.pivot = new Vector2(1, .5f); rr.sizeDelta = new Vector2(32, 36); rr.anchoredPosition = new Vector2(-38, 0);
            var iconGo = go.transform.Find("Crystal")?.gameObject ?? new GameObject("Crystal", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            var icon = iconGo.GetComponent<UnityEngine.UI.Image>() ?? iconGo.AddComponent<UnityEngine.UI.Image>();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/Portal Experience Crystal.prefab").GetComponent<SpriteRenderer>().sprite;
            ResourceIconSizing.Apply(icon, source); icon.raycastTarget = false;
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(1, .5f);
            icon.rectTransform.anchoredPosition = new Vector2(-20, 0);
            var widget = go.GetComponent<GameFoundation.UI.WaveProvokeButton>() ?? go.AddComponent<GameFoundation.UI.WaveProvokeButton>();
            var so = new SerializedObject(widget);
            so.FindProperty("button").objectReferenceValue = button; so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("reward").objectReferenceValue = reward; so.FindProperty("crystalIcon").objectReferenceValue = icon; so.ApplyModifiedPropertiesWithoutUndo();
            so = new SerializedObject(view); so.FindProperty("provokeButton").objectReferenceValue = widget; so.ApplyModifiedPropertiesWithoutUndo();
            var tip = go.GetComponent<ResourceTooltipTrigger>() ?? go.AddComponent<ResourceTooltipTrigger>();
            tip.Initialize("Мгновенно вызывает волну врагов и даёт кристаллы: 1 за каждые полные 10 секунд оставшегося времени.", rect);
            so = new SerializedObject(tip); so.FindProperty("alwaysBelow").boolValue = true; so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        Add(table, "world.wave.provoke", "Спровоцировать нападение", "Provoke attack");
        Add(table, "world.wave.provoke.tooltip", "Мгновенно вызывает волну врагов и даёт кристаллы: 1 за каждые полные 10 секунд оставшегося времени.", "Immediately summons the enemy wave and grants crystals: 1 for every full 10 seconds remaining.");
        EditorUtility.SetDirty(table); AssetDatabase.SaveAssets();
    }
    private static UnityEngine.UI.Text ProvokeText(Transform parent, string name, Font font, int size)
    {
        var go = parent.Find(name)?.gameObject ?? new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var text = go.GetComponent<UnityEngine.UI.Text>() ?? go.AddComponent<UnityEngine.UI.Text>();
        text.font = font; text.fontSize = size; text.raycastTarget = false; text.resizeTextForBestFit = false;
        return text;
    }
}
#endif
