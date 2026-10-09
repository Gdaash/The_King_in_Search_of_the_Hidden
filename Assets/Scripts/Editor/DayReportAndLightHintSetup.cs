#if UNITY_EDITOR
using System;
using System.Linq;
using GameFoundation.Base;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class DayReportAndLightHintSetup
{
    public static void Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        MergeShelterColumns("Assets/Prefabs/Base/Variants/Base Islands Backup.prefab");
        MergeShelterColumns("Assets/Prefabs/Base/Variants/Base Location Variant.prefab");
        InstallStartHint();
        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        Translate(table, "world.lights.start_work", "Запустить работу", "Start work");
        Translate(table, "world.lights.manual_mode_hint", "Автоповтор выключен. ЛКМ по гексу с лучом запускает работу.",
            "Auto-repeat is off. Left-click a lit hex to start work.");
        Translate(table, "report.shelter", "Укрытие", "Shelter");
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssetIfDirty(table);
    }

    static void MergeShelterColumns(string path)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (var popup in root.GetComponentsInChildren<DayReportPopup>(true))
            {
                var row = (DayReportRow)new SerializedObject(popup).FindProperty("rowTemplate").objectReferenceValue;
                var shelter = (TMP_Text)new SerializedObject(row).FindProperty("shelterLabel").objectReferenceValue;
                shelter.name = "Shelter";
                foreach (var label in popup.GetComponentsInChildren<TMP_Text>(true))
                {
                    switch (label.name)
                    {
                        case "BaseSpent": Object.DestroyImmediate(label.gameObject); break;
                        case "Start": Place(label, -200, 140); break;
                        case "Run": Place(label, 40, 190); break;
                        case "End": Place(label, 515, 240); break;
                        case "BaseGain":
                        case "Shelter":
                            label.name = "Shelter";
                            Place(label, 260, 210);
                            if (label != shelter)
                            {
                                label.text = "Укрытие";
                                var localized = label.GetComponent<LocalizedText>() ?? label.gameObject.AddComponent<LocalizedText>();
                                var serialized = new SerializedObject(localized);
                                serialized.FindProperty("key").stringValue = "report.shelter";
                                serialized.ApplyModifiedPropertiesWithoutUndo();
                            }
                            break;
                    }
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void Place(TMP_Text label, float x, float width)
    {
        label.rectTransform.anchoredPosition = new Vector2(x, label.rectTransform.anchoredPosition.y);
        label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    public static void InstallStartHint()
    {
        var mouse = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Resources/LBM.png");
        if (mouse == null) throw new InvalidOperationException("Left mouse sprite is missing.");
        var root = PrefabUtility.LoadPrefabContents(CrystalHexHoverSetup.PrefabPath);
        try
        {
            var serialized = new SerializedObject(root.GetComponent<CrystalHexHover>());
            var views = serialized.FindProperty("views");
            for (int i = 0; i < views.arraySize; i++)
            {
                var view = views.GetArrayElementAtIndex(i);
                var row = (GameObject)view.FindPropertyRelative("startRow").objectReferenceValue;
                if (row == null)
                {
                    var recall = (GameObject)view.FindPropertyRelative("recallRow").objectReferenceValue;
                    row = Object.Instantiate(recall, recall.transform.parent);
                    row.name = "Start Work";
                    row.transform.SetSiblingIndex(recall.transform.GetSiblingIndex());
                }
                var icon = row.GetComponentInChildren<Image>(true);
                icon.name = "LBM";
                icon.sprite = mouse;
                icon.raycastTarget = false;
                icon.rectTransform.sizeDelta = mouse.rect.size * 2;
                var sizing = icon.GetComponent<LayoutElement>();
                sizing.minWidth = sizing.preferredWidth = mouse.rect.width * 2;
                sizing.minHeight = sizing.preferredHeight = mouse.rect.height * 2;
                var label = row.GetComponentInChildren<TMP_Text>(true);
                label.name = "Start Label";
                label.text = "Запустить работу";
                row.SetActive(false);
                view.FindPropertyRelative("startRow").objectReferenceValue = row;
                view.FindPropertyRelative("startCaption").objectReferenceValue = label;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, CrystalHexHoverSetup.PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void Translate(LocalizationTable table, string key, string ru, string en)
    {
        var entry = table.entries.FirstOrDefault(e => e.key == key);
        if (entry == null) { entry = new LocalizationTable.Entry { key = key }; table.entries.Add(entry); }
        entry.values = new() { ru, en };
    }
}
#endif
