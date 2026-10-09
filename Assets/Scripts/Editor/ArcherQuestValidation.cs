#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using GameFoundation.UI;
using System.Linq;
using GameFoundation.Base;
using GameFoundation.Quests;
using GameFoundation.Saves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ArcherQuestValidation
{
    const string Key = "ArcherQuestValidation";
    static IEnumerator routine;
    static readonly System.Collections.Generic.List<string> checks = new();
    static ArcherQuestValidation() { EditorApplication.playModeStateChanged += Changed; }
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode first.");
        SessionState.SetString(Key + ".scene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Base.unity");
        SessionState.SetBool(Key, true);
        BuildingUpgradeValidation.Begin();
        SaveSlotPrefs.SetInt("Global_Resource_Archer", 0);
        SaveSlotPrefs.SetInt("foundation.quest.royal_magic_tribute.accepted",1);
        SaveSlotPrefs.SetInt("foundation.quest.royal_magic_tribute.deadline",31);
        SaveSlotPrefs.SetInt("foundation.unlock.building.refugees", 0);
        SaveSlotPrefs.SetInt("foundation.quest.first_expedition_supplies.completed", 1);
        SaveSlotPrefs.SetInt("foundation.quest.first_expedition_supplies.claimed", 1);
        SaveSlotPrefs.SetInt("foundation.quest.build_smithy_two_bows.completed", 1);
        SaveSlotPrefs.SetInt("foundation.quest.build_smithy_two_bows.claimed", 1);
        SaveSlotPrefs.SetInt("foundation.unlock.building.archery_range", 1);
        SaveSlotPrefs.SetInt("foundation.quest.recruit_two_archers.completed", 0);
        SaveSlotPrefs.SetInt("foundation.quest.recruit_two_archers.accepted", 1);
        SaveSlotPrefs.SetInt("foundation.quest.recruit_two_archers.claimed", 0);
        SaveSlotPrefs.Save();
    }
    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) { checks.Clear(); routine = Test(); EditorApplication.update += Tick; }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick;
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Key + ".scene", ""));
            BuildingUpgradeValidation.Restore();
            SessionState.SetBool(Key, false);
        }
    }
    static void Tick()
    {
        try { if (routine.MoveNext()) return; checks.Add("COMPLETE"); }
        catch (Exception ex) { checks.Add("FAIL " + ex); }
        Directory.CreateDirectory("Temp");
        File.WriteAllLines("Temp/ArcherQuestValidation.txt", checks);
        EditorApplication.update -= Tick;
        EditorApplication.ExitPlaymode();
    }
    static IEnumerator Test()
    {
        yield return null;
        void Check(bool value, string text) { if (!value) throw new Exception(text); checks.Add("PASS " + text); }
        {
            var catalog = AssetDatabase.LoadAssetAtPath<QuestCatalog>("Assets/Resources/Quests/Quest Catalog.asset");
            var quest = AssetDatabase.LoadAssetAtPath<QuestDefinition>("Assets/Resources/Quests/Recruit Two Archers.asset");
            var construction = UnityEngine.Object.FindObjectsByType<BaseBuildingConstruction>(FindObjectsSortMode.None);
            var camp = construction.First(b => b.BuildingId == "refugees");
            var range = construction.First(b => b.BuildingId == "archery_range");
            var archer = quest.requirements[0].resource;
            Check(QuestProgress.Current(catalog) == quest && !camp.ConstructionUnlocked, "Archer quest follows supplies; refugee camp starts locked");
            range.ConfirmBuild();
            Check(range.IsBuilt, "Archery range built");
            var training = UnityEngine.Object.FindObjectsByType<MilitaryTrainingView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(v => new SerializedObject(v).FindProperty("warrior").objectReferenceValue == archer);
            var weapon = (ResourceType)new SerializedObject(training).FindProperty("weapon").objectReferenceValue;
            GlobalResourceManager.Instance.SetResourceAmount(weapon, 2);
            training.Arm();
            Check(GlobalResourceManager.Instance.GetResourceAmount(archer) == 1 && !QuestProgress.IsComplete(quest) && !camp.ConstructionUnlocked, "First recruited archer does not unlock camp");
            training.Arm();
            Check(GlobalResourceManager.Instance.GetResourceAmount(archer) == 2 && QuestProgress.IsComplete(quest) && !camp.ConstructionUnlocked, "Second recruited archer completes quest while camp stays locked");
            Check(!camp.CanAffordConstruction, "Completed but unclaimed quest does not permit construction");
            var panel = UnityEngine.Object.FindObjectsByType<QuestPanel>(FindObjectsSortMode.None).First(p=>!RoyalTributeSetup.IsRoyal(p));
            var claim = (UnityEngine.UI.Button)new SerializedObject(panel).FindProperty("claimButton").objectReferenceValue;
            var canvas = claim.GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var sourceRect = (RectTransform)claim.transform;
            var origin = RectTransformUtility.WorldToScreenPoint(camera, sourceRect.TransformPoint(sourceRect.rect.center));
            claim.onClick.Invoke();
            Check(QuestProgress.Current(catalog) == null && camp.ConstructionUnlocked, "Claim unlocks camp and dismisses completed quest");
            var flight = UnityEngine.Object.FindFirstObjectByType<QuestRewardFlight>();
            Check(flight != null && flight.gameObject.activeInHierarchy, "Unlock flight survives quest panel disappearing");
            var stars = flight.GetComponentsInChildren<UnityEngine.UI.Image>();
            var highlight = camp.ConstructionButton.GetComponent<BuildingButtonHighlight>() ?? camp.GetComponent<BuildingButtonHighlight>();
            Check(stars.Length == 8 && stars.All(i => i.sprite == highlight.StarSprite && i.rectTransform.sizeDelta == i.sprite.rect.size * 2f && !i.raycastTarget), "Eight stars use building artwork at native double-pixel size and do not block clicks");
            float sampleAt = Time.realtimeSinceStartup + 0.35f;
            while (Time.realtimeSinceStartup < sampleAt) yield return null;
            var color = stars[0].color;
            Check(Mathf.Approximately(color.r, highlight.StarColor.r) && Mathf.Approximately(color.g, highlight.StarColor.g) && Mathf.Approximately(color.b, highlight.StarColor.b), "Flight uses the shared building label green");
            var position = RectTransformUtility.WorldToScreenPoint(camera, stars[0].rectTransform.position);
            var targetRect = (RectTransform)camp.ConstructionButton.transform;
            var targetCanvas = camp.ConstructionButton.GetComponentInParent<Canvas>().rootCanvas;
            var targetCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
            var destination = RectTransformUtility.WorldToScreenPoint(targetCamera, targetRect.TransformPoint(targetRect.rect.center));
            float fraction = (position.x - origin.x) / (destination.x - origin.x);
            Check(fraction > 0 && fraction < 1 && position.y > Mathf.Lerp(origin.y, destination.y, fraction) + 5, "Stars travel above the straight path toward refugee construction button");
            float timeout = Time.realtimeSinceStartup + 3f;
            while (flight != null && Time.realtimeSinceStartup < timeout) yield return null;
            Check(flight == null, "Flight particles clean up after arrival");
            Check(camp.CanAffordConstruction, "Free camp can be constructed after reward claim");
            training.Disarm();
            Check(camp.ConstructionUnlocked, "Unlock remains after disarming an archer");
            Check(!QuestProgress.TryClaim(quest, GlobalResourceManager.Instance), "Duplicate claim rejected");
        }
    }
}
#endif
