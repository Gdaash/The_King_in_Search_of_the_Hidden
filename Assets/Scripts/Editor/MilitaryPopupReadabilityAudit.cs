using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameFoundation.Base;
using GameFoundation.Localization;
using GameFoundation.Saves;
using GameFoundation.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MilitaryPopupReadabilityAudit
{
    const string Folder = "Temp/MilitaryPopupReadability/";
    static void Check(bool ok, string message)
    {
        File.AppendAllText(Folder + "report.txt", (ok ? "PASS " : "FAIL ") + message + "\n");
        if (!ok) throw new Exception(message);
    }

    public static async Task RunPlay()
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "report.txt", "Military popup readability at " + Screen.width + "x" + Screen.height + "\n");
        string language = PlayerPrefs.GetString("foundation.language", "ru");
        try
        {
            foreach (string id in new[] { "fort", "archery_range" })
                SaveSlotPrefs.SetInt("foundation.building." + id + ".built", 1);
            var load = SceneManager.LoadSceneAsync("Assets/Scenes/Base.unity");
            while (!load.isDone) await Task.Delay(30);
            await Task.Delay(600);
            var root = Object.FindObjectsByType<BaseUIController>(FindObjectsSortMode.None).Single().transform.root;
            foreach (var popup in root.GetComponentsInChildren<MilitaryTrainingView>(true))
            {
                popup.gameObject.SetActive(true);
                foreach (string lang in new[] { "ru", "en" })
                {
                    LocalizationService.Instance.SetLanguage(lang);
                    await Task.Delay(350);
                    Canvas.ForceUpdateCanvases();
                    string name = popup.name.Replace(" ", "-") + "-" + lang;
                    ScreenCapture.CaptureScreenshot(Folder + name + ".png");
                    await Task.Delay(300);
                    var fit = popup.GetComponentInChildren<CanvasWindowFit>();
                    Check(Mathf.Approximately(fit.transform.localScale.x, 1), name + " original window scale restored");
                    int texts = 0;
                    foreach (var text in popup.GetComponentsInChildren<Text>())
                    {
                        if (!text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text) || text.text == "×") continue;
                        Check(text.fontSize >= 20, name + " readable font: " + text.name);
                        Check(text.preferredHeight <= text.rectTransform.rect.height + .5f,
                            name + " full text fits: " + text.text.Replace("\n", " | ") + " (" + text.preferredHeight + "/" + text.rectTransform.rect.height + ")");
                        texts++;
                    }
                    File.AppendAllText(Folder + "report.txt", "Visible labels checked=" + texts + "\n");
                    var card = popup.GetComponentInChildren<UnitDescriptionView>();
                    var scroll = card.GetComponentInChildren<ScrollRect>();
                    Check(scroll.content.rect.height <= scroll.viewport.rect.height + .5f,
                        name + " all stats fit together: " + scroll.content.rect.height + "/" + scroll.viewport.rect.height);
                    scroll.verticalNormalizedPosition = 0;
                    await Task.Delay(150);
                    Canvas.ForceUpdateCanvases();
                    var last = scroll.content.GetComponentsInChildren<UnitStatRowView>().Last();
                    var corners = new Vector3[4];
                    ((RectTransform)last.transform).GetWorldCorners(corners);
                    Check(scroll.viewport.InverseTransformPoint(corners[0]).y >= scroll.viewport.rect.yMin - .5f,
                        name + " last stat reachable without footer overlap");
                    scroll.verticalNormalizedPosition = 1;
                    var upgrade = popup.GetComponentInChildren<BuildingUpgradeButton>();
                    ((RectTransform)upgrade.transform).GetWorldCorners(corners);
                    var canvas = upgrade.GetComponentInParent<Canvas>().rootCanvas;
                    var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                    Check(corners.All(c => { var p = RectTransformUtility.WorldToScreenPoint(camera, c); return p.x >= 0 && p.x <= Screen.width && p.y >= 0 && p.y <= Screen.height; }),
                        name + " upgrade button inside screen");
                }
                popup.Close();
            }
            File.AppendAllText(Folder + "report.txt", "COMPLETED\n");
        }
        finally { LocalizationService.Instance?.SetLanguage(language); }
    }
}
