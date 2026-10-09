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
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BuildingPriceAudit
{
    const string Folder = "Temp/BuildingPrices/";
    static readonly string[] Ids = { "blacksmith", "housing", "refugees", "archery_range", "fort", "castle", "magic_library", "laboratory" };
    static readonly int[][] Prices = { new[]{1,3,0}, new[]{10,0,0}, new[]{0,0,0}, new[]{3,0,0}, new[]{0,20,0}, new[]{0,70,0}, new[]{0,8,3}, new[]{3,2,0} };
    static T Ref<T>(Object o, string field) where T : Object => new SerializedObject(o).FindProperty(field).objectReferenceValue as T;
    static void Check(bool ok, string message)
    {
        File.AppendAllText(Folder + "report.txt", (ok ? "PASS " : "FAIL ") + message + "\n");
        if (!ok) throw new Exception(message);
    }

    public static async Task RunPlay()
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "report.txt", "Construction and upgrade price integration\n");
        string language = PlayerPrefs.GetString("foundation.language", "ru");
        try
        {
            foreach (string id in Ids) SaveSlotPrefs.SetInt("foundation.building." + id + ".built", 0);
            foreach (string id in new[] { "housing", "fort", "archery_range", "portal" })
                SaveSlotPrefs.SetInt("foundation.building." + id + ".upgradeLevel", 0);
            SaveSlotPrefs.SetInt("foundation.building.portal.built", 0);
            var load = SceneManager.LoadSceneAsync("Assets/Scenes/Base.unity");
            while (!load.isDone) await Task.Delay(30);
            await Task.Delay(600);
            LocalizationService.Instance.SetLanguage("ru");
            var root = Object.FindObjectsByType<BaseUIController>(FindObjectsSortMode.None).Single().transform.root;
            var buildings = root.GetComponentsInChildren<BaseBuildingConstruction>(true);
            var manager = GlobalResourceManager.Instance;
            var resources = new[] { "Wood", "Stone", "MagicOre" }.Select(n => AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/" + n + ".asset")).ToArray();
            void Stock(int[] amounts) { for (int i = 0; i < resources.Length; i++) manager.SetResourceAmount(resources[i], amounts[i]); }
            int[] Amounts() => resources.Select(manager.GetResourceAmount).ToArray();
            Check(BuildingUpgradeService.IsBuilt("portal"), "portal available before any construction");
            for (int index = 0; index < Ids.Length; index++)
            {
                string id = Ids[index];
                int[] cost = Prices[index];
                var b = buildings.Single(x => x.BuildingId == id);
                for (int i = 0; i < resources.Length; i++)
                    Check((b.Wood == resources[i] ? b.WoodCost : 0) + (b.Stone == resources[i] ? b.StoneCost : 0) == cost[i], id + " configured " + resources[i].name + " price");
                Stock(new int[3]);
                if (cost.Sum() > 0)
                {
                    Check(!b.CanAffordConstruction, id + " cannot build without resources");
                    b.ConfirmBuild();
                    Check(!b.IsBuilt && Amounts().All(x => x == 0), id + " rejected purchase changes nothing");
                    var shortStock = (int[])cost.Clone();
                    shortStock[Array.FindIndex(cost, x => x > 0)]--;
                    Stock(shortStock);
                    await Task.Delay(80);
                    var button = Ref<Button>(b, "buildButton");
                    var highlight = button.GetComponent<BuildingButtonHighlight>();
                    Check(!button.IsInteractable() && Ref<Text>(highlight, "label").color == Ref<ButtonVisualTheme>(highlight, "theme").unavailableLabelColor,
                        id + " insufficient stock gives disabled gray button");
                }
                Stock(cost);
                Check(b.CanAffordConstruction, id + " exact stock is enough");
                var popup = Ref<BuildingConstructionConfirmation>(b, "confirmation");
                popup.Open(b);
                await Task.Delay(150);
                int visible = 0;
                foreach (string field in new[] { "woodIcon", "stoneIcon" })
                {
                    var icon = Ref<Image>(popup, field);
                    if (!icon.enabled) continue;
                    visible++;
                    Check(icon.rectTransform.rect.size == icon.sprite.rect.size * 2, id + " UI icon native x2 size");
                    var resource = field == "woodIcon" ? b.Wood : b.Stone;
                    Check(icon.sprite == resource.resourceIcon, id + " correct resource icon");
                }
                Check(visible == cost.Count(x => x > 0), id + " no zero-cost resource icons");
                if (visible == 0) Check(Ref<Text>(popup, "woodAmount").text == "Бесплатно", "free camp label");
                if (id == "magic_library" || id == "refugees" || id == "fort")
                {
                    ScreenCapture.CaptureScreenshot(Folder + id + ".png");
                    await Task.Delay(300);
                }
                popup.Confirm();
                Check(b.IsBuilt && Amounts().All(x => x == 0), id + " spends exactly the required resources");
                Stock(new[] { 4, 5, 6 });
                b.ConfirmBuild();
                Check(Amounts().SequenceEqual(new[] { 4, 5, 6 }), id + " duplicate confirmation cannot charge again");
            }
            foreach (string id in new[] { "housing", "archery_range", "fort", "portal" })
            {
                var definition = BuildingUpgradeService.Catalog.Find(id);
                for (int level = 0; level < definition.levels.Count; level++)
                {
                    int[] cost = id == "housing" ? new[] { 10, 0, 0 } : id == "archery_range" ? new[] { 15, 0, 0 }
                        : id == "fort" ? new[] { 0, 15, 0 } : new[] { 0, 3 + level, 1 + 3 * level };
                    var shortStock = (int[])cost.Clone();
                    shortStock[Array.FindIndex(cost, x => x > 0)]--;
                    Stock(shortStock);
                    Check(!BuildingUpgradeService.TryUpgrade(id) && Amounts().SequenceEqual(shortStock), id + " level " + (level + 1) + " rejects insufficient stock without charging");
                    Stock(cost);
                    Check(BuildingUpgradeService.TryUpgrade(id) && Amounts().All(x => x == 0) && BuildingUpgradeService.Level(id) == level + 1,
                        id + " level " + (level + 1) + " exact price and level advance");
                }
                Stock(new[] { 100, 100, 100 });
                Check(!BuildingUpgradeService.TryUpgrade(id) && Amounts().SequenceEqual(new[] { 100, 100, 100 }), id + " maximum level cannot charge");
            }
            File.AppendAllText(Folder + "report.txt", "COMPLETED\n");
        }
        finally { LocalizationService.Instance?.SetLanguage(language); }
    }
}
