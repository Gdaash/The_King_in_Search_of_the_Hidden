using GameFoundation.MetaProgression;
using UnityEngine;

namespace GameFoundation.Base
{
    /// <summary>Shows a building-button badge when the building can perform an action now.</summary>
    public sealed class BuildingActionAvailabilityIndicator : MonoBehaviour
    {
        public enum ActionType { CartPurchase, BlacksmithProduction, MilitaryTraining, BuildingConstruction, LaboratoryUpgrade, PortalTravel, RefugeeAdmission, None }

        [SerializeField] private GameObject marker;
        [SerializeField] private ActionType actionType;
        [SerializeField] private BaseBuildingConstruction construction;
        [SerializeField] private WarehouseCartPurchaseView warehouse;
        [SerializeField] private BlacksmithProductionView blacksmith;
        [SerializeField] private MilitaryTrainingView training;
        [SerializeField] private SkillButton[] laboratorySkills;
        [SerializeField] private GlobalStats laboratoryStats;
        [SerializeField] private GameFoundation.UI.BuildingButtonHighlight highlight;
        [SerializeField] private string upgradeBuildingId;

        private bool daySubscribed;

        private void Awake()
        {
            if (construction == null) construction = GetComponent<BaseBuildingConstruction>();
            Refresh();
        }

        private void OnEnable()
        {
            if (actionType == ActionType.LaboratoryUpgrade)
            {
                if (laboratoryStats != null) laboratoryStats.OnStatsUpdated += Refresh;
            }
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            BuildingUpgradeService.Changed += Refresh;
            GameFoundation.Quests.ContentUnlocks.Changed += Refresh;
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
            if (laboratoryStats != null) laboratoryStats.OnStatsUpdated -= Refresh;
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
            BuildingUpgradeService.Changed -= Refresh;
            GameFoundation.Quests.ContentUnlocks.Changed -= Refresh;
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
            if (marker != null) marker.SetActive(false);
            bool actionAvailable = actionType switch
            {
                ActionType.CartPurchase => warehouse != null && warehouse.CanBuyCart,
                ActionType.BlacksmithProduction => (blacksmith != null && blacksmith.CanProduceAny) || (warehouse != null && warehouse.CanBuyCart),
                ActionType.MilitaryTraining => training != null && training.CanArmWarrior,
                ActionType.BuildingConstruction => construction != null && construction.CanAffordConstruction,
                ActionType.LaboratoryUpgrade => HasAvailableLaboratoryUpgrade(),
                ActionType.PortalTravel => DayCycleService.Instance != null && !DayCycleService.Instance.EnteredToday,
                ActionType.RefugeeAdmission => DayCycleService.Instance != null && DayCycleService.Instance.RefugeesAvailable > 0 && BuildingUpgradeService.CanAdmitResident,
                _ => false
            };
            bool buildingReady = actionType == ActionType.BuildingConstruction || construction == null || construction.IsBuilt;
            bool upgrade = !string.IsNullOrEmpty(upgradeBuildingId) && BuildingUpgradeService.CanUpgrade(upgradeBuildingId);
            if (highlight != null) highlight.SetAvailable(buildingReady && (actionAvailable || upgrade));
        }

        private bool HasAvailableLaboratoryUpgrade()
        {
            if (laboratoryStats != null && laboratoryStats.UpgradeTable != null)
            {
                foreach (var entry in laboratoryStats.UpgradeTable.entries)
                    if (laboratoryStats.CanPurchaseUpgrade(entry)) return true;
                return false;
            }
            if (laboratorySkills == null) return false;
            foreach (SkillButton skill in laboratorySkills)
                if (skill != null && skill.CanPurchaseNow) return true;
            return false;
        }
    }
}
