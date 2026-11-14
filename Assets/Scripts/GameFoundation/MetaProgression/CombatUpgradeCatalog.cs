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
            new(Stat.Projectiles, 1, Stat.Energy, 75, 50, 35),
            new(Stat.Damage, 80, Stat.Interval, 80, 60, 40),
            new(Stat.Interval, -30, Stat.Energy, 60, 40, 25),
            new(Stat.Area, 35, Stat.Damage, -35, -25, -15),
            new(Stat.Area, 50, Stat.Interval, 60, 45, 30),
            new(Stat.Range, 60, Stat.Damage, -35, -25, -15),
            new(Stat.Duration, 70, Stat.Interval, 60, 45, 30),
            new(Stat.Control, 75, Stat.Damage, -35, -25, -15),
            new(Stat.Chain, 1, Stat.FirstHit, -50, -35, -25),
            new(Stat.Support, 80, Stat.Interval, 80, 55, 35),
            new(Stat.Armor, 2, Stat.Drain, 100, 70, 50),
            new(Stat.Projectiles, 1, Stat.Range, -40, -30, -20),
            new(Stat.Projectiles, 2, Stat.Damage, -60, -50, -40),
            new(Stat.Projectiles, 2, Stat.Interval, 100, 75, 50),
            new(Stat.Damage, 60, Stat.Range, -45, -35, -25),
            new(Stat.Damage, 70, Stat.Energy, 100, 75, 50),
            new(Stat.Damage, 70, Stat.Area, -45, -35, -25),
            new(Stat.Damage, 60, Stat.Speed, -45, -35, -25),
            new(Stat.Interval, -25, Stat.Damage, -25, -18, -10),
            new(Stat.Interval, -35, Stat.Range, -45, -35, -25),
            new(Stat.Area, 50, Stat.Range, -40, -30, -20),
            new(Stat.Area, 60, Stat.Energy, 80, 55, 35),
            new(Stat.Range, 70, Stat.Interval, 60, 45, 30),
            new(Stat.Speed, 80, Stat.Range, -40, -30, -20),
            new(Stat.Speed, 100, Stat.Energy, 40, 25, 15),
            new(Stat.Duration, 80, Stat.Area, -40, -30, -20),
            new(Stat.Tick, -30, Stat.Area, -40, -30, -20),
            new(Stat.Control, 75, Stat.Energy, 60, 40, 25),
            new(Stat.Support, 70, Stat.Range, -40, -30, -20),
            new(Stat.Armor, 2, Stat.Area, -40, -30, -20),
            new(Stat.ChainChance, 35, Stat.Energy, 45, 30, 20),
            new(Stat.ChainRetention, 30, Stat.FirstHit, -25, -18, -10),
            new(Stat.ChainLimit, 2, Stat.Interval, 25, 15, 10),
            new(Stat.ChainChance, 20, Stat.ChainRetention, -15, -10, -5),
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
