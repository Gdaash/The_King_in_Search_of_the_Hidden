#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using GameFoundation.Base;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using System.Linq;

public static class PortalDemoSetup
{
    public static void Apply()
    {
        const string path = "Assets/Prefabs/Base/Portal Popup.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponentInChildren<PortalPopupView>(true);
            foreach (var row in view.locations)
            {
                bool locked = PortalProgression.IsDemoLocked(row.location);
                if (locked && row.demoOverlay == null)
                {
                    var go = new GameObject("Demo unavailable", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                    go.transform.SetParent(row.transform, false);
                    var rect = (RectTransform)go.transform;
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                    var image = go.GetComponent<UnityEngine.UI.Image>();
                    image.sprite = row.surface.sprite; image.type = UnityEngine.UI.Image.Type.Sliced;
                    image.color = new Color(.06f, .045f, .075f, .88f); image.raycastTarget = false;
                    var textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
                    textObject.transform.SetParent(go.transform, false);
                    var textRect = (RectTransform)textObject.transform;
                    textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = new Vector2(10, 5); textRect.offsetMax = new Vector2(-10, -5);
                    var text = textObject.GetComponent<UnityEngine.UI.Text>();
                    text.font = row.title.font; text.fontSize = 20; text.alignment = TextAnchor.MiddleCenter;
                    text.text = "Недоступно в демо"; text.color = new Color(.84f, .76f, .65f); text.raycastTarget = false;
                    row.demoOverlay = go;
                }
                row.button.interactable = !locked;
                if (row.demoOverlay != null) row.demoOverlay.SetActive(locked);
            }
            if (view.locations.Count(r => !PortalProgression.IsDemoLocked(r.location)) != 1)
                throw new System.Exception("Demo must have exactly one destination.");
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        var entry = table.entries.FirstOrDefault(e => e.key == "base.portal.demo_unavailable");
        if (entry == null) { entry = new LocalizationTable.Entry { key = "base.portal.demo_unavailable" }; table.entries.Add(entry); }
        while (entry.values.Count < table.languages.Count) entry.values.Add("");
        entry.values[table.languages.IndexOf("ru")] = "Недоступно в демо";
        entry.values[table.languages.IndexOf("en")] = "Unavailable in the demo";
        EditorUtility.SetDirty(table); AssetDatabase.SaveAssets();
        if (BuildingUpgradeService.CanUpgrade("portal") || BuildingUpgradeService.TryUpgrade("portal"))
            throw new System.Exception("Portal upgrade must be blocked in demo.");
        Debug.Log("PORTAL_DEMO_OK: first destination only, locked overlays, upgrades blocked, RU/EN installed.");
    }
}
#endif
