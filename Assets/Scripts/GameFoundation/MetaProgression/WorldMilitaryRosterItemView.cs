using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GameFoundation.UI;

namespace GameFoundation.MetaProgression
{
    /// <summary>Inspector-authored visual template for one deployed warrior in the World HUD.</summary>
    public sealed class WorldMilitaryRosterItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IScrollHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private Image healthFill;
        [SerializeField] private MilitaryStarsView starsView;
        [SerializeField] private Color healthyColor = new(0.28f, 0.8f, 0.35f, 1f);
        [SerializeField] private Color warningColor = new(0.93f, 0.7f, 0.18f, 1f);
        [SerializeField] private Color criticalColor = new(0.9f, 0.18f, 0.18f, 1f);
        [Header("Описание при наведении")]
        [SerializeField] private UnitDescriptionCatalog descriptions;
        [SerializeField] private UnitDescriptionTooltip detailsTooltip;
        [SerializeField, Min(0f)] private float tooltipDelay = .18f;

        private Health health;
        private MilitaryExperience experience;
        private int starCount;
        private MilitaryProfile storedProfile;
        private UnitDescriptionDefinition description;
        private GameObject liveUnit;
        private float storedHealth = 1f;
        private float showAt;
        private bool hovering;
        private bool showing;

        public void SetUnit(GameObject unit)
        {
            Health unitHealth = unit != null ? unit.GetComponent<Health>() : null;
            MilitaryExperience unitExperience = unit != null ? unit.GetComponent<MilitaryExperience>() : null;
            SpriteRenderer spriteRenderer = unit != null ? unit.GetComponentInChildren<SpriteRenderer>() : null;
            SetIcon(spriteRenderer != null ? spriteRenderer.sprite : null, unitHealth != null ? unitHealth.NormalizedHealth : 0f,
                MilitaryExperienceService.Stars(unitExperience?.Profile));
            health = unitHealth;
            experience = unitExperience;
            liveUnit = unit;
            storedProfile = unitExperience?.Profile;
            description = descriptions != null ? descriptions.Find(storedProfile?.type) : null;
            if (description != null && description.resource != null && icon != null)
                ResourceIconSizing.Apply(icon, description.resource.resourceIcon);
        }

        public void SetIcon(Sprite sprite, float normalizedHealth, int stars = 0)
        {
            health = null;
            experience = null;
            liveUnit = null;
            storedProfile = null;
            description = null;
            storedHealth = normalizedHealth;
            starCount = stars;
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.preserveAspect = true;
            }
            SetHealth(normalizedHealth);
            if (starsView != null) starsView.SetStars(starCount);
        }

        public void Refresh()
        {
            SetHealth(health != null ? health.NormalizedHealth : storedProfile != null ? MilitaryExperienceService.HealthPercent(storedProfile) : storedHealth);
            if (experience != null) starCount = experience.Stars;
            else if (storedProfile != null) starCount = MilitaryExperienceService.Stars(storedProfile);
            if (starsView != null) starsView.SetStars(starCount);
        }

        public void SetProfile(ResourceType resource, MilitaryProfile profile)
        {
            SetIcon(resource != null ? resource.resourceIcon : null, MilitaryExperienceService.HealthPercent(profile), MilitaryExperienceService.Stars(profile));
            storedProfile = profile;
            description = descriptions != null && resource != null ? descriptions.Find(resource.name) : null;
            if (icon != null && resource != null) ResourceIconSizing.Apply(icon, resource.resourceIcon);
        }

        public void OnPointerEnter(PointerEventData eventData) { hovering = true; showAt = Time.unscaledTime + tooltipDelay; }
        public void OnPointerExit(PointerEventData eventData) => HideDetails();
        public void OnSelect(BaseEventData eventData) { hovering = true; showAt = Time.unscaledTime; }
        public void OnDeselect(BaseEventData eventData) => HideDetails();
        public void OnScroll(PointerEventData eventData) { if (showing && detailsTooltip != null) detailsTooltip.Scroll(this, eventData.scrollDelta.y); }
        private void Update()
        {
            if (!hovering || showing || Time.unscaledTime < showAt || description == null || detailsTooltip == null) return;
            detailsTooltip.Show(this, description, storedProfile, liveUnit);
            showing = true;
        }
        private void OnDisable() => HideDetails();
        private void HideDetails()
        {
            hovering = false; showing = false;
            if (detailsTooltip != null) detailsTooltip.Hide(this);
        }

        private void SetHealth(float value)
        {
            if (healthFill == null) return;
            value = Mathf.Clamp01(value);

            // The roster bar uses a plain UI image without a source sprite. In that
            // configuration Image.fillAmount can keep rendering the entire rectangle
            // on some Canvas render paths, despite holding the correct value. Resize
            // the fill from its left edge as well, so the displayed width always
            // matches the stored health on both the Base and World rosters.
            RectTransform fillRect = healthFill.rectTransform;
            RectTransform barRect = fillRect.parent as RectTransform;
            if (barRect != null)
            {
                fillRect.anchorMin = new Vector2(0f, .5f);
                fillRect.anchorMax = new Vector2(0f, .5f);
                fillRect.pivot = new Vector2(0f, .5f);
                fillRect.anchoredPosition = Vector2.zero;
                fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barRect.rect.width * value);
                fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barRect.rect.height);
            }

            healthFill.type = Image.Type.Simple;
            healthFill.fillAmount = value;
            healthFill.color = value <= .25f ? criticalColor : value <= .5f ? warningColor : healthyColor;
        }
    }
}
