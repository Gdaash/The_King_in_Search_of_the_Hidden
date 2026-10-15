using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace GameFoundation.Quests
{
    [RequireComponent(typeof(Button))]
    public sealed class DialogueHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
    {
        [SerializeField] private Color holdFillColor = new Color(.9811321f, .8801197f, .33784264f, 1);
        [SerializeField, Min(.1f)] private float holdDuration = .7f;
        [SerializeField] private Material holdTintMaterial;
        [SerializeField] private Sprite leftMouseSprite;
        [SerializeField] private Sprite tooltipBackground;
        private Material fillMaterial;
        private GameObject holdTooltip;
        private TextMeshProUGUI tooltipLabel;
        private bool hovered;
        private Button button;
        private RectTransform clip;
        private Func<bool> ready;
        private Action complete;
        private float duration, elapsed;
        private bool holding;
        private bool consumeClick;
        public bool ConsumeClick() { bool value = consumeClick; consumeClick = false; return value; }
        public float Progress => duration > 0 ? elapsed / duration : 0;

        public void Configure(Func<bool> canHold, Action onComplete)
            => Configure(canHold, onComplete, holdDuration, holdFillColor);

        public void Configure(Func<bool> canHold, Action onComplete, float seconds, Color color)
        {
            ready = canHold; complete = onComplete; duration = Mathf.Max(.1f, seconds);
            button = GetComponent<Button>();
            if (clip == null && button.targetGraphic is Image source)
            {
                var mask = new GameObject("Hold Progress", typeof(RectTransform), typeof(RectMask2D), typeof(LayoutElement));
                clip = (RectTransform)mask.transform;
                clip.SetParent(source.transform, false); clip.SetAsFirstSibling();
                mask.GetComponent<LayoutElement>().ignoreLayout = true;
                clip.anchorMin = Vector2.zero; clip.anchorMax = new Vector2(0, 1);
                clip.pivot = new Vector2(0, .5f); clip.offsetMin = clip.offsetMax = Vector2.zero;
                var fill = new GameObject("Button Fill", typeof(RectTransform), typeof(Image));
                var fillRect = (RectTransform)fill.transform; fillRect.SetParent(clip, false);
                fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = new Vector2(0, 1);
                fillRect.pivot = new Vector2(0, .5f); fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
                var image = fill.GetComponent<Image>(); image.sprite = source.sprite; image.type = source.type;
                image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier; image.color = color; image.raycastTarget = false;
                if (holdTintMaterial != null)
                {
                    fillMaterial = new Material(holdTintMaterial);
                    fillMaterial.SetColor("_Color", color);
                    fillMaterial.SetFloat("_Brightness", 3);
                    image.material = fillMaterial;
                    image.color = Color.white;
                }
            }
            CreateTooltip();
            ResetHold();
        }
        private void CreateTooltip()
        {
            if (holdTooltip != null || leftMouseSprite == null) return;
            holdTooltip = new GameObject("Hold LMB Tooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)holdTooltip.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -12);
            rect.sizeDelta = new Vector2(180, 56);
            var background = holdTooltip.GetComponent<Image>();
            background.sprite = tooltipBackground; background.type = Image.Type.Sliced; background.raycastTarget = false;
            var textObject = new GameObject("Hint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(rect, false);
            tooltipLabel = textObject.GetComponent<TextMeshProUGUI>();
            var source = GetComponentInChildren<TMP_Text>(true);
            if (source != null) tooltipLabel.font = source.font;
            tooltipLabel.fontSize = 24;
            tooltipLabel.alignment = TextAlignmentOptions.Center; tooltipLabel.color = new Color(1, .9f, .55f);
            tooltipLabel.raycastTarget = false;
            tooltipLabel.rectTransform.anchorMin = tooltipLabel.rectTransform.anchorMax = new Vector2(.5f, .5f);
            tooltipLabel.rectTransform.sizeDelta = new Vector2(88, 48);
            tooltipLabel.rectTransform.anchoredPosition = new Vector2(-26, 0);
            var iconObject = new GameObject("LMB Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(rect, false);
            var icon = iconObject.GetComponent<Image>(); icon.sprite = leftMouseSprite; icon.raycastTarget = false;
            icon.rectTransform.sizeDelta = leftMouseSprite.rect.size * 2;
            icon.rectTransform.anchoredPosition = new Vector2(52, 0);
            holdTooltip.SetActive(false);
        }
        public void OnPointerEnter(PointerEventData e) { hovered = true; RefreshTooltip(); }
        private void RefreshTooltip()
        {
            if (holdTooltip == null) return;
            bool visible = hovered && button != null && button.IsInteractable() && ready != null && ready();
            holdTooltip.SetActive(visible);
            if (visible) tooltipLabel.text = (GameFoundation.Localization.LocalizationService.Instance?.Language == "en" ? "Hold" : "Зажми");
        }
        public void OnPointerDown(PointerEventData e)
        {
            consumeClick = false;
            if (e.button != PointerEventData.InputButton.Left || button == null || !button.IsInteractable() || ready == null || !ready()) return;
            ResetHold(); holding = true;
        }
        public void OnPointerUp(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) ResetHold(); }
        public void OnPointerExit(PointerEventData e) { hovered = false; RefreshTooltip(); ResetHold(); }
        public void ResetHold()
        {
            holding = false; elapsed = 0;
            if (clip != null) clip.gameObject.SetActive(false);
        }
        private void OnDisable() { hovered = false; if (holdTooltip != null) holdTooltip.SetActive(false); ResetHold(); }
        private void OnDestroy() { if (fillMaterial != null) Destroy(fillMaterial); }
        private void Update()
        {
            RefreshTooltip();
            if (!holding) return;
            if (ready == null || !ready() || !button.IsInteractable()) { ResetHold(); return; }
            elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
            if (clip != null)
            {
                clip.gameObject.SetActive(true);
                float width = ((RectTransform)button.targetGraphic.transform).rect.width;
                clip.sizeDelta = new Vector2(width * Progress, 0);
                var fill = (RectTransform)clip.GetChild(0); fill.sizeDelta = new Vector2(width, 0);
            }
            if (elapsed < duration) return;
            ResetHold(); consumeClick = true; complete?.Invoke();
        }
    }
}
