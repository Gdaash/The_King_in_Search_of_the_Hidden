public static class ScientificUpgrades
{
    public enum GatheringResource { None, Wood, Food, Stone, IronOre, MagicOre }
    public const string SharpKnives = "science.sharp_knives";
    public const string SharpPickaxes = "science.sharp_pickaxes";
    public const string HardenedPickaxes = "science.hardened_pickaxes";
    public const string CrystalCutters = "science.crystal_cutters";
    public const string QuietAxes = "science.quiet_axes";
    public const string QuietHunting = "science.quiet_hunting";
    public const string QuietQuarries = "science.quiet_quarries";
    public const string QuietMines = "science.quiet_mines";
    public const string CrystalEfficiency = "science.crystal_efficiency";
    public const string CrystalAbundance = "science.crystal_abundance";
    public const string StartingLights = "science.starting_lights";
    public static string SpeedUpgrade(GatheringResource resource) => resource switch
    {
        GatheringResource.Wood => SharpAxes, GatheringResource.Food => SharpKnives,
        GatheringResource.Stone => SharpPickaxes, GatheringResource.IronOre => HardenedPickaxes,
        GatheringResource.MagicOre => CrystalCutters, _ => null
    };
    public static string QuietUpgrade(GatheringResource resource) => resource switch
    {
        GatheringResource.Wood => QuietAxes, GatheringResource.Food => QuietHunting,
        GatheringResource.Stone => QuietQuarries,
        GatheringResource.IronOre or GatheringResource.MagicOre => QuietMines, _ => null
    };
    public static float ReduceAlarm(float amount, float reduction) => amount <= 0 ? 0 : UnityEngine.Mathf.Max(1, amount - reduction);
    public static int RequiredCrystals(int baseAmount, int rank) => UnityEngine.Mathf.Max(1,
        (int)(((long)baseAmount * (10 - UnityEngine.Mathf.Clamp(rank, 0, 5)) + 9) / 10));
    public const string PortalArrows = "Scientific1";
    public const string StrongWalls = "Scientific1.1";
    public const string FastHex = "Scientific1.2";
    public const string SharpAxes = "Scientific1.3";
    public const string QuietScouting = "Scientific1.4";
    public const string WarriorRetreat = "Scientific1.2.1";
    public const string WarriorBaseRegen = "Scientific1.2.1.1";
    public static readonly string[] Flashlights = {
        "Scientific1.1.1", "Scientific1.1.1.1", "Scientific1.1.1.1.1",
        "Scientific1.1.1.1.1.1", "Scientific1.1.1.1.1.1.1" };
}
