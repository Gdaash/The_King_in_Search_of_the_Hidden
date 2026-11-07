using GameFoundation.Audio;
using GameFoundation.Localization;
using GameFoundation.Saves;
using GameFoundation.Settings;
using GameFoundation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
namespace GameFoundation.MetaProgression
{
    public sealed class PortalWeaponSlots : MonoBehaviour
    {
        public const float SlotSize = 64f;
        private const float SlotGap = 8f;
        private static readonly Color ActiveFrame = new(.49f, .86f, .42f);
        private static readonly Color IdleFrame = new Color32(127, 112, 138, 255);

        public enum Context { Assignment, Battle, Preview }
        public Context context;
        public PortalTowerBalance balance;
        public PortalWeaponSlotView[] slots;
        public Sprite offIcon;
        public TMP_Text caption;
        public PortalArsenalPopup Owner { get; private set; }
        public bool AllowsAssignment => context == Context.Assignment && Owner != null && Owner.IsBaseArsenal;
        private PortalTowerProgression progression;
        private LocalizationService language;
        private UnityEngine.UI.Image dragIcon;
        public void Bind(PortalTowerBalance config, Context mode, PortalArsenalPopup owner = null)
        { balance = config; context = mode; Owner = owner; Refresh(); }
        private void OnEnable()
        {
            if (balance == null) balance = Resources.Load<PortalTowerBalance>("World/PortalTowerBalance");
            PortalWeaponLoadout.Changed += Refresh; SaveSlotPrefs.SelectionChanged += Refresh;
            language = LocalizationService.Instance; if (language != null) language.LanguageChanged += Refresh;
            BindProgression(); Refresh();
        }
        private void Start() { BindProgression(); Refresh(); }
        private void BindProgression()
        {
            if (context != Context.Battle || progression == PortalTowerProgression.Instance) return;
            if (progression != null) progression.Changed -= Refresh;
            progression = PortalTowerProgression.Instance;
            if (progression != null) progression.Changed += Refresh;
        }
        private void OnDisable()
        {
            PortalWeaponLoadout.Changed -= Refresh; SaveSlotPrefs.SelectionChanged -= Refresh;
            if (language != null) language.LanguageChanged -= Refresh;
            if (progression != null) progression.Changed -= Refresh;
            progression = null; EndDrag();
            TooltipManager.Instance?.Hide();
        }
        private bool BattleInputAllowed => progression != null && progression.UsesEnergyLoadout && !progression.ExpeditionEnded &&
            !PortalArsenalPopup.IsOpen && !PortalTowerProgression.IsChoosingUpgrade && !FirstRunLightTutorial.BlocksInput &&
            !WorldEscapeController.ResultsActive && !PortalCloudTransition.IsLoading &&
            FindFirstObjectByType<GameMenuController>() == null;
        private void LateUpdate()
        {
            if (context != Context.Battle || progression == null || slots == null) return;
            for (int i = 1; i < slots.Length; i++)
                if (slots[i] != null)
                    slots[i].SetReloadProgress(progression.ReloadProgress(PortalWeaponLoadout.Get(i)));
        }

        private void Update()
        {
            if (context == Context.Preview) return;
#if UNITY_EDITOR
            if (EditorWaveCheatPopup.IsOpen) return;
#endif
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && (selected.GetComponent<UnityEngine.UI.InputField>() != null || selected.GetComponent<TMP_InputField>() != null)) return;
            for (int i = 0; i < PortalWeaponLoadout.SlotCount; i++)
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)) || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + i)))
                { Activate(i); break; }
        }
        public bool Assign(int index, PortalTowerBalance.Weapon weapon)
        {
            if (!AllowsAssignment || !PortalWeaponLoadout.Assign(index, weapon, balance.arsenal)) return false;
            GameAudioController.PlayUI(GameAudioCue.UiConfirm); return true;
        }
        public void Activate(int index)
        {
            if (AllowsAssignment && index > 0) { Assign(index, Owner.SelectedWeapon); return; }
            if (context == Context.Battle)
            {
                if (!BattleInputAllowed || (index > 0 && PortalWeaponLoadout.Get(index) == PortalTowerBalance.Weapon.None)) return;
                progression.SelectWeapon(PortalWeaponLoadout.Get(index));
            }
            else if (context == Context.Assignment || context == Context.Preview)
            { PortalWeaponLoadout.SetStartingSlot(index); GameAudioController.PlayUI(GameAudioCue.UiConfirm); }
            Refresh();
        }
        public void Refresh()
        {
            if (balance == null || slots == null) return;
            if (!balance.useEnergyLoadout) { gameObject.SetActive(false); return; }
            if (caption != null)
            {
                caption.gameObject.SetActive(context == Context.Preview);
                caption.text = PortalArsenalPopup.T("slots.title", "Оружие для вылазки");
            }
            var panel = (RectTransform)transform;
            panel.sizeDelta = new Vector2(slots.Length * SlotSize + Mathf.Max(0, slots.Length - 1) * SlotGap, SlotSize);
            bool highlightActiveWeapon = context == Context.Battle && progression != null;
            for (int i = 0; i < slots.Length; i++)
            {
                var view = slots[i]; if (view == null) continue;
                var rect = (RectTransform)view.transform;
                rect.sizeDelta = new Vector2(SlotSize, SlotSize);
                rect.anchoredPosition = new Vector2((i - (slots.Length - 1) * .5f) * (SlotSize + SlotGap), 0);
                view.Bind(this, i);
                var weapon = PortalWeaponLoadout.Get(i); var definition = balance.FindWeapon(weapon);
                view.icon.gameObject.SetActive(i == 0 || definition != null);
                if (i == 0) ResourceIconSizing.Apply(view.icon, offIcon);
                else if (definition != null) ResourceIconSizing.Apply(view.icon, definition.icon);
                if (view.icon.sprite != null)
                {
                    // Weapon artwork fits inside the compact frame, leaving room for its key.
                    var size = view.icon.sprite.rect.size * 2;
                    view.icon.rectTransform.sizeDelta = size * Mathf.Min(1, 48 / Mathf.Max(size.x, size.y));
                }
                view.number.text = (i + 1).ToString();
                view.button.interactable = i == 0 || AllowsAssignment || definition != null;
                view.feedback.Toggled = highlightActiveWeapon && (i == 0 || definition != null) &&
                    progression.ActiveWeapon == weapon;
                view.button.targetGraphic.color = view.feedback.Toggled ? ActiveFrame : IdleFrame;
                view.number.color = view.feedback.Toggled ? new Color(.56f,.86f,.47f) : new Color(.94f,.91f,.82f);
            }
        }
        public string Hint(int index)
        {
            if (index == 0) return PortalArsenalPopup.T("slots.off", "Выключить оружие. Энергия не расходуется.");
            var weapon = balance.FindWeapon(PortalWeaponLoadout.Get(index));
            if (weapon?.weapon == PortalTowerBalance.Weapon.DefenseField)
                return PortalArsenalPopup.WeaponTitle(weapon) + "\n" +
                    PortalArsenalPopup.T("stat.energyPerSecond", "Энергия в секунду") + $": {weapon.energyPerSecond:0.##}";
            return weapon == null ? PortalArsenalPopup.T("slots.empty", "Пустой слот. Назначьте оружие в магической башне.") :
                PortalArsenalPopup.WeaponTitle(weapon) + "\n" + string.Format(PortalArsenalPopup.T("slots.cost", "Энергия за выстрел: {0}"), Mathf.Max(1, weapon.energyPerShot));
        }
        public bool BeginDrag(PortalTowerBalance.Weapon weapon, PointerEventData pointer)
        {
            if (!AllowsAssignment || !Owner.CanAssign(weapon)) return false;
            EndDrag();
            var go = new GameObject("Weapon drag preview", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(GetComponentInParent<Canvas>().rootCanvas.transform, false);
            dragIcon = go.GetComponent<UnityEngine.UI.Image>(); dragIcon.raycastTarget = false;
            ResourceIconSizing.Apply(dragIcon, balance.FindWeapon(weapon).icon); dragIcon.color = new Color(1,1,1,.85f);
            MoveDrag(pointer); return true;
        }
        public void MoveDrag(PointerEventData pointer)
        {
            if (dragIcon == null) return;
            var parent = (RectTransform)dragIcon.transform.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pointer.position, pointer.pressEventCamera, out var local)) dragIcon.rectTransform.anchoredPosition = local;
        }
        public void EndDrag() { if (dragIcon != null) Destroy(dragIcon.gameObject); dragIcon = null; }
    }
}
