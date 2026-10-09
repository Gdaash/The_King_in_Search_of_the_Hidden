using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameFoundation.Base;
using GameFoundation.UI;

namespace GameFoundation.Quests
{
    /// <summary>Visual-only effect hosted outside the quest panel, which disappears on claim.</summary>
    public sealed class QuestRewardFlight : MonoBehaviour
    {
        sealed class Particle
        {
            public Image image, target;
            public RectTransform targetRect;
            public Canvas targetCanvas;
            public Color tint = Color.white;
            public float arcHeight = 65f;
            public Vector2 origin;
            public float delay;
        }
        readonly List<Particle> particles = new();
        RectTransform rect;
        Camera uiCamera;
        float elapsed;
        bool unlockFlight;
        const float Duration = .8f;

        public static void PlayUnlocks(Canvas canvas, Vector2 screenOrigin, List<ContentUnlockDefinition> unlocks)
        {
            if (canvas == null || unlocks == null || unlocks.Count == 0) return;
            canvas = canvas.rootCanvas;
            QuestRewardFlight effect = null;
            var used = new HashSet<RectTransform>();
            Canvas.ForceUpdateCanvases();
            foreach (var building in Object.FindObjectsByType<BaseBuildingConstruction>(FindObjectsSortMode.None))
            {
                if (building.IsBuilt || building.RequiredUnlock == null ||
                    !unlocks.Contains(building.RequiredUnlock) || building.ConstructionButton == null) continue;
                var button = building.ConstructionButton;
                var target = (RectTransform)button.transform;
                if (!used.Add(target)) continue;
                var highlight = button.GetComponent<BuildingButtonHighlight>() ?? building.GetComponent<BuildingButtonHighlight>();
                if (highlight == null || highlight.StarSprite == null) continue;
                if (effect == null)
                {
                    var go = new GameObject("Quest building unlock flight", typeof(RectTransform), typeof(Canvas), typeof(QuestRewardFlight));
                    go.transform.SetParent(canvas.transform, false);
                    var overlay = go.GetComponent<Canvas>();
                    overlay.overrideSorting = true; overlay.sortingOrder = 32010;
                    effect = go.GetComponent<QuestRewardFlight>();
                    effect.unlockFlight = true;
                    effect.rect = (RectTransform)go.transform;
                    effect.rect.anchorMin = Vector2.zero; effect.rect.anchorMax = Vector2.one;
                    effect.rect.offsetMin = effect.rect.offsetMax = Vector2.zero;
                    effect.uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                }
                RectTransformUtility.ScreenPointToLocalPointInRectangle(effect.rect, screenOrigin, effect.uiCamera, out var start);
                var targetCanvas = button.GetComponentInParent<Canvas>().rootCanvas;
                for (int i = 0; i < 8; i++)
                {
                    var image = new GameObject("Unlock star", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    image.transform.SetParent(effect.rect, false);
                    image.sprite = highlight.StarSprite;
                    image.rectTransform.sizeDelta = image.sprite.rect.size * 2f;
                    image.raycastTarget = false;
                    image.rectTransform.anchoredPosition = start;
                    image.color = Color.clear;
                    effect.particles.Add(new Particle
                    {
                        image = image, targetRect = target, targetCanvas = targetCanvas,
                        origin = start, delay = i * .065f, tint = highlight.StarColor,
                        arcHeight = 80f + (i % 3) * 14f
                    });
                }
            }
        }

        public static void Play(Canvas canvas, Vector2 screenOrigin, List<QuestDefinition.ResourceReward> rewards)
        {
            if (canvas == null || rewards == null) return;
            canvas = canvas.rootCanvas;
            var destinations = new Dictionary<ResourceType, Image>();
            foreach (var panel in canvas.GetComponentsInChildren<ResourceUI>())
                if (panel.DisplaysGlobalResources)
                    foreach (var reward in rewards)
                    {
                        if (reward?.resource == null) continue;
                        var icon = panel.GetResourceIcon(reward.resource);
                        if (icon != null && icon.isActiveAndEnabled) destinations[reward.resource] = icon;
                    }
            if (destinations.Count == 0) return;
            var go = new GameObject("Quest reward flight", typeof(RectTransform), typeof(Canvas), typeof(QuestRewardFlight));
            go.transform.SetParent(canvas.transform, false);
            var overlay = go.GetComponent<Canvas>(); overlay.overrideSorting = true; overlay.sortingOrder = 32010;
            var effect = go.GetComponent<QuestRewardFlight>();
            effect.rect = (RectTransform)go.transform;
            effect.rect.anchorMin = Vector2.zero; effect.rect.anchorMax = Vector2.one;
            effect.rect.offsetMin = effect.rect.offsetMax = Vector2.zero;
            effect.uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(effect.rect, screenOrigin, effect.uiCamera, out var start);
            int group = 0;
            foreach (var reward in rewards)
            {
                if (reward?.resource == null || reward.amount <= 0 || !destinations.TryGetValue(reward.resource, out var destination)) continue;
                int count = Mathf.Min(6, reward.amount);
                for (int i = 0; i < count; i++)
                {
                    var image = new GameObject("Reward icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    image.transform.SetParent(effect.rect, false); image.raycastTarget = false;
                    ResourceIconSizing.Apply(image, reward.resource.resourceIcon);
                    image.rectTransform.anchoredPosition = start;
                    image.color = Color.clear;
                    effect.particles.Add(new Particle { image = image, target = destination, origin = start, delay = i * .07f + group * .1f });
                }
                group++;
            }
            if (effect.particles.Count == 0) Destroy(go);
        }
        void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            bool remaining = false;
            foreach (var particle in particles)
            {
                if (particle.image == null) continue;
                if (particle.target == null && particle.targetRect == null) { Destroy(particle.image.gameObject); continue; }
                float t = (elapsed - particle.delay) / Duration;
                if (t >= 1) { Destroy(particle.image.gameObject); continue; }
                remaining = true;
                if (t < 0) continue;
                var destinationCanvas = particle.targetCanvas != null ? particle.targetCanvas : particle.target.canvas.rootCanvas;
                var camera = destinationCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : destinationCanvas.worldCamera;
                var destination = particle.targetRect != null ? particle.targetRect : particle.target.rectTransform;
                var screen = RectTransformUtility.WorldToScreenPoint(camera, destination.TransformPoint(destination.rect.center));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, uiCamera, out var end);
                float eased = t * t * (3 - 2 * t);
                particle.image.rectTransform.anchoredPosition = Vector2.Lerp(particle.origin, end, eased) + Vector2.up * (Mathf.Sin(t * Mathf.PI) * particle.arcHeight);
                var color = particle.tint;
                color.a *= Mathf.Clamp01((1 - t) * 8);
                particle.image.color = color;
            }
            if (!remaining)
            {
                if (unlockFlight) GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.ContentUnlock, .5f);
                Destroy(gameObject);
            }
        }
    }
}
