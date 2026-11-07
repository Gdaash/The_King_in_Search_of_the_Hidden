using GameFoundation.Localization;
namespace GameFoundation.Combat
{
    public static class CombatEffectText
    {
        public static string Description(DamageType theme)
        {
            string key, fallback;
            switch (theme)
            {
                case DamageType.Physical: key = "combat.effect.stun"; fallback = "Шанс ненадолго оглушить цель."; break;
                case DamageType.Fire: key = "combat.effect.burn"; fallback = "Шанс поджечь цель. Горение наносит периодический урон."; break;
                case DamageType.Ice: key = "combat.effect.chill"; fallback = "Замедляет движение и атаки цели."; break;
                case DamageType.Electric: key = "combat.effect.lightning"; fallback = "Шанс перескочить на ближайшую цель. До трёх перескоков, каждый вдвое слабее."; break;
                default: key = "combat.effect.echo"; fallback = "Шанс вызвать магическое эхо: часть урона повторяется с задержкой."; break;
            }
            var service = LocalizationService.Instance;
            var value = service != null ? service.Get(key) : key;
            return value == key ? fallback : value;
        }
    }
}
