using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using GameFoundation.MetaProgression;
using GameFoundation.UI;
using Object = UnityEngine.Object;

public static class EnemyLevelAudit
{
    static int checks;
    static void Check(bool value, string label)
    {
        if (!value) throw new Exception(label);
        checks++;
        File.AppendAllText("Temp/EnemyLevelAudit.txt", "PASS " + label + "\n");
    }
    public static async Task RunPlay()
    {
        checks = 0;
        File.WriteAllText("Temp/EnemyLevelAudit.txt", "Enemy levels Play Mode\n");
        var alarm = new GameObject("Level audit alarm").AddComponent<AlarmSystem>();
        var settings = new SerializedObject(alarm);
        settings.FindProperty("createRuntimeUiWhenMissing").boolValue = false;
        settings.ApplyModifiedPropertiesWithoutUndo();
        int events = 0;
        alarm.EnemyLevelChanged += _ => events++;
        foreach (float value in new[] { 0f, 99f, 100f, 109f, 109.99f, 110f, 120f, 590f, 1000f })
        {
            alarm.SetAlarm(value);
            Check(alarm.EnemyLevel == Mathf.Clamp(1 + Mathf.FloorToInt(Mathf.Max(0, value - 100) / 10), 1, 50), "alarm boundary " + value);
        }
        alarm.ResetAlarm(); alarm.SetAlarm(109);
        int before = events;
        alarm.AddAlarm(1);
        Check(alarm.EnemyLevel == 2 && events == before + 1, "incoming orb crosses overflow level once");
        var reservation = new GameObject("Reservation");
        alarm.ReserveActionAlarm(reservation, 9);
        Check(alarm.PreviewOrbRaisesLevel(1, 0), "preview includes reserved actions above cap");
        alarm.CancelActionAlarm(reservation);
        Check(!alarm.PreviewOrbRaisesLevel(1, 0), "cancellation removes reserved overflow");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/OctopusRaider.prefab");
        var old = Object.Instantiate(prefab);
        alarm.RegisterSpawnedEnemy(prefab, old);
        Check(old.GetComponent<EnemyLevel>().Level == 2, "spawn captures level");
        Check(Mathf.Approximately(old.GetComponent<Health>().MaxHealth, 77), "health scaling");
        Check(Mathf.Approximately(old.GetComponent<Health>().CurrentHealth, 77), "spawn begins at full scaled health");
        Check(old.GetComponent<MilitaryExperience>() == null, "enemy has no experience component");
        alarm.SetAlarm(120);
        Check(old.GetComponent<EnemyLevel>().Level == 2, "existing enemy keeps rank");
        foreach (var name in new[] { "Octopus", "OctopusWarrior", "OctopusRaider", "OctopusGuardian", "CursedMage", "CursedKnight" })
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/" + name + ".prefab");
            var unit = Object.Instantiate(p);
            alarm.RegisterSpawnedEnemy(p, unit);
            Check(unit.GetComponent<EnemyLevel>().Level == 3 && Mathf.Approximately(MilitaryExperience.Multiplier(unit.transform), 1.2f), name + " rank and combat multiplier");
            Check(Mathf.Approximately(unit.GetComponent<Health>().NormalizedHealth, 1), name + " full health");
            Object.Destroy(unit);
        }
        var canvas = new GameObject("Audit UI", typeof(Canvas));
        var roster = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/HUD/Enemy Roster.prefab"), canvas.transform);
        await Task.Delay(150);
        var view = roster.GetComponent<EnemyRosterView>();
        var stars = (MilitaryStarsView)new SerializedObject(view).FindProperty("levelStars").objectReferenceValue;
        var text = (UnityEngine.UI.Text)new SerializedObject(stars).FindProperty("overflowCount").objectReferenceValue;
        Check(text.text == "3", "roster common rank");
        alarm.SetAlarm(590);
        Check(text.text == "50", "roster level 50");
        alarm.AddAlarm(100);
        Check(alarm.EnemyLevel == 50 && alarm.PreviewOrbCount(1) == 0, "maximum level capped");
        alarm.ResetAlarm();
        Check(alarm.EnemyLevel == 1 && text.text == "1", "reset restores starting level");
        Object.Destroy(old); Object.Destroy(roster); Object.Destroy(canvas); Object.Destroy(reservation); Object.Destroy(alarm.gameObject);
        File.AppendAllText("Temp/EnemyLevelAudit.txt", "PASSED " + checks + " checks\n");
    }
}
