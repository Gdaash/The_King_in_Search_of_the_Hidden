#if UNITY_EDITOR
using GameFoundation.Quests;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestDialogueDefinition))]
public sealed class QuestDialogueDefinitionEditor : Editor
{
    Editor questEditor;
    bool showQuest = true;
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Дублируйте префаб разговора. Задайте уникальный Id, персонажа, реплики и задание. Добавьте префаб в Conversations у Quest Dialogue на базе. Последняя реплика выдаёт задание по кнопке.", MessageType.Info);
        DrawDefaultInspector();
        var d = (QuestDialogueDefinition)target;
        if(d.quest==null)return;
        showQuest=EditorGUILayout.Foldout(showQuest,"Условия и награда задания",true);
        if(showQuest){Editor.CreateCachedEditor(d.quest,null,ref questEditor);questEditor.OnInspectorGUI();}
    }
    void OnDisable(){if(questEditor!=null)DestroyImmediate(questEditor);}
}
#endif
