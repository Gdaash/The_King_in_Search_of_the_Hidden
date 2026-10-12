using TMPro;
using UnityEngine;

namespace GameFoundation.Localization
{
    /// <summary>Preserves authored and dynamically assigned text when the player changes language.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(500)]
    public sealed class LocalizedGameText : MonoBehaviour
    {
        [SerializeField,TextArea]string source;
        TMP_Text tmp;
        UnityEngine.UI.Text legacy;
        string rendered,language;
        bool initialized;
        void Awake(){tmp=GetComponent<TMP_Text>();legacy=GetComponent<UnityEngine.UI.Text>();}
        void LateUpdate()
        {
            if(!tmp&&!legacy)return;
            string current=tmp?tmp.text:legacy.text;
            var service=LocalizationService.Instance;if(!service)return;
            if(!initialized||current!=rendered){source=GameText.Source(current);initialized=true;}
            if(current==rendered&&language==service.Language)return;
            language=service.Language;rendered=GameText.Get(source);
            if(current!=rendered){if(tmp)tmp.text=rendered;else legacy.text=rendered;}
        }
    }
}
