using GameFoundation.MetaProgression;
using GameFoundation.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameFoundation.Saves
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject slotsPanel;
        [SerializeField] private Button slotsCloseButton;
        [SerializeField] private Button[] slotButtons = new Button[3];
        [SerializeField] private TMP_Text[] slotLabels = new TMP_Text[3];
        [SerializeField] private SettingsPopupController settingsPopup;
        [SerializeField] private string gameScene = "Base";

        private void Awake()
        {
            if (playButton) playButton.onClick.AddListener(OpenSlots);
            if (settingsButton) settingsButton.onClick.AddListener(OpenSettings);
            if (exitButton) exitButton.onClick.AddListener(Exit);
            if (slotsCloseButton) slotsCloseButton.onClick.AddListener(CloseSlots);
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int slot = i + 1;
                if (slotButtons[i]) slotButtons[i].onClick.AddListener(() => PlaySlot(slot));
            }
            if (slotsPanel) slotsPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (playButton) playButton.onClick.RemoveListener(OpenSlots);
            if (settingsButton) settingsButton.onClick.RemoveListener(OpenSettings);
            if (exitButton) exitButton.onClick.RemoveListener(Exit);
            if (slotsCloseButton) slotsCloseButton.onClick.RemoveListener(CloseSlots);
        }

        public void OpenSlots()
        {
            for (int i = 0; i < slotLabels.Length && i < 3; i++)
            {
                if (slotLabels[i] == null) continue;
                int slot = i + 1;
                if (!SaveSlotPrefs.SlotExists(slot))
                {
                    slotLabels[i].text = $"Ячейка {slot}\nНовое сохранение";
                    continue;
                }
                int seconds = Mathf.FloorToInt(SaveSlotPrefs.SlotPlaySeconds(slot));
                slotLabels[i].text = $"Ячейка {slot}\n{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}  ·  Люди: {SaveSlotPrefs.SlotResource(slot, "Human")}  ·  Магическая руда: {SaveSlotPrefs.SlotResource(slot, "MagicOre")}";
            }
            if (slotsPanel) slotsPanel.SetActive(true);
        }

        public void CloseSlots()
        {
            if (slotsPanel) slotsPanel.SetActive(false);
        }

        public void OpenSettings() => settingsPopup?.Open();

        private void PlaySlot(int slot)
        {
            SaveSlotPrefs.Select(slot);
            DayResourceLedger.ResetForSlot();
            SceneManager.LoadScene(gameScene);
        }

        private static void Exit() => Application.Quit();
    }
}
