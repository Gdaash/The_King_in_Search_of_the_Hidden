#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using GameFoundation.Bestiary;
using GameFoundation.Combat;
using GameFoundation.Saves;

public static class EnemyResearchValidation
{
    public static void Run()
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        var service = typeof(BestiaryService);
        var saveField = service.GetField("save", flags); var slotField = service.GetField("loadedSlot", flags);
        var counts = (IDictionary)service.GetField("killCounts", flags).GetValue(null);
        var originals = new List<DictionaryEntry>(); foreach (DictionaryEntry e in counts) originals.Add(e);
        var oldSave = saveField.GetValue(null); var oldSlot = slotField.GetValue(null);
        var selected = typeof(SaveSlotPrefs).GetField("_selected", flags); var oldSelected = selected.GetValue(null);
        var storage = typeof(SaveSlotPrefs).Assembly.GetType("GameFoundation.Saves.GameSaveService");
        var suspend = storage.GetField("WritesSuspended", BindingFlags.Static | BindingFlags.NonPublic);
        var oldSuspended = suspend.GetValue(null); GameObject go = null, strong = null;
        try
        {
            // Exercise real counting and damage without writing any player save.
            suspend.SetValue(null, true);
            var temporary = Activator.CreateInstance(saveField.FieldType);
            saveField.SetValue(null, temporary); slotField.SetValue(null, SaveSlotPrefs.SelectedSlot); counts.Clear();
            go = new GameObject("Research validation"); go.SetActive(false); go.tag = "Enemy1";
            var identity = go.AddComponent<EnemyIdentity>(); Identity(identity, "Octopus", 1);
            var combat = go.AddComponent<Combatant>(); combat.baseHealth = 1000; combat.baseArmor = 2;
            var health = go.AddComponent<Health>(); health.OnHealthChanged = new UnityEngine.Events.UnityEvent<float>();
            typeof(Health).GetField("bestiaryIdentity", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(health, identity);
            var types = (DamageType[])Enum.GetValues(typeof(DamageType));
            var baseline = new Dictionary<DamageType, float>();
            foreach (var type in types) { health.SetNormalizedHealth(1); health.TakeDamage(20, type); baseline[type] = health.MaxHealth - health.CurrentHealth; }
            for (int i = 0; i < 49; i++) BestiaryService.RegisterKill(identity);
            Check(BestiaryService.Kills(identity.Id) == 49 && !BestiaryService.IsStudied(identity), "49 must remain unstudied");
            BestiaryService.RegisterKill(identity);
            Check(BestiaryService.IsStudied(identity), "50 unlocks Octopus weakness");
            foreach (var type in types)
            {
                health.SetNormalizedHealth(1); health.TakeDamage(20, type);
                Check(Mathf.Abs((health.MaxHealth - health.CurrentHealth) - baseline[type] * 1.2f) < .001f, "Damage multiplier: " + type);
            }
            strong = new GameObject("Strong enemy validation"); strong.SetActive(false);
            var toxic = strong.AddComponent<EnemyIdentity>(); Identity(toxic, "ToxicDemon", 12);
            Check(BestiaryService.Kills(toxic.Id) == 0, "Species progress must be independent");
            for (int i = 0; i < 9; i++) BestiaryService.RegisterKill(toxic);
            Check(!BestiaryService.IsStudied(toxic), "9 must remain unstudied"); BestiaryService.RegisterKill(toxic);
            Check(BestiaryService.IsStudied(toxic), "10 unlocks ToxicDemon weakness");
            var json = JsonUtility.ToJson(temporary);
            Check(JsonUtility.ToJson(JsonUtility.FromJson(json, saveField.FieldType)) == json, "Save round trip");
            for (int rank = 2; rank <= 12; rank++) Check(BestiaryService.RequiredKills(rank) < BestiaryService.RequiredKills(rank - 1), "Threshold order");
            Debug.Log("ENEMY_RESEARCH_VALIDATED: 49/50 and 9/10 boundaries, independent species, all damage types after armor x1.2, save round trip. Player saves untouched.");
        }
        finally
        {
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
            if (strong != null) UnityEngine.Object.DestroyImmediate(strong);
            counts.Clear(); foreach (var e in originals) counts.Add(e.Key, e.Value);
            saveField.SetValue(null, oldSave); slotField.SetValue(null, oldSlot); selected.SetValue(null, oldSelected); suspend.SetValue(null, oldSuspended);
        }
    }
    static void Identity(EnemyIdentity identity, string id, int rank)
    { var so = new SerializedObject(identity); so.FindProperty("persistentId").stringValue = id; so.FindProperty("dangerRank").intValue = rank; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void Check(bool ok, string label) { if (!ok) throw new Exception(label); }
}
#endif
