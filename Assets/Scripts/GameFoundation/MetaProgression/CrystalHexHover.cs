using System;
using GameFoundation.Localization;
using TMPro;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    /// <summary>Two authored views let the previous hex fade out while the next fades in.</summary>
    public sealed class CrystalHexHover : MonoBehaviour
    {
        [Serializable]
        private sealed class View
        {
            public Transform root;
            public SpriteRenderer frame;
            public TMP_Text caption;
            public CanvasGroup captionGroup;
            public GameObject recallRow;
            public TMP_Text recallCaption;
            public CanvasGroup energyGroup;
            public CrystalCellBar energyBar;
            public HexAlarmPreview alarmPreview;
            [NonSerialized] public Transform anchor;
            [NonSerialized] public LogisticFlag flag;
            [NonSerialized] public WorldFlashlightAvailability.HoverState state;
            [NonSerialized] public float alpha, clickTime = -1, clickAlpha;
            [NonSerialized] public bool canRecall;
        }
        [SerializeField] private WorldFlashlightAvailability crystal;
        [SerializeField] private Camera worldCamera;
        [Header("Flag D colour variants")]
        [SerializeField] private Sprite availableSprite;
        [SerializeField] private Sprite workingSprite;
        [SerializeField] private Sprite cancelSprite;
        [SerializeField] private Sprite noEnergySprite;
        [Header("Animation (unscaled seconds)")]
        [SerializeField, Min(.01f)] private float fadeSeconds = .3f;
        [SerializeField, Min(.01f)] private float clickSeconds = .22f;
        [SerializeField, Range(0, .5f)] private float bumpAmount = .12f;
        [SerializeField] private View[] views;
        private int current = -1;
        private WorldFlashlightAvailability.HoverTarget desired;

        private void Awake()
        {
            if (crystal == null) crystal = FindFirstObjectByType<WorldFlashlightAvailability>();
            if (worldCamera == null) worldCamera = Camera.main;
            foreach (var view in views)
            { view.root.gameObject.SetActive(false); if (view.energyBar != null) view.energyBar.Bind(crystal); Paint(view, 0); }
        }
        private void OnEnable() { if (crystal != null) crystal.TargetClicked += AnimateClick; }
        private void OnDisable()
        {
            if (crystal != null) crystal.TargetClicked -= AnimateClick;
            foreach (var view in views)
            { ReleaseFlag(view); view.alpha = 0; view.clickTime = -1; view.root.gameObject.SetActive(false); }
            current = -1;
        }
        private void LateUpdate()
        {
            desired = default;
            if (crystal != null && worldCamera != null && !crystal.Escaped &&
                Input.mousePosition.x >= 0 && Input.mousePosition.y >= 0 &&
                Input.mousePosition.x <= Screen.width && Input.mousePosition.y <= Screen.height &&
                !crystal.IsPointerOverBlockingUI(Input.mousePosition))
                desired = crystal.GetHoverTarget(worldCamera.ScreenToWorldPoint(Input.mousePosition));
            Present(desired, Time.unscaledDeltaTime);
        }
        public void Present(WorldFlashlightAvailability.HoverTarget target, float deltaTime)
        {
            desired = target;
            if (desired.Anchor != null && (current < 0 || views[current].anchor != desired.Anchor))
            {
                current = (current + 1) % views.Length;
                var view = views[current];
                ReleaseFlag(view);
                view.anchor = desired.Anchor;
                view.alpha = 0;
                view.clickTime = -1;
                SetState(view, desired.State);
                view.root.gameObject.SetActive(true);
            }
            float dt = Mathf.Max(0, deltaTime);
            for (int i = 0; i < views.Length; i++)
            {
                var view = views[i];
                bool show = i == current && desired.Anchor != null && desired.Anchor == view.anchor;
                if (view.anchor != null) view.root.position = view.anchor.position;
                // The visual stays at the tile origin without inheriting its destruction/bump animation.
                view.root.rotation = Quaternion.identity;
                if (show && view.flag != desired.Flag) { ReleaseFlag(view); view.flag = desired.Flag; }
                if (show)
                {
                    view.canRecall = desired.CanRecallHumans;
                    if (view.alarmPreview != null) view.alarmPreview.Show(crystal.GetActionAlarm(desired));
                }
                if (view.flag != null) view.flag.SetCrystalHoverVisible(true);
                if (view.clickTime >= 0)
                {
                    view.clickTime += dt;
                    float t = Mathf.Clamp01(view.clickTime / Mathf.Max(.01f, clickSeconds));
                    view.root.localScale = Vector3.one * (1 + bumpAmount * Mathf.Sin(t * Mathf.PI));
                    view.alpha = view.clickAlpha * (1 - t);
                    if (t >= 1) { view.clickTime = -1; if (show) SetState(view, desired.State); }
                }
                else
                {
                    view.root.localScale = Vector3.one;
                    bool stateChanged = show && view.state != desired.State;
                    view.alpha = Mathf.MoveTowards(view.alpha, show && !stateChanged ? 1 : 0, dt / Mathf.Max(.01f, fadeSeconds));
                    if (view.alpha == 0 && stateChanged) SetState(view, desired.State);
                }
                Paint(view, view.alpha);
                if (!show && view.alpha == 0 && view.clickTime < 0)
                { ReleaseFlag(view); view.root.gameObject.SetActive(false); view.anchor = null; }
            }
        }
        public void AnimateClick(WorldFlashlightAvailability.HoverTarget target)
        {
            if (current < 0) return;
            var view = views[current];
            if (view.anchor != target.Anchor) return;
            view.clickAlpha = view.alpha;
            view.clickTime = 0;
        }
        private void SetState(View view, WorldFlashlightAvailability.HoverState state)
        {
            view.state = state;
            view.frame.sprite = state switch
            {
                WorldFlashlightAvailability.HoverState.WaitingForResources => cancelSprite,
                WorldFlashlightAvailability.HoverState.RecallOnly => cancelSprite,
                WorldFlashlightAvailability.HoverState.Working => workingSprite,
                WorldFlashlightAvailability.HoverState.NoEnergy => noEnergySprite,
                WorldFlashlightAvailability.HoverState.CrystalBusy => noEnergySprite,
                _ => availableSprite
            };
        }
        private static void Paint(View view, float alpha)
        {
            view.frame.color = new Color(1, 1, 1, alpha);
            string key = view.state == WorldFlashlightAvailability.HoverState.WaitingForResources ? "world.crystal.cancel" :
                view.state == WorldFlashlightAvailability.HoverState.NoEnergy ? "world.crystal.no_energy" :
                view.state == WorldFlashlightAvailability.HoverState.CrystalBusy ? "world.crystal.busy" : null;
            string text = key == null ? "" : LocalizationService.Instance?.Get(key);
            if (key != null && (string.IsNullOrEmpty(text) || text == key))
                text = view.state == WorldFlashlightAvailability.HoverState.WaitingForResources ? "отменить действие" :
                    view.state == WorldFlashlightAvailability.HoverState.CrystalBusy ? "кристалл занят" : "нет энергии";
            if (view.caption.text != text) view.caption.text = text;
            bool canCancel = view.state == WorldFlashlightAvailability.HoverState.WaitingForResources;
            bool showMouseRow = canCancel || view.canRecall;
            bool hasCaption = !string.IsNullOrEmpty(text) && !canCancel;
            view.caption.gameObject.SetActive(hasCaption);
            view.caption.alpha = view.captionGroup != null ? 1 : alpha;
            if (view.recallRow != null) view.recallRow.SetActive(showMouseRow);
            if (view.recallCaption != null)
            {
                const string recallKey = "world.crystal.recall_humans";
                string recall = LocalizationService.Instance?.Get(recallKey);
                if (string.IsNullOrEmpty(recall) || recall == recallKey) recall = "Вернуть людей в портал";
                if (canCancel) recall = text;
                if (view.recallCaption.text != recall) view.recallCaption.text = recall;
            }
            if (view.captionGroup != null)
            {
                view.captionGroup.alpha = hasCaption || showMouseRow ? alpha : 0;
                // Width is fitted to the visible text/icon rows by the prefab's layout.
                ((RectTransform)view.captionGroup.transform).SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical, showMouseRow ? (hasCaption ? 98 : 66) : 48);
            }
            if (view.energyGroup != null)
                view.energyGroup.alpha = view.state == WorldFlashlightAvailability.HoverState.Available ||
                    view.state == WorldFlashlightAvailability.HoverState.NoEnergy ||
                    view.state == WorldFlashlightAvailability.HoverState.CrystalBusy ? alpha : 0;
        }
        private static void ReleaseFlag(View view)
        { if (view.flag != null) view.flag.SetCrystalHoverVisible(false); view.flag = null; }
    }
}
