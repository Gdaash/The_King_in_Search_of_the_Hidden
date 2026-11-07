using GameFoundation.Audio;
using GameFoundation.Localization;
using GameFoundation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    /// <summary>Prototype recruitment entry in the warehouse window until faction selection is available.</summary>
    public sealed class EngineeringBureauRecruitment : MonoBehaviour
    {
        private const int WoodCost = 5;
        private ResourceType human, wood, engineer;
        private Button recruit;
        private TMP_Text title, label, count, humanPrice, woodPrice;

        public void Initialize(Button template, ResourceType resident)
        {
            human = resident;
            wood = ResourceCatalog.Find("Wood");
            engineer = ResourceCatalog.Find("Engineer");
            if (human == null || wood == null || engineer == null) return;

            if ((transform.Find("Window") ?? transform) is not RectTransform parent) return;
            var card = CreateRect("Engineering Bureau", parent, new Vector2(820, 172), new Vector2(0, -60));
            var background = card.gameObject.AddComponent<Image>();
            background.sprite = Resources.Load<ButtonVisualTheme>("UI/ButtonVisualTheme")?.popupPanelSprite;
            background.type = Image.Type.Sliced;
            title = Text("Title", card, 25, new Vector2(0, 49), new Vector2(750, 36));
            var action = CreateRect("Recruit Engineer", card, new Vector2(640, 72), new Vector2(-60, -29));
            var image = action.gameObject.AddComponent<Image>();
            if (template != null && template.targetGraphic is Image templateImage)
            {
                image.sprite = templateImage.sprite;
                image.type = templateImage.type;
                image.color = templateImage.color;
            }
            else { image.sprite = Resources.Load<ButtonVisualTheme>("UI/ButtonVisualTheme")?.textButtonSprite; image.type = Image.Type.Sliced; }
            recruit = action.gameObject.AddComponent<Button>();
            recruit.targetGraphic = image;
            recruit.transition = Selectable.Transition.None;
            recruit.onClick.AddListener(Recruit);
            action.gameObject.AddComponent<UnifiedButtonFeedback>();
            label = Text("Action label", action, 22, new Vector2(-128, 0), new Vector2(340, 50));
            label.alignment = TextAlignmentOptions.MidlineLeft;
            action.gameObject.AddComponent<ActionButtonLabelColor>().Configure(null, label);
            humanPrice = Cost(action, human, 1, 160);
            woodPrice = Cost(action, wood, WoodCost, 250);
            ResourceIcon(card, engineer, new Vector2(310, -29));
            count = Text("Engineers in reserve", card, 24, new Vector2(360, -29), new Vector2(48, 40));
            Refresh();
        }

        private void OnEnable() => GlobalResourceManager.OnResourceChanged += OnResourceChanged;
        private void OnDisable() => GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
        private void OnResourceChanged(ResourceType _, int __) => Refresh();

        private void Recruit()
        {
            var resources = GlobalResourceManager.Instance;
            if (resources == null || !resources.TryExchangeResources(human, 1, wood, WoodCost, engineer, 1)) return;
            GameAudioController.PlayUI(GameAudioCue.RecruitWarrior, .7f);
            GameNotifications.Post(Translate("engineering.recruited", "Инженер нанят"), NotificationKind.Positive, engineer.resourceIcon);
            Refresh();
        }

        public void Refresh()
        {
            if (recruit == null) return;
            title.text = Translate("engineering.bureau", "Инженерное бюро");
            label.text = Translate("engineering.recruit", "Нанять инженера");
            var resources = GlobalResourceManager.Instance;
            recruit.interactable = resources != null && resources.GetResourceAmount(human) >= 1 &&
                resources.GetResourceAmount(wood) >= WoodCost;
            BaseDisabledButtonHint.Set(recruit, BaseDisabledButtonHint.Reason.Resources);
            humanPrice.color = resources != null && resources.GetResourceAmount(human) >= 1 ? new Color(.55f,.85f,.35f) : new Color(1f,.3f,.35f);
            woodPrice.color = resources != null && resources.GetResourceAmount(wood) >= WoodCost ? new Color(.55f,.85f,.35f) : new Color(1f,.3f,.35f);
            count.text = (resources != null ? resources.GetResourceAmount(engineer) : 0).ToString();
        }

        private static string Translate(string key, string fallback)
        {
            var result = LocalizationService.Instance?.Get(key);
            return string.IsNullOrEmpty(result) || result == key ? fallback : result;
        }

        private static TMP_Text Cost(RectTransform parent, ResourceType resource, int amount, float x)
        {
            ResourceIcon(parent, resource, new Vector2(x - 10, 0));
            var text = Text("Cost", parent, 23, new Vector2(x + 24, 0), new Vector2(35, 38));
            text.text = amount.ToString();
            return text;
        }

        private static void ResourceIcon(RectTransform parent, ResourceType resource, Vector2 position)
        {
            var rect = CreateRect("Resource icon", parent, Vector2.zero, position);
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            ResourceIconSizing.Apply(image, resource.resourceIcon);
        }

        private static TMP_Text Text(string name, RectTransform parent, float size, Vector2 position, Vector2 dimensions)
        {
            var text = CreateRect(name, parent, dimensions, position).gameObject.AddComponent<TextMeshProUGUI>();
            var font = Resources.Load<TMP_FontAsset>("Fonts/Pixellari Reading UI");
            if (font != null) text.font = font;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = 22;
            text.fontSizeMax = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(.95f, .88f, .73f);
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRect(string name, RectTransform parent, Vector2 size, Vector2 position)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }
    }
}
