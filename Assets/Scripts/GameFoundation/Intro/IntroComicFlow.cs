using GameFoundation.Saves;
using UnityEngine;

namespace GameFoundation.Intro
{
    public static class IntroComicFlow
    {
        public const string SceneName = "IntroComic";
        public const string PendingSaveKey = "foundation.intro.pending";
        private static string destinationScene = "Base";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetDestination() => destinationScene = "Base";

        /// <summary>Called after selecting the slot; isNewSlot must be captured before Select creates it.</summary>
        public static string Prepare(bool isNewSlot, string gameScene)
        {
            destinationScene = gameScene;
            if (isNewSlot)
            {
                SaveSlotPrefs.SetInt(PendingSaveKey, 1);
                SaveSlotPrefs.Save();
            }

            // Missing key means an existing, possibly legacy save. Never replay its introduction.
            return SaveSlotPrefs.HasKey(PendingSaveKey) && SaveSlotPrefs.GetInt(PendingSaveKey) == 1
                ? SceneName : gameScene;
        }

        public static string Complete()
        {
            // Opening the scene directly in the Editor must not mark the user's save as new.
            if (SaveSlotPrefs.HasKey(PendingSaveKey) && SaveSlotPrefs.GetInt(PendingSaveKey) == 1)
            {
                SaveSlotPrefs.SetInt(PendingSaveKey, 0);
                SaveSlotPrefs.Save();
            }
            return destinationScene;
        }
    }
}
