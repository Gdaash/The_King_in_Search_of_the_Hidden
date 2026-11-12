using UnityEngine;

namespace GameFoundation.UI
{
    /// <summary>Keeps a sibling full-screen dimmer in the same active state as its popup.</summary>
    public sealed class PopupDimmerLink : MonoBehaviour
    {
        [SerializeField] private GameObject dimmer;

        internal void Bind(GameObject background) => dimmer = background;

        private CanvasGroup GetFadeGroup()
        {
            if (dimmer == null) return null;
            return dimmer.TryGetComponent<CanvasGroup>(out var group) ? group : dimmer.AddComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (dimmer == null) return;
            dimmer.SetActive(true);
            var window = transform.Find("Window") as RectTransform;
            if (window == null && transform.Find("Close") != null)
                window = transform as RectTransform;
            if (window != null)
            {
                var internalDimmer = window != transform
                    ? transform.Find("Screen Dimmer")?.GetComponent<UnityEngine.UI.Graphic>()
                    : null;
                PopupMotion.Show(gameObject, window, internalDimmer, GetFadeGroup());
            }
        }

        private void OnDisable()
        {
            if (dimmer != null) dimmer.SetActive(false);
        }
    }
}
