using GameFoundation.Localization;
using UnityEngine;

namespace GameFoundation.UI
{
    [CreateAssetMenu(menuName = "Game Foundation/UI/Unit description")]
    public sealed class UnitDescriptionDefinition : ScriptableObject
    {
        [Tooltip("Тот же префаб, который выходит из портала. Числа читаются из его компонентов.")]
        public GameObject unitPrefab;
        public ResourceType resource;
        public ResourceType food;
        public GlobalStats scientificStats;
        [Header("Представление врага")]
        public bool isEnemy;
        public Sprite portraitIcon;
        public string fallbackTitle;
        public string titleKey;
        public string roleKey;
        public string fallbackRole;
        public string descriptionKey;
        [TextArea] public string fallbackDescription;

        public Sprite Portrait => portraitIcon != null ? portraitIcon : resource != null ? resource.resourceIcon : null;
        public string Title => UnitDescriptionText.Get(titleKey, !string.IsNullOrEmpty(fallbackTitle) ? fallbackTitle : resource != null ? resource.resourceName : name);
        public string Role => UnitDescriptionText.Get(roleKey, fallbackRole);
        public string Description => UnitDescriptionText.Get(descriptionKey, fallbackDescription);
    }

    internal static class UnitDescriptionText
    {
        public static string Get(string key, string fallback)
        {
            string value = LocalizationService.Instance != null ? LocalizationService.Instance.Get(key) : key;
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }
    }
}
