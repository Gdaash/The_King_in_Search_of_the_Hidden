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

    [Header("Цвета черепов")]
    [SerializeField] private Color lockedSkullColor = Color.black;
    [SerializeField] private Color unlockedSkullColor = Color.white;

    [Header("Анимация (всего 3 секунды)")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.25f;
    [SerializeField, Min(0f)] private float visibleDuration = 2.5f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.25f;

    private readonly List<Image> skulls = new();

    public IEnumerator Play(int level, int totalLevels)
    {
        SetContent(level, totalLevels);
        gameObject.SetActive(true);
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
            if (i < totalLevels) skull.color = i < level ? unlockedSkullColor : lockedSkullColor;
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
