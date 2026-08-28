#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HexMapGenerator))]
public class HexMapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Рисуем все стандартные поля (префабы, настройки и т.д.)
        DrawDefaultInspector();

        HexMapGenerator generator = (HexMapGenerator)target;

        EditorGUILayout.Space(15);
        
        // Рисуем кнопки в одну строку
        EditorGUILayout.BeginHorizontal();
        
        // Кнопка генерации
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f); // Зеленоватый оттенок
        if (GUILayout.Button("🔨 Сгенерировать карту", GUILayout.Height(35)))
        {
            generator.GenerateMap();
        }
        
        // Кнопка очистки
        GUI.backgroundColor = new Color(0.8f, 0.4f, 0.4f); // Красноватый оттенок
        if (GUILayout.Button("🗑️ Очистить карту", GUILayout.Height(35)))
        {
            generator.ClearMap();
        }
        
        // Возвращаем стандартный цвет кнопок
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
    }
}
#endif