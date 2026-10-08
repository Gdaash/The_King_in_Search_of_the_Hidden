using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameFoundation.Base;
using GameFoundation.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class ShelterButtonsAudit
{
    const string Report = "Temp/ShelterButtons/report.txt";
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        File.AppendAllText(Report, "PASS " + message + "\n");
    }

    public static async Task RunPlay()
    {
        Directory.CreateDirectory("Temp/ShelterButtons");
        File.WriteAllText(Report, "Shelter button variants: actual Base scene\n");
        var load = SceneManager.LoadSceneAsync("Assets/Scenes/Base.unity");
        while (!load.isDone) await Task.Delay(30);
        await Task.Delay(600);
        var owners = Object.FindObjectsByType<WorldBuildingButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var buttons = owners.SelectMany(w => w.GetComponentsInChildren<Button>(true)).Distinct().ToArray();
        Check(buttons.Length == 24, "24 building/construction/upgrade buttons");
        foreach (var b in buttons)
        {
            var feedback = b.GetComponent<UnifiedButtonFeedback>();
            var so = new SerializedObject(feedback);
            Check(so.FindProperty("useHoverGraphicAlpha").boolValue && so.FindProperty("idleGraphicAlpha").intValue == 0 &&
                so.FindProperty("hoverGraphicAlpha").intValue == 150, b.name + " shared alpha settings");
            Check(b.targetGraphic && b.targetGraphic.transform != b.transform &&
                so.FindProperty("scaleTarget").objectReferenceValue == b.targetGraphic.rectTransform,
                b.name + " independent visual target");
            Check(!b.targetGraphic.GetComponents<Outline>().Any(o => o.enabled), b.name + " no duplicate background");
            var label = b.targetGraphic.GetComponentsInChildren<Text>(true).First(t => t.name == "Label");
            var textOutline = label.GetComponent<Outline>();
            Check(textOutline && textOutline.enabled && textOutline.effectColor == Color.black,
                b.name + " black text outline inherited");
            var effects = label.GetComponents<BaseMeshEffect>();
            var spacing = label.GetComponent<LetterSpacing>();
            Check(!spacing || Array.IndexOf(effects, spacing) < Array.IndexOf(effects, textOutline),
                b.name + " spacing runs before outline");
            if (b.GetComponent<BuildingUpgradeButton>())
            {
                Check(!label.gameObject.activeSelf, b.name + " text is hidden");
                var rect = (RectTransform)b.transform;
                Check(rect.sizeDelta == new Vector2(44, 48) && rect.anchorMin == new Vector2(1, .5f) &&
                    rect.pivot == new Vector2(0, .5f) && rect.anchoredPosition == new Vector2(18, 0),
                    b.name + " narrow button anchored to the right");
                var arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Common/ArrowSkills.png");
                var arrow = b.targetGraphic.GetComponentsInChildren<Image>(true).Single(i => i.sprite == arrowSprite);
                Check(arrow.rectTransform.sizeDelta == arrow.sprite.rect.size * 2 &&
                    Vector3.Dot(arrow.transform.TransformDirection(Vector3.left), b.transform.up) > .99f,
                    b.name + " arrow points up at native UI scale x2");
                if (b.gameObject.activeInHierarchy && b.GetComponent<CanvasGroup>().blocksRaycasts)
                {
                    var canvas = b.GetComponentInParent<Canvas>();
                    var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera ?? Camera.main;
                    var rayPointer = new PointerEventData(EventSystem.current)
                    { position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)) };
                    var hits = new System.Collections.Generic.List<RaycastResult>();
                    EventSystem.current.RaycastAll(rayPointer, hits);
                    Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == b,
                        b.transform.parent.name + " upgrade receives mouse rays outside the parent button");
                }
            }
        }
        var visible = buttons.Where(b => b.gameObject.activeInHierarchy && b.targetGraphic.enabled).ToArray();
        var interactable = visible.Select(b => b.interactable).ToArray();
        var events = visible.Select(b => new PointerEventData(EventSystem.current)
        { pointerCurrentRaycast = new RaycastResult { gameObject = b.gameObject } }).ToArray();
        try
        {
            for (int i = 0; i < visible.Length; i++)
            {
                visible[i].GetComponent<UnifiedButtonFeedback>().OnPointerExit(events[i]);
                visible[i].GetComponent<UnifiedButtonFeedback>().OnDeselect(events[i]);
            }
            await Task.Delay(650);
            foreach (var b in visible) Check(b.targetGraphic.color.a == 0f, b.name + " idle is fully transparent");
            Capture("idle");
            for (int i = 0; i < visible.Length; i++) visible[i].GetComponent<UnifiedButtonFeedback>().OnPointerEnter(events[i]);
            await Task.Delay(650);
            foreach (var b in visible) Check(Mathf.Abs(b.targetGraphic.color.a - 150f / 255f) < .00001f, b.name + " hover alpha 150");
            for (int i = 0; i < visible.Length; i++) visible[i].GetComponent<UnifiedButtonFeedback>().OnPointerDown(events[i]);
            await Task.Delay(650);
            foreach (var b in visible) Check(Mathf.Abs(b.targetGraphic.color.a - 150f / 255f) < .00001f, b.name + " pressed alpha 150");
            for (int i = 0; i < visible.Length; i++)
            {
                visible[i].GetComponent<UnifiedButtonFeedback>().OnPointerUp(events[i]);
                visible[i].interactable = false;
            }
            await Task.Delay(650);
            foreach (var b in visible) Check(Mathf.Abs(b.targetGraphic.color.a - 150f / 255f) < .00001f, b.name + " unavailable hover alpha 150");
        }
        finally
        {
            for (int i = 0; i < visible.Length; i++)
            {
                visible[i].interactable = interactable[i];
                visible[i].GetComponent<UnifiedButtonFeedback>().OnPointerExit(events[i]);
            }
        }
        await Task.Delay(650);
        foreach (var b in visible) Check(b.targetGraphic.color.a == 0f, b.name + " pointer exit restores exact zero");
        var portal = owners.Single(w => w.name == "Portal");
        var main = portal.GetComponent<Button>();
        var upgrade = portal.GetComponentInChildren<BuildingUpgradeButton>(true).GetComponent<Button>();
        var pointer = new PointerEventData(EventSystem.current) { pointerCurrentRaycast = new RaycastResult { gameObject = upgrade.gameObject } };
        main.GetComponent<UnifiedButtonFeedback>().OnPointerEnter(pointer);
        upgrade.GetComponent<UnifiedButtonFeedback>().OnPointerEnter(pointer);
        await Task.Delay(650);
        Check(main.targetGraphic.color.a == 0 && Mathf.Abs(upgrade.targetGraphic.color.a - 150f / 255f) < .00001f,
            "hovering upgrade does not highlight its parent building");
        Capture("upgrade-hover");
        main.GetComponent<UnifiedButtonFeedback>().OnPointerExit(pointer);
        upgrade.GetComponent<UnifiedButtonFeedback>().OnPointerExit(pointer);
        var lab = owners.Single(w => w.name == "Laboratory");
        var hovered = lab.GetComponentsInChildren<Button>().First(b => b.targetGraphic.enabled);
        var hover = new PointerEventData(EventSystem.current) { pointerCurrentRaycast = new RaycastResult { gameObject = hovered.gameObject } };
        hovered.GetComponent<UnifiedButtonFeedback>().OnPointerEnter(hover);
        await Task.Delay(650);
        Capture("hover");
        hovered.GetComponent<UnifiedButtonFeedback>().OnPointerExit(hover);
        File.AppendAllText(Report, "COMPLETED\n");
    }

    static void Capture(string name)
    {
        var camera = Camera.main;
        var rt = new RenderTexture(1708, 960, 24);
        var tex = new Texture2D(1708, 960, TextureFormat.RGB24, false);
        var old = camera.targetTexture; var active = RenderTexture.active;
        try
        {
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1708, 960), 0, 0); tex.Apply();
            File.WriteAllBytes("Temp/ShelterButtons/" + name + ".png", tex.EncodeToPNG());
        }
        finally { camera.targetTexture = old; RenderTexture.active = active; Object.Destroy(rt); Object.Destroy(tex); }
    }
}
