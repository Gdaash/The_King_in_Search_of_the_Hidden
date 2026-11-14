using System;
using System.Collections.Generic;
using UnityEngine;
namespace GameFoundation.MetaProgression
{
    [CreateAssetMenu(menuName = "Game Foundation/Weapon Challenges")]
    public sealed class PortalWeaponChallengeTable : ScriptableObject
    {
        [Serializable] public sealed class KillRequirement { public string enemyId; [Min(1)] public int count; }
        [Serializable] public sealed class Enhancement
        {
            // Cumulative bonuses at this rank, not another copy added on load.
            public float damage, intervalReduction, support;
            public List<KillRequirement> kills = new();
        }
        [Serializable] public sealed class WeaponChallenges
        {
            public PortalTowerBalance.Weapon weapon;
            public List<Enhancement> ranks = new();
        }
        public List<WeaponChallenges> weapons = new();
        public WeaponChallenges Find(PortalTowerBalance.Weapon weapon) => weapons.Find(x => x.weapon == weapon);
    }
}
