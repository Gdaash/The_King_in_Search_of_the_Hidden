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
            bool exists = SaveSlotPrefs.SlotExists(slot);
            resources.SetActive(exists);
            if (!exists) { summary.text = GameText.Format("Ячейка {0}\nНовое сохранение", slot); return; }
            int seconds = Mathf.FloorToInt(SaveSlotPrefs.SlotPlaySeconds(slot));
            summary.text = GameText.Format("Ячейка {0}", slot) + "   ·   " + $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}";
            ResourceIconSizing.Apply(humanIcon, human.resourceIcon);
            ResourceIconSizing.Apply(oreIcon, magicOre.resourceIcon);
            humanAmount.text = SaveSlotPrefs.SlotResource(slot, human.Id).ToString();
            oreAmount.text = SaveSlotPrefs.SlotResource(slot, magicOre.Id).ToString();
        }
    }
}
