#if UNITY_EDITOR
using UnityEditor;
using GameFoundation;
public static class MilitaryLightTutorialDebug
{
    private const string Menu = "Tools/Playtest/Force Military Light Tutorial";
    [MenuItem(Menu)]
    public static void Toggle() => EditorPrefs.SetBool(PlaytestSettings.MilitaryTutorialDebugKey + "." + UnityEngine.Application.dataPath, !PlaytestSettings.ForceMilitaryLightTutorial);
    [MenuItem(Menu, true)]
    private static bool Validate() { UnityEditor.Menu.SetChecked(Menu, PlaytestSettings.ForceMilitaryLightTutorial); return true; }
}
#endif
