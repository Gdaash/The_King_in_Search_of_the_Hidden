using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

public static class ProductionFragmentsAudit
{
    private const string Report = "Temp/ProductionFragmentsAudit.txt";
    private static int checks;
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        File.AppendAllText(Report, "PASS " + description + "\n"); checks++;
    }
    private static SpriteRenderer[] Visible(ProductionFragments effect) =>
        effect.GetComponentsInChildren<SpriteRenderer>().Where(s => s.enabled).ToArray();
    private static async Task Until(Func<bool> condition)
    {
        double deadline = EditorApplication.timeSinceStartup + 6;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timed out waiting for particle simulation");
            await Task.Delay(30);
        }
    }

    public static async Task RunPlay()
    {
        File.WriteAllText(Report, "Production fragments Play Mode checks\n"); checks = 0;
        GameSpeedControls.SetSimulationSpeed(1);
        foreach (string name in new[] { "Forest 1", "Forest 2", "Forest 3", "Stone 1", "Stone 2", "Stone 3", "Iron Ore Mine", "Magic Ore Mine",
                     "Animals/Chickens 1", "Animals/Chickens 2", "Animals/Chickens 3", "Animals/Boars 1", "Animals/Boars 2", "Animals/Boars 3" })
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/" + name + ".prefab"));
            try
            {
                foreach (var timer in root.GetComponentsInChildren<TimerController>(true))
                {
                    var settings = new SerializedObject(timer);
                    settings.FindProperty("runOnStart").boolValue = false;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    timer.OnTimerEnd = new UnityEvent();
                }
                await Task.Delay(100);
                foreach (var effect in root.GetComponentsInChildren<ProductionFragments>(true))
                {
                    // Activate each authored stage independently; the game's cycle events are left out of this visual test.
                    var timer = effect.GetComponent<TimerController>();
                    var ancestor = effect.transform;
                    while (ancestor != null && ancestor != root.transform) { ancestor.gameObject.SetActive(true); ancestor = ancestor.parent; }
                    Check(!effect.IsEmitting && Visible(effect).Length == 0, name + " idle: no fragments");
                    timer.SetDurationAndStart(4);
                    var allowedDust = new SerializedObject(effect).FindProperty("dustSprites");
                    var dustSprites = Enumerable.Range(0, allowedDust.arraySize).Select(i => allowedDust.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                    await Until(() => Visible(effect).Any(s => dustSprites.Contains(s.sprite)) &&
                                      Visible(effect).Any(s => !dustSprites.Contains(s.sprite)));
                    Check(effect.IsEmitting && Visible(effect).Length > 0, name + " working: fragments emitted");
                    var allowed = new SerializedObject(effect).FindProperty("fragments");
                    var sprites = Enumerable.Range(0, allowed.arraySize).Select(i => allowed.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                    Check(Visible(effect).All(s => sprites.Contains(s.sprite) || dustSprites.Contains(s.sprite)), name + " uses assigned material and dust");
                    Check(Visible(effect).Any(s => dustSprites.Contains(s.sprite)) && Visible(effect).Any(s => sprites.Contains(s.sprite)), name + " dust accompanies fragments");
                    if (name.StartsWith("Animals/"))
                    {
                        var animals = root.GetComponentsInChildren<HexAnimalWander>();
                        Check(animals.Length > 0 && Visible(effect).All(p => animals.Any(a => Vector2.Distance(p.transform.position, a.transform.position) < 0.8f)), name + " particles follow current animal positions");
                        if (name.EndsWith("3")) Capture(root, name.Replace("Animals/", ""));
                    }
                    if (name == "Forest 3")
                    {
                        Capture(root);
                        GameSpeedControls.SetSimulationSpeed(0);
                        var positions = Visible(effect).Select(s => s.transform.position).ToArray();
                        await Task.Delay(150);
                        Check(!effect.IsEmitting && positions.SequenceEqual(Visible(effect).Select(s => s.transform.position)), "pause freezes particles");
                        GameSpeedControls.SetSimulationSpeed(1);
                    }
                    timer.SetDurationAndStart(0.08f);
                    await Until(() => !effect.IsEmitting && Visible(effect).Length == 0);
                    Check(!effect.IsEmitting && Visible(effect).Length == 0, name + " completed: no emission, trail cleared");
                    timer.SetDurationAndStart(2);
                    await Until(() => Visible(effect).Length > 0);
                    Check(effect.IsEmitting && Visible(effect).Length > 0, name + " next cycle resumes emission");
                    timer.StopForEscape();
                    await Until(() => Visible(effect).Length == 0);
                    Check(!effect.IsEmitting && Visible(effect).Length == 0, name + " cancelled: no emission, trail cleared");
                    effect.gameObject.SetActive(false);
                    Check(Visible(effect).Length == 0, name + " disabled: cleared");
                }
            }
            finally { Object.Destroy(root); }
            await Task.Delay(50);
        }
        File.AppendAllText(Report, "ALL PASSED: " + checks + " checks\n");
    }

    private static void Capture(GameObject root, string suffix = "")
    {
        var go = new GameObject("Particle preview camera");
        var camera = go.AddComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 1.8f;
        camera.transform.position = root.transform.position + new Vector3(0, 0.35f, -10);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.13f, .12f, .15f);
        var rt = new RenderTexture(640, 640, 24); var previous = RenderTexture.active;
        var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); image.Apply();
            File.WriteAllBytes("Temp/ProductionFragmentsPreview" + suffix + ".png", image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; camera.targetTexture = null; Object.Destroy(rt); Object.Destroy(image); Object.Destroy(go); }
    }
}
