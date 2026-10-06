#if UNITY_EDITOR
using System.Linq;
using GameFoundation.Base;
using GameFoundation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class NotificationFeedSetup
{
    public const string FeedPath = "Assets/Prefabs/UI/HUD/Notification Feed.prefab";
    private const string RowPath = "Assets/Prefabs/UI/HUD/Notification Row.prefab";

    public static void CreatePrefabs()
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pixellari Cyrillic/Pixellari-Cyrillic.ttf");
        var row = new GameObject("Notification Row", typeof(RectTransform), typeof(CanvasGroup), typeof(NotificationRow));
        var rect = (RectTransform)row.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.sizeDelta = new Vector2(440, 40);
        var icon = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        icon.transform.SetParent(row.transform, false);
        icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0, .5f);
        icon.rectTransform.pivot = new Vector2(0, .5f);
        icon.rectTransform.sizeDelta = new Vector2(32, 32);
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        var text = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Shadow)).GetComponent<Text>();
        text.transform.SetParent(row.transform, false);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        text.font = font;
        text.fontSize = 22;
        text.lineSpacing = 1.2f;
        text.alignment = TextAnchor.MiddleRight;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;
        text.raycastTarget = false;
        text.GetComponent<Shadow>().effectColor = new Color(0, 0, 0, .95f);
        text.GetComponent<Shadow>().effectDistance = new Vector2(-2, -2);
        var s = new SerializedObject(row.GetComponent<NotificationRow>());
        s.FindProperty("label").objectReferenceValue = text;
        s.FindProperty("icon").objectReferenceValue = icon;
        s.FindProperty("group").objectReferenceValue = row.GetComponent<CanvasGroup>();
        s.ApplyModifiedPropertiesWithoutUndo();
        BakeIconPadding(row.GetComponent<NotificationRow>());
        var savedRow = PrefabUtility.SaveAsPrefabAsset(row, RowPath).GetComponent<NotificationRow>();
        Object.DestroyImmediate(row);

        var feed = new GameObject("Notification Feed", typeof(RectTransform), typeof(CanvasGroup), typeof(Canvas), typeof(NotificationFeed));
        var area = (RectTransform)feed.transform;
        area.anchorMin = area.anchorMax = area.pivot = Vector2.zero;
        area.anchoredPosition = new Vector2(17, 92);
        area.sizeDelta = new Vector2(440, 520);
        feed.GetComponent<CanvasGroup>().blocksRaycasts = false;
        feed.GetComponent<CanvasGroup>().interactable = false;
        feed.GetComponent<Canvas>().overrideSorting = true;
        feed.GetComponent<Canvas>().sortingOrder = 500;
        s = new SerializedObject(feed.GetComponent<NotificationFeed>());
        var template = Object.Instantiate(savedRow, feed.transform, false);
        template.name = "Notification Template";
        template.GetComponentInChildren<Text>().text = "Пример уведомления";
        template.GetComponentInChildren<UnityEngine.UI.Image>().gameObject.SetActive(false);
        template.gameObject.SetActive(false);
        s.FindProperty("rowPrefab").objectReferenceValue = template;
        s.FindProperty("unitCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnitDescriptionCatalog>("Assets/Resources/UI/Unit Details/Unit Description Catalog.asset");
        s.FindProperty("enemyCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnitDescriptionCatalog>("Assets/Resources/UI/Unit Details/Enemy Description Catalog.asset");
        s.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(feed, FeedPath);
        Object.DestroyImmediate(feed);
    }

    public static void BakeIconPadding(NotificationRow row)
    {
        var sprites = AssetDatabase.FindAssets("t:ResourceType").Select(g => AssetDatabase.LoadAssetAtPath<ResourceType>(AssetDatabase.GUIDToAssetPath(g))?.resourceIcon)
            .Concat(AssetDatabase.FindAssets("t:UnitDescriptionDefinition").Select(g => AssetDatabase.LoadAssetAtPath<UnitDescriptionDefinition>(AssetDatabase.GUIDToAssetPath(g))?.Portrait))
            .Where(s => s != null).Distinct().ToArray();
        var data = new SerializedObject(row);
        var entries = data.FindProperty("iconPadding");
        entries.arraySize = 0;
        foreach (var sprite in sprites)
        {
            var path = AssetDatabase.GetAssetPath(sprite.texture);
            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;
            var texture = new Texture2D(2, 2);
            try
            {
                if (!texture.LoadImage(System.IO.File.ReadAllBytes(path))) continue;
                if (texture.width != sprite.texture.width || texture.height != sprite.texture.height) continue;
                var pixels = texture.GetPixels32();
                var rect = sprite.rect;
                int left = (int)rect.width, right = -1;
                for (int y = 0; y < (int)rect.height; y++)
                    for (int x = 0; x < (int)rect.width; x++)
                        if (pixels[((int)rect.y + y) * texture.width + (int)rect.x + x].a > 0)
                        { left = Mathf.Min(left, x); right = Mathf.Max(right, x); }
                if (right < 0) continue;
                entries.arraySize++;
                var entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                entry.FindPropertyRelative("sprite").objectReferenceValue = sprite;
                entry.FindPropertyRelative("left").floatValue = left;
                entry.FindPropertyRelative("right").floatValue = rect.width - 1 - right;
            }
            finally { Object.DestroyImmediate(texture); }
        }
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void InstallScenes()
    {
        var original = SceneManager.GetActiveScene();
        foreach (string path in new[] { "Assets/Scenes/Base.unity", "Assets/Scenes/World.unity" })
        {
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var canvas = roots.SelectMany(r => r.GetComponentsInChildren<Canvas>(true))
                    .Where(c => c.gameObject.activeInHierarchy && c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
                    .OrderByDescending(c => c.GetComponentsInChildren<RectTransform>(true).Length).First();
                if (!roots.SelectMany(r => r.GetComponentsInChildren<NotificationFeed>(true)).Any())
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FeedPath), canvas.transform);
                    instance.transform.SetAsLastSibling();
                }
                var feed = roots.SelectMany(r => r.GetComponentsInChildren<NotificationFeed>(true)).First();
                var follow = feed.GetComponent<BesideMilitaryPanel>();
                if (follow != null) Object.DestroyImmediate(follow);
                foreach (var controller in roots.SelectMany(r => r.GetComponentsInChildren<BaseUIController>(true)))
                {
                    var s = new SerializedObject(controller);
                    var status = s.FindProperty("statusText").objectReferenceValue as Text;
                    if (status != null)
                    {
                        status.gameObject.SetActive(false);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(status.gameObject);
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new System.Exception("Could not save " + path);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        SceneManager.SetActiveScene(original);
    }

    public static string ValidateAndRender()
    {
        var preview = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null;
        try
        {
            var cameraObject = new GameObject("Preview Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = preview;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.07f, .06f, .09f);
            camera.orthographic = true;
            camera.orthographicSize = 540;
            camera.transform.position = new Vector3(0, 0, -10);
            var canvasObject = new GameObject("Preview Canvas", typeof(RectTransform), typeof(Canvas));
            SceneManager.MoveGameObjectToScene(canvasObject, preview);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(1920, 1080);
            target = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = target;
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(FeedPath), canvas.transform);
            var feed = root.GetComponent<NotificationFeed>();
            for (int i = 0; i < 12; i++) feed.Add("Событие " + i, NotificationKind.Normal, null);
            var rows = root.GetComponentsInChildren<NotificationRow>();
            if (rows.Length != 8 || rows[0].GetComponentInChildren<Text>().text != "Событие 4")
                throw new System.Exception("Row limit or ordering failed");
            feed.enabled = false;
            // Edit-mode components do not receive runtime OnDisable consistently: replace the isolated preview instance.
            Object.DestroyImmediate(root);
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(FeedPath), canvas.transform);
            feed = root.GetComponent<NotificationFeed>();
            var wood = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Wood.asset");
            var ore = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/MagicOre.asset");
            var crown = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Crown.asset");
            var berry = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Berry.asset");
            feed.AddEvent(new[] {
                new NotificationPart("+1", NotificationKind.Positive, crown.resourceIcon),
                new NotificationPart("-13", NotificationKind.Negative, berry.resourceIcon),
                new NotificationPart("Наступил следующий день", NotificationKind.Normal, null)
            });
            var shortRow = root.GetComponentInChildren<NotificationRow>();
            if (shortRow.Height > 48.1f) throw new System.Exception("Compact day notification unexpectedly wrapped");
            feed.AddEvent(new[] {
                new NotificationPart("+10", NotificationKind.Positive, crown.resourceIcon),
                new NotificationPart("-13", NotificationKind.Negative, berry.resourceIcon),
                new NotificationPart("+20", NotificationKind.Positive, wood.resourceIcon),
                new NotificationPart("-2", NotificationKind.Negative, ore.resourceIcon),
                new NotificationPart("Наступил следующий день", NotificationKind.Normal, null)
            });
            if (root.GetComponentsInChildren<NotificationRow>()[1].Height <= shortRow.Height)
                throw new System.Exception("Long notification did not wrap");
            Canvas.ForceUpdateCanvases();
            rows = root.GetComponentsInChildren<NotificationRow>();
            float bottom = 0;
            for (int i = rows.Length - 1; i >= 0; i--)
            {
                rows[i].Rect.anchoredPosition = new Vector2(0, bottom);
                bottom += rows[i].Height + 6;
            }
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                row.GetComponent<CanvasGroup>().alpha = 1;
                var text = row.GetComponentInChildren<Text>();
                if (text.preferredHeight > row.Height + .1f) throw new System.Exception("Text height overflow");
                foreach (var icon in row.GetComponentsInChildren<UnityEngine.UI.Image>())
                    if (icon.sprite != null && icon.rectTransform.sizeDelta != icon.sprite.rect.size * 2)
                        throw new System.Exception("Icon pixel scale failed");
                if (i > 0 && row.Rect.anchoredPosition.y + row.Height >= rows[i - 1].Rect.anchoredPosition.y)
                    throw new System.Exception("Rows overlap");
            }
            camera.Render();
            var old = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            image.Apply();
            string path = System.IO.Path.GetFullPath("Temp/NotificationFeedPreview.png");
            System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            RenderTexture.active = old;
            camera.targetTexture = null;
            return path;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
            if (target != null) Object.DestroyImmediate(target);
        }
    }
}
#endif
