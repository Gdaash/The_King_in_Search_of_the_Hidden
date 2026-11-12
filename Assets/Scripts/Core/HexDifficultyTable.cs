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
        [Range(1, GameFoundation.MetaProgression.DayCycleService.MaxPortals)] public int level = 1;
        public List<HexManager.HexGroupSettings> groups = new();
    }

    public List<Difficulty> difficulties = new();

    public IReadOnlyList<HexManager.HexGroupSettings> GetGroups(int level)
    {
        Difficulty selected = difficulties.FirstOrDefault(item => item.level == level);
        selected ??= difficulties.FirstOrDefault(item => item.level == 1);
        return selected != null ? selected.groups : Array.Empty<HexManager.HexGroupSettings>();
    }

    public bool TryGetExactGroups(int level, out IReadOnlyList<HexManager.HexGroupSettings> groups)
    {
        Difficulty selected = difficulties.FirstOrDefault(item => item.level == level);
        groups = selected != null ? selected.groups : Array.Empty<HexManager.HexGroupSettings>();
        return selected != null;
    }
}
