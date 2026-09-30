using GameFoundation.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameFoundation.Bestiary
{
    /// <summary>One discovered monster in the library. The full stat card remains shared with World.</summary>
    public sealed class BestiaryEntryView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IScrollHandler
    {
        [SerializeField] private UnityEngine.UI.Image icon;
        [SerializeField] private UnityEngine.UI.Image background;
        [SerializeField] private UnityEngine.UI.Text title;
        [SerializeField] private UnityEngine.UI.Text role;
        [SerializeField] private Color normalBackground = new(.25f, .20f, .28f, .65f);
        [SerializeField] private Color hoverBackground = new(.42f, .32f, .46f, .9f);
        [SerializeField, Min(0f)] private float tooltipDelay = .18f;

        private UnitDescriptionDefinition definition;
        private UnitDescriptionTooltip tooltip;
        private bool hovering;
        private bool showing;
        private float showAt;

        public void Bind(UnitDescriptionDefinition data, UnitDescriptionTooltip detailsTooltip)
        {
            HideDetails();
            definition = data;
            tooltip = detailsTooltip;
            if (icon != null)
            {
                icon.enabled = data != null && data.Portrait != null;
                ResourceIconSizing.Apply(icon, data != null ? data.Portrait : null);
            }
            if (title != null) title.text = data != null ? data.Title : string.Empty;
            if (role != null) role.text = data != null ? data.Role : string.Empty;
        }

        public void OnPointerEnter(PointerEventData eventData) => BeginHover(tooltipDelay);
        public void OnPointerExit(PointerEventData eventData) => HideDetails();
        public void OnSelect(BaseEventData eventData) => BeginHover(0f);
        public void OnDeselect(BaseEventData eventData) => HideDetails();
        public void OnScroll(PointerEventData eventData)
        {
            if (showing && tooltip != null) tooltip.Scroll(this, eventData.scrollDelta.y);
        }

        private void Update()
        {
            if (!hovering || showing || definition == null || tooltip == null || Time.unscaledTime < showAt) return;
            tooltip.Show(this, definition, null, null);
            showing = true;
        }

        private void BeginHover(float delay)
        {
            hovering = true;
            showAt = Time.unscaledTime + delay;
            if (background != null) background.color = hoverBackground;
        }

        private void OnDisable() => HideDetails();

        private void HideDetails()
        {
            hovering = false;
            showing = false;
            if (tooltip != null) tooltip.Hide(this);
            if (background != null) background.color = normalBackground;
        }
    }
}
