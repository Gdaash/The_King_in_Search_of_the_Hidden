using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameFoundation.Base
{
    [RequireComponent(typeof(Light2D))]
    public sealed class BuildingLight : MonoBehaviour
    {
        [SerializeField] private BaseBuildingConstruction construction;
        [SerializeField] private Light2D lightSource;

        private void Awake()
        {
            if (lightSource != null) lightSource.enabled = false;
        }

        private void OnEnable() => BuildingUpgradeService.Changed += Refresh;
        private void Start() => Refresh();
        private void OnDisable() => BuildingUpgradeService.Changed -= Refresh;

        public void Refresh()
        {
            if (lightSource != null)
                lightSource.enabled = construction == null || construction.IsBuilt;
        }
    }
}
