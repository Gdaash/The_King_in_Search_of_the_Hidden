using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Quests
{
    /// <summary>Visual-only effect hosted outside the quest panel, which disappears on claim.</summary>
    public sealed class QuestRewardFlight : MonoBehaviour
    {
        sealed class Particle
        {
            public Image image, target;
            public Vector2 origin;
            public float delay;
        }
        readonly List<Particle> particles = new();
        RectTransform rect;
        Camera uiCamera;
        float elapsed;
        const float Duration = .8f;

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
                if (particle.target == null) { Destroy(particle.image.gameObject); continue; }
                float t = (elapsed - particle.delay) / Duration;
                if (t >= 1) { Destroy(particle.image.gameObject); continue; }
                remaining = true;
                if (t < 0) continue;
                var destinationCanvas = particle.target.canvas.rootCanvas;
                var camera = destinationCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : destinationCanvas.worldCamera;
                var screen = RectTransformUtility.WorldToScreenPoint(camera, particle.target.rectTransform.TransformPoint(particle.target.rectTransform.rect.center));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, uiCamera, out var end);
                float eased = t * t * (3 - 2 * t);
                particle.image.rectTransform.anchoredPosition = Vector2.Lerp(particle.origin, end, eased) + Vector2.up * (Mathf.Sin(t * Mathf.PI) * 65);
                particle.image.color = new Color(1, 1, 1, Mathf.Clamp01((1 - t) * 8));
            }
            if (!remaining) Destroy(gameObject);
        }
    }
}
