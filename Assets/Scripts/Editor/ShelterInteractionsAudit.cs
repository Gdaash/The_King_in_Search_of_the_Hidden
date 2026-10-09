using System;
using System.Collections.Generic;
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
using Object=UnityEngine.Object;

public static class ShelterInteractionsAudit
{
    const string Folder="Temp/ShelterInteractions/";
    static void Check(bool condition,string text){if(!condition)throw new Exception(text);File.AppendAllText(Folder+"report.txt","PASS "+text+"\n");}
    static T Ref<T>(Object o,string field) where T:Object => new SerializedObject(o).FindProperty(field).objectReferenceValue as T;
    static string Id(BuildingUpgradeButton u)=>new SerializedObject(u).FindProperty("buildingId").stringValue;
    static async Task Click(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var canvas=button.GetComponentInParent<Canvas>();var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera??Camera.main;
        var rt=(RectTransform)button.transform;
        var ev=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,rt.TransformPoint(rt.rect.center)),button=PointerEventData.InputButton.Left};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(ev,hits);
        Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,button.name+" receives actual pointer ray");
        ev.pointerCurrentRaycast=hits[0];
        ExecuteEvents.Execute(button.gameObject,ev,ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(button.gameObject,ev,ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(button.gameObject,ev,ExecuteEvents.pointerClickHandler);
        await Task.Delay(180);
    }
    public static async Task RunPlay()
    {
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"report.txt","Shelter interaction integration audit\n");
        var language=PlayerPrefs.GetString("foundation.language","ru");
        try
        {
            foreach(var id in new[]{"housing","fort","archery_range","blacksmith","laboratory","refugees","castle","magic_library"})
            {SaveSlotPrefs.SetInt("foundation.building."+id+".built",0);SaveSlotPrefs.SetInt("foundation.building."+id+".upgradeLevel",0);}
            SaveSlotPrefs.SetInt("foundation.building.portal.upgradeLevel",0);
            var load=SceneManager.LoadSceneAsync("Assets/Scenes/Base.unity");while(!load.isDone)await Task.Delay(30);await Task.Delay(800);
            LocalizationService.Instance.SetLanguage("ru");
            var controller=Object.FindObjectsByType<BaseUIController>(FindObjectsSortMode.None).Single();var root=controller.transform.root;
            var owners=root.GetComponentsInChildren<WorldBuildingButton>(true);
            Check(owners.Length==10,"ten building entities, no square");
            Check(!root.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Action Available Indicator"),"no exclamation badges on active shelter");
            Check(owners.All(o=>o.GetComponentInChildren<BuildingUpgradeButton>(true)==null),"upgrades removed from map buttons");
            var resources=GlobalResourceManager.Instance;
            foreach(var c in owners.Select(o=>o.GetComponent<BaseBuildingConstruction>()).Where(c=>c!=null))
            {resources.AddResource(c.Wood,100);resources.AddResource(c.Stone,100);}
            var portalCost=BuildingUpgradeService.Next("portal");resources.AddResource(portalCost.resourceA,100);
            await Task.Delay(400);
            var housing=owners.Single(o=>o.name=="Housing");
            var hs=new SerializedObject(housing).FindProperty("artworkSprites");Check(hs.arraySize==8,"all seven houses and market/well artwork belong to housing");
            var glow=root.GetComponentInChildren<BuildingGlowPulse>(true);var sr=Ref<SpriteRenderer>(glow,"glow");Check(!sr.enabled,"unbuilt magic library has no glow");
            foreach(var owner in owners)
            {
                var button=owner.PointerTarget.GetComponent<Button>();var layout=button.GetComponent<BuildingButtonLayout>();layout.Refresh();
                var label=Ref<Text>(layout,"label");Check(((RectTransform)button.transform).rect.width>=label.preferredWidth+24,owner.name+" text fits button");
            }
            var laboratory=owners.Single(o=>o.name=="Laboratory").PointerTarget.GetComponent<BuildingButtonLayout>();
            float russian=((RectTransform)laboratory.transform).rect.width;string ruLabel=Ref<Text>(laboratory,"label").text;LocalizationService.Instance.SetLanguage("en");await Task.Delay(500);
            File.AppendAllText(Folder+"report.txt",$"LANG ru={ruLabel} width={russian}; en={Ref<Text>(laboratory,"label").text} width={((RectTransform)laboratory.transform).rect.width}\n");
            var lc=owners.Single(o=>o.name=="Laboratory").GetComponent<BaseBuildingConstruction>();
            File.AppendAllText(Folder+"report.txt",$"LOCALE {LocalizationService.Instance.Language} translated={LocalizationService.Instance.Get("base.base_panel.laboratory.label")} subscribed={typeof(BaseBuildingConstruction).GetField("languageSubscribed",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(lc)} key={new SerializedObject(lc).FindProperty("nameKey").stringValue}\n");
            Check(((RectTransform)laboratory.transform).rect.width!=russian,"localization resizes building button");LocalizationService.Instance.SetLanguage("ru");await Task.Delay(200);
            var blacksmith=owners.Single(o=>o.name=="Blacksmith");var construction=blacksmith.GetComponent<BaseBuildingConstruction>();
            var build=Ref<Button>(construction,"buildButton");var popup=Ref<BuildingConstructionConfirmation>(construction,"confirmation");
            int woodBefore=resources.GetResourceAmount(construction.Wood),stoneBefore=resources.GetResourceAmount(construction.Stone);
            await Click(build);Check(popup.isActiveAndEnabled&&!construction.IsBuilt,"build opens confirmation without spending");
            Check(resources.GetResourceAmount(construction.Wood)==woodBefore,"opening confirmation costs nothing");
            await Capture("construction-confirmation");await Click(Ref<Button>(popup,"cancel"));Check(!construction.IsBuilt,"No cancels construction");
            await Click(build);await Click(Ref<Button>(popup,"confirm"));popup.Confirm();
            Check(construction.IsBuilt&&resources.GetResourceAmount(construction.Wood)==woodBefore-construction.WoodCost&&resources.GetResourceAmount(construction.Stone)==stoneBefore-construction.StoneCost,"Yes builds exactly once and spends exact prices");
            await Click(blacksmith.GetComponent<Button>());var workshop=Ref<WarehouseCartPurchaseView>(controller,"cartWorkshop");
            Check(workshop.isActiveAndEnabled&&workshop.name=="Blacksmith Popup","cart workshop is in blacksmith");
            var cart=Ref<ResourceType>(workshop,"cart");var wood=Ref<ResourceType>(workshop,"wood");
            int carts=resources.GetResourceAmount(cart),timber=resources.GetResourceAmount(wood);
            await Click(Ref<Button>(workshop,"buyButton"));Check(resources.GetResourceAmount(cart)==carts+1&&resources.GetResourceAmount(wood)==timber-workshop.WoodCost,"cart crafted with exact resource cost");
            await Capture("blacksmith");controller.CloseBlacksmith();
            foreach(var id in new[]{"housing","fort","archery_range","magic_library"})
            {
                var c=owners.Select(o=>o.GetComponent<BaseBuildingConstruction>()).First(c=>c!=null&&c.BuildingId==id);
                await Click(Ref<Button>(c,"buildButton"));await Click(Ref<Button>(popup,"confirm"));
            }
            Check(sr.enabled,"built magic library glow becomes visible");float min=1,max=0;
            for(int i=0;i<35;i++){min=Mathf.Min(min,sr.color.a);max=Mathf.Max(max,sr.color.a);await Task.Delay(100);}
            Check(min>=.49f&&max<=1.01f&&max-min>.4f,"glow smoothly spans alpha 0.5 to 1");
            var upgrades=root.GetComponentsInChildren<BuildingUpgradeButton>(true);Check(upgrades.Length==4,"four popup upgrade controls");
            foreach(var u in upgrades)
            {
                string id=Id(u);var p=u.GetComponentInParent<MilitaryTrainingView>(true)?.gameObject;
                if(id=="portal")controller.OpenMap();else if(id=="housing")controller.OpenHousing();else p.SetActive(true);
                await Task.Delay(200);var label=Ref<Text>(u,"label");Check(label.gameObject.activeInHierarchy&&label.text.Contains("0/"),id+" upgrade label displays numeric level");
                var cost=BuildingUpgradeService.Next(id);if(cost.resourceA)resources.AddResource(cost.resourceA,100);if(cost.resourceB)resources.AddResource(cost.resourceB,100);
                await Click(u.GetComponent<Button>());Check(BuildingUpgradeService.Level(id)==1,id+" popup upgrade works");
                await Capture(id+"-popup");if(id=="portal")controller.CloseMap();else if(id=="housing")controller.CloseHousing();else p.SetActive(false);
            }
            var available=blacksmith.GetComponent<BuildingButtonHighlight>();Check(available.Available,"blacksmith availability highlights label");
            var stars=root.GetComponentsInChildren<RisingLabelStars>().Where(s=>s.Emit).ToArray();Check(stars.Length>0,"green star particles emit for available actions");
            await Task.Delay(500);Canvas.ForceUpdateCanvases();
            foreach(var star in stars)
            {
                var mesh=star.canvasRenderer.GetMesh();Check(mesh!=null&&mesh.vertexCount>0,"visible star mesh on "+star.transform.parent.parent.name);
            }
            var light=root.GetComponentsInChildren<Light2DFlicker>().Single(l=>l.name=="Portal Light");var light2d=light.GetComponent<UnityEngine.Rendering.Universal.Light2D>();float radius=light2d.pointLightOuterRadius;await Task.Delay(500);Check(Mathf.Abs(radius-light2d.pointLightOuterRadius)>.0001f,"existing flicker script pulses portal light");
            var embers=root.GetComponentInChildren<PortalEmbers>();Check(embers.GetComponentsInChildren<SpriteRenderer>().Count(s=>s.enabled)>2,"portal emits glow fragments");
            await Capture("shelter-effects");
            File.AppendAllText(Folder+"report.txt","COMPLETED\n");
        }
        finally {LocalizationService.Instance?.SetLanguage(language);}
    }
    static async Task Capture(string name)
    {
        ScreenCapture.CaptureScreenshot(Folder+name+".png");await Task.Delay(350);
    }
}
