using GameFoundation.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class BuildingConstructionConfirmation : MonoBehaviour
    {
        [SerializeField] private Text title;
        [SerializeField] private Image woodIcon, stoneIcon;
        [SerializeField] private Text woodAmount, stoneAmount;
        [SerializeField] private Button confirm, cancel;
        [SerializeField] private Text confirmLabel, cancelLabel;
        private BaseBuildingConstruction target;
        public BaseBuildingConstruction Target => target;

        private void Awake() { confirm.onClick.AddListener(Confirm); cancel.onClick.AddListener(Cancel); }
        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += ResourceChanged;
            if (LocalizationService.Instance != null) LocalizationService.Instance.LanguageChanged += Refresh;
            Refresh();
        }
        private void OnDisable()
        {
            GlobalResourceManager.OnResourceChanged -= ResourceChanged;
            if (LocalizationService.Instance != null) LocalizationService.Instance.LanguageChanged -= Refresh;
        }
        public void Open(BaseBuildingConstruction building)
        {
            target = building;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            Refresh();
            GameFoundation.UI.PopupMotion.Show(gameObject, transform.Find("Window") as RectTransform, GetComponent<Image>());
        }
        public void Cancel()
        {
            target = null;
            GameFoundation.UI.PopupMotion.Hide(gameObject);
        }
        public void Confirm()
        {
            // Clear the selection before spending: duplicate clicks cannot buy twice.
            var building = target;
            if (building == null || !building.CanAffordConstruction) return;
            target = null;
            GameFoundation.UI.PopupMotion.Hide(gameObject, building.ConfirmBuild);
        }
        private void ResourceChanged(ResourceType _, int __) => Refresh();
        private void Refresh()
        {
            if (target == null) return;
            bool en = LocalizationService.Instance?.Language == "en";
            title.text = (en ? "Build \"" : GameFoundation.Localization.GameText.Get("Построить \"")) + target.DisplayName + (en ? "\"?" : "\"?");
            confirmLabel.text = en ? "Yes" : GameFoundation.Localization.GameText.Get("Да");
            cancelLabel.text = en ? "No" : GameFoundation.Localization.GameText.Get("Нет");
            bool first = SetPrice(woodIcon, woodAmount, target.Wood, target.WoodCost);
            bool second = SetPrice(stoneIcon, stoneAmount, target.Stone, target.StoneCost);
            if (!first && !second)
            {
                woodAmount.enabled = true;
                woodAmount.text = en ? "Free" : GameFoundation.Localization.GameText.Get("Бесплатно");
                woodAmount.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 360);
                SetX(woodAmount.rectTransform, 0);
            }
            else if (confirm.GetComponent<GameFoundation.UI.PriceButtonLayout>() == null)
            {
                float firstWidth = first ? woodIcon.rectTransform.rect.width + 12 + woodAmount.rectTransform.rect.width : 0;
                float secondWidth = second ? stoneIcon.rectTransform.rect.width + 12 + stoneAmount.rectTransform.rect.width : 0;
                float x = -(firstWidth + secondWidth + (first && second ? 64 : 0)) * .5f;
                if (first) { PlacePrice(woodIcon, woodAmount, x); x += firstWidth + 64; }
                if (second) PlacePrice(stoneIcon, stoneAmount, x);
            }
            confirm.interactable = target.CanAffordConstruction;
            var layout = confirm.GetComponent<GameFoundation.UI.PriceButtonLayout>();
            if (layout != null) { layout.Invalidate(); layout.Refresh(); }
            BaseDisabledButtonHint.Set(confirm, BaseDisabledButtonHint.Reason.Resources);
        }

        private static bool SetPrice(Image icon, Text amount, ResourceType resource, int cost)
        {
            bool visible = resource != null && cost > 0;
            ResourceIconSizing.Apply(icon, resource != null ? resource.resourceIcon : null);
            icon.enabled = visible;
            amount.enabled = visible;
            amount.text = cost.ToString();
            amount.color = resource == null || GlobalResourceManager.Instance != null && GlobalResourceManager.Instance.GetResourceAmount(resource) >= cost
                ? new Color(.55f, .85f, .35f) : new Color(1f, .3f, .35f);
            amount.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(48, amount.preferredWidth + 12));
            return visible;
        }

        private static void PlacePrice(Image icon, Text amount, float left)
        {
            SetX(icon.rectTransform, left + icon.rectTransform.rect.width * .5f);
            SetX(amount.rectTransform, left + icon.rectTransform.rect.width + 12 + amount.rectTransform.rect.width * .5f);
        }

        private static void SetX(RectTransform rect, float x)
        {
            var position = rect.anchoredPosition;
            position.x = x;
            rect.anchoredPosition = position;
        }
    }
}
