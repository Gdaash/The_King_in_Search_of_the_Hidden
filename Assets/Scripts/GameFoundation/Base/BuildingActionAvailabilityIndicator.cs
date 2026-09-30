using GameFoundation.MetaProgression;
using UnityEngine;

namespace GameFoundation.Base
{
    /// <summary>Shows a building-button badge when the building can perform an action now.</summary>
    public sealed class BuildingActionAvailabilityIndicator : MonoBehaviour
    {
        public enum ActionType { CartPurchase, BlacksmithProduction, MilitaryTraining, BuildingConstruction, LaboratoryUpgrade, PortalTravel }

        [SerializeField] private GameObject marker;
        [SerializeField] private ActionType actionType;
        [SerializeField] private BaseBuildingConstruction construction;
        [SerializeField] private WarehouseCartPurchaseView warehouse;
        [SerializeField] private BlacksmithProductionView blacksmith;
        [SerializeField] private MilitaryTrainingView training;
        [SerializeField] private SkillButton[] laboratorySkills;

        private bool daySubscribed;

        private void Awake()
        {
            if (construction == null) construction = GetComponent<BaseBuildingConstruction>();
            Refresh();
        }

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            SubscribeDay();
            Refresh();
        }

        private void Start()
        {
            SubscribeDay();
            Refresh();
        }

        private void OnDisable()
        {
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
            if (daySubscribed && DayCycleService.Instance != null) DayCycleService.Instance.Changed -= Refresh;
            daySubscribed = false;
        }
        private void OnResourceChanged(ResourceType _, int __) => Refresh();

        private void SubscribeDay()
        {
            if (daySubscribed || DayCycleService.Instance == null) return;
            DayCycleService.Instance.Changed += Refresh;
            daySubscribed = true;
        }

        private void Refresh()
        {
            if (marker == null) return;
            bool actionAvailable = actionType switch
            {
                ActionType.CartPurchase => warehouse != null && warehouse.CanBuyCart,
                ActionType.BlacksmithProduction => blacksmith != null && blacksmith.CanProduceAny,
                ActionType.MilitaryTraining => training != null && training.CanArmWarrior,
                ActionType.BuildingConstruction => construction != null && construction.CanAffordConstruction,
                ActionType.LaboratoryUpgrade => HasAvailableLaboratoryUpgrade(),
                ActionType.PortalTravel => DayCycleService.Instance != null && !DayCycleService.Instance.EnteredToday,
                _ => false
            };
            bool buildingReady = actionType == ActionType.BuildingConstruction || construction == null || construction.IsBuilt;
            marker.SetActive(buildingReady && actionAvailable);
        }

        private bool HasAvailableLaboratoryUpgrade()
        {
            if (laboratorySkills == null) return false;
            foreach (SkillButton skill in laboratorySkills)
                if (skill != null && skill.CanPurchaseNow) return true;
            return false;
        }
    }
}
