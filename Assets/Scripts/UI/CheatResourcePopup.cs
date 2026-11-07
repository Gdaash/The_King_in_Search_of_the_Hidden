using System;
using System.Collections.Generic;
using GameFoundation.Base;
using GameFoundation.Localization;
using GameFoundation.Quests;
using GameFoundation.Saves;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class CheatResourcePopup : MonoBehaviour
{
    [Serializable]
    public sealed class ResourceRow
    {
        public ResourceType resource;
        public Image icon;
        public Text nameText;
        public Text amountText;
        public Button subtractHundred;
        public Button subtractTen;
        public Button subtractFive;
        public Button subtractOne;
        public Button addOne;
        public Button addFive;
        public Button addTen;
        public Button addHundred;
    }

    [SerializeField] private GameObject window;
    [SerializeField] private GameObject dimmer;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button unlockAllBuildingsButton;
    [SerializeField] private Button addAllHundredButton;
    [SerializeField] private Button subtractAllHundredButton;
    [SerializeField] private Button refreshWarriorsButton;
    private bool changingAllResources;
    private TMP_Text questDayLabel;
    private readonly List<Button> dayButtons = new();
    [SerializeField] private Button resetCurrentSaveButton;
    [SerializeField] private ContentUnlockDefinition[] buildingUnlocks;
    [SerializeField] private List<ResourceRow> rows = new List<ResourceRow>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private GameObject resetBuildingsRail;
    private readonly List<Button> buildingResetButtons = new();
#endif

    private void Awake()
    {
        unlockAllBuildingsButton?.onClick.AddListener(UnlockAllBuildings);
        addAllHundredButton?.onClick.AddListener(AddAllHundred);
        subtractAllHundredButton?.onClick.AddListener(SubtractAllHundred);
        resetCurrentSaveButton?.onClick.AddListener(CurrentSaveReset.Reload);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        foreach (ResourceRow row in rows)
        {
            if (row == null || row.resource == null) continue;
            Bind(row.subtractHundred, row.resource, -100);
            Bind(row.subtractTen, row.resource, -10);
            Bind(row.subtractFive, row.resource, -5);
            Bind(row.subtractOne, row.resource, -1);
            Bind(row.addOne, row.resource, 1);
            Bind(row.addFive, row.resource, 5);
            Bind(row.addTen, row.resource, 10);
            Bind(row.addHundred, row.resource, 100);

            if (row.icon != null)
            {
                ResourceIconSizing.Apply(row.icon, row.resource.resourceIcon);
                row.icon.enabled = row.resource.resourceIcon != null;
            }
            if (row.nameText != null)
                row.nameText.text = row.resource.resourceName;
        }

        refreshWarriorsButton?.onClick.AddListener(RefreshWarriors);
        CreateQuestDayControls();
        if (window != null)
            window.SetActive(false);
        if (dimmer != null)
            dimmer.SetActive(false);
    }

    private void OnEnable()
    {
        GlobalResourceManager.OnResourceChanged += OnResourceChanged;
    }

    private void OnDisable()
    {
        GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
    }

    private void Update()
    {
        if (GameFoundation.MetaProgression.PortalCloudTransition.IsLoading) return;
        if (GameFoundation.Base.ForestForagingService.IsPending || GameFoundation.MetaProgression.PortalTowerProgression.IsChoosingUpgrade) return;
        if (Input.GetKeyDown(KeyCode.L) && !IsEditingText())
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (window != null && !window.activeSelf) PrepareBuildingResets();
#endif
            if (window != null) window.SetActive(!window.activeSelf);
            if (dimmer != null) dimmer.SetActive(window != null && window.activeSelf);
            Refresh();
        }
        else if (window != null && window.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }

        // MainMenu has no resource manager until a save slot has been opened.
        if (window != null && window.activeSelf && _lastManager != GlobalResourceManager.Instance)
            Refresh();
    }

    private void CreateQuestDayControls()
    {
        if (addAllHundredButton == null) return;
        var parent = addAllHundredButton.transform.parent;
        questDayLabel = new GameObject("Quest days",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        questDayLabel.transform.SetParent(parent,false); questDayLabel.font = Resources.Load<TMP_FontAsset>("Fonts/Pixellari Reading UI");
        questDayLabel.fontSize=24;questDayLabel.alignment=TextAlignmentOptions.Center;questDayLabel.raycastTarget=false;
        questDayLabel.rectTransform.sizeDelta=new Vector2(300,50);questDayLabel.rectTransform.anchoredPosition=new Vector2(535,-235);
        int[] offsets={-1,-5,-10,1,5,10};
        for(int i=0;i<offsets.Length;i++)
        {
            int delta=offsets[i];var button=Instantiate(delta<0 && subtractAllHundredButton!=null ? subtractAllHundredButton : addAllHundredButton,parent);button.name="Quest day "+delta;
            foreach(var localizer in button.GetComponentsInChildren<MonoBehaviour>(true))
                if(localizer is LocalizedText || localizer is LocalizedGameText) { localizer.enabled=false;Destroy(localizer); }
            button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>{GameFoundation.MetaProgression.DayCycleService.Instance?.ShiftQuestDays(delta);Refresh();});
            var rect=(RectTransform)button.transform;rect.sizeDelta=new Vector2(92,52);rect.anchoredPosition=new Vector2(437+(i%3)*98,-290-(i/3)*62);
            string text=(delta>0?"+":"")+delta;
            foreach(var label in button.GetComponentsInChildren<Text>(true)){label.text=text;label.fontSize=24;label.resizeTextForBestFit=false;}
            foreach(var label in button.GetComponentsInChildren<TMP_Text>(true)){label.text=text;label.fontSize=24;label.enableAutoSizing=false;}
            dayButtons.Add(button);
        }
    }

    private GlobalResourceManager _lastManager;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void PrepareBuildingResets()
    {
        if (resetBuildingsRail != null || unlockAllBuildingsButton == null) return;
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var entries = new SortedDictionary<string, string>(StringComparer.CurrentCulture);
        var ids = new HashSet<string>();
        foreach (var construction in FindObjectsByType<BaseBuildingConstruction>(FindObjectsInactive.Include))
        {
            if (construction.gameObject.scene != scene || string.IsNullOrEmpty(construction.BuildingId) || !ids.Add(construction.BuildingId)) continue;
            entries[construction.DisplayName] = construction.BuildingId;
        }
        if (entries.Count == 0) return;
        if (ids.Contains("housing"))
            foreach (string id in new[] { GameFoundation.MetaProgression.ExpeditionTasks.Coop,
                GameFoundation.MetaProgression.ExpeditionTasks.Tannery, HousingDistrict.Fields })
            {
                var annex = BuildingUpgradeService.Catalog?.Find(id);
                if (annex != null) entries[CheatText(annex.nameKey, annex.displayName)] = id;
            }
        entries[CheatText("base.base_panel.portal.label", "Портал")] = GameFoundation.MetaProgression.PortalProgression.BuildingId;
        GlobalStats lab = null;
        foreach (var list in FindObjectsByType<LaboratoryUpgradeList>(FindObjectsInactive.Include))
            if (list.gameObject.scene == scene && list.portalProgression == null && list.stats?.UpgradeTable != null)
            { lab = list.stats; break; }

        var expandedPanel = (RectTransform)unlockAllBuildingsButton.transform.parent;
        float originalPanelWidth = expandedPanel.rect.width;
        const float railWidth = 332f;
        var children = new List<Transform>();
        foreach (Transform child in expandedPanel) children.Add(child);
        var originalContent = ResetRect(expandedPanel, "Resource controls", expandedPanel.rect.size, Vector2.right * (railWidth / 2));
        foreach (var child in children) child.SetParent(originalContent, false);
        expandedPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalPanelWidth + railWidth);
        var fit = expandedPanel.GetComponent<GameFoundation.UI.CanvasWindowFit>() ??
            expandedPanel.gameObject.AddComponent<GameFoundation.UI.CanvasWindowFit>();
        fit.Fit();
        var rail = ResetRect(expandedPanel, "Building resets", new Vector2(294, expandedPanel.rect.height - 60),
            Vector2.left * (originalPanelWidth / 2));
        resetBuildingsRail = rail.gameObject;
        var theme = Resources.Load<GameFoundation.UI.ButtonVisualTheme>("UI/ButtonVisualTheme");
        var frame = rail.gameObject.AddComponent<Image>();
        frame.sprite = theme?.popupPanelSprite;
        frame.type = Image.Type.Sliced;
        frame.raycastTarget = false;
        float top = rail.rect.height / 2;
        var heading = ResetLabel(rail, "Heading", new Vector2(272, 48), Vector2.up * (top - 30), 24);
        heading.text = CheatText("cheat.reset_buildings", "Сброс построек");
        heading.color = new Color(.91f, .77f, .48f);
        float step = Mathf.Min(62, (rail.rect.height - 84) / entries.Count);
        int index = 0;
        foreach (var entry in entries)
        {
            string id = entry.Value;
            var buttonRect = ResetRect(rail, "Reset " + id, new Vector2(262, Mathf.Min(52, step - 6)),
                Vector2.up * (top - 74 - step * index++));
            var image = buttonRect.gameObject.AddComponent<Image>();
            image.sprite = theme?.textButtonSprite;
            image.type = Image.Type.Sliced;
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            buttonRect.gameObject.AddComponent<GameFoundation.UI.UnifiedButtonFeedback>();
            var label = ResetLabel(buttonRect, "Caption", new Vector2(238, buttonRect.rect.height - 4), Vector2.zero, 22);
            label.text = entry.Key;
            var color = buttonRect.gameObject.AddComponent<GameFoundation.UI.ActionButtonLabelColor>();
            color.Configure(null, label);
            color.Negative = true;
            button.onClick.AddListener(() =>
            {
                BuildingCheatReset.Reset(id, lab);
                Refresh();
            });
            buildingResetButtons.Add(button);
        }
    }

    private static string CheatText(string key, string fallback)
    {
        string value = LocalizationService.Instance?.Get(key);
        return string.IsNullOrEmpty(value) || value == key ? fallback : value;
    }

    private static RectTransform ResetRect(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static TMP_Text ResetLabel(RectTransform parent, string name, Vector2 size, Vector2 position, int fontSize)
    {
        var label = ResetRect(parent, name, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        var font = Resources.Load<TMP_FontAsset>("Fonts/Pixellari Reading UI");
        if (font != null) label.font = font;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }
#endif

    private static bool IsEditingText()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected != null &&
               (selected.GetComponent<InputField>() != null || selected.GetComponent<TMP_InputField>() != null);
    }

    private void Bind(Button button, ResourceType resource, int delta)
    {
        if (button != null)
            button.onClick.AddListener(() => Change(resource, delta));
    }

    private void Change(ResourceType resource, int delta)
    {
        GlobalResourceManager manager = GlobalResourceManager.Instance;
        if (manager == null || resource == null) return;

        int current = manager.GetResourceAmount(resource);
        if (delta < 0 && current < -delta) return;

        int next = (int)Math.Min(int.MaxValue, (long)current + delta);
        manager.SetResourceAmount(resource, next);
        Refresh();
    }

    public void RefreshWarriors()
    {
        if (GlobalResourceManager.Instance == null) return;
        foreach (var deployment in FindObjectsByType<GameFoundation.MetaProgression.WorldMilitaryDeploymentController>(FindObjectsSortMode.None))
            deployment.RefreshAndDeployStoredWarriors();
        foreach (var roster in FindObjectsByType<GameFoundation.MetaProgression.WorldMilitaryRosterView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            roster.RebuildRoster();
        foreach (var roster in FindObjectsByType<GameFoundation.Base.BaseMilitaryOverview>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            roster.RebuildRoster();
        Canvas.ForceUpdateCanvases();
        Refresh();
    }

    private void AddAllHundred() => ChangeAll(100);
    private void SubtractAllHundred() => ChangeAll(-100);
    private void ChangeAll(int delta)
    {
        var manager = GlobalResourceManager.Instance;
        if (manager == null) return;
        var changed = new HashSet<ResourceType>();
        changingAllResources = true;
        try
        {
            foreach (var row in rows)
            {
                if (row?.resource == null || !changed.Add(row.resource)) continue;
                int next = (int)Math.Max(0L, Math.Min(int.MaxValue, (long)manager.GetResourceAmount(row.resource) + delta));
                manager.SetResourceAmount(row.resource, next);
            }
        }
        finally { changingAllResources = false; Refresh(); }
    }

    private void OnResourceChanged(ResourceType _, int __)
    {
        if (!changingAllResources && window != null && window.activeSelf)
            Refresh();
    }

    private void Refresh()
    {
        _lastManager = GlobalResourceManager.Instance;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        foreach (var button in buildingResetButtons) SetInteractable(button, _lastManager != null);
#endif
        var calendar = GameFoundation.MetaProgression.DayCycleService.Instance;
        foreach(var button in dayButtons) SetInteractable(button,calendar!=null);
        if(questDayLabel!=null)questDayLabel.text=(LocalizationService.Instance?.Get("cheat.quest_days")??"Quest days")+": "+(calendar?.Day??1);
        SetInteractable(refreshWarriorsButton, _lastManager != null &&
            (FindFirstObjectByType<GameFoundation.MetaProgression.WorldMilitaryDeploymentController>() != null ||
             FindFirstObjectByType<GameFoundation.Base.BaseMilitaryOverview>() != null));
        SetInteractable(addAllHundredButton, _lastManager != null);
        SetInteractable(subtractAllHundredButton, _lastManager != null);
        foreach (ResourceRow row in rows)
        {
            if (row == null) continue;
            bool available = _lastManager != null && row.resource != null;
            int amount = available ? _lastManager.GetResourceAmount(row.resource) : 0;
            if (row.amountText != null)
                row.amountText.text = available ? amount.ToString() : "–";
            if (row.nameText != null && row.resource != null)
            {
                string key = "resource." + row.resource.Id + ".name";
                string translated = LocalizationService.Instance != null ? LocalizationService.Instance.Get(key) : key;
                row.nameText.text = translated != key ? translated : row.resource.resourceName;
            }

            SetInteractable(row.subtractHundred, available && amount >= 100);
            SetInteractable(row.subtractTen, available && amount >= 10);
            SetInteractable(row.subtractFive, available && amount >= 5);
            SetInteractable(row.subtractOne, available && amount >= 1);
            SetInteractable(row.addOne, available && amount < int.MaxValue);
            SetInteractable(row.addFive, available && amount <= int.MaxValue - 5);
            SetInteractable(row.addTen, available && amount <= int.MaxValue - 10);
            SetInteractable(row.addHundred, available && amount <= int.MaxValue - 100);
        }
    }

    private static void SetInteractable(Button button, bool value)
    {
        if (button != null) button.interactable = value;
    }

    private void UnlockAllBuildings()
    {
        foreach (var unlock in buildingUnlocks ?? System.Array.Empty<ContentUnlockDefinition>())
            if (unlock != null) ContentUnlocks.Grant(unlock);
        foreach (var unlock in Resources.LoadAll<ContentUnlockDefinition>("Quests/BuildingUnlocks"))
            ContentUnlocks.Grant(unlock);
        SaveSlotPrefs.Save();
        ContentUnlocks.NotifyChanged();
    }

    private void OnDestroy()
    {
        unlockAllBuildingsButton?.onClick.RemoveListener(UnlockAllBuildings);
        addAllHundredButton?.onClick.RemoveListener(AddAllHundred);
        subtractAllHundredButton?.onClick.RemoveListener(SubtractAllHundred);
        resetCurrentSaveButton?.onClick.RemoveListener(CurrentSaveReset.Reload);
        refreshWarriorsButton?.onClick.RemoveListener(RefreshWarriors);
    }

    private void Close()
    {
        if (window != null) window.SetActive(false);
        if (dimmer != null) dimmer.SetActive(false);
    }
}
