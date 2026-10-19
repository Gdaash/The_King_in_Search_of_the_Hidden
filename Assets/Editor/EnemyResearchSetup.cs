#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using GameFoundation.Bestiary;
using GameFoundation.Localization;
using System.Linq;

public static class EnemyResearchSetup
{
    public static void Apply()
    {
        foreach (string path in new[] {"Assets/Prefabs/Base/Bestiary Entry.prefab", "Assets/Prefabs/Base/Magic Library Popup.prefab"})
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var entry in root.GetComponentsInChildren<BestiaryEntryView>(true))
                {
                    var so = new SerializedObject(entry);
                    var role = (UnityEngine.UI.Text)so.FindProperty("role").objectReferenceValue;
                    var offset = role.rectTransform.offsetMax; offset.x = -104; role.rectTransform.offsetMax = offset;
                    var badge = entry.GetComponentInChildren<EnemyResearchBadge>(true);
                    if (badge == null)
                    {
                        var go = new GameObject("Enemy Research", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(EnemyResearchBadge));
                        go.transform.SetParent(entry.transform, false);
                        var rect = (RectTransform)go.transform;
                        rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
                        rect.pivot = new Vector2(1, 0); rect.anchoredPosition = new Vector2(-12, 5); rect.sizeDelta = new Vector2(84, 26);
                        var image = go.GetComponent<UnityEngine.UI.Image>();
                        image.sprite = entry.GetComponent<UnityEngine.UI.Image>().sprite; image.type = UnityEngine.UI.Image.Type.Sliced;
                        image.color = new Color(.19f, .15f, .22f, 1);
                        var textObject = new GameObject("Progress", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
                        textObject.transform.SetParent(go.transform, false);
                        var textRect = (RectTransform)textObject.transform; textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                        textRect.offsetMin = new Vector2(4, 0); textRect.offsetMax = new Vector2(-4, 0);
                        var text = textObject.GetComponent<UnityEngine.UI.Text>(); text.font = role.font; text.fontSize = 18;
                        text.alignment = TextAnchor.MiddleCenter; text.text = "0 / 50"; text.color = new Color(.95f, .83f, .49f); text.raycastTarget = false;
                        badge = go.GetComponent<EnemyResearchBadge>(); badge.label = text;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        var e = table.entries.FirstOrDefault(x => x.key == "bestiary.research.weakness");
        if (e == null) { e = new LocalizationTable.Entry { key = "bestiary.research.weakness" }; table.entries.Add(e); }
        while (e.values.Count < table.languages.Count) e.values.Add("");
        e.values[table.languages.IndexOf("ru")] = "Уязвимость найдена!\nВесь урон по этому монстру увеличен на 20%.";
        e.values[table.languages.IndexOf("en")] = "Weakness discovered!\nAll damage taken by this monster is increased by 20%.";
        EditorUtility.SetDirty(table); AssetDatabase.SaveAssets();
        Debug.Log("ENEMY_RESEARCH_UI_OK: editable badges and RU/EN weakness tooltip installed.");
    }
}
#endif
