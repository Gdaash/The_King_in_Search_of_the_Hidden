using GameFoundation.Quests;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestCatalog))]
public sealed class QuestCatalogEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Задания — отдельные ассеты. Порядок в списке определяет порядок выдачи. Цели учитывают ресурсы в запасе одновременно.", MessageType.Info);
        DrawDefaultInspector();
        if (GUILayout.Button("Создать задание и добавить в список"))
        {
            string path=EditorUtility.SaveFilePanelInProject("Новое задание","New Quest","asset","Выберите место для задания","Assets/Resources/Quests");
            if(string.IsNullOrEmpty(path))return;
            var quest=ScriptableObject.CreateInstance<QuestDefinition>();quest.id=System.Guid.NewGuid().ToString("N");quest.title="Новое задание";
            AssetDatabase.CreateAsset(quest,path);
            serializedObject.Update();var list=serializedObject.FindProperty("quests");int index=list.arraySize;list.arraySize++;list.GetArrayElementAtIndex(index).objectReferenceValue=quest;serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();Selection.activeObject=quest;
        }
    }
}

[CustomEditor(typeof(QuestDefinition))]
public sealed class QuestDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("Количество — требуемый запас. Пояснение цели и описание видны при наведении на панель. Ресурс задаётся прямой ссылкой на ResourceType.",MessageType.Info);
        using(new EditorGUI.DisabledScope(true))EditorGUILayout.PropertyField(serializedObject.FindProperty("id"),new GUIContent("Ключ сохранения"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("title"),new GUIContent("Заголовок"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"),new GUIContent("Подробное описание"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("requirements"),new GUIContent("Цели: ресурс, количество, пояснение"),true);
        serializedObject.ApplyModifiedProperties();
        var quest=(QuestDefinition)target;
        bool duplicate=false;
        foreach(var guid in AssetDatabase.FindAssets("t:QuestDefinition"))
        {
            var other=AssetDatabase.LoadAssetAtPath<QuestDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if(other!=quest && other.id==quest.id){duplicate=true;break;}
        }
        if(duplicate)
        {
            EditorGUILayout.HelpBox("Этот ключ сохранения уже используется другим заданием. Копии нужен новый ключ.",MessageType.Error);
            if(GUILayout.Button("Назначить новый ключ этой копии"))
            { Undo.RecordObject(quest,"New quest identity");quest.id=System.Guid.NewGuid().ToString("N");EditorUtility.SetDirty(quest); }
        }
        if(quest.requirements.Count==0)EditorGUILayout.HelpBox("Добавьте хотя бы одну цель.",MessageType.Warning);
        foreach(var goal in quest.requirements)if(goal==null||goal.resource==null||goal.amount<1)EditorGUILayout.HelpBox("Каждой цели нужны ресурс и положительное количество.",MessageType.Warning);
    }
}
