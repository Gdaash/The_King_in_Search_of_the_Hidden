#if UNITY_EDITOR || DEVELOPMENT_BUILD
using GameFoundation.MetaProgression;
using GameFoundation.Quests;
using GameFoundation.Saves;
using UnityEngine;

namespace GameFoundation.Base
{
    internal static class BuildingCheatReset
    {
        internal static void Reset(string id, GlobalStats laboratory = null)
        {
            if (string.IsNullOrEmpty(id) || GlobalResourceManager.Instance == null || GameSaveService.WritesSuspended) return;
            using var batch = SaveSlotPrefs.Batch();
            ResetConstruction(id);
            switch (id)
            {
                case "housing":
                    HousingDistrict.ResetHouseCards();
                    ResetConstruction(ExpeditionTasks.Coop);
                    ResetConstruction(HousingDistrict.Fields);
                    HousingDistrict.ResetCoopProduction();
                    break;
                case ExpeditionTasks.Coop:
                    HousingDistrict.ResetCoopProduction();
                    break;
                case "blacksmith":
                    ResetConstruction(CraftQuarterLayout.Smithy);
                    ResetConstruction(ExpeditionTasks.Tannery);
                    ResetConstruction(CraftQuarterLayout.Tannery);
                    ResetConstruction(CraftQuarterLayout.LeatherShop);
                    ResetConstruction(CraftQuarterLayout.CharcoalPit);
                    CraftProduction.Reset();
                    EquipmentUpgrades.ResetBlacksmithUpgrades();
                    break;
                case "laboratory":
                    if (laboratory?.UpgradeTable != null) laboratory.ResetLaboratoryResearch();
                    break;
                case "magic_library":
                    PortalArsenalProgress.ResetUnlocks();
                    break;
                case "castle":
                    foreach (var idToDisable in new[] { RoyalDecreeService.CautiousWarriors,
                        RoyalDecreeService.FinishOffEnemies, RoyalDecreeService.ForestForaging })
                        RoyalDecreeService.SetEnabled(idToDisable, false);
                    break;
                case "refugees":
                    SaveSlotPrefs.SetInt("foundation.refugees.firstArrival", 0);
                    break;
                case PortalProgression.BuildingId:
                    PortalProgression.ResetDestinationUpgrades();
                    break;
            }
            SaveSlotPrefs.Save();
            BuildingUpgradeService.NotifyChanged();
            QuestProgress.NotifyChanged();
        }

        private static void ResetConstruction(string id)
        {
            string prefix = "foundation.building." + id;
            SaveSlotPrefs.SetInt(prefix + ".built", 0);
            SaveSlotPrefs.DeleteKey(prefix + ".builtDay");
            SaveSlotPrefs.SetInt(prefix + ".upgradeLevel", 0);
        }
    }
}
#endif
