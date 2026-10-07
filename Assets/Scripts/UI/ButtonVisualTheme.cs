using UnityEngine;

namespace GameFoundation.UI
{
    [CreateAssetMenu(menuName = "Game Foundation/UI/Button Visual Theme")]
    public sealed class ButtonVisualTheme : ScriptableObject
    {
        [Header("Artwork")]
        public Sprite textButtonSprite;

        [Header("Action label colors / Цвета надписей кнопок")]
        [Tooltip("Доступное действие: вооружить, купить, включить, отправить.")]
        public Color positiveLabelColor = new Color(.56f, .69f, .53f, 1f);
        [Tooltip("Доступная отмена: разоружить, отключить, отменить.")]
        public Color negativeLabelColor = new Color(.75f, .52f, .53f, 1f);
        [Tooltip("Действие недоступно, в том числе при нехватке ресурсов.")]
        public Color unavailableLabelColor = new Color(.55f, .51f, .59f, 1f);
        [Tooltip("Навигация и выбор параметров без покупки или отмены.")]
        public Color neutralLabelColor = new Color(.94f, .91f, .82f, 1f);

        [Header("Colors")]
        public Color normalColor = Color.white;
        public Color hoverColor = new Color(1f, 0.92f, 0.72f, 1f);
        public Color pressedColor = new Color(0.82f, 0.63f, 0.42f, 1f);
        public Color disabledColor = new Color(0.43f, 0.40f, 0.44f, 1f);
        public Color hoverOutlineColor = new Color(1f, 0.85f, 0.3f, 0.95f);
        public Vector2 outlineDistance = new Vector2(2f, -2f);
        [Range(0f, 1f)] public float hoverColorStrength = 0.8f;

        [Header("Motion")]
        [Min(1f)] public float hoverScale = 1.08f;
        [Range(0.5f, 1f)] public float pressedScale = 0.95f;
        [Min(0.1f)] public float transitionSpeed = 16f;
    }
}
