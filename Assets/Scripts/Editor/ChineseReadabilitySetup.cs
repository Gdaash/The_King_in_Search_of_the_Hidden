using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using GameFoundation.Localization;

public static class ChineseReadabilitySetup
{
    public static string Apply()
    {
        const string fontPath = "Assets/Fonts/Chinese/NotoSansCJKsc-Bold.otf";
        const string assetPath = "Assets/Fonts/Chinese/Noto Sans CJK Bold UI.asset";
        var source = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (source == null) throw new System.Exception("Chinese font is missing");
        var bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (bold == null)
        {
            bold = TMP_FontAsset.CreateFontAsset(source, 48, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            bold.name = "Noto Sans CJK Bold UI";
            AssetDatabase.CreateAsset(bold, assetPath);
            AssetDatabase.AddObjectToAsset(bold.material, bold);
            foreach (var atlas in bold.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, bold);
        }
        bold.fallbackFontAssetTable = new List<TMP_FontAsset>();
        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets/Fonts" }))
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (font == bold) continue;
            var fallbacks = font.fallbackFontAssetTable ?? new List<TMP_FontAsset>();
            fallbacks.Remove(bold);
            int index = fallbacks.FindIndex(f => f != null && (f.name == "Chinese UI" || f.name == "Unifont Last Resort"));
            fallbacks.Insert(index < 0 ? fallbacks.Count : index, bold);
            font.fallbackFontAssetTable = fallbacks;
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssetIfDirty(font);
        }
        var serialized = new SerializedObject(bold);
        serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(bold);
        AssetDatabase.SaveAssetIfDirty(bold);
        foreach (string guid in AssetDatabase.FindAssets("t:Font", new[] { "Assets/Fonts" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == fontPath) continue;
            var importer = AssetImporter.GetAtPath(path) as TrueTypeFontImporter;
            if (importer == null) continue;
            var references = importer.fontReferences.Where(f => f != null && f != source).ToList();
            int index = references.FindIndex(f => f.name.Contains("Unifont"));
            references.Insert(index < 0 ? references.Count : index, source);
            importer.fontReferences = references.ToArray();
            var names = importer.fontNames.Where(name => !source.fontNames.Contains(name)).ToList();
            int nameIndex = names.FindIndex(name => name.Contains("Unifont"));
            names.InsertRange(nameIndex < 0 ? names.Count : nameIndex, source.fontNames);
            importer.fontNames = names.ToArray();
            EditorUtility.SetDirty(importer);
            AssetDatabase.WriteImportSettingsIfDirty(path);
            importer.SaveAndReimport();
        }
        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        var entry = table.entries.Find(e => e.key == "dialogue.hold");
        if (entry == null) { entry = new LocalizationTable.Entry { key = "dialogue.hold" }; table.entries.Add(entry); }
        var values = new Dictionary<string, string> {
            {"ru", "Зажми"}, {"en", "Hold"}, {"fr", "Maintenir"}, {"zh-Hans", "按住"},
            {"it", "Tieni premuto"}, {"de", "Gedrückt halten"}, {"es-ES", "Mantén pulsado"}, {"pt-BR", "Segure"}
        };
        entry.values = table.languages.Select(language => values[language]).ToList();
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssetIfDirty(table);
        return "Chinese bold fallback installed; dialogue.hold translated in all eight languages";
    }
}
