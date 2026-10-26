using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Localization
{
    /// <summary>Both first launch and settings use the languages supported by the active table.</summary>
    [DisallowMultipleComponent]
    public sealed class LanguageSelectionList : MonoBehaviour
    {
        public RectTransform container, window;
        public Button template;
        public bool firstLaunch;
        public static bool RequiredChoicePending => !PlayerPrefs.HasKey("foundation.language");
        private readonly List<Button> buttons=new();
        
        private void OnEnable(){if(firstLaunch&&!RequiredChoicePending){gameObject.SetActive(false);return;}Rebuild();}
        private void Start(){if(gameObject.activeSelf)Rebuild();}
        public void Rebuild()
        {
            // Nested prefab overrides can leave a second list on the same popup.
            if (GetComponents<LanguageSelectionList>()[0] != this) return;
            var service=LocalizationService.Instance;if(service?.Table==null)return;
            for (int i=container.childCount-1;i>=0;i--)
            {
                var child=container.GetChild(i).gameObject;
                if(child==template.gameObject)continue;
                child.SetActive(false);if(Application.isPlaying)Destroy(child);else DestroyImmediate(child);
            }
            buttons.Clear();
            template.gameObject.SetActive(false);
            var languages=service.Table.languages;
            for(int i=0;i<languages.Count;i++)
            {
                string code=languages[i];var button=Instantiate(template,container);buttons.Add(button);button.gameObject.SetActive(true);
                foreach(var localized in button.GetComponentsInChildren<LocalizedText>(true)){localized.enabled=false;Destroy(localized);}
                foreach(var localized in button.GetComponentsInChildren<LocalizedGameText>(true)){localized.enabled=false;Destroy(localized);}
                var rect=(RectTransform)button.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=new Vector2(224,52);rect.anchoredPosition=new Vector2(languages.Count==1?0:i%2==0?-124:124,-(i/2)*68);
                string name;try{name=CultureInfo.GetCultureInfo(code).NativeName;}catch(CultureNotFoundException){name=code;}
                if(code=="ru")name="\u0420\u0443\u0441\u0441\u043a\u0438\u0439";if(code=="en")name="English";
                if(code=="zh-Hans")name="\u7b80\u4f53\u4e2d\u6587";
                var text=button.GetComponentInChildren<UnityEngine.UI.Text>(true);if(text!=null)text.text=name;
                var tmp=button.GetComponentInChildren<TMPro.TMP_Text>(true);if(tmp!=null)tmp.text=name;
                if(text!=null){text.resizeTextForBestFit=true;text.resizeTextMinSize=16;text.resizeTextMaxSize=24;}
                if(tmp!=null){tmp.enableAutoSizing=true;tmp.fontSizeMin=16;tmp.fontSizeMax=24;}
                button.onClick=new Button.ButtonClickedEvent();button.onClick.AddListener(()=>Choose(code));
            }
            ApplyLayout(languages.Count);
        }
        public void ApplyLayout(int languageCount)
        {
            float rows=Mathf.Ceil(languageCount/2f);container.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,rows*68-16);
            float extra=Mathf.Max(0,rows-1)*68;
            if(firstLaunch && window!=null)window.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,160+extra);
            if(firstLaunch)
            {
                container.anchorMin=container.anchorMax=new Vector2(.5f,1);container.pivot=new Vector2(.5f,1);container.anchoredPosition=new Vector2(0,-86);
                var heading=window.Find("Title") as RectTransform;
                if(heading!=null){
                    foreach(var localized in heading.GetComponents<LocalizedText>())localized.enabled=false;
                    foreach(var localized in heading.GetComponents<LocalizedGameText>())localized.enabled=false;
                    var title=heading.GetComponent<TMPro.TMP_Text>();if(title!=null)title.text="Choose language";
                    var legacy=heading.GetComponent<Text>();if(legacy!=null)legacy.text="Choose language";
                    heading.anchorMin=heading.anchorMax=new Vector2(.5f,1);heading.pivot=new Vector2(.5f,1);heading.anchoredPosition=new Vector2(0,-22);}
            }
            else if(window!=null)
            {
                float height=424+rows*68-16;
                float top=height*.5f;
                void Place(string name,float x,float y)
                {
                    var rect=window.Find(name) as RectTransform;if(rect==null)return;
                    rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
                    rect.anchoredPosition=new Vector2(x,top-y);
                }
                var frame=window.Find("Artwork Frame") as RectTransform;
                if(frame!=null)frame.sizeDelta=new Vector2(660,height);
                Place("Title",0,54);Place("Close",298,32);
                var titleRect=window.Find("Title") as RectTransform;
                if(titleRect!=null)
                {
                    titleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,64);
                    var titleText=titleRect.GetComponent<Text>();
                    if(titleText!=null)titleText.verticalOverflow=VerticalWrapMode.Overflow;
                }
                Place("Master Label",-166,130);Place("Master",128,133);
                Place("Music Label",-166,195);Place("Music",128,198);
                Place("Effects Label",-166,260);Place("Effects",128,263);
                Place("Fullscreen",-166,320);Place("VSync",128,320);
                container.anchorMin=container.anchorMax=new Vector2(.5f,.5f);
                container.pivot=new Vector2(.5f,1);
                container.anchoredPosition=new Vector2(0,top-380);
            }
            if(firstLaunch)transform.SetAsLastSibling();
        }
        private void Choose(string code){LocalizationService.Instance?.SetLanguage(code);if(firstLaunch)gameObject.SetActive(false);}
    }
}
