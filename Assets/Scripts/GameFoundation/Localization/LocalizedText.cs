using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Localization
{
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;
        private TMP_Text tmp; private Text legacy;
        private bool subscribed;
        private void Awake() { tmp=GetComponent<TMP_Text>(); legacy=GetComponent<Text>(); }
        private void OnEnable() { Subscribe(); Refresh(); }
        private void Start() { Subscribe(); Refresh(); }
        private void OnDisable() { if (subscribed && LocalizationService.Instance != null) LocalizationService.Instance.LanguageChanged -= Refresh; subscribed = false; }
        private void Subscribe() { if (subscribed || LocalizationService.Instance == null) return; LocalizationService.Instance.LanguageChanged += Refresh; subscribed = true; }
        public void Refresh() { if (LocalizationService.Instance == null) return; var value=LocalizationService.Instance.Get(key); if(tmp!=null) tmp.text=value; if(legacy!=null) legacy.text=value; }
    }
}
