using GameFoundation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
namespace GameFoundation.MetaProgression
{
    public sealed class PortalArsenalWeaponRow : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public UnityEngine.UI.Button button;
        public UnityEngine.UI.Image icon;
        public TMP_Text title, state;
        public GameFoundation.UI.UnifiedButtonFeedback feedback;
        private PortalArsenalPopup popup;
        private PortalTowerBalance.Weapon weapon;
        public PortalTowerBalance.Weapon Weapon => weapon;
        private bool dragging;
        private int lastDragFrame = -1;
        private PopupChoiceRowStyle rowStyle;
        private PopupChoicePrice price;
        private UnityEngine.UI.Image challengeLock;
        private TMP_Text challengeCount;
        private bool shiftedForLock;

        internal void SetPurchase(PortalArsenalCatalog.Entry entry, bool unlocked)
        {
            bool showPrice = !unlocked && entry?.requirement == PortalArsenalCatalog.Requirement.Resource;
            if (showPrice)
                price ??= new PopupChoicePrice((RectTransform)button.transform, state.font, 20);
            price?.Set(showPrice ? entry.resource : null, showPrice ? entry.amount : 0);
            bool locked = !unlocked && !PortalArsenalProgress.MeetsPrerequisites(entry);
            (state.text, state.color) = PopupChoiceRowStyle.PurchaseStatus(unlocked, locked, PortalArsenalProgress.CanUnlock(entry));
            if (unlocked && PortalWeaponEnhancements.Table?.Find(weapon) != null)
                state.text += "  " + PortalWeaponEnhancements.Rank(weapon) + "/" + PortalWeaponEnhancements.Table.Find(weapon).ranks.Count;
            if (locked && challengeLock == null)
            {
                challengeLock = new GameObject("Challenge lock", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
                challengeLock.transform.SetParent(button.transform, false);
                challengeLock.rectTransform.anchorMin = challengeLock.rectTransform.anchorMax = new Vector2(0, .5f);
                challengeLock.rectTransform.anchoredPosition = new Vector2(30, 12);
                ResourceIconSizing.Apply(challengeLock, Resources.Load<ButtonVisualTheme>("UI/ButtonVisualTheme").lockIconSprite);
                challengeLock.raycastTarget = false;
                challengeCount = new GameObject("Remaining enhancements", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
                challengeCount.transform.SetParent(button.transform, false);
                challengeCount.rectTransform.anchorMin = challengeCount.rectTransform.anchorMax = new Vector2(0, .5f);
                challengeCount.rectTransform.sizeDelta = new Vector2(42, 28);
                challengeCount.rectTransform.anchoredPosition = new Vector2(30, -16);
                challengeCount.font = state.font; challengeCount.fontSize = 22;
                challengeCount.alignment = TextAlignmentOptions.Center; challengeCount.raycastTarget = false;
            }
            if (challengeLock != null)
            {
                challengeLock.gameObject.SetActive(locked);
                challengeCount.gameObject.SetActive(locked && PortalArsenalProgress.RemainingEnhancements(entry) > 0);
                challengeCount.text = PortalArsenalProgress.RemainingEnhancements(entry).ToString();
            }
            if (locked != shiftedForLock)
            {
                float shift = locked ? 40 : -40;
                icon.rectTransform.anchoredPosition += new Vector2(shift, 0);
                foreach (var label in new[] { title, state }) label.rectTransform.offsetMin += new Vector2(shift, 0);
                shiftedForLock = locked;
            }
            var hint = PortalArsenalProgress.PrerequisiteHint(entry);
            GameFoundation.Base.BaseDisabledButtonHint.Set(button, GameFoundation.Base.BaseDisabledButtonHint.Reason.Research,
                actionDetail: locked ? hint : null);
        }

        internal void SetSelected(bool selected)
        {
            if (!popup.IsBaseArsenal)
            {
                feedback.Toggled = selected;
                return;
            }
            rowStyle ??= new PopupChoiceRowStyle(button, (UnityEngine.UI.Image)button.targetGraphic);
            rowStyle.SetSelected(selected);
        }
        public void Bind(PortalArsenalPopup view, PortalTowerBalance.Weapon value)
        {
            popup = view; weapon = value;
            button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => { if (!dragging && Time.frameCount > lastDragFrame) popup.ClickWeapon(weapon); });
        }
        public void OnPointerEnter(PointerEventData data) { if (popup != null && !popup.IsBaseArsenal) popup.PreviewWeapon(weapon); }
        public void OnSelect(BaseEventData data) { if (popup != null && !popup.IsBaseArsenal) popup.PreviewWeapon(weapon); }
        public void OnBeginDrag(PointerEventData data) { dragging = popup != null && popup.weaponSlots != null && popup.weaponSlots.BeginDrag(weapon, data); }
        public void OnDrag(PointerEventData data) { if (dragging) popup.weaponSlots.MoveDrag(data); }
        public void OnEndDrag(PointerEventData data) { if (dragging) { popup.weaponSlots.EndDrag(); lastDragFrame = Time.frameCount; } dragging = false; }
    }
}
