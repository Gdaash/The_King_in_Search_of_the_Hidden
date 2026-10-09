using DG.Tweening;
using System.Collections;
using GameFoundation.Quests;
using GameFoundation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.MetaProgression
{
    public sealed class FirstPortalRunPresentation : MonoBehaviour
    {
        [SerializeField] private QuestDefinition suppliesQuest;
        [SerializeField] private AlarmSystem alarm;
        [SerializeField] private Button escapeButton;
        [SerializeField, Min(0f)] private float notificationEndDelay = 1f;
        [SerializeField, Min(1f)] private float introductionScale = 1.5f;
        [SerializeField, Min(0f)] private float centerHoldDuration = 0.6f;
        [SerializeField, Min(0.1f)] private float travelDuration = 0.9f;

        private bool firstRun;
        private bool released;
        private bool revealing;
        private Sequence introduction;
        private Coroutine revealDelay;
        private RectTransform homeSlot;
        private Vector2 homePosition;
        private Vector3 homeScale;

        public bool WaveReleased => released;

        private void Awake()
        {
            firstRun = DayCycleService.IsFirstPortalRun;
            if (!firstRun || escapeButton == null) return;
            var rect = (RectTransform)escapeButton.transform;
            homePosition = rect.anchoredPosition;
            homeScale = rect.localScale;
            escapeButton.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (!firstRun) return;
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            QuestProgress.Changed += CheckSupplies;
            if (alarm != null) alarm.DangerNotificationsFinished += RevealEscape;
        }

        private void Start() => CheckSupplies();

        private void OnDisable()
        {
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
            QuestProgress.Changed -= CheckSupplies;
            if (alarm != null) alarm.DangerNotificationsFinished -= RevealEscape;
            introduction?.Kill();
            if (revealDelay != null) StopCoroutine(revealDelay);
            if (homeSlot != null) Destroy(homeSlot.gameObject);
        }

        private void OnResourceChanged(ResourceType _, int __) => CheckSupplies();

        private void CheckSupplies()
        {
            if (!firstRun || released || alarm == null || suppliesQuest == null) return;
            var resources = GlobalResourceManager.Instance;
            if (!QuestProgress.IsComplete(suppliesQuest) &&
                (resources == null || !suppliesQuest.HasRequiredStock(resources.GetResourceAmount))) return;
            // Resource and quest events can arrive synchronously during the same delivery.
            released = true;
            if (!alarm.ReleaseFirstRunWave()) released = false;
        }

        private void RevealEscape()
        {
            if (!released || revealing || escapeButton == null) return;
            revealing = true;
            revealDelay = StartCoroutine(RevealAfterNotification());
        }

        private IEnumerator RevealAfterNotification()
        {
            yield return new WaitForSecondsRealtime(notificationEndDelay);
            revealDelay = null;
            AnimateEscape();
        }

        private void AnimateEscape()
        {
            escapeButton.gameObject.SetActive(true);
            escapeButton.interactable = false;
            var feedback = escapeButton.GetComponent<UnifiedButtonFeedback>();
            bool feedbackEnabled = feedback != null && feedback.enabled;
            if (feedback != null) feedback.enabled = false;
            var rect = (RectTransform)escapeButton.transform;
            var canvas = escapeButton.GetComponentInParent<Canvas>().rootCanvas;
            Canvas.ForceUpdateCanvases();
            var homeParent = (RectTransform)rect.parent;
            int homeSibling = rect.GetSiblingIndex();
            var homeAnchorMin = rect.anchorMin;
            var homeAnchorMax = rect.anchorMax;
            var homePivot = rect.pivot;
            var homeSize = rect.sizeDelta;
            var homeRotation = rect.localRotation;
            homePosition = rect.anchoredPosition;
            homeScale = rect.localScale;
            var visualSize = rect.rect.size;
            // Hold the layout slot while the actual button moves outside its LayoutGroup.
            homeSlot = new GameObject("Escape introduction home slot", typeof(RectTransform),
                typeof(LayoutElement)).GetComponent<RectTransform>();
            homeSlot.SetParent(homeParent, false);
            homeSlot.SetSiblingIndex(homeSibling);
            homeSlot.sizeDelta = visualSize;
            var layout = homeSlot.GetComponent<LayoutElement>();
            layout.preferredWidth = visualSize.x;
            layout.preferredHeight = visualSize.y;
            rect.SetParent(canvas.transform, true);
            var travelScale = rect.localScale;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = visualSize;
            Canvas.ForceUpdateCanvases();
            var targetWorld = homeSlot.TransformPoint(homeSlot.rect.center);
            rect.localScale = travelScale * introductionScale;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                (RectTransform)rect.parent, screenCenter, camera, out var centerWorld))
            {
                // Center the visible rectangle, not its pivot (which may be at a corner).
                rect.position = centerWorld - rect.TransformVector(rect.rect.center);
            }
            var group = escapeButton.GetComponent<CanvasGroup>();
            if (group == null) group = escapeButton.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            introduction = DOTween.Sequence().SetUpdate(true);
            introduction.Append(group.DOFade(1f, 0.25f));
            introduction.AppendInterval(centerHoldDuration);
            introduction.Append(rect.DOMove(targetWorld, travelDuration).SetEase(Ease.InOutCubic));
            introduction.Join(rect.DOScale(travelScale, travelDuration).SetEase(Ease.InOutCubic));
            introduction.OnComplete(() =>
            {
                rect.SetParent(homeParent, false);
                rect.SetSiblingIndex(homeSibling);
                rect.anchorMin = homeAnchorMin;
                rect.anchorMax = homeAnchorMax;
                rect.pivot = homePivot;
                rect.sizeDelta = homeSize;
                rect.anchoredPosition = homePosition;
                rect.localRotation = homeRotation;
                rect.localScale = homeScale;
                homeSlot.gameObject.SetActive(false);
                Destroy(homeSlot.gameObject);
                homeSlot = null;
                Canvas.ForceUpdateCanvases();
                group.blocksRaycasts = true;
                escapeButton.interactable = true;
                if (feedback != null) feedback.enabled = feedbackEnabled;
            });
        }
    }
}
