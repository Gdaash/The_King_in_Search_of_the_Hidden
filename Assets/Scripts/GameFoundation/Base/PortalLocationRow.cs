using GameFoundation.MetaProgression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class PortalLocationRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
    {
        public PortalLocationDefinition location;
        public Button button;
        public Image surface, border, selection, lockIcon;
        public Text title, status;
        public Color selectedColor = new(.74f, .64f, .42f);
        public Color normalColor = new(.35f, .30f, .39f);
        private PortalPopupView owner;
        private bool hovering;
        private bool selected;

        private void Awake() => button.onClick.AddListener(Choose);
        public void Bind(PortalPopupView view) => owner = view;
        private void Choose() => owner?.Select(location);
        public void OnSelect(BaseEventData _) => Choose();
        public void OnPointerEnter(PointerEventData _) { hovering = true; UpdateColor(); }
        public void OnPointerExit(PointerEventData _) { hovering = false; UpdateColor(); }
        private void OnDisable() { hovering = false; }

        public void Refresh(PortalLocationDefinition current)
        {
            selected = current == location;
            bool unlocked = PortalProgression.IsUnlocked(location);
            title.text = PortalProgression.Name(location);
            title.color = unlocked ? new Color(.94f, .91f, .82f) : new Color(.68f, .63f, .72f);
            status.text = unlocked ? PortalPopupView.Tr("base.portal.available", "Доступно") :
                string.Format(PortalPopupView.Tr("base.portal.requires_level", "Улучшите портал до ур. {0}"), location.RequiredPortalLevel);
            status.color = unlocked ? new Color(.58f, .77f, .52f) : new Color(.68f, .63f, .72f);
            lockIcon.enabled = !unlocked;
            selection.enabled = selected;
            // Locked destinations can be selected to preview their contents.
            button.interactable = true;
            UpdateColor();
        }

        private void UpdateColor()
        {
            border.color = selected ? selectedColor : hovering ? Color.Lerp(normalColor, selectedColor, .5f) : normalColor;
            surface.color = selected || hovering ? new Color(.20f, .16f, .24f) : new Color(.14f, .115f, .17f);
        }
    }
}
