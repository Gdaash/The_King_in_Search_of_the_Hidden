using GameFoundation.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameFoundation.MetaProgression
{
    /// <summary>Shows the current food warning in the standard tooltip while Escape is hovered.</summary>
    [DisallowMultipleComponent]
    public sealed class FoodSufficiencyHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private ResourceType foodResource;
        [SerializeField] private ResourceType[] dailyFoodConsumers;
        [SerializeField] private Color enoughColor = new(0.43f, 0.86f, 0.46f, 1f);
        [SerializeField] private Color insufficientColor = new(1f, 0.36f, 0.38f, 1f);

        private bool suppressed;
        private bool hovering;
        private bool tooltipShown;
        private RectTransform tooltipTarget;

        private void Awake() => tooltipTarget = transform as RectTransform;

        private void OnEnable()
        {
            suppressed = false;
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
        }

        private void OnDisable()
        {
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
            HideTooltip();
        }

        private void Update()
        {
            // Retry if the standard tooltip manager appears after this HUD.
            if (tooltipShown && TooltipManager.Instance == null) tooltipShown = false;
            if (hovering && !suppressed && !tooltipShown)
                ShowTooltip();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
            if (!suppressed) ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData) => HideTooltip();

        public void HideForEscape()
        {
            suppressed = true;
            HideTooltip();
        }

        private void OnResourceChanged(ResourceType resource, int amount)
        {
            if (suppressed || !hovering || !IsRelevant(resource)) return;
            ShowTooltip();
        }

        private bool IsRelevant(ResourceType changed)
        {
            if (changed == null) return false;
            if (changed == foodResource) return true;
            if (dailyFoodConsumers == null) return false;
            foreach (ResourceType consumer in dailyFoodConsumers)
                if (consumer == changed) return true;
            return false;
        }

        private bool HasEnoughFood()
        {
            GlobalResourceManager resources = GlobalResourceManager.Instance;
            if (resources == null || foodResource == null || dailyFoodConsumers == null) return false;

            int requiredFood = 0;
            foreach (ResourceType consumer in dailyFoodConsumers)
                if (consumer != null)
                    requiredFood += Mathf.Max(0, resources.GetResourceAmount(consumer));

            return resources.GetResourceAmount(foodResource) >= requiredFood;
        }

        private void ShowTooltip()
        {
            if (TooltipManager.Instance == null) return;
            if (GlobalResourceManager.Instance == null || foodResource == null || dailyFoodConsumers == null)
                return;
            if (tooltipTarget == null) tooltipTarget = transform as RectTransform;
            if (tooltipTarget == null) return;

            bool enough = HasEnoughFood();
            Color color = enough ? enoughColor : insufficientColor;
            string hex = ColorUtility.ToHtmlStringRGB(color);
            string message = enough ? "Еды хватает" : "Недостаточно еды";
            TooltipManager.Instance.Show($"<color=#{hex}>{message}</color>", tooltipTarget);
            tooltipShown = true;
        }

        private void HideTooltip()
        {
            bool shouldHide = tooltipShown;
            hovering = false;
            tooltipShown = false;
            if (shouldHide && TooltipManager.Instance != null) TooltipManager.Instance.Hide();
        }
    }
}
