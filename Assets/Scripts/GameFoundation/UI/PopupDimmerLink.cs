using UnityEngine;

namespace GameFoundation.UI
{
    /// <summary>Keeps a sibling full-screen dimmer in the same active state as its popup.</summary>
    public sealed class PopupDimmerLink : MonoBehaviour
    {
        [SerializeField] private GameObject dimmer;

        private void OnEnable()
        {
            if (dimmer != null) dimmer.SetActive(true);
        }

        private void OnDisable()
        {
            if (dimmer != null) dimmer.SetActive(false);
        }
    }
}
