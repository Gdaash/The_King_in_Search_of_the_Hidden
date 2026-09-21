using PlayerPrefs = GameFoundation.Saves.SaveSlotPrefs;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    public static class DayResourceLedger
    {
        [Serializable]
        public sealed class Entry
        {
            public string resource;
            public int start;
            public int runChange;
            public int baseGained;
            public int baseSpent;
            public int end;
        }

        [Serializable]
        public sealed class Report
        {
            public int day;
            public int starved;
            public int runDurationSeconds;
            public List<Entry> entries = new();
        }

        [Serializable]
        private sealed class State
        {
            public int day;
            public int starved;
            public int runDurationSeconds;
            public bool runActive;
            public List<Entry> entries = new();
            public List<Entry> runStart = new();
        }

        private const string SaveKey = "foundation.dayResourceLedger";
        private static State _state;
        private static float _runStartedAt;
        public static Report LastReport { get; private set; }

        public static void ResetForSlot()
        {
            _state = null;
            LastReport = null;
            _runStartedAt = 0f;
        }

        public static void EnsureDay(int day)
        {
            if (_state == null && PlayerPrefs.HasKey(SaveKey))
                _state = JsonUtility.FromJson<State>(PlayerPrefs.GetString(SaveKey));
            if (_state != null && _state.day == day) return;
            _state = new State { day = day };
            foreach (var pair in Snapshot())
                _state.entries.Add(new Entry { resource = pair.Key, start = pair.Value });
            Save();
        }

        public static void BeginRun()
        {
            EnsureDay(DayCycleService.Instance != null ? DayCycleService.Instance.Day : 1);
            if (_state.runActive) return;
            _state.runActive = true;
            _runStartedAt = Time.realtimeSinceStartup;
            _state.runStart.Clear();
            foreach (var pair in Snapshot())
                _state.runStart.Add(new Entry { resource = pair.Key, start = pair.Value });
            Save();
        }

        public static void EndRun(int durationSeconds = -1)
        {
            if (_state == null || !_state.runActive) return;
            var end = Snapshot();
            var start = new Dictionary<string, int>();
            foreach (var entry in _state.runStart) start[entry.resource] = entry.start;
            var names = new HashSet<string>(start.Keys);
            names.UnionWith(end.Keys);
            foreach (string name in names)
            {
                start.TryGetValue(name, out int before);
                end.TryGetValue(name, out int after);
                GetEntry(name).runChange += after - before;
            }
            _state.runDurationSeconds += durationSeconds >= 0 ? durationSeconds
                : _runStartedAt > 0f ? Mathf.Max(0, Mathf.FloorToInt(Time.realtimeSinceStartup - _runStartedAt)) : 0;
            _state.runActive = false;
            _state.runStart.Clear();
            Save();
        }

        public static void RecordBaseChange(ResourceType type, int change)
        {
            if (type == null || change == 0 || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Base")
                return;
            EnsureDay(DayCycleService.Instance != null ? DayCycleService.Instance.Day : 1);
            Entry entry = GetEntry(type.name);
            if (change > 0) entry.baseGained += change;
            else entry.baseSpent -= change;
            Save();
        }

        public static void RecordStarvation(int deaths)
        {
            if (deaths <= 0) return;
            EnsureDay(DayCycleService.Instance != null ? DayCycleService.Instance.Day : 1);
            _state.starved += deaths;
            Save();
        }

        public static Report FinishDay(int day)
        {
            EnsureDay(day);
            EndRun();
            var end = Snapshot();
            foreach (var pair in end) GetEntry(pair.Key);
            var report = new Report { day = day, starved = _state.starved, runDurationSeconds = _state.runDurationSeconds };
            foreach (var entry in _state.entries)
            {
                end.TryGetValue(entry.resource, out int amount);
                report.entries.Add(new Entry
                {
                    resource = entry.resource, start = entry.start, runChange = entry.runChange,
                    baseGained = entry.baseGained, baseSpent = entry.baseSpent, end = amount
                });
            }
            LastReport = report;
            return report;
        }

        public static void StartNextDay(int day)
        {
            _state = null;
            EnsureDay(day);
        }

        private static Entry GetEntry(string resource)
        {
            foreach (var entry in _state.entries)
                if (entry.resource == resource) return entry;
            var added = new Entry { resource = resource };
            _state.entries.Add(added);
            return added;
        }

        private static Dictionary<string, int> Snapshot()
        {
            var result = new Dictionary<string, int>();
            var manager = GlobalResourceManager.Instance;
            if (manager == null) return result;
            foreach (var pair in manager.GetAllResourcesData())
                if (pair.Key != null) result[pair.Key.name] = pair.Value;
            if (manager.AvailableResources != null)
                foreach (var resource in manager.AvailableResources)
                    if (resource != null && !result.ContainsKey(resource.name))
                        result[resource.name] = manager.GetResourceAmount(resource);
            return result;
        }

        private static void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(_state));
            PlayerPrefs.Save();
        }
    }
}


