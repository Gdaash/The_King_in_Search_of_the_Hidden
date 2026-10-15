#if UNITY_EDITOR
using GameFoundation;
using UnityEditor;
using UnityEngine;

public static class PlaytestSettingsMenu
{
    private const string MenuPath = "Tools/Those UnderHex/Тестирование/Отключить обучение и задания";

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        bool disabled = !EditorPrefs.GetBool(PlaytestSettings.PreferenceKey, false);
        EditorPrefs.SetBool(PlaytestSettings.PreferenceKey, disabled);
        Menu.SetChecked(MenuPath, disabled);
        Debug.Log(disabled
            ? "Тестирование: обучение, диалоги заданий и задания отключены для следующих запусков Play Mode."
            : "Тестирование: обучение и задания включены для следующих запусков Play Mode.");
    }

    [MenuItem(MenuPath, true)]
    private static bool Validate()
    {
        Menu.SetChecked(MenuPath, EditorPrefs.GetBool(PlaytestSettings.PreferenceKey, false));
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
#endif
