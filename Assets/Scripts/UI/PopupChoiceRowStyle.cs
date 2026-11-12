using UnityEngine;
using UnityEngine.UI;
using GameFoundation.Localization;

namespace GameFoundation.UI
{
    /// <summary>Shared framed selection row for popup lists with a separate detail action.</summary>
    internal sealed class PopupChoiceRowStyle
    {
        private static Sprite normalFrame, selectedFrame;
        private readonly Image frame;

        public PopupChoiceRowStyle(Button button, Image background)
        {
            background.sprite = null;
            background.type = Image.Type.Simple;
            button.transition = Selectable.Transition.None;
            // Discard any tint applied by the original button transition before this row was styled.
            button.targetGraphic?.CrossFadeColor(Color.white, 0f, true, true);
            var feedback = button.GetComponent<UnifiedButtonFeedback>() ?? button.gameObject.AddComponent<UnifiedButtonFeedback>();
            feedback.UseChoiceRowFeedback();
            background.color = new Color32(39, 34, 48, 255);

            normalFrame ??= Resources.Load<Sprite>("UI/Laboratory Choice Frame");
            selectedFrame ??= Resources.Load<Sprite>("UI/Popup Choice Frame Selected");
            var existingFrame = background.transform.Find("Choice frame");
            frame = existingFrame != null ? existingFrame.GetComponent<Image>() :
                new GameObject("Choice frame", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            frame.transform.SetParent(background.transform, false);
            frame.transform.SetAsFirstSibling();
            frame.rectTransform.anchorMin = Vector2.zero;
            frame.rectTransform.anchorMax = Vector2.one;
            frame.rectTransform.offsetMin = frame.rectTransform.offsetMax = Vector2.zero;
            frame.type = Image.Type.Sliced;
            frame.color = Color.white;
            frame.pixelsPerUnitMultiplier = 1;
            frame.fillCenter = false;
            // The frame receives clicks across the card, including its transparent center.
            frame.raycastTarget = true;
        }

        public void SetSelected(bool selected) => frame.sprite = selected ? selectedFrame : normalFrame;

        internal static void PlaceCaptions(Text title, Text status, float left, float titleRight, float statusRight)
        {
            const float gap = 3f;
            const float statusHeight = 24f;
            PlaceCaption(title, left, titleRight, 0, 28);
            float titleHeight = Mathf.Max(28, Mathf.Ceil(title.preferredHeight));
            PlaceCaption(title, left, titleRight, (statusHeight + gap) * .5f, titleHeight);
            PlaceCaption(status, left, statusRight, -(titleHeight + gap) * .5f, statusHeight);
        }

        private static void PlaceCaption(Text label, float left, float right, float y, float height)
        {
            var rect = label.rectTransform;
            label.alignment = TextAnchor.MiddleLeft;
            rect.anchorMin = new Vector2(0, .5f);
            rect.anchorMax = new Vector2(1, .5f);
            rect.offsetMin = new Vector2(left, y - height * .5f);
            rect.offsetMax = new Vector2(-right, y + height * .5f);
        }

        public static (string text, Color color) PurchaseStatus(bool complete, bool locked, bool affordable, bool portal = false, bool construction = false)
        {
            bool builtStatus = portal || construction;
            var (key, fallback, color) = complete
                ? (builtStatus ? "base.housing.built" : "laboratory.complete", builtStatus ? "Построено" : "Изучено", new Color(.92f, .83f, .60f))
                : locked ? ("laboratory.locked", "Заблокировано", new Color(.64f, .59f, .68f))
                : affordable ? (construction ? "base.choice.can_build" : portal ? "base.portal.can_upgrade" : "laboratory.available", construction ? "Можно построить" : portal ? "Можно улучшить" : "Можно изучить", new Color(.55f, .85f, .48f))
                : ("laboratory.no_resources", "Не хватает ресурсов", new Color(.99f, .49f, .50f));
            var value = LocalizationService.Instance?.Get(key);
            return (string.IsNullOrEmpty(value) || value == key ? fallback : value, color);
        }
    }
}
