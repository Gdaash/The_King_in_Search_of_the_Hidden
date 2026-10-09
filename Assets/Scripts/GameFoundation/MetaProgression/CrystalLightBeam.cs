using DeskCat.FindIt.Scripts.Core.Main.Utility.DragObj;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameFoundation.MetaProgression
{
    /// <summary>Flashlight visuals and delivery flag for one independently assigned light.</summary>
    public sealed class CrystalLightBeam : MonoBehaviour
    {
        [SerializeField] private LogisticFlag flag;
        [SerializeField] private FlashlightController controller;
        [SerializeField] private Light2D groundLight;
        [SerializeField] private FlashlightActivationButton activationButton;
        [SerializeField] private Collider2D markerCollider;
        public LogisticFlag Flag => flag;
        public FlashlightActivationButton ActivationButton => activationButton;
        public Vector2 MarkerPosition => flag != null ? (Vector2)flag.transform.position : Vector2.zero;
        public bool MarkerVisible => flag != null && flag.gameObject.activeInHierarchy;
        public bool ContainsMarkerPoint(Vector2 point) => MarkerVisible && markerCollider != null && markerCollider.OverlapPoint(point);
        public void Initialize()
        {
            if (flag == null) flag = GetComponentInChildren<LogisticFlag>(true);
            if (controller == null) controller = GetComponentInChildren<FlashlightController>(true);
            if (groundLight == null && flag != null) groundLight = flag.GetComponentInChildren<Light2D>(true);
            if (markerCollider == null && flag != null) markerCollider = flag.GetComponent<Collider2D>();
            if (flag != null)
            {
                var drag = flag.GetComponent<DragObj>();
                if (drag != null) { drag.CanDrag = false; drag.enabled = false; }
                flag.SetCrystalTarget(null);
            }
            foreach (var button in GetComponentsInChildren<FlashlightActivationButton>(true)) button.gameObject.SetActive(false);
            Clear();
        }
        public void ShowIdle(Vector2 position) => Show(position, null, true);
        public void Show(Vector2 position, ResourceRequester requester, bool lit)
        {
            if (flag != null)
            {
                flag.transform.position = position;
                flag.transform.rotation = Quaternion.identity;
                flag.SetCrystalTarget(requester);
                flag.gameObject.SetActive(true);
                flag.enabled = true;
            }
            if (controller != null)
            {
                controller.SetCrystalTarget(position, lit);
                controller.gameObject.SetActive(true);
            }
            if (groundLight != null) { groundLight.gameObject.SetActive(lit); groundLight.enabled = lit; }
        }
        public void Clear()
        {
            if (controller != null)
            {
                controller.SetCrystalTarget(flag != null ? (Vector2)flag.transform.position : Vector2.zero, false);
                controller.gameObject.SetActive(false);
            }
            if (groundLight != null) groundLight.gameObject.SetActive(false);
            if (flag != null) { flag.SetCrystalTarget(null); flag.gameObject.SetActive(false); }
        }
    }
}
