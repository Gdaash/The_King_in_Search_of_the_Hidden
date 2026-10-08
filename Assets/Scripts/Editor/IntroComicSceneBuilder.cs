using System;
using System.Collections.Generic;
using System.Linq;
using GameFoundation.Intro;
using GameFoundation.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

public static class IntroComicSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/IntroComic.unity";
    public const string ArtDirectory = "Assets/Sprites/Ui/IntroComic/";
    public const string TablePath = "Assets/Resources/Localization/Base Localization.asset";
    public const string FontPath = "Assets/Fonts/Intro Comic/Intro Comic UI.asset";
    private static readonly Vector2 SourceSize = new(1672f, 941f);

    public static readonly string[,] Translations =
    {
        { "intro.summons", "Король призвал вас ко двору для награждения.", "The king summoned you to court to receive an honour." },
        { "intro.award", "Славный рыцарь! За боевые заслуги вы награждаетесь собственным наделом земли!", "Noble knight! For your valour in battle, you are awarded a plot of land of your own!" },
        { "intro.own_land", "Своя земля…", "My own land…" },
        { "intro.portal", "…с порталом в Тёмные земли.", "…with a portal to the Dark Lands." },
        { "intro.tribute", "С этого дня вы обязаны в знак благодарности за такой подарок приносить ко двору магическую руду.", "From this day forth, in gratitude for this gift, you are obliged to bring magical ore to the royal court." },
        { "intro.objection", "Но, Ваше Величество…", "But, Your Majesty…" },
        { "intro.continue", "Нажмите, чтобы продолжить", "Click to continue" },
        { "intro.begin", "Нажмите, чтобы начать игру", "Click to begin your adventure" }
    };

    [MenuItem("Tools/Intro Comic/Create Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Build the comic outside Play Mode.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
            throw new InvalidOperationException("IntroComic already exists. Edit the existing scene instead of overwriting it.");

        var sprites = new[] { ImportSprite("01_award.png"), ImportSprite("02_decree.png"), ImportSprite("03_dismissal.png") };
        LocalizationTable table = AddTranslations();
        TMP_FontAsset font = CreateFont(table);
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var cameraObject = new GameObject("Comic Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 1 << 5;

            var canvasObject = new GameObject("Intro Comic Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.layer = 5;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform background = Rect("Advance Anywhere", canvasObject.transform);
            Stretch(background);
            var backdrop = background.gameObject.AddComponent<UnityEngine.UI.Image>();
            backdrop.color = Color.black;
            var button = background.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = backdrop;
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };

            RectTransform viewport = Rect("Comic Viewport", canvasObject.transform);
            Stretch(viewport);
            viewport.offsetMin = new Vector2(16f, 38f);
            viewport.offsetMax = new Vector2(-16f, -8f);
            RectTransform art = Rect("Artwork (2x Source Pixels)", viewport);
            art.sizeDelta = SourceSize * 2f;
            art.localScale = Vector3.one * (1034f / (SourceSize.y * 2f));

            var pages = new List<IntroComicController.Slide>();
            pages.Add(Page(art, sprites[0], "01 Award", new[]
            {
                new Rect(0, 0, 1672, 510), new Rect(0, 510, 494, 431),
                new Rect(494, 510, 684, 431), new Rect(1178, 510, 494, 431)
            }));
            pages.Add(Page(art, sprites[1], "02 Decree", new[]
            {
                new Rect(0, 0, 1672, 510), new Rect(0, 510, 740, 431), new Rect(740, 510, 932, 431)
            }));
            pages.Add(Page(art, sprites[2], "03 Dismissal", new[]
            {
                new Rect(0, 0, 862, 494), new Rect(862, 0, 809, 494), new Rect(0, 494, 1671, 447)
            }));

            Text(pages[0], 0, new Rect(45, 26, 410, 60), new Vector2(0, 0), "intro.summons", font, table);
            Text(pages[0], 0, new Rect(665, 43, 455, 86), new Vector2(0, 0), "intro.award", font, table);
            Text(pages[0], 1, new Rect(34, 549, 188, 59), new Vector2(0, 510), "intro.own_land", font, table);
            Text(pages[0], 2, new Rect(771, 547, 352, 48), new Vector2(494, 510), "intro.portal", font, table);
            Text(pages[1], 0, new Rect(531, 44, 616, 89), new Vector2(0, 0), "intro.tribute", font, table);
            Text(pages[1], 1, new Rect(482, 550, 194, 74), new Vector2(0, 510), "intro.objection", font, table);

            GameObject continueHint = Hint(canvasObject.transform, "Continue Hint", "intro.continue", font, table);
            GameObject finishHint = Hint(canvasObject.transform, "Begin Hint", "intro.begin", font, table);
            finishHint.SetActive(false);
            var controller = canvasObject.AddComponent<IntroComicController>();
            var settings = new SerializedObject(controller);
            settings.FindProperty("viewport").objectReferenceValue = viewport;
            settings.FindProperty("artworkRoot").objectReferenceValue = art;
            settings.FindProperty("advanceButton").objectReferenceValue = button;
            settings.FindProperty("continueHint").objectReferenceValue = continueHint;
            settings.FindProperty("finishHint").objectReferenceValue = finishHint;
            var slideProperty = settings.FindProperty("slides");
            slideProperty.arraySize = pages.Count;
            for (int i = 0; i < pages.Count; i++)
            {
                var slide = slideProperty.GetArrayElementAtIndex(i);
                slide.FindPropertyRelative("root").objectReferenceValue = pages[i].root;
                var sectionProperty = slide.FindPropertyRelative("sections");
                sectionProperty.arraySize = pages[i].sections.Length;
                for (int j = 0; j < pages[i].sections.Length; j++)
                    sectionProperty.GetArrayElementAtIndex(j).objectReferenceValue = pages[i].sections[j];
                pages[i].root.SetActive(i == 0);
                for (int j = 0; j < pages[i].sections.Length; j++) pages[i].sections[j].gameObject.SetActive(j == 0);
            }
            settings.ApplyModifiedPropertiesWithoutUndo();

            var localization = new GameObject("Comic Localization").AddComponent<LocalizationService>();
            var localizationSettings = new SerializedObject(localization);
            localizationSettings.FindProperty("table").objectReferenceValue = table;
            localizationSettings.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            EditorSceneManager.SaveScene(scene, ScenePath);
            var buildScenes = EditorBuildSettings.scenes.ToList();
            if (!buildScenes.Any(item => item.path == ScenePath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }
            Debug.Log("IntroComic created: 3 pages, 10 panels, ru/en localization, registered in Build Settings.");
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static Sprite ImportSprite(string name)
    {
        string path = ArtDirectory + name;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing comic image: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.alphaIsTransparency = true;
        var textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        textureSettings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(textureSettings);
        importer.SaveAndReimport();
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Vector2 expectedSize = name == "03_dismissal.png" ? new Vector2(1671f, 941f) : SourceSize;
        if (!sprite || sprite.rect.size != expectedSize)
            throw new InvalidOperationException("Comic image dimensions changed; verify panel and balloon bounds: " + path);
        return sprite;
    }

    private static LocalizationTable AddTranslations()
    {
        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(TablePath);
        if (!table) throw new InvalidOperationException("Shared localization table is missing.");
        for (int i = 0; i < Translations.GetLength(0); i++)
        {
            string key = Translations[i, 0];
            var entry = table.entries.Find(item => item.key == key);
            if (entry == null) { entry = new LocalizationTable.Entry { key = key }; table.entries.Add(entry); }
            while (entry.values.Count < table.languages.Count) entry.values.Add(string.Empty);
            for (int language = 0; language < table.languages.Count; language++)
            {
                if (table.languages[language] == "ru") entry.values[language] = Translations[i, 1];
                if (table.languages[language] == "en") entry.values[language] = Translations[i, 2];
            }
        }
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssetIfDirty(table);
        return table;
    }

    private static TMP_FontAsset CreateFont(LocalizationTable table)
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (existing) return existing;
        if (!AssetDatabase.IsValidFolder("Assets/Fonts/Intro Comic")) AssetDatabase.CreateFolder("Assets/Fonts", "Intro Comic");
        var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Examples & Extras/Fonts/Roboto-Bold.ttf");
        if (!source) throw new InvalidOperationException("Comic source font is missing.");
        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA,
            1024, 1024, AtlasPopulationMode.Dynamic, true);
        font.name = "Intro Comic UI";
        AssetDatabase.CreateAsset(font, FontPath);
        string characters = string.Concat(table.entries.Where(e => e.key.StartsWith("intro.")).SelectMany(e => e.values));
        if (!font.TryAddCharacters(characters, out string missing))
            throw new InvalidOperationException("Comic font lacks characters: " + missing);
        foreach (Texture2D atlas in font.atlasTextures) if (atlas && !AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, font);
        if (font.material && !AssetDatabase.Contains(font.material)) AssetDatabase.AddObjectToAsset(font.material, font);
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssetIfDirty(font);
        return font;
    }

    private static IntroComicController.Slide Page(RectTransform parent, Sprite sprite, string name, Rect[] sections)
    {
        RectTransform root = Rect(name, parent);
        root.sizeDelta = sprite.rect.size * 2f;
        var groups = new CanvasGroup[sections.Length];
        for (int i = 0; i < sections.Length; i++)
        {
            Rect bounds = sections[i];
            RectTransform section = Rect("Section " + (i + 1), root);
            TopLeft(section, bounds.position * 2f, bounds.size * 2f);
            section.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            groups[i] = section.gameObject.AddComponent<CanvasGroup>();
            groups[i].blocksRaycasts = false;
            groups[i].interactable = false;
            RectTransform artwork = Rect("Artwork", section);
            TopLeft(artwork, -bounds.position * 2f, sprite.rect.size * 2f);
            var image = artwork.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
        }
        return new IntroComicController.Slide { root = root.gameObject, sections = groups };
    }

    private static void Text(IntroComicController.Slide slide, int section, Rect bounds, Vector2 panelOrigin,
        string key, TMP_FontAsset font, LocalizationTable table)
    {
        RectTransform rect = Rect(key, slide.sections[section].transform);
        TopLeft(rect, (bounds.position - panelOrigin) * 2f, bounds.size * 2f);
        TMP_Text text = Label(rect, key, font, table);
        text.fontSize = 56f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 36f;
        text.fontSizeMax = 56f;
        text.color = new Color32(23, 20, 17, 255);
    }

    private static GameObject Hint(Transform parent, string name, string key, TMP_FontAsset font, LocalizationTable table)
    {
        RectTransform rect = Rect(name, parent);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 5f);
        rect.sizeDelta = new Vector2(1400f, 28f);
        TMP_Text text = Label(rect, key, font, table);
        text.fontSize = 22f;
        text.color = new Color32(191, 180, 163, 255);
        return rect.gameObject;
    }

    private static TMP_Text Label(RectTransform rect, string key, TMP_FontAsset font, LocalizationTable table)
    {
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = table.Get(key, "ru");
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.margin = Vector4.zero;
        var localized = rect.gameObject.AddComponent<LocalizedText>();
        var settings = new SerializedObject(localized);
        settings.FindProperty("key").stringValue = key;
        settings.ApplyModifiedPropertiesWithoutUndo();
        return text;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void TopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(position.x, -position.y);
        rect.sizeDelta = size;
    }
}
