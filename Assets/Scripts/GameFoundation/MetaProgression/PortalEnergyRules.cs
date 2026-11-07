using UnityEngine;
namespace GameFoundation.MetaProgression
{
    public static class PortalEnergyRules
    {
        public static int Capacity(PortalTowerBalance balance) => Mathf.Max(1, balance.crystalCapacity) * Mathf.Max(1, balance.energyPerCrystal);
        public static int LevelThreshold(PortalTowerBalance balance, int level, int efficiencyRank)
        {
            int total = 0;
            for (int i = 1; i < level; i++) total += ScientificUpgrades.RequiredCrystals(balance.RequiredExperience(i), efficiencyRank);
            return total * Mathf.Max(1, balance.energyPerCrystal);
        }
        public static int LightThreshold(PortalTowerBalance balance, int index, int efficiencyRank)
        {
            // Energy thresholds are independent of the legacy level/upgrade prices.
            int crystals = balance.lightCrystalThresholds != null && index < balance.lightCrystalThresholds.Length
                ? balance.lightCrystalThresholds[index] : 6 * (index + 1) * (index + 1);
            return Mathf.Max(1, crystals) * Mathf.Max(1, balance.energyPerCrystal);
        }
        public static int BonusLights(PortalTowerBalance balance, int energy, int efficiencyRank)
        {
            int count = 0;
            for (int i = 0; i < WorldFlashlightAvailability.MaximumCount - 1; i++)
                if (energy >= LightThreshold(balance, i, efficiencyRank)) count++;
            return count;
        }
        public static int Level(PortalTowerBalance balance, int energy, int efficiencyRank)
        {
            int level = 1, remaining = energy / Mathf.Max(1, balance.energyPerCrystal);
            while (remaining >= ScientificUpgrades.RequiredCrystals(balance.RequiredExperience(level), efficiencyRank))
            { remaining -= ScientificUpgrades.RequiredCrystals(balance.RequiredExperience(level), efficiencyRank); level++; }
            return level;
        }
    }
}
