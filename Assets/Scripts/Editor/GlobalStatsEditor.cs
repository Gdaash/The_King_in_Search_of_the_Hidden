using GameFoundation.Saves;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GlobalStats))]
internal sealed class GlobalStatsEditor : Editor
{
    private bool _showUpgrades;

    public override void OnInspectorGUI()
    {
        var stats = (GlobalStats)target;
        var usesAxes = serializedObject.FindProperty("applySharpAxes").boolValue;
        if (usesAxes)
        {
            bool purchased = stats.HasUpgrade(ScientificUpgrades.SharpAxes);
            float multiplier = purchased ? stats.FindUpgradeDefinition(ScientificUpgrades.SharpAxes)?.effectValue ?? 1f : 1f;
            EditorGUILayout.HelpBox(
                $"ИТОГОВОЕ ВРЕМЯ: {stats.TotalProductionTime:0.##} с\n" +
                $"Базовое: {stats.baseProductionTime:0.##} с; «Заточить топоры»: " +
                (purchased ? $"куплено, ×{multiplier:0.##}" : "не куплено") +
                $"; ячейка сохранения: {SaveSlotPrefs.SelectedSlot}.", MessageType.Info);
        }

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Текущие параметры", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("Ячейка сохранения", SaveSlotPrefs.SelectedSlot);
            EditorGUILayout.FloatField("Здоровье", stats.TotalMaxHealth);
            EditorGUILayout.FloatField("Время производства", stats.TotalProductionTime);
            EditorGUILayout.FloatField("Множитель апгрейда открытия гекса", stats.HexOpeningTimeMultiplier);
            EditorGUILayout.FloatField("Снижение тревоги", stats.HexAlarmReduction);
            EditorGUILayout.Toggle("Может стрелять", stats.CanAttack);
            EditorGUILayout.IntField("Доступно фонарей", stats.AvailableFlashlightCount);
        }

        var progress = serializedObject.FindProperty("scientificProgressStats").objectReferenceValue as GlobalStats;
        var source = progress != null ? progress : stats;
        var table = new SerializedObject(source).FindProperty("scientificUpgradeTable").objectReferenceValue as ScientificUpgradeTable;
        if (table == null) return;

        _showUpgrades = EditorGUILayout.Foldout(_showUpgrades, "Покупки лаборатории", true);
        if (!_showUpgrades) return;
        using (new EditorGUI.DisabledScope(true))
        {
            foreach (var entry in table.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id)) continue;
                EditorGUILayout.ToggleLeft($"{entry.id} — {entry.title} ({entry.effectValue:g})", source.HasUpgrade(entry.id));
            }
        }
    }

    public override bool RequiresConstantRepaint() => true;
}
