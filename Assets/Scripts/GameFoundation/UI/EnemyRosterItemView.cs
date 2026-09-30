using GameFoundation.Localization;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameFoundation.UI
{
    /// <summary>One enemy type and its living population; shares the player's stat tooltip.</summary>
    public sealed class EnemyRosterItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IScrollHandler
    {
        [SerializeField] private UnityEngine.UI.Image icon;
        [SerializeField] private UnityEngine.UI.Image background;
        [SerializeField] private UnityEngine.UI.Text title;
        [SerializeField] private UnityEngine.UI.Text amount;
        [SerializeField] private Color normalBackground = new(.25f, .20f, .28f, .65f);
        [SerializeField] private Color hoverBackground = new(.42f, .32f, .46f, .9f);
        [SerializeField] private Color aliveColor = new(.96f, .92f, .82f);
        [SerializeField] private Color emptyColor = new(.57f, .53f, .60f);
        [SerializeField, Min(0f)] private float tooltipDelay = .18f;
        private UnitDescriptionDefinition definition;
        private UnitDescriptionTooltip tooltip;
        private LocalizationService localization;
        private bool hovering, showing;
        private float showAt;

        public void Bind(UnitDescriptionDefinition data, UnitDescriptionTooltip sharedTooltip, int count)
        {
            HideDetails();
            definition = data;
            tooltip = sharedTooltip;
            if (icon != null)
            {
                icon.enabled = data != null && data.Portrait != null;
                ResourceIconSizing.Apply(icon, data != null ? data.Portrait : null);
            }
            RefreshTitle();
            SetCount(count);
        }
        public void SetCount(int count)
        {
            if (amount != null) { amount.text = Mathf.Max(0, count).ToString(); amount.color = count > 0 ? aliveColor : emptyColor; }
            if (icon != null) icon.color = count > 0 ? Color.white : new Color(.65f, .65f, .65f, 1f);
        }
        private void RefreshTitle() { if (title != null && definition != null) title.text = definition.Title; }
        private void OnEnable()
        {
            localization = LocalizationService.Instance;
            if (localization != null) localization.LanguageChanged += RefreshTitle;
        }
        private void OnDisable()
        {
            if (localization != null) localization.LanguageChanged -= RefreshTitle;
            HideDetails();
        }
        public void OnPointerEnter(PointerEventData data) => BeginHover(tooltipDelay);
        public void OnPointerExit(PointerEventData data) => HideDetails();
        public void OnSelect(BaseEventData data) => BeginHover(0f);
        public void OnDeselect(BaseEventData data) => HideDetails();
        public void OnScroll(PointerEventData data) { if (showing && tooltip != null) tooltip.Scroll(this, data.scrollDelta.y); }
        private void BeginHover(float delay)
        {
            hovering = true; showAt = Time.unscaledTime + delay;
            if (background != null) background.color = hoverBackground;
        }
        private void Update()
        {
            if (!hovering || showing || Time.unscaledTime < showAt || definition == null || tooltip == null) return;
            // A group describes the type, not a random injured member of that type.
            tooltip.Show(this, definition, null, null);
            showing = true;
        }
        private void HideDetails()
        {
            hovering = showing = false;
            if (tooltip != null) tooltip.Hide(this);
            if (background != null) background.color = normalBackground;
        }
    }
}
