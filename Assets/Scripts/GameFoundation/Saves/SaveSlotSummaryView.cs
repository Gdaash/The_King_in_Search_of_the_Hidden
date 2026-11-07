using GameFoundation.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Saves
{
    public sealed class SaveSlotSummaryView : MonoBehaviour
    {
        [SerializeField] private TMP_Text summary;
        [SerializeField] private GameObject resources;
        [SerializeField] private ResourceType human, magicOre;
        [SerializeField] private Image humanIcon, oreIcon;
        [SerializeField] private TMP_Text humanAmount, oreAmount;
        public void Present(int slot)
        {
            summary.rectTransform.anchorMin=summary.rectTransform.anchorMax=summary.rectTransform.pivot=new Vector2(.5f,.5f);summary.rectTransform.sizeDelta=new Vector2(836,64);summary.rectTransform.anchoredPosition=Vector2.zero;summary.fontSize=26;summary.textWrappingMode=TextWrappingModes.NoWrap;
            var resourceRect=(RectTransform)resources.transform;resourceRect.sizeDelta=new Vector2(520,56);resourceRect.anchoredPosition=Vector2.zero;
            bool exists = SaveSlotPrefs.SlotExists(slot);
            resources.SetActive(exists);
            if (!exists) { summary.rectTransform.anchoredPosition=Vector2.zero;summary.alignment=TextAlignmentOptions.Center;summary.rectTransform.sizeDelta=new Vector2(836,84);summary.textWrappingMode=TextWrappingModes.Normal;summary.text = GameText.Format("Ячейка {0}\nНовое сохранение", slot); return; }
            int seconds = Mathf.FloorToInt(SaveSlotPrefs.SlotPlaySeconds(slot));
            summary.text = GameText.Format("Ячейка {0}", slot) + "   ·   " + $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}";
            ResourceIconSizing.Apply(humanIcon, human.resourceIcon);
            ResourceIconSizing.Apply(oreIcon, magicOre.resourceIcon);
            humanAmount.text = SaveSlotPrefs.SlotResource(slot, human.Id).ToString();
            oreAmount.text = SaveSlotPrefs.SlotResource(slot, magicOre.Id).ToString();
            LayoutResources();
        }
        private void LayoutResources()
        {
            const float iconGap = 10f, pairGap = 24f;
            float humanWidth = AmountWidth(humanAmount), oreWidth = AmountWidth(oreAmount);
            float firstWidth = humanIcon.rectTransform.rect.width + iconGap + humanWidth;
            float secondWidth = oreIcon.rectTransform.rect.width + iconGap + oreWidth;
            float totalWidth = firstWidth + pairGap + secondWidth;
            var rect = (RectTransform)resources.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(totalWidth, 56f);
            float rowWidth = ((RectTransform)transform).rect.width;
            rect.anchoredPosition = new Vector2(rowWidth * .5f - 24 - totalWidth * .5f, 0);
            float summaryWidth = rowWidth - 48 - totalWidth - 32;
            summary.rectTransform.sizeDelta = new Vector2(summaryWidth, 64);
            summary.rectTransform.anchoredPosition = new Vector2(-rowWidth * .5f + 24 + summaryWidth * .5f, 0);
            summary.alignment = TextAlignmentOptions.MidlineLeft;
            LayoutPair(humanIcon, humanAmount, -totalWidth * .5f, humanWidth, iconGap);
            LayoutPair(oreIcon, oreAmount, -totalWidth * .5f + firstWidth + pairGap, oreWidth, iconGap);
        }
        private static float AmountWidth(TMP_Text text)
        {
            // Slot summaries are measured while their popup is inactive. In a player build,
            // TMP Awake has not assigned the shared font material yet.
            if (text.fontSharedMaterial == null && text.font != null)
                text.fontSharedMaterial = text.font.material;
            text.enableAutoSizing = false;
            text.fontSize = 26f;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return Mathf.Max(24f, Mathf.Ceil(text.GetPreferredValues(text.text).x) + 4f);
        }
        private static void LayoutPair(Image icon, TMP_Text amount, float left, float width, float gap)
        {
            var iconRect = icon.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(.5f, .5f);
            iconRect.anchoredPosition = new Vector2(left + iconRect.rect.width * .5f, 0);
            var amountRect = amount.rectTransform;
            amountRect.anchorMin = amountRect.anchorMax = new Vector2(.5f, .5f);
            amountRect.pivot = new Vector2(0, .5f);
            amountRect.sizeDelta = new Vector2(width, 40f);
            amountRect.anchoredPosition = new Vector2(left + iconRect.rect.width + gap, 0);
        }
    }
}
