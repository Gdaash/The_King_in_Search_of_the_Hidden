#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GameFoundation.Quests;
using GameFoundation.Localization;

public static class QuestNarrativeRevision
{
    [Serializable] public class QuestCopy { public string path, id, title, description, status; public string[] purposes; }
    [Serializable] public class DialogueCopy { public string path, id, speaker; public string[] lines; }
    [Serializable] public class Copy { public QuestCopy[] quests; public DialogueCopy[] dialogues; public LocalizationTable.Entry[] localization; }
    static Copy Read() => JsonUtility.FromJson<Copy>(File.ReadAllText("Assets/Editor/QuestNarrativeCopy.json"));
    public static string[] Lines(string id, string[] fallback) => Read().dialogues.FirstOrDefault(d => d.id == id)?.lines ?? fallback;

    public static void Apply()
    {
        var copy = Read();
        foreach (var c in copy.quests)
        {
            var q = AssetDatabase.LoadAssetAtPath<QuestDefinition>(c.path);
            if (q == null || q.id != c.id || q.requirements.Count != c.purposes.Length)
                throw new InvalidOperationException("Quest mismatch: " + c.path);
            Undo.RecordObject(q, "Quest narrative revision");
            q.title = c.title; q.description = c.description; q.inProgressStatus = c.status;
            for (int i = 0; i < q.requirements.Count; i++) q.requirements[i].purpose = c.purposes[i];
            EditorUtility.SetDirty(q);
        }
        foreach (var c in copy.dialogues)
        {
            var root = PrefabUtility.LoadPrefabContents(c.path);
            try
            {
                var d = root.GetComponent<QuestDialogueDefinition>();
                if (d == null || d.id != c.id) throw new InvalidOperationException("Dialogue mismatch: " + c.path);
                d.speakerName = c.speaker; d.lines = c.lines;
                PrefabUtility.SaveAsPrefabAsset(root, c.path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        foreach (var path in new[] {"Assets/Prefabs/UI/Dialogue/First Run Light Tutorial.prefab", "Assets/Prefabs/UI/Dialogue/Quest Dialogue.prefab"})
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component == null) continue;
                    var so = new SerializedObject(component); var prop = so.GetIterator();
                    while (prop.NextVisible(true))
                        if (prop.propertyType == SerializedPropertyType.String)
                        {
                            if (prop.stringValue == "Старик Пью") prop.stringValue = "Стайк Пью";
                            else if (prop.stringValue == "Old Pew") prop.stringValue = "Stike Pew";
                        }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        if (!table.languages.SequenceEqual(new[] {"ru", "en"})) throw new InvalidOperationException("Unexpected locale order.");
        Undo.RecordObject(table, "Quest narrative localization");
        foreach (var incoming in copy.localization)
        {
            var existing = table.entries.FirstOrDefault(e => e.key == incoming.key);
            if (existing == null) table.entries.Add(incoming);
            else existing.values = incoming.values;
        }
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
        // Refresh the source-text translation cache if the editor is already running.
        typeof(GameText).GetField("cachedTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.SetValue(null, null);
        foreach (var c in copy.quests)
            foreach (var value in new[] {c.title, c.description, c.status}.Concat(c.purposes))
                if (!table.entries.Any(e => e.values[0] == value && !string.IsNullOrWhiteSpace(e.values[1])))
                    throw new InvalidOperationException("Missing translation: " + value);
        foreach (var c in copy.dialogues)
            foreach (var line in c.lines)
                if (!table.entries.Any(e => e.values[0] == line && !string.IsNullOrWhiteSpace(e.values[1])))
                    throw new InvalidOperationException("Missing dialogue translation.");
        Debug.Log("NARRATIVE_REVISION_OK: 6 quests, 6 conversations, RU/EN translations complete; mechanics unchanged.");
    }
}
#endif
