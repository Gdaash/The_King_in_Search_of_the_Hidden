namespace GameFoundation.Combat
{
    /// <summary>On-hit behavior only. Every attack uses the same armor calculation.</summary>
    public static class CombatHitEffects
    {
        public const float StunChance = .1f;
        public const float StunSeconds = .45f;
        public const float StunRecoverySeconds = 1f;
        public const float IgniteChance = .2f;
        public const float BurnSeconds = 3f;
        public const float BurnTickSeconds = 1f;
        public const float BurnDamageFraction = .35f;
        public const float EchoChance = .15f;
        public const float EchoDelaySeconds = .4f;
        public const float EchoDamageFraction = .5f;
        public const float LightningJumpChance = .15f;
        public const int MaximumLightningJumps = 3;
        public const float LightningDamageFraction = .5f;
        public static float NextLightningDamage(float damage) => damage * LightningDamageFraction;
        public const float ChillSeconds = 1.5f;
        public const float ChillFraction = .35f;
        public static bool Procs(float roll, float chance) => roll >= 0 && roll < chance;
    }
}
