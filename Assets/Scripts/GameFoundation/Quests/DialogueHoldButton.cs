using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.Quests
{
    [RequireComponent(typeof(Button))]
    public sealed class DialogueHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Button button;
        private RectTransform clip;
        private Func<bool> ready;
        private Action complete;
        private float duration, elapsed;
        private bool holding;
        private bool consumeClick;
        public bool ConsumeClick() { bool value = consumeClick; consumeClick = false; return value; }
        public float Progress => duration > 0 ? elapsed / duration : 0;

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
            }
            ResetHold();
        }
        public void OnPointerDown(PointerEventData e)
        {
            consumeClick = false;
            if (e.button != PointerEventData.InputButton.Left || button == null || !button.IsInteractable() || ready == null || !ready()) return;
            ResetHold(); holding = true;
        }
        public void OnPointerUp(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) ResetHold(); }
        public void OnPointerExit(PointerEventData e) => ResetHold();
        public void ResetHold()
        {
            holding = false; elapsed = 0;
            if (clip != null) clip.gameObject.SetActive(false);
        }
        private void OnDisable() => ResetHold();
        private void Update()
        {
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
