using GameFoundation.Saves;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GlobalStats))]
internal sealed class GlobalStatsEditor : Editor
{
    private bool _showUpgrades;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var stats = (GlobalStats)target;
        var usesAxes = serializedObject.FindProperty("applySharpAxes").boolValue;
        if (usesAxes)
        {
            bool purchased = stats.HasUpgrade(ScientificUpgrades.SharpAxes);
            float multiplier = purchased ? stats.FindUpgradeDefinition(ScientificUpgrades.SharpAxes)?.effectValue ?? 1f : 1f;
            EditorGUILayout.HelpBox(
                $"Базовое время задаётся в Duration таймера в префабе.\n" +
                $"Модификатор «Заточить топоры»: " +
                (purchased ? $"куплено, ×{multiplier:0.##}" : "не куплено") +
                $"; ячейка сохранения: {SaveSlotPrefs.SelectedSlot}.", MessageType.Info);
        }

        if(stats.usesPrefabCombatBases)
        {
            EditorGUILayout.HelpBox("Базовые HP, скорость и броня находятся в Combatant префаба; атака — в CombatWeapon; сопротивления — в CombatDefenseProfile. Здесь только модификаторы.",MessageType.Info);
            DrawPropertiesExcluding(serializedObject,"baseMaxHealth","baseSpeed","baseAttackCooldown","baseAttackRange","damageSettings","resistances");
            var damage=serializedObject.FindProperty("damageSettings");
            for(int i=0;i<damage.arraySize;i++){var d=damage.GetArrayElementAtIndex(i);EditorGUILayout.PropertyField(d.FindPropertyRelative("bonusDamage"),new GUIContent("Бонус урона "+(DamageType)d.FindPropertyRelative("type").enumValueIndex));}
            var resistance=serializedObject.FindProperty("resistances");
            for(int i=0;i<resistance.arraySize;i++){var r=resistance.GetArrayElementAtIndex(i);EditorGUILayout.PropertyField(r.FindPropertyRelative("bonusResist"),new GUIContent("Бонус защиты "+(DamageType)r.FindPropertyRelative("type").enumValueIndex));}
            serializedObject.ApplyModifiedProperties();
        }
        else DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Текущие параметры", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("Ячейка сохранения", SaveSlotPrefs.SelectedSlot);
            EditorGUILayout.FloatField(stats.usesPrefabCombatBases?"Бонус здоровья":"Здоровье", stats.usesPrefabCombatBases?stats.bonusHealth:stats.TotalMaxHealth);
            EditorGUILayout.FloatField("Сокращение цикла, с", stats.bonusProductionSpeed);
            EditorGUILayout.FloatField("Множитель времени производства", stats.ProductionTimeMultiplier);
            EditorGUILayout.FloatField("Множитель апгрейда открытия гекса", stats.HexOpeningTimeMultiplier);
            EditorGUILayout.FloatField("Снижение тревоги", stats.HexAlarmReduction);
            EditorGUILayout.Toggle("Может стрелять", stats.CanAttack);
            EditorGUILayout.IntField("Доступно лучей света", stats.AvailableFlashlightCount);
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
