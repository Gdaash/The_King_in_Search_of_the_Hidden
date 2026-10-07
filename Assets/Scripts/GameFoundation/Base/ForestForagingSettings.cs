using UnityEngine;
namespace GameFoundation.Base
{
    [CreateAssetMenu(menuName = "Game Foundation/Forest Foraging Settings")]
    public sealed class ForestForagingSettings : ScriptableObject
    {
        public ResourceType influence, food, humans, swordsmen, archers;
        [Min(0)] public int cost = 5;
        [Min(.1f)] public float duration = 2f;
        [Range(0,1)] public float survivalChance = .5f;
        [Min(0)] public int minimumFood = 1;
        [Min(0)] public int maximumFood = 2;
    }
}
