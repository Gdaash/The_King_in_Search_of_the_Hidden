using DeskCat.FindIt.Scripts.Core.Main.Utility.DragObj;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameFoundation.MetaProgression
{
    /// <summary>Existing flashlight visuals and delivery flag, controlled by one crystal cell.</summary>
    public sealed class CrystalLightBeam : MonoBehaviour
    {
        [SerializeField] private LogisticFlag flag;
        [SerializeField] private FlashlightController controller;
        [SerializeField] private Light2D groundLight;
        public LogisticFlag Flag => flag;
        public void Initialize()
        {
            if (flag == null) flag = GetComponentInChildren<LogisticFlag>(true);
            if (controller == null) controller = GetComponentInChildren<FlashlightController>(true);
            if (groundLight == null && flag != null) groundLight = flag.GetComponentInChildren<Light2D>(true);
            if (flag != null)
            {
                var drag = flag.GetComponent<DragObj>();
                if (drag != null) { drag.CanDrag = false; drag.enabled = false; }
                flag.SetCrystalTarget(null);
            }
            foreach (var button in GetComponentsInChildren<FlashlightActivationButton>(true)) button.gameObject.SetActive(false);
            Clear();
        }
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
