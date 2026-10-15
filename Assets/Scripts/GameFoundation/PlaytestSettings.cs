using UnityEngine;

namespace GameFoundation
{
    /// <summary>Local Editor preferences, frozen for the duration of each play session.</summary>
    public static class PlaytestSettings
    {
#if UNITY_EDITOR
        public static string PreferenceKey => "ThoseUnderHex.Playtest.DisableTutorialsAndQuests." + Application.dataPath;
        private static bool disabledForRun;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() => disabledForRun = UnityEditor.EditorPrefs.GetBool(PreferenceKey, false);

        public static bool TutorialsAndQuestsDisabled => Application.isPlaying
            ? disabledForRun : UnityEditor.EditorPrefs.GetBool(PreferenceKey, false);
#else
        public static bool TutorialsAndQuestsDisabled => false;
#endif
    }
}
