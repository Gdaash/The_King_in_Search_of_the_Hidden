using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TimerController))]
internal sealed class TimerControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var stats = serializedObject.FindProperty("stats").objectReferenceValue as GlobalStats;
        float baseDuration = serializedObject.FindProperty("duration").floatValue;
        float effectiveDuration = stats != null ? stats.ApplyProductionTimeModifiers(baseDuration) : Mathf.Max(.2f, baseDuration);
        EditorGUILayout.HelpBox(
            $"БАЗОВОЕ ВРЕМЯ ИЗ ПРЕФАБА: {baseDuration:0.##} с.\n" +
            $"С УЛУЧШЕНИЯМИ: {effectiveDuration:0.##} с. " +
            (stats != null ? $"Модификаторы из GlobalStats «{stats.name}»: −{stats.bonusProductionSpeed:0.##} с, ×{stats.ProductionTimeMultiplier:0.##}."
                : "Модификаторы не назначены."), MessageType.Info);

        if (Application.isPlaying)
        {
            var timer = (TimerController)target;
            EditorGUILayout.HelpBox(
                $"ФАКТИЧЕСКИЙ ЗАПУЩЕННЫЙ ЦИКЛ: {timer.ActiveCycleDuration:0.##} с. " +
                $"ОСТАЛОСЬ: {timer.TimeRemaining:0.##} с. " +
                "Здесь уже учтены игровые улучшения.", MessageType.None);
        }

        DrawDefaultInspector();
    }

    public override bool RequiresConstantRepaint() => true;
}
