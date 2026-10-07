using UnityEngine;
using UnityEngine.UI;
namespace GameFoundation.Base
{
    public sealed class RoyalDecreePopupView : MonoBehaviour
    {
        [SerializeField] private Button closeButton;
        public GameObject disableConfirmation;
        public Text disableMessage;
        public Button confirmDisableButton, cancelDisableButton;
        private RoyalDecreeRow selected;
        private void OnEnable()
        {
            closeButton.onClick.RemoveListener(Close); closeButton.onClick.AddListener(Close);
            confirmDisableButton.onClick.RemoveListener(DisableSelected); confirmDisableButton.onClick.AddListener(DisableSelected);
            cancelDisableButton.onClick.RemoveListener(CancelDisable); cancelDisableButton.onClick.AddListener(CancelDisable);
            CancelDisable();
        }
        private void OnDisable()
        {
            if(closeButton)closeButton.onClick.RemoveListener(Close);
            if(confirmDisableButton)confirmDisableButton.onClick.RemoveListener(DisableSelected);
            if(cancelDisableButton)cancelDisableButton.onClick.RemoveListener(CancelDisable);
        }
        public void Open() => gameObject.SetActive(true);
        public void Close() { if (!ForestForagingService.IsPending) gameObject.SetActive(false); }
        public void ConfirmDisable(RoyalDecreeRow row)
        {
            selected = row;
            disableMessage.text = "Отключить указ «" + row.decree.title + "»?\n\nПотраченное влияние не вернётся.\nПовторное включение снова потребует оплаты.";
            disableConfirmation.SetActive(true);
        }
        public void CancelDisable() { selected = null; if(disableConfirmation)disableConfirmation.SetActive(false); }
        private void DisableSelected()
        {
            if (selected != null && !ForestForagingService.IsPending) RoyalDecreeService.SetEnabled(selected.decree.id, false);
            CancelDisable();
        }
    }
}
