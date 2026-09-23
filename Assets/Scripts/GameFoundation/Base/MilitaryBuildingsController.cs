using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class MilitaryBuildingsController : MonoBehaviour
    {
        [SerializeField] private Button fortButton;
        [SerializeField] private Button archeryRangeButton;
        [SerializeField] private GameObject fortPopup;
        [SerializeField] private GameObject archeryRangePopup;

        private void Awake()
        {
            if (fortButton != null) fortButton.onClick.AddListener(() => Open(fortPopup));
            if (archeryRangeButton != null) archeryRangeButton.onClick.AddListener(() => Open(archeryRangePopup));
            BindClose(fortPopup);
            BindClose(archeryRangePopup);
            if (fortPopup != null) fortPopup.SetActive(false);
            if (archeryRangePopup != null) archeryRangePopup.SetActive(false);
        }

        private static void BindClose(GameObject popup)
        {
            if (popup == null) return;
            var close = popup.transform.Find("Close")?.GetComponent<Button>();
            if (close != null) close.onClick.AddListener(() => popup.SetActive(false));
        }

        private static void Open(GameObject popup)
        {
            if (popup != null) popup.SetActive(true);
        }
    }
}
