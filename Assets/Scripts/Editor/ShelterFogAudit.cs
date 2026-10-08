using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ShelterFogAudit
{
    public static async Task RunPlay()
    {
        string folder = "Temp/FogRuntime";
        Directory.CreateDirectory(folder);
        await LoadBase();
        await Task.Delay(1000);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/World/Base/Layers/38_Shelter_Fog.png");
        var fog = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Single(s => s.sprite == sprite);
        var camera = Camera.main;
        if (!camera || !fog.enabled || !fog.gameObject.activeInHierarchy) throw new Exception("Fog/camera inactive");
        var material = fog.sharedMaterial;
        File.WriteAllText(folder + "/report.txt", "Actual Base scene Play Mode; shader=" + material.shader.name +
            " speed=" + material.GetFloat("_WindSpeed") + " pixels=" + material.GetFloat("_MotionPixels") +
            " preview=" + material.GetFloat("_PreviewTime") + "\n");
        for (int frame = 0; frame < 9; frame++)
        {
            File.AppendAllText(folder + "/report.txt", $"frame={frame} time={Time.time:F3} unscaled={Time.unscaledTime:F3} scale={Time.timeScale}\n");
            Capture(camera, fog, folder + "/scene_" + frame + ".png", false);
            Capture(camera, fog, folder + "/fog_" + frame + ".png", true);
            await Task.Delay(1000);
        }
        File.AppendAllText(folder + "/report.txt", "Completed actual-time captures without preview overrides.\n");
    }

    private static async Task LoadBase()
    {
        var load = SceneManager.LoadSceneAsync("Assets/Scenes/Base.unity");
        while (!load.isDone) await Task.Delay(30);
    }

    private static void Capture(Camera camera, SpriteRenderer fog, string file, bool isolated)
    {
        var rt = new RenderTexture(854, 480, 24);
        var tex = new Texture2D(854, 480, TextureFormat.RGB24, false);
        var old = camera.targetTexture; var active = RenderTexture.active;
        int mask = camera.cullingMask, layer = fog.gameObject.layer;
        var clear = camera.clearFlags; var color = camera.backgroundColor;
        try
        {
            if (isolated) { fog.gameObject.layer = 31; camera.cullingMask = int.MinValue; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; }
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 854, 480), 0, 0); tex.Apply(); File.WriteAllBytes(file, tex.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = old; RenderTexture.active = active; fog.gameObject.layer = layer;
            camera.cullingMask = mask; camera.clearFlags = clear; camera.backgroundColor = color;
            Object.Destroy(rt); Object.Destroy(tex);
        }
    }
}
