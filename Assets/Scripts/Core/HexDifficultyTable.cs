using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Foundation/World/Hex Difficulty Table")]
public sealed class HexDifficultyTable : ScriptableObject
{
    [Serializable]
    public sealed class Difficulty
    {
        [Range(1, 5)] public int level = 1;
        public List<HexManager.HexGroupSettings> groups = new();
    }

    public List<Difficulty> difficulties = new();

    public IReadOnlyList<HexManager.HexGroupSettings> GetGroups(int level)
    {
        Difficulty selected = difficulties.FirstOrDefault(item => item.level == Mathf.Clamp(level, 1, 5));
        selected ??= difficulties.FirstOrDefault(item => item.level == 1);
        return selected != null ? selected.groups : Array.Empty<HexManager.HexGroupSettings>();
    }
}
