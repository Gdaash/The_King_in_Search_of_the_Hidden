#if UNITY_EDITOR
using GameFoundation.Audio;
using UnityEditor;
using UnityEngine;

public static class QuestAudioSetup
{
    public static void Apply()
    {
        foreach(var name in new[]{"Dialogue_Word","Quest_Complete","Quest_Fly"})
        {
            var importer=(AudioImporter)AssetImporter.GetAtPath("Assets/Audio/SFX/"+name+".wav");
            importer.forceToMono=true;
            var settings=importer.defaultSampleSettings;settings.preloadAudioData=true;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
        }
        var lib=AssetDatabase.LoadAssetAtPath<GameAudioLibrary>("Assets/Resources/GameAudioLibrary.asset");
        lib.dialogueWord=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Dialogue_Word.wav");
        lib.dialogueOpen=lib.uiOpen;lib.dialogueNext=lib.uiClick;lib.dialogueClose=lib.uiClick;
        lib.questAccept=lib.uiConfirm;lib.questComplete=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Quest_Complete.wav");lib.questReward=lib.resourceGain;lib.contentUnlock=lib.uiConfirm;lib.questFly=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Quest_Fly.wav");
        lib.refugeesArrival=lib.uiOpen;lib.refugeeAdmit=lib.uiConfirm;lib.recruitWarrior=lib.uiConfirm;lib.disarmWarrior=lib.uiClick;
        foreach(GameAudioCue cue in System.Enum.GetValues(typeof(GameAudioCue)))if(lib.Get(cue)==null)throw new System.Exception("Missing cue: "+cue);
        EditorUtility.SetDirty(lib);AssetDatabase.SaveAssets();Debug.Log("Quest audio configured; all cues have clips.");
    }
}
#endif
