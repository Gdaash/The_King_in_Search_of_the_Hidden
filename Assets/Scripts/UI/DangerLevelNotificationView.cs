using System;
using System.Collections;
using System.Collections.Generic;
using GameFoundation.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DangerLevelNotificationView : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private RectTransform skullContainer;
    [SerializeField] private Image skullTemplate;

    [Header("Текст")]
    [SerializeField] private string localizationKey = "world.danger_level_notification.title";
    [SerializeField] private string fallbackFormat = "Уровень опасности: {0}";

    [Header("Состояния черепов")]
    [SerializeField] private Sprite activeSkullSprite;
    [SerializeField] private Sprite inactiveSkullSprite;

    [Header("Анимация (всего 3 секунды)")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.25f;
    [SerializeField, Min(0f)] private float visibleDuration = 2.5f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.25f;

    private readonly List<Image> skulls = new();
    private readonly Vector3[] corners = new Vector3[4];
    private RectTransform followedBar, upperDecoration, rootCanvas;
    private bool aboveBar;

    public void FollowBar(RectTransform bar, bool above, RectTransform decoration = null)
    {
        followedBar = bar;
        aboveBar = above;
        upperDecoration = decoration;
        rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas.transform as RectTransform;
        AlignToBar();
    }

    private void LateUpdate() => AlignToBar();

    private void AlignToBar()
    {
        if (followedBar == null || rootCanvas == null) return;
        const float gap = 12f;
        float edge = BarEdge(followedBar);
        if (aboveBar && upperDecoration != null && upperDecoration.gameObject.activeInHierarchy)
            edge = Mathf.Max(edge, BarEdge(upperDecoration));
        var rect = (RectTransform)transform;
        float halfHeight = Mathf.Abs(rootCanvas.InverseTransformVector(rect.TransformVector(Vector3.up * rect.rect.height * .5f)).y);
        float y = edge + (aboveBar ? 1 : -1) * (gap + halfHeight);
        rect.position = rootCanvas.TransformPoint(new Vector3(rootCanvas.rect.center.x, y));
    }

    private float BarEdge(RectTransform bar)
    {
        bar.GetWorldCorners(corners);
        return rootCanvas.InverseTransformPoint(corners[aboveBar ? 1 : 0]).y;
    }

    public void ShowLightChange(int change, int lightCount, Sprite crystalIcon)
    {
        int maximum = GameFoundation.MetaProgression.WorldFlashlightAvailability.MaximumCount;
        EnsureSkulls(maximum);
        if (skullContainer != null)
        {
            skullContainer.gameObject.SetActive(crystalIcon != null);
            skullContainer.anchorMin = skullContainer.anchorMax = skullContainer.pivot = new Vector2(.5f, .5f);
            skullContainer.anchoredPosition = new Vector2(0, -24);
        }
        for (int i = 0; i < skulls.Count; i++)
        {
            var crystal = skulls[i];
            crystal.gameObject.SetActive(i < maximum);
            ResourceIconSizing.Apply(crystal, crystalIcon);
            crystal.color = i < lightCount ? Color.white : new Color(.13f, .11f, .15f);
        }
        if (titleText != null)
        {
            var rect = titleText.rectTransform;
            rect.anchorMin = new Vector2(0, .5f);
            rect.anchorMax = new Vector2(1, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0, 24);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 60);
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 22;
            titleText.fontSizeMax = titleText.fontSize;
            int count = Mathf.Abs(change);
            string key = change > 0 ? "world.energy.light_on" : "world.energy.light_off";
            string fallback = change > 0 ? "Включился ещё один луч" : "Выключился один луч";
            if (count > 1)
            {
                key += "_multiple";
                fallback = change > 0 ? "Включились лучи: {0}" : "Выключились лучи: {0}";
            }
            var translated = LocalizationService.Instance?.Get(key);
            titleText.text = string.Format(string.IsNullOrEmpty(translated) || translated == key ? fallback : translated, count);
        }
        gameObject.SetActive(true);
        StartCoroutine(PlayLightChange());
    }

    private IEnumerator PlayLightChange()
    {
        yield return Animate();
        Destroy(gameObject);
    }

    public IEnumerator Play(int level, int totalLevels)
    {
        SetContent(level, totalLevels);
        gameObject.SetActive(true);
        yield return Animate();
    }

    private IEnumerator Animate()
    {
        yield return Fade(0f, 1f, fadeInDuration);
        if (visibleDuration > 0f) yield return new WaitForSecondsRealtime(visibleDuration);
        yield return Fade(1f, 0f, fadeOutDuration);
    }

    public void SetContent(int level, int totalLevels)
    {
        level = Mathf.Clamp(level, 1, Mathf.Max(1, totalLevels));
        totalLevels = Mathf.Max(1, totalLevels);
        if (titleText != null) titleText.text = string.Format(GetTitleFormat(), level);
        EnsureSkulls(totalLevels);
        for (int i = 0; i < skulls.Count; i++)
        {
            Image skull = skulls[i];
            skull.gameObject.SetActive(i < totalLevels);
            if (i >= totalLevels) continue;
            skull.sprite = i < level ? activeSkullSprite : inactiveSkullSprite;
            skull.color = Color.white;
            skull.enabled = skull.sprite != null;
            if (skull.sprite != null) skull.rectTransform.sizeDelta = skull.sprite.rect.size * 2f;
        }
    }

    private string GetTitleFormat()
    {
        string translated = LocalizationService.Instance != null ? LocalizationService.Instance.Get(localizationKey) : null;
        return string.IsNullOrEmpty(translated) || translated == localizationKey ? fallbackFormat : translated;
    }

    private void EnsureSkulls(int count)
    {
        if (skullTemplate == null || skullContainer == null) return;
        if (skulls.Count == 0) skulls.Add(skullTemplate);
        while (skulls.Count < count)
        {
            Image skull = Instantiate(skullTemplate, skullContainer);
            skull.name = $"Skull {skulls.Count + 1}";
            skulls.Add(skull);
        }
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (canvasGroup == null) yield break;
        if (duration <= 0f) { canvasGroup.alpha = to; yield break; }
        float elapsed = 0f;
        canvasGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, t * t * (3f - 2f * t));
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        titleText = GetComponentInChildren<TMP_Text>(true);
    }
}
