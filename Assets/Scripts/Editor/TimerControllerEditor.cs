using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TimerController))]
internal sealed class TimerControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var stats = serializedObject.FindProperty("stats").objectReferenceValue as GlobalStats;
        float effectiveDuration = stats != null
            ? stats.TotalProductionTime
            : serializedObject.FindProperty("duration").floatValue;
        EditorGUILayout.HelpBox(
            $"ДЛИТЕЛЬНОСТЬ ЦИКЛА: {effectiveDuration:0.##} с. " +
            (stats != null ? $"Берётся из GlobalStats «{stats.name}». Поле Duration ниже запасное."
                : "Берётся из локального поля Duration."), MessageType.Info);

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
