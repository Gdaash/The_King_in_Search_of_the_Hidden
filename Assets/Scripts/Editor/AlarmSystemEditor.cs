#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(AlarmSystem))]
public sealed class AlarmSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var alarm=(AlarmSystem)target;
        if(alarm.DifficultyTable==null){EditorGUILayout.HelpBox("Назначьте таблицу волн. Данные волн хранятся только в ScriptableObject.",MessageType.Warning);return;}
    }
}
#endif
