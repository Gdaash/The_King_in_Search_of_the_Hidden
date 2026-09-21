using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GlobalResourceManager))]
public sealed class GlobalResourceManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("saveKeyPrefix"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("availableResources"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("initialValues"), true);
        serializedObject.ApplyModifiedProperties();

        var manager = (GlobalResourceManager)target;
        manager.RefreshDisplay();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Текущие значения", EditorStyles.boldLabel);

        var resources = manager.AvailableResources;
        if (resources == null) return;
        foreach (var resource in resources)
        {
            if (resource == null) continue;
            EditorGUI.BeginChangeCheck();
            int amount = EditorGUILayout.IntField(resource.name, manager.GetResourceAmount(resource));
            if (EditorGUI.EndChangeCheck())
            {
                manager.SetResourceAmount(resource, amount);
                EditorUtility.SetDirty(manager);
            }
        }
    }

    public override bool RequiresConstantRepaint() => true;
}
