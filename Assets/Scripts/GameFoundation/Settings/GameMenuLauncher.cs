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
        private void Update()
        {
            if (GameFoundation.MetaProgression.PortalArsenalPopup.IsOpen || GameFoundation.MetaProgression.PortalArsenalPopup.ClosedFrame == Time.frameCount) return;
#if UNITY_EDITOR
            if (EditorWaveCheatPopup.IsOpen || EditorWaveCheatPopup.ClosedFrame == Time.frameCount) return;
#endif
            // The popup is inactive while closed, so its launcher owns the opening shortcut.
            if (Input.GetKeyDown(KeyCode.Escape) && (!menu || !menu.gameObject.activeInHierarchy && menu.ClosedFrame != Time.frameCount) &&
                !GameFoundation.MetaProgression.PortalCloudTransition.IsLoading)
                Open();
        }
        public void Open()
        {
            if (GameFoundation.MetaProgression.PortalArsenalPopup.IsOpen) return;
            if (!menuPrefab) return;
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            if (!settings) { settings = Instantiate(settingsPrefab, canvas.transform); settings.gameObject.SetActive(false); }
            if (!menu) menu = Instantiate(menuPrefab, canvas.transform);
            menu.Open(settings);
        }
        private void OnDestroy() => GetComponent<Button>().onClick.RemoveListener(Open);
    }
}
