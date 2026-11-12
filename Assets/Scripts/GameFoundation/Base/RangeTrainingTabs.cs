using GameFoundation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    [RequireComponent(typeof(MilitaryTrainingView))]
    public sealed class RangeTrainingTabs : MonoBehaviour
    {
        [SerializeField] private Button slingersTab, archersTab;
        [SerializeField] private ResourceType slinger, archer, bow;
        [SerializeField] private UnitDescriptionDefinition slingerDescription, archerDescription;
        private bool archersSelected;
        private bool districtRange;
        internal bool CanTrainSlinger(MilitaryTrainingView barracks) => barracks.CanArm(null, slinger);
        internal void SelectSlinger(MilitaryTrainingView barracks) => barracks.SelectUnit(null, slinger, slingerDescription);

        internal void UseDistrictRange()
        {
            districtRange = true;
            slingersTab.onClick.RemoveAllListeners();
            slingersTab.interactable = false;
            var label = slingersTab.GetComponentInChildren<Text>(true);
            label.GetComponent<GameFoundation.Localization.LocalizedText>()?.SetKey("base.range.crossbowmen");
            if (slingersTab.TryGetComponent<BaseDisabledButtonHint>(out var hint)) hint.enabled = false;
        }
        public bool CanTrain => GetComponent<MilitaryTrainingView>().CanArm(null, slinger) && !districtRange ||
            GetComponent<MilitaryTrainingView>().CanArm(bow, archer);

        private void Awake()
        {
            if (!districtRange) slingersTab.onClick.AddListener(() => Select(false));
            archersTab.onClick.AddListener(() => Select(true));
        }

        private void OnEnable()
        {
            BuildingUpgradeService.Changed += Refresh;
            Refresh();
        }
        private void OnDisable() => BuildingUpgradeService.Changed -= Refresh;
        private void Refresh() => Select(archersSelected && BuildingUpgradeService.IsBuilt("archery_range"));

        private void Select(bool archers)
        {
            archersSelected = archers && BuildingUpgradeService.IsBuilt("archery_range");
            var view = GetComponent<MilitaryTrainingView>();
            if (archersSelected || districtRange) view.SelectUnit(bow, archer, archerDescription);
            else view.SelectUnit(null, slinger, slingerDescription);
            archersTab.interactable = BuildingUpgradeService.IsBuilt("archery_range");
            if (archersTab.TryGetComponent<BaseDisabledButtonHint>(out var hint)) hint.enabled = false;
            RefreshIndicators();
        }

        internal void RefreshIndicators()
        {
            var view = GetComponent<MilitaryTrainingView>();
            Tint(slingersTab, !districtRange && !archersSelected, !districtRange && view.CanArm(null, slinger));
            Tint(archersTab, districtRange || archersSelected, view.CanArm(bow, archer));
        }

        internal static void Tint(Button tab, bool selected, bool canTrain)
        {
            if (tab.TryGetComponent<ActionButtonLabelColor>(out var style)) style.enabled = false;
            var feedback = tab.GetComponent<UnifiedButtonFeedback>() ?? tab.gameObject.AddComponent<UnifiedButtonFeedback>();
            feedback.Toggled = false;
            feedback.SetGoldSelection(selected);
            var label = tab.GetComponentInChildren<Text>(true);
            if (label != null) label.color = !tab.interactable ? new(.55f, .51f, .57f) :
                canTrain ? new(.55f, .85f, .54f) : new(.94f, .91f, .82f);
        }
    }
}
