using DG.Tweening;
using TMPro;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    /// <summary>Shows food status beside the escape button when it changes during an expedition.</summary>
    [DisallowMultipleComponent]
    public sealed class FoodSufficiencyHint : MonoBehaviour
    {
        [SerializeField] private ResourceType foodResource;
        [SerializeField] private ResourceType[] dailyFoodConsumers;
        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color enoughColor = new(0.43f, 0.86f, 0.46f, 1f);
        [SerializeField] private Color insufficientColor = new(1f, 0.36f, 0.38f, 1f);
        [SerializeField, Min(0.05f)] private float slideDuration = 0.28f;
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.18f;

        private bool initialized;
        private bool wasEnough;
        private bool suppressed;
        private bool visible;
        private int lastFoodAmount;

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            InitializeHidden();
        }

        private void OnDisable()
        {
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
            KillTweens();
            SetVisualState(false);
        }

        public void HideForEscape()
        {
            suppressed = true;
            KillTweens();
            SetVisualState(false);
        }

        private void InitializeHidden()
        {
            initialized = false;
            suppressed = false;
            visible = false;
            SetVisualState(false);
            if (GlobalResourceManager.Instance == null || foodResource == null || dailyFoodConsumers == null)
                return;

            wasEnough = HasEnoughFood();
            lastFoodAmount = GlobalResourceManager.Instance.GetResourceAmount(foodResource);
            initialized = true;
        }

        private void OnResourceChanged(ResourceType resource, int amount)
        {
            if (suppressed || !IsRelevant(resource)) return;
            if (!initialized)
            {
                InitializeHidden();
                return;
            }

            bool foodWasSpent = resource == foodResource && amount < lastFoodAmount;
            if (resource == foodResource) lastFoodAmount = amount;
            bool enoughNow = HasEnoughFood();
            if (enoughNow == wasEnough)
            {
                if (foodWasSpent && !enoughNow) Show(false);
                return;
            }

            wasEnough = enoughNow;
            Show(enoughNow);
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

        private void Show(bool enough)
        {
            if (panel == null || canvasGroup == null || label == null) return;

            label.text = enough ? "Еды хватает" : "Недостаточно еды";
            label.color = enough ? enoughColor : insufficientColor;
            if (visible) return;

            visible = true;
            panel.gameObject.SetActive(true);
            KillTweens();
            SetScaleX(0f);
            canvasGroup.alpha = 0f;
            panel.DOScaleX(1f, slideDuration).SetEase(Ease.OutCubic).SetUpdate(true);
            canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);
        }

        private void SetVisualState(bool shown)
        {
            if (panel != null) SetScaleX(shown ? 1f : 0f);
            if (canvasGroup != null) canvasGroup.alpha = shown ? 1f : 0f;
            visible = shown;
        }

        private void SetScaleX(float value)
        {
            if (panel == null) return;
            Vector3 scale = panel.localScale;
            scale.x = value;
            panel.localScale = scale;
        }

        private void KillTweens()
        {
            if (panel != null) panel.DOKill();
            if (canvasGroup != null) canvasGroup.DOKill();
        }
    }
}
