using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    /// <summary>Binds the built Castle button to its decree popup.</summary>
    public sealed class CastleBuildingController : MonoBehaviour
    {
        [SerializeField] private Button castleButton;
        [SerializeField] private RoyalDecreePopupView popup;

        private void Awake()
        {
            if (castleButton != null) castleButton.onClick.AddListener(Open);
            if (popup != null) popup.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (castleButton != null) castleButton.onClick.RemoveListener(Open);
        }

        public void Open() => popup?.Open();
    }
}
