using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GameFoundation.Intro;
using GameFoundation.Localization;
using GameFoundation.Saves;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using PlayerPrefs = UnityEngine.PlayerPrefs;

[InitializeOnLoad]
public static class IntroComicAudit
{
    public const string OutputDirectory = "Docs/Art/IntroComic/IntegrationValidation";
    private const string SessionKey = "IntroComicAudit.State";
    private static int checks;
    [Serializable] private sealed class State
    {
        public bool hadSelection;
        public int selection;
        public bool hadLanguage;
        public string language;
        public string originalScene;
        public string originalPlayModeStartScene;
        public string originalSlotOne;
    }

    static IntroComicAudit() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

    [MenuItem("Tools/Intro Comic/Validate Assets and Progression")]
    public static void RunEdit()
    {
        Directory.CreateDirectory(OutputDirectory);
        File.WriteAllText(OutputDirectory + "/edit-checks.txt", "Intro comic: asset and progression validation\n");
        checks = 0;
        var progress = new IntroComicProgress(new[] { 4, 3, 3 });
        int[,] expected = { {0,1}, {0,2}, {0,3}, {0,4}, {1,1}, {1,2}, {1,3}, {2,1}, {2,2}, {2,3} };
        for (int i = 0; i < expected.GetLength(0); i++)
        {
            Check(progress.PageIndex == expected[i, 0] && progress.RevealedSections == expected[i, 1] && !progress.IsComplete,
                "Click " + i + " reveals only the expected panel", "edit-checks.txt");
            progress.Advance();
        }
        Check(progress.IsComplete, "Final panel requires its own completion click", "edit-checks.txt");
        progress.Advance();
        Check(progress.PageIndex == 2 && progress.RevealedSections == 3, "Completed progression ignores further input", "edit-checks.txt");
        var single = new IntroComicProgress(new[] { 1 });
        Check(!single.IsComplete && single.IsLastSection, "Single-panel page is visible before completion", "edit-checks.txt");
        single.Advance();
        Check(single.IsComplete, "Single-panel comic finishes on click", "edit-checks.txt");

        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(IntroComicSceneBuilder.TablePath);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(IntroComicSceneBuilder.FontPath);
        for (int i = 0; i < IntroComicSceneBuilder.Translations.GetLength(0); i++)
        {
            string key = IntroComicSceneBuilder.Translations[i, 0];
            foreach (string locale in table.languages)
            {
                string value = table.Get(key, locale);
                Check(!string.IsNullOrWhiteSpace(value) && value != key, key + " translated in " + locale, "edit-checks.txt");
                Check(font.HasCharacters(value), key + " glyph coverage in " + locale, "edit-checks.txt");
            }
        }
        foreach (string filename in new[] { "01_award.png", "02_decree.png", "03_dismissal.png" })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(IntroComicSceneBuilder.ArtDirectory + filename);
            Check(importer.spritePixelsPerUnit == 32 && importer.filterMode == FilterMode.Point &&
                  importer.textureCompression == TextureImporterCompression.Uncompressed && !importer.mipmapEnabled,
                filename + " import settings", "edit-checks.txt");
        }
        Check(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == IntroComicSceneBuilder.ScenePath),
            "Intro scene enabled in Build Settings", "edit-checks.txt");
        Scene scene = EditorSceneManager.OpenScene(IntroComicSceneBuilder.ScenePath, OpenSceneMode.Additive);
        try
        {
            var objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            Check(objects.All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0),
                "No missing scripts in IntroComic", "edit-checks.txt");
            var images = objects.Select(t => t.GetComponent<UnityEngine.UI.Image>()).Where(image => image && image.sprite).ToArray();
            Check(images.Length == 10 && images.All(image => image.rectTransform.sizeDelta == image.sprite.rect.size * 2f),
                "All ten panel images use twice their source sprite dimensions", "edit-checks.txt");
            Check(objects.Count(t => t.GetComponent<EventSystem>()) == 1, "Exactly one EventSystem", "edit-checks.txt");
            Check(objects.Count(t => t.GetComponent<LocalizedText>()) == 8, "Every narrative and hint label is localized", "edit-checks.txt");
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
        File.AppendAllText(OutputDirectory + "/edit-checks.txt", "PASS: " + checks + " checks\n");
        Debug.Log("IntroComic edit checks passed: " + checks);
    }

    [MenuItem("Tools/Intro Comic/Validate New Game in Play Mode")]
    public static void BeginPlay()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Start the audit outside Play Mode.");
        if (SaveSlotPrefs.SlotExists(3)) throw new InvalidOperationException("Audit requires an empty slot 3; no existing save will be replaced.");
        Scene current = SceneManager.GetActiveScene();
        if (SceneManager.sceneCount != 1) throw new InvalidOperationException("Audit needs one scene open.");
        var state = new State
        {
            hadSelection = PlayerPrefs.HasKey("foundation.selectedSaveSlot"),
            selection = PlayerPrefs.GetInt("foundation.selectedSaveSlot", 1),
            hadLanguage = PlayerPrefs.HasKey("foundation.language"),
            language = PlayerPrefs.GetString("foundation.language", "en"),
            originalScene = current.path,
            originalPlayModeStartScene = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),
            originalSlotOne = PlayerPrefs.GetString("foundation.slot.1.saveData", "")
        };
        SessionState.SetString(SessionKey, JsonUtility.ToJson(state));
        Directory.CreateDirectory(OutputDirectory);
        File.WriteAllText(OutputDirectory + "/play-checks.txt", "Intro comic: real scene and UI-event validation using empty slot 3\n");
        // Unity preserves the currently open scene, including unsaved edits, across Play Mode.
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MainMenu.unity");
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        string json = SessionState.GetString(SessionKey, "");
        if (string.IsNullOrEmpty(json)) return;
        if (change == PlayModeStateChange.EnteredPlayMode) RunPlay();
        if (change == PlayModeStateChange.EnteredEditMode)
        {
            State state = JsonUtility.FromJson<State>(json);
            // Slot 3 was verified empty before this audit; only audit-created data is removed.
            PlayerPrefs.DeleteKey("foundation.slot.3.created");
            PlayerPrefs.DeleteKey("foundation.slot.3.saveData");
            if (state.hadSelection) PlayerPrefs.SetInt("foundation.selectedSaveSlot", state.selection);
            else PlayerPrefs.DeleteKey("foundation.selectedSaveSlot");
            if (state.hadLanguage) PlayerPrefs.SetString("foundation.language", state.language);
            else PlayerPrefs.DeleteKey("foundation.language");
            PlayerPrefs.Save();
            ResetSaveCaches();
            bool untouched = state.originalSlotOne == PlayerPrefs.GetString("foundation.slot.1.saveData", "");
            File.AppendAllText(OutputDirectory + "/play-checks.txt", (untouched ? "PASS" : "FAIL") + " existing slot 1 unchanged; temporary slot removed; preferences restored\n");
            SessionState.EraseString(SessionKey);
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(state.originalPlayModeStartScene) ? null :
                AssetDatabase.LoadAssetAtPath<SceneAsset>(state.originalPlayModeStartScene);
        }
    }

    // The restored Editor scene can read save data before EnteredEditMode runs. Invalidate only
    // in-memory caches after restoring preferences, without selecting/creating a real slot.
    public static void ResetSaveCaches()
    {
        typeof(SaveSlotPrefs).GetField("_selected", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, 0);
        typeof(SaveSlotPrefs).Assembly.GetType("GameFoundation.Saves.GameSaveService")?
            .GetMethod("ResetCache", BindingFlags.Static | BindingFlags.NonPublic)?.Invoke(null, null);
    }

    private static async void RunPlay()
    {
        checks = 0;
        try
        {
            await Task.Delay(500);
            SelectTestSlot();
            await Task.Delay(500);
            Check(SceneManager.GetActiveScene().name == IntroComicFlow.SceneName, "Empty slot enters IntroComic");
            Check(SaveSlotPrefs.GetInt(IntroComicFlow.PendingSaveKey) == 1, "New intro persisted as pending");
            IntroComicController controller = Object.FindAnyObjectByType<IntroComicController>();
            Check(controller.PageIndex == 0 && controller.RevealedSections == 1, "First panel is shown automatically");
            await Capture("initial-panel", 1920, 1080);
            await Click();
            Check(controller.RevealedSections == 2, "Actual pointer click reveals the second panel");

            SceneManager.LoadScene("MainMenu");
            await Task.Delay(400);
            SelectTestSlot();
            await Task.Delay(400);
            controller = Object.FindAnyObjectByType<IntroComicController>();
            Check(controller && controller.PageIndex == 0 && controller.RevealedSections == 1,
                "Interrupted introduction replays when its slot is selected again");

            await Capture("first-panel", 1920, 1080);
            int[] counts = { 4, 3, 3 };
            for (int page = 0; page < counts.Length; page++)
            {
                for (int panel = 1; panel < counts[page]; panel++)
                {
                    await Click();
                    Check(controller.PageIndex == page && controller.RevealedSections == panel + 1,
                        "Page " + (page + 1) + ", panel " + (panel + 1) + " appears alone");
                }
                Check(!controller.IsComplete, "Fully revealed page " + (page + 1) + " remains visible");
                foreach (string language in new[] { "ru", "en" })
                {
                    LocalizationService.Instance.SetLanguage(language);
                    await Task.Delay(150);
                    ValidateText(language);
                    await Capture("page-" + (page + 1) + "-" + language, 1920, 1080);
                }
                if (page < counts.Length - 1)
                {
                    await Click();
                    Check(controller.PageIndex == page + 1 && controller.RevealedSections == 1,
                        "Next page opens with only its first panel");
                }
            }
            await Capture("page-3-4x3", 1280, 960);
            await Capture("page-3-ultrawide", 2560, 1080);
            await Click();
            await Task.Delay(600);
            Check(SceneManager.GetActiveScene().name == "Base", "Final click starts gameplay in Base");
            Check(SaveSlotPrefs.GetInt(IntroComicFlow.PendingSaveKey) == 0, "Completed introduction persisted");
            SceneManager.LoadScene("MainMenu");
            await Task.Delay(350);
            SelectTestSlot();
            await Task.Delay(500);
            Check(SceneManager.GetActiveScene().name == "Base", "Continuing an existing save bypasses the comic");
            SaveSlotPrefs.DeleteKey(IntroComicFlow.PendingSaveKey);
            Check(IntroComicFlow.Prepare(false, "Base") == "Base", "Legacy save without intro flag bypasses the comic");
            File.AppendAllText(OutputDirectory + "/play-checks.txt", "PASS: " + checks + " runtime checks\n");
        }
        catch (Exception exception)
        {
            File.AppendAllText(OutputDirectory + "/play-checks.txt", "FAIL: " + exception + "\n");
            Debug.LogException(exception);
        }
        finally { EditorApplication.isPlaying = false; }
    }

    private static void SelectTestSlot()
    {
        var menu = Object.FindAnyObjectByType<MainMenuController>();
        menu.OpenSlots();
        var settings = new SerializedObject(menu);
        var slot = (UnityEngine.UI.Button)settings.FindProperty("slotButtons").GetArrayElementAtIndex(2).objectReferenceValue;
        slot.onClick.Invoke();
    }

    private static async Task Click()
    {
        var data = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
            button = PointerEventData.InputButton.Left
        };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        Check(hits.Count > 0 && hits[0].gameObject.name == "Advance Anywhere",
            "Mouse raycast reaches comic input through the artwork; hits=" + string.Join(",", hits.Select(hit => hit.gameObject.name)));
        ExecuteEvents.Execute(hits[0].gameObject, data, ExecuteEvents.pointerClickHandler);
        await Task.Delay(250);
    }

    private static void ValidateText(string language)
    {
        foreach (LocalizedText localized in Object.FindObjectsByType<LocalizedText>())
        {
            var label = localized.GetComponent<TMP_Text>();
            if (!label) continue;
            string key = new SerializedObject(localized).FindProperty("key").stringValue;
            label.ForceMeshUpdate();
            Check(label.text == LocalizationService.Instance.Get(key), key + " reacts to " + language);
            Check(!label.isTextOverflowing && label.textBounds.size.x <= label.rectTransform.rect.width + 1f &&
                  label.textBounds.size.y <= label.rectTransform.rect.height + 1f, key + " fits its balloon in " + language);
        }
    }

    private static async Task Capture(string name, int width, int height)
    {
        var camera = Camera.main;
        RenderTexture previousTarget = camera.targetTexture;
        var target = RenderTexture.GetTemporary(width, height, 24);
        try
        {
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            await Task.Delay(100);
            camera.Render();
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(OutputDirectory + "/" + name + ".png", texture.EncodeToPNG());
            }
            finally { Object.Destroy(texture); RenderTexture.active = previousActive; }
        }
        finally { camera.targetTexture = previousTarget; RenderTexture.ReleaseTemporary(target); }
        await Task.Delay(100);
    }

    private static void Check(bool condition, string message, string file = "play-checks.txt")
    {
        if (!condition) throw new InvalidOperationException(message);
        checks++;
        File.AppendAllText(OutputDirectory + "/" + file, "PASS " + message + "\n");
    }
}
