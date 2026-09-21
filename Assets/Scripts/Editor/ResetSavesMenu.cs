using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    public static class ResetSavesMenu
    {
        private const string MenuPath = "Tools/Сбросить сохранения";

        [MenuItem(MenuPath)]
        private static void ResetSaves()
        {
            if (!EditorUtility.DisplayDialog(
                    "Сбросить сохранения?",
                    "Будут удалены все PlayerPrefs этой игры: ресурсы, покупки, прогресс и настройки. Отменить действие нельзя.",
                    "Сбросить",
                    "Отмена"))
                return;

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("Сохранения PlayerPrefs сброшены. Запустите игру заново, чтобы обновить состояние сцены.");
        }

        [MenuItem(MenuPath, true)]
        private static bool CanResetSaves() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
