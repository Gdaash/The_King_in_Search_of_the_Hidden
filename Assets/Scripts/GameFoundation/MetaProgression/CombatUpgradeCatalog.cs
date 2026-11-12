using System.Collections.Generic;
using GameFoundation.Combat;
using UnityEngine;
using Weapon = GameFoundation.MetaProgression.PortalTowerBalance.Weapon;
using Definition = GameFoundation.MetaProgression.PortalTowerBalance.WeaponDefinition;

namespace GameFoundation.MetaProgression
{
    internal static class CombatUpgradeCatalog
    {
        internal enum Stat
        {
            Damage, Interval, Area, Range, Duration, Speed, Energy, Projectiles,
            Control, Chain, FirstHit, Support, Armor, Drain, Tick,
            ChainChance, ChainRetention, ChainLimit
        }

        internal readonly struct Upgrade
        {
            public readonly Stat bonus, drawback;
            public readonly int benefit;
            private readonly Vector3Int penalties;

            public Upgrade(Stat bonus, int benefit, Stat drawback, int green, int blue, int purple)
            {
                this.bonus = bonus;
                this.benefit = benefit;
                this.drawback = drawback;
                penalties = new Vector3Int(green, blue, purple);
            }

            public int Penalty(PortalTowerProgression.UpgradeCrystal quality) => penalties[(int)quality];
            public bool Applies(Definition weapon) => Supports(weapon, bonus) && Supports(weapon, drawback);
            public Stat OfferType => bonus is Stat.ChainChance or Stat.ChainRetention or Stat.ChainLimit ? Stat.Chain : bonus;
        }

        public static readonly IReadOnlyList<Upgrade> All = new Upgrade[]
        {
            new(Stat.Projectiles, 1, Stat.Energy, 100, 70, 50),
            new(Stat.Damage, 100, Stat.Interval, 100, 75, 50),
            new(Stat.Interval, -50, Stat.Energy, 75, 50, 30),
            new(Stat.Area, 50, Stat.Damage, -50, -35, -25),
            new(Stat.Area, 65, Stat.Interval, 90, 65, 45),
            new(Stat.Range, 75, Stat.Damage, -45, -30, -20),
            new(Stat.Duration, 100, Stat.Interval, 80, 55, 35),
            new(Stat.Control, 100, Stat.Damage, -50, -35, -25),
            new(Stat.Chain, 1, Stat.FirstHit, -50, -35, -25),
            new(Stat.Support, 100, Stat.Interval, 100, 70, 50),
            new(Stat.Armor, 2, Stat.Drain, 100, 70, 50),
            new(Stat.Projectiles, 1, Stat.Range, -50, -35, -25),
            new(Stat.Projectiles, 2, Stat.Damage, -70, -55, -40),
            new(Stat.Projectiles, 2, Stat.Interval, 120, 90, 65),
            new(Stat.Damage, 80, Stat.Range, -60, -45, -30),
            new(Stat.Damage, 100, Stat.Energy, 120, 85, 60),
            new(Stat.Damage, 90, Stat.Area, -60, -45, -30),
            new(Stat.Damage, 75, Stat.Speed, -65, -50, -35),
            new(Stat.Interval, -45, Stat.Damage, -45, -32, -22),
            new(Stat.Interval, -50, Stat.Range, -60, -45, -30),
            new(Stat.Area, 70, Stat.Range, -55, -40, -25),
            new(Stat.Area, 80, Stat.Energy, 100, 70, 50),
            new(Stat.Range, 100, Stat.Interval, 90, 65, 45),
            new(Stat.Speed, 100, Stat.Range, -55, -40, -25),
            new(Stat.Speed, 120, Stat.Energy, 40, 25, 15),
            new(Stat.Duration, 100, Stat.Area, -60, -45, -30),
            new(Stat.Tick, -45, Stat.Area, -55, -40, -25),
            new(Stat.Control, 100, Stat.Energy, 75, 50, 30),
            new(Stat.Support, 100, Stat.Range, -55, -40, -25),
            new(Stat.Armor, 2, Stat.Area, -60, -45, -30),
            new(Stat.ChainChance, 15, Stat.Energy, 45, 30, 20),
            new(Stat.ChainRetention, 20, Stat.FirstHit, -35, -25, -15),
            new(Stat.ChainLimit, 2, Stat.Interval, 45, 30, 20),
            new(Stat.ChainChance, 10, Stat.ChainRetention, -15, -10, -5),
            new(Stat.ChainLimit, 1, Stat.ChainChance, -8, -5, -3)
        };

        internal static bool IsFlat(Stat stat) => stat is Stat.Projectiles or Stat.Chain or Stat.Armor or Stat.ChainLimit;
        internal static bool IsPercentagePoint(Stat stat) => stat is Stat.ChainChance or Stat.ChainRetention;

        private static bool Supports(Definition w, Stat stat) => stat switch
        {
            Stat.Damage => !w.IsSupport,
            Stat.Interval => !w.continuous,
            Stat.Area => w.deliveryMethod == PortalTowerBalance.DeliveryMethod.Area ||
                w.combat != null && w.combat.targetMode == CombatWeapon.TargetMode.Area,
            Stat.Range => w.range > 0,
            Stat.Duration => w.HasDuration,
            Stat.Speed or Stat.Projectiles => w.deliveryMethod == PortalTowerBalance.DeliveryMethod.Projectile && !w.IsSupport,
            Stat.Energy => w.energyPerSecond <= 0,
            Stat.Control => !w.IsSupport && w.damageType is DamageType.Ice or DamageType.Physical,
            Stat.Chain or Stat.FirstHit or Stat.ChainChance or Stat.ChainRetention or Stat.ChainLimit
                => !w.IsSupport && w.damageType == DamageType.Electric,
            Stat.Support => w.weapon is Weapon.HealingRay or Weapon.MagicShield,
            Stat.Armor or Stat.Drain => w.weapon == Weapon.DefenseField,
            Stat.Tick => w.HasTicks && !w.IsSupport,
            _ => false
        };
    }
}
