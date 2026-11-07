using GameFoundation.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Saves
{
    public sealed class SaveSlotsPopupView : MonoBehaviour
    {
        public RectTransform window;
        public Button closeButton;
        public TMP_Text heading;
        public Button[] slots, deleteButtons;
        public SaveSlotSummaryView[] summaries;
        public GameObject confirmation;
        public TMP_Text confirmationText;
        public Button confirmYes, confirmNo;
        private int pendingSlot;
        private LocalizationService language;
        private void Awake()
        {
            closeButton.onClick.AddListener(Close);
            for (int i=0;i<deleteButtons.Length;i++){int slot=i+1;deleteButtons[i].onClick.AddListener(()=>RequestDelete(slot));}
            confirmYes.onClick.AddListener(ConfirmDelete); confirmNo.onClick.AddListener(CancelDelete);
        }
        private void OnEnable(){language=LocalizationService.Instance;if(language!=null)language.LanguageChanged+=Refresh;CancelDelete();Refresh();}
        private void OnDisable(){if(language!=null)language.LanguageChanged-=Refresh;CancelDelete();}
        private void Update(){if(Input.GetKeyDown(KeyCode.Escape)){if(confirmation.activeSelf)CancelDelete();else Close();}}
        public void Close(){CancelDelete();gameObject.SetActive(false);}
        public void Refresh()
        {
            heading.text=Tr("menu.slots.title","Выберите сохранение");
            window.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,1200);
            heading.rectTransform.anchorMin=heading.rectTransform.anchorMax=new Vector2(.5f,1);heading.rectTransform.pivot=new Vector2(.5f,1);heading.rectTransform.sizeDelta=new Vector2(1044,48);heading.fontSize=36;
            float height=116+slots.Length*100+Mathf.Max(0,slots.Length-1)*20;
            window.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
            heading.rectTransform.anchoredPosition=new Vector2(0,-24);
            float top=88;
            for(int i=0;i<slots.Length;i++)
            {
                bool exists=SaveSlotPrefs.SlotExists(i+1);
                var slotRect=(RectTransform)slots[i].transform;
                slotRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,exists?800:1104);
                Place(slotRect,top,100);slotRect.anchoredPosition=new Vector2(exists?-152:0,-top);
                summaries[i].Present(i+1);
                deleteButtons[i].gameObject.SetActive(exists);
                var deleteRect=(RectTransform)deleteButtons[i].transform;
                Place(deleteRect,top,100);deleteRect.sizeDelta=new Vector2(280,100);deleteRect.anchoredPosition=new Vector2(412,-top);
                var label=deleteButtons[i].GetComponentInChildren<TMP_Text>(true);
                label.text=Tr("menu.slots.delete","Удалить сохранение");label.textWrappingMode=TextWrappingModes.Normal;label.enableAutoSizing=true;label.fontSizeMin=22;label.fontSizeMax=26;
                label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(24,10);label.rectTransform.offsetMax=new Vector2(-24,-10);
                foreach(var color in deleteButtons[i].GetComponents<GameFoundation.UI.ActionButtonLabelColor>()){color.Neutral=false;color.Negative=true;}
                top+=120;
            }
            closeButton.transform.SetAsLastSibling();closeButton.interactable=true;
            var closeRect=(RectTransform)closeButton.transform;closeRect.anchorMin=closeRect.anchorMax=closeRect.pivot=Vector2.one;closeRect.sizeDelta=new Vector2(40,40);closeRect.anchoredPosition=new Vector2(-12,-12);
            if(closeButton.targetGraphic!=null)closeButton.targetGraphic.raycastTarget=true;
            confirmationText.text=Tr("menu.slots.delete_confirm","Вы действительно хотите удалить это сохранение?\nДанное действие необратимо.");
            confirmYes.GetComponentInChildren<TMP_Text>(true).text=Tr("common.yes","Да");
            confirmNo.GetComponentInChildren<TMP_Text>(true).text=Tr("common.no","Нет");
            foreach(var color in confirmYes.GetComponents<GameFoundation.UI.ActionButtonLabelColor>()){color.Neutral=false;color.Negative=true;}
            foreach(var color in confirmNo.GetComponents<GameFoundation.UI.ActionButtonLabelColor>())color.Neutral=true;
            if(confirmation.activeSelf)confirmation.transform.SetAsLastSibling();
        }
        private static void Place(RectTransform rect,float top,float height){rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-top);rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);}
        public void RequestDelete(int slot){if(slot<1||slot>3||!SaveSlotPrefs.SlotExists(slot))return;pendingSlot=slot;confirmation.SetActive(true);confirmation.transform.SetAsLastSibling();}
        public void CancelDelete(){pendingSlot=0;if(confirmation!=null)confirmation.SetActive(false);}
        public void ConfirmDelete(){if(pendingSlot==0)return;if(SaveSlotPrefs.ClearSlot(pendingSlot)){CancelDelete();Refresh();}}
        internal static string Tr(string key,string fallback){string text=LocalizationService.Instance?.Get(key);return string.IsNullOrEmpty(text)||text==key?fallback:text;}
    }
}
