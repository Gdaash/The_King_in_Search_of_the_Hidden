using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Settings
{
    [RequireComponent(typeof(Button))]
    public sealed class GameMenuLauncher : MonoBehaviour
    {
        [SerializeField] private GameMenuController menuPrefab;
        [SerializeField] private SettingsPopupController settings;
        [SerializeField] private SettingsPopupController settingsPrefab;
        private GameMenuController menu;
        private void Awake() => GetComponent<Button>().onClick.AddListener(Open);
        public void Open()
        {
            if (!menuPrefab) return;
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            if (!settings) { settings = Instantiate(settingsPrefab, canvas.transform); settings.gameObject.SetActive(false); }
            if (!menu) menu = Instantiate(menuPrefab, canvas.transform);
            menu.Open(settings);
        }
        private void OnDestroy() => GetComponent<Button>().onClick.RemoveListener(Open);
    }
}
