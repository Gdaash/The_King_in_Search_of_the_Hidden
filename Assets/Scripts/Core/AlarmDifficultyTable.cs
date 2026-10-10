using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Foundation/World/Alarm Difficulty Table")]
public sealed class AlarmDifficultyTable : ScriptableObject
{
    [Serializable]
    public sealed class Difficulty
    {
        [Range(1, 5)] public int level = 1;
        [Min(1)] public float maximumAlarm = 300f;
        public List<AlarmThreshold> thresholds = new();
    }

    public List<Difficulty> difficulties = new();

    public IReadOnlyList<AlarmThreshold> GetThresholds(int level)
    {
        Difficulty selected = difficulties.FirstOrDefault(item => item.level == Mathf.Clamp(level, 1, 5));
        selected ??= difficulties.FirstOrDefault(item => item.level == 1);
        return selected != null ? selected.thresholds : Array.Empty<AlarmThreshold>();
    }
    public float GetMaximumAlarm(int level)
    {
        var selected=difficulties.FirstOrDefault(item=>item.level==Mathf.Clamp(level,1,5)) ?? difficulties.FirstOrDefault(item=>item.level==1);
        return Mathf.Max(1,selected != null ? selected.maximumAlarm : 300f);
    }
}
