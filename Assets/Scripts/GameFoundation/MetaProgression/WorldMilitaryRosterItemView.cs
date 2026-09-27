using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.MetaProgression
{
    /// <summary>Inspector-authored visual template for one deployed warrior in the World HUD.</summary>
    public sealed class WorldMilitaryRosterItemView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Image healthFill;
        [SerializeField] private MilitaryStarsView starsView;
        [SerializeField] private Color healthyColor = new(0.28f, 0.8f, 0.35f, 1f);
        [SerializeField] private Color warningColor = new(0.93f, 0.7f, 0.18f, 1f);
        [SerializeField] private Color criticalColor = new(0.9f, 0.18f, 0.18f, 1f);

        private Health health;
        private MilitaryExperience experience;
        private int starCount;

        public void SetUnit(GameObject unit)
        {
            Health unitHealth = unit != null ? unit.GetComponent<Health>() : null;
            MilitaryExperience unitExperience = unit != null ? unit.GetComponent<MilitaryExperience>() : null;
            SpriteRenderer spriteRenderer = unit != null ? unit.GetComponentInChildren<SpriteRenderer>() : null;
            SetIcon(spriteRenderer != null ? spriteRenderer.sprite : null, unitHealth != null ? unitHealth.NormalizedHealth : 0f,
                MilitaryExperienceService.Stars(unitExperience?.Profile));
            health = unitHealth;
            experience = unitExperience;
        }

        public void SetIcon(Sprite sprite, float normalizedHealth, int stars = 0)
        {
            health = null;
            experience = null;
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
            SetHealth(health != null ? health.NormalizedHealth : 0f);
            if (experience != null) starCount = experience.Stars;
            if (starsView != null) starsView.SetStars(starCount);
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
