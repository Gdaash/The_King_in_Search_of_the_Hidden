namespace GameFoundation.MetaProgression
{
    /// <summary>Enemy loot allowance for one expedition, independent of hex rewards and deliveries.</summary>
    public sealed class EnemyCrystalDropBudget
    {
        public const int CrystalsPerOpenedHex = 5;
        public const float DropChance = .1f;

        public int OpenedHexes { get; private set; }
        public int DroppedCrystals { get; private set; }
        public int Limit => OpenedHexes * CrystalsPerOpenedHex;
        public int Remaining => Limit - DroppedCrystals;

        public void RegisterOpenedHex() => OpenedHexes++;
        public bool CanDrop(float roll) => Remaining > 0 && roll >= 0f && roll < DropChance;

        // Called only after a crystal was successfully spawned, not when it is collected.
        public void RecordDrop()
        {
            if (Remaining > 0) DroppedCrystals++;
        }
    }
}
