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

        internal void SetPurchase(PortalArsenalCatalog.Entry entry, bool unlocked)
        {
            bool showPrice = !unlocked && entry?.requirement == PortalArsenalCatalog.Requirement.Resource;
            if (showPrice)
                price ??= new PopupChoicePrice((RectTransform)button.transform, state.font, 20);
            price?.Set(showPrice ? entry.resource : null, showPrice ? entry.amount : 0);
            (state.text, state.color) = PopupChoiceRowStyle.PurchaseStatus(unlocked, entry == null ||
                PortalProgression.Level < entry.requiredPortalLevel, PortalArsenalProgress.CanUnlock(entry));
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
