using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace EditorTools
{
    public static class SceneContextAutoAdder
    {
        [MenuItem("Tools/Zenject/Add SceneContext to All Scenes")]
        public static void AddSceneContextToAllScenes()
        {
            // Получаем все сцены из Build Settings
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");
            
            int successCount = 0;
            int skipCount = 0;
            int errorCount = 0;

            foreach (var guid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                
                try
                {
                    // Открываем сцену
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    
                    // Проверяем, есть ли уже SceneContext на сцене
                    var sceneContexts = scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<SceneContext>())
                        .ToArray();

                    if (sceneContexts.Length > 0)
                    {
                        Debug.Log($"[SKIP] Сцена '{scene.name}' уже содержит SceneContext");
                        skipCount++;
                        continue;
                    }

                    // Создаём GameObject с SceneContext
                    var sceneContextGO = new GameObject("SceneContext");
                    sceneContextGO.AddComponent<SceneContext>();

                    // Помечаем сцену как изменённую
                    EditorSceneManager.MarkSceneDirty(scene);
                    
                    // Сохраняем сцену
                    EditorSceneManager.SaveScene(scene);
                    
                    Debug.Log($"[OK] Добавлен SceneContext в сцену '{scene.name}'");
                    successCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[ERROR] Ошибка обработки сцены '{scenePath}': {e.Message}");
                    errorCount++;
                }
            }

            // Выводим итог
            Debug.Log($"\n===== РЕЗУЛЬТАТ =====");
            Debug.Log($"Успешно: {successCount}");
            Debug.Log($"Пропущено (уже есть SceneContext): {skipCount}");
            Debug.Log($"Ошибок: {errorCount}");
            Debug.Log($"=====================\n");
        }
    }
}
