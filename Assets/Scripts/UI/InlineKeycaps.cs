using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    // Key references keep their place in the text flow; artwork follows the rendered glyphs.
    public sealed class InlineKeycaps : MonoBehaviour
    {
        private TMP_Text text;
        private string previousText;
        private Vector2 previousSize;
        private readonly List<Image> keys = new();
        public static void SetText(TMP_Text target, string value)
        {
            var view = target.GetComponent<InlineKeycaps>() ?? target.gameObject.AddComponent<InlineKeycaps>();
            view.text = target;
            target.text = Regex.Replace(value, @"\{key:([A-Za-z0-9]+)\}",
                "<link=\"key:$1\"><nobr><color=#00000000> $1 </color></nobr></link>");
        }

        private void LateUpdate()
        {
            if (text == null) return;
            if (previousText == text.text && previousSize == text.rectTransform.rect.size && !text.havePropertiesChanged) return;
            previousText = text.text;
            previousSize = text.rectTransform.rect.size;
            text.ForceMeshUpdate();
            int used = 0;
            for (int i = 0; i < text.textInfo.linkCount; i++)
            {
                var link = text.textInfo.linkInfo[i];
                string id = link.GetLinkID();
                if (!id.StartsWith("key:")) continue;
                var first = text.textInfo.characterInfo[link.linkTextfirstCharacterIndex];
                var last = text.textInfo.characterInfo[link.linkTextfirstCharacterIndex + link.linkTextLength - 1];
                if (used == keys.Count)
                {
                    var key = new GameObject("Inline key", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    key.transform.SetParent(text.transform, false);
                    key.sprite = Resources.Load<ButtonVisualTheme>("UI/ButtonVisualTheme").textButtonSprite;
                    key.type = Image.Type.Sliced;
                    key.pixelsPerUnitMultiplier = key.canvas.referencePixelsPerUnit / (key.sprite.pixelsPerUnit * 2f);
                    key.raycastTarget = false;
                    var caption = new GameObject("Key", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                    caption.transform.SetParent(key.transform, false);
                    caption.rectTransform.anchorMin = Vector2.zero;
                    caption.rectTransform.anchorMax = Vector2.one;
                    caption.rectTransform.offsetMin = caption.rectTransform.offsetMax = Vector2.zero;
                    caption.alignment = TextAlignmentOptions.Center;
                    caption.raycastTarget = false;
                    keys.Add(key);
                }
                var image = keys[used++];
                image.gameObject.SetActive(true);
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = text.rectTransform.pivot;
                image.rectTransform.sizeDelta = new Vector2(Mathf.Ceil(last.xAdvance - first.origin), Mathf.Ceil(text.fontSize + 8));
                image.rectTransform.anchoredPosition = new Vector2((first.origin + last.xAdvance) * .5f, (first.ascender + first.descender) * .5f);
                var label = image.GetComponentInChildren<TMP_Text>();
                label.font = text.font; label.fontSize = text.fontSize; label.color = text.color;
                label.text = id.Substring(4);
            }
            for (; used < keys.Count; used++) keys[used].gameObject.SetActive(false);
        }
    }
}
