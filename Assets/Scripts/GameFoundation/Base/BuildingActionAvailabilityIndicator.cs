using GameFoundation.MetaProgression;
using UnityEngine;

namespace GameFoundation.Base
{
    /// <summary>Shows a building-button badge when the building can perform an action now.</summary>
    public sealed class BuildingActionAvailabilityIndicator : MonoBehaviour
    {
        public enum ActionType { CartPurchase, BlacksmithProduction, MilitaryTraining, BuildingConstruction, LaboratoryUpgrade, PortalTravel, RefugeeAdmission, None, MagicTowerUnlock }

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

        private System.Func<bool> militaryDistrict;
        internal void SetMilitaryDistrict(System.Func<bool> available) => militaryDistrict = available;

        private bool daySubscribed;
        private RoyalDecreeRow[] royalDecrees;
        private PortalTowerBalance portalBalance;
        private PortalArsenalPopup arsenal;

        public void SetRoyalDecrees(RoyalDecreePopupView popup)
        {
            royalDecrees = popup != null ? popup.GetComponentsInChildren<RoyalDecreeRow>(true) : null;
            Refresh();
        }

        private void Awake()
        {
            if (construction == null) construction = GetComponent<BaseBuildingConstruction>();
            if (actionType == ActionType.MagicTowerUnlock)
            {
                portalBalance = Resources.Load<PortalTowerBalance>("World/PortalTowerBalance");
                if (portalBalance != null && portalBalance.arsenalPopupPrefab != null)
                    arsenal = portalBalance.arsenalPopupPrefab.GetComponent<PortalArsenalPopup>();
            }
            Refresh();
        }

        private void OnEnable()
        {
            if (actionType == ActionType.LaboratoryUpgrade)
            {
                if (laboratoryStats != null) laboratoryStats.OnStatsUpdated += Refresh;
            }
            if (actionType == ActionType.MagicTowerUnlock)
            {
                PortalArsenalProgress.UnlocksChanged += Refresh;
                GameFoundation.Saves.SaveSlotPrefs.SelectionChanged += Refresh;
            }
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            RoyalDecreeService.Changed += OnDecreeChanged;
            BuildingUpgradeService.Changed += Refresh;
            CraftProduction.Changed += Refresh;
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
            PortalArsenalProgress.UnlocksChanged -= Refresh;
            GameFoundation.Saves.SaveSlotPrefs.SelectionChanged -= Refresh;
            if (laboratoryStats != null) laboratoryStats.OnStatsUpdated -= Refresh;
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
            RoyalDecreeService.Changed -= OnDecreeChanged;
            BuildingUpgradeService.Changed -= Refresh;
            CraftProduction.Changed -= Refresh;
            GameFoundation.Quests.ContentUnlocks.Changed -= Refresh;
            if (daySubscribed && DayCycleService.Instance != null) DayCycleService.Instance.Changed -= Refresh;
            daySubscribed = false;
        }
        private void OnResourceChanged(ResourceType _, int __) => Refresh();
        private void OnDecreeChanged(string _, bool __) => Refresh();

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
                ActionType.MilitaryTraining => training != null && (training.TryGetComponent<RangeTrainingTabs>(out var range) ? range.CanTrain : training.CanArmWarrior),
                ActionType.BuildingConstruction => construction != null && construction.CanAffordConstruction,
                ActionType.LaboratoryUpgrade => HasAvailableLaboratoryUpgrade(),
                ActionType.PortalTravel => DayCycleService.Instance != null && !DayCycleService.Instance.EnteredToday && DayCycleService.Instance.HasExpeditionResidents,
                ActionType.RefugeeAdmission => DayCycleService.Instance != null && DayCycleService.Instance.RefugeesAvailable > 0 && BuildingUpgradeService.CanAdmitResident,
                ActionType.MagicTowerUnlock => arsenal != null && portalBalance.useSelectableArsenal &&
                    portalBalance.useEnergyLoadout && arsenal.HasAffordableBaseUnlock(portalBalance),
                _ => HasAvailableDecree()
            };
            bool buildingReady = actionType == ActionType.BuildingConstruction || construction == null || construction.IsBuilt;
            bool upgrade = upgradeBuildingId == "housing" ? HousingDistrict.HasAvailableAction()
                : !string.IsNullOrEmpty(upgradeBuildingId) && BuildingUpgradeService.CanUpgrade(upgradeBuildingId);
            if (highlight != null) highlight.SetAvailable(buildingReady && (militaryDistrict?.Invoke() ?? (actionAvailable || upgrade)));
        }

        private bool HasAvailableDecree()
        {
            if (royalDecrees == null) return false;
            foreach (var row in royalDecrees)
                if (row != null && RoyalDecreeService.CanEnable(row.decree)) return true;
            return false;
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
