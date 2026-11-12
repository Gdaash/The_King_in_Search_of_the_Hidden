using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    [DefaultExecutionOrder(1000)]
    public sealed class PopupMotion : MonoBehaviour
    {
        [SerializeField] private float moveTime = .25f;
        [SerializeField] private float settleTime = .1f;
        [SerializeField] private float closeTime = .25f;
        [SerializeField] private float verticalOffset = 24f;
        [SerializeField] private float initialScale = .95f;
        [SerializeField] private float peakScale = 1.02f;

        private RectTransform window;
        private CanvasWindowFit fit;
        private CanvasGroup group;
        private CanvasGroup dimmer;
        private Graphic backdrop;
        private float dimmerOpacity;
        private Color backdropColor;
        private Vector3 position, scale;
        private float elapsed, openingOpacity;
        private float offset, multiplier, closeOffset, closeScale;
        private bool closing, playing, interactable;
        private Action closed;

        internal static void ShowDimmed(GameObject popup)
        {
            if (Find(popup) is PopupMotion active && active.playing) return;
            if (popup.transform.Find("Window") is not RectTransform target) return;
            var backdrop = popup.transform.Find("Screen Dimmer")?.GetComponent<Graphic>();
            if (backdrop == null) popup.TryGetComponent(out backdrop);
            if (backdrop != null) Show(popup, target, backdrop);
        }

        public static PopupMotion Show(GameObject popup, RectTransform target, Graphic backdrop = null, CanvasGroup dimmer = null)
        {
            if (target == null) return null;
            var motion = Find(popup);
            if (motion == null)
            {
                var template = Resources.Load<GameObject>("UI/Popup Motion");
                if (template == null) return null;
                motion = Instantiate(template, popup.transform).GetComponent<PopupMotion>();
                motion.name = template.name;
            }
            motion.Open(target, backdrop, dimmer);
            return motion;
        }

        private static PopupMotion Find(GameObject popup) =>
            popup.transform.Find("Popup Motion")?.GetComponent<PopupMotion>();

        public static void Hide(GameObject popup, Action afterClose = null)
        {
            if (popup == null || !popup.activeInHierarchy) return;
            void Finish()
            {
                popup.SetActive(false);
                afterClose?.Invoke();
            }
            var motion = Find(popup);
            if (motion != null) motion.Close(Finish);
            else Finish();
        }

        public static void AnimateClose(GameObject popup, Action finish)
        {
            var motion = Find(popup);
            if (motion != null) motion.Close(finish);
            else finish();
        }

        private void Open(RectTransform target, Graphic shade, CanvasGroup shadeGroup)
        {
            Restore();
            window = target;
            fit = target.GetComponent<CanvasWindowFit>();
            if (fit == null && target.GetComponentInParent<Canvas>() is Canvas canvas)
                CanvasWindowFit.CenterVertically(target, canvas.rootCanvas.transform as RectTransform);
            if (!target.TryGetComponent(out group)) group = target.gameObject.AddComponent<CanvasGroup>();
            dimmer = shadeGroup;
            backdrop = shade;
            if (dimmer != null) dimmerOpacity = dimmer.alpha;
            if (backdrop != null) backdropColor = backdrop.color;
            position = target.localPosition;
            scale = target.localScale;
            interactable = group.interactable;
            group.interactable = false;
            elapsed = 0;
            closing = false;
            playing = true;
            Apply(0);
        }

        public void Close(Action onClosed)
        {
            if (closing) return;
            if (window == null) { onClosed(); return; }
            openingOpacity = group.alpha;
            closeOffset = offset;
            closeScale = multiplier;
            group.interactable = false;
            elapsed = 0;
            closing = true;
            playing = true;
            closed = onClosed;
        }

        private void LateUpdate()
        {
            if (!playing) return;
            float duration = closing ? closeTime : moveTime + settleTime;
            elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
            Apply(elapsed);
            if (elapsed < duration) return;
            var callback = closed;
            Restore();
            callback?.Invoke();
        }

        private void Apply(float time)
        {
            if (fit != null)
            {
                window.localPosition = position;
                window.localScale = scale;
                fit.Fit();
                position = window.localPosition;
                scale = window.localScale;
            }
            float progress = Mathf.Clamp01(time / (closing ? closeTime : moveTime));
            float eased = Mathf.SmoothStep(0, 1, progress);
            offset = closing ? Mathf.Lerp(closeOffset, 1, eased) : 1 - eased;
            multiplier = closing ? Mathf.Lerp(closeScale, initialScale, eased) :
                time <= moveTime ? Mathf.Lerp(initialScale, peakScale, eased) :
                Mathf.SmoothStep(peakScale, 1, (time - moveTime) / settleTime);
            window.localPosition = position + Vector3.down * (verticalOffset * offset);
            window.localScale = Vector3.Scale(scale, new Vector3(multiplier, multiplier, 1));
            group.alpha = closing ? openingOpacity * (1 - eased) : eased;
            if (dimmer != null) dimmer.alpha = dimmerOpacity * group.alpha;
            if (backdrop != null)
            {
                Color color = backdropColor;
                color.a *= group.alpha;
                backdrop.color = color;
            }
        }

        private void OnDisable() => Restore();

        private void Restore()
        {
            if (!playing) return;
            playing = closing = false;
            closed = null;
            if (dimmer != null) dimmer.alpha = dimmerOpacity;
            if (backdrop != null) backdrop.color = backdropColor;
            if (window == null) return;
            window.localPosition = position;
            window.localScale = scale;
            group.alpha = 1;
            group.interactable = interactable;
        }
    }
}
