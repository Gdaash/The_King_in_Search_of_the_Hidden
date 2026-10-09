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
using Object = UnityEngine.Object;

public static class BlacksmithVisualAudit
{
    const string Folder="Temp/BlacksmithVisuals/";
    static T Ref<T>(Object target,string field)where T:Object=>new SerializedObject(target).FindProperty(field).objectReferenceValue as T;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);File.AppendAllText(Folder+"report.txt","PASS "+message+"\n");}
    static async Task Click(Button button)
    {
        Canvas.ForceUpdateCanvases();var canvas=button.GetComponentInParent<Canvas>();var rt=(RectTransform)button.transform;
        var ev=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rt.TransformPoint(rt.rect.center)),button=PointerEventData.InputButton.Left};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(ev,hits);
        File.AppendAllText(Folder+"report.txt",$"RAY {button.name} screen={ev.position} hits={string.Join(",",hits.Take(5).Select(h=>h.gameObject.name))}\n");
        Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"pointer reaches "+button.name);
        ev.pointerCurrentRaycast=hits[0];button.GetComponent<UnifiedButtonFeedback>().OnPointerEnter(ev);await Task.Delay(300);
        Check(button.transform.localScale.x>1.02f,"hover grows "+button.name);
        ExecuteEvents.Execute(button.gameObject,ev,ExecuteEvents.pointerClickHandler);button.GetComponent<UnifiedButtonFeedback>().OnPointerExit(ev);await Task.Delay(200);
    }
    public static async Task RunPlay()
    {
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"report.txt","Blacksmith visuals and original sprite stars\n");
        string language=PlayerPrefs.GetString("foundation.language","ru");
        try
        {
            SaveSlotPrefs.SetInt("foundation.building.blacksmith.built",1);
            var load=SceneManager.LoadSceneAsync("Assets/Scenes/Base.unity");while(!load.isDone)await Task.Delay(30);await Task.Delay(700);
            var controller=Object.FindObjectsByType<BaseUIController>(FindObjectsSortMode.None).Single();controller.OpenBlacksmith();
            await Task.Delay(300);ScreenCapture.CaptureScreenshot(Folder+"craft-before.png");await Task.Delay(350);
            var view=controller.transform.root.GetComponentsInChildren<BlacksmithProductionView>(true).Single();
            var cart=view.GetComponent<WarehouseCartPurchaseView>();var manager=GlobalResourceManager.Instance;
            var data=new SerializedObject(view);
            foreach(var field in new[]{"swordRecipe","bowRecipe"})
            {
                var recipe=data.FindProperty(field);var input=(ResourceType)recipe.FindPropertyRelative("input").objectReferenceValue;
                var output=(ResourceType)recipe.FindPropertyRelative("output").objectReferenceValue;
                var button=(Button)recipe.FindPropertyRelative("produceButton").objectReferenceValue;
                manager.AddResource(input,100);int before=manager.GetResourceAmount(input),stock=manager.GetResourceAmount(output);
                await Click(button);
                Check(manager.GetResourceAmount(input)==before-recipe.FindPropertyRelative("inputAmount").intValue&&manager.GetResourceAmount(output)==stock+recipe.FindPropertyRelative("outputAmount").intValue,field+" spends and produces exact amounts");
            }
            var wood=Ref<ResourceType>(cart,"wood");var cartType=Ref<ResourceType>(cart,"cart");manager.AddResource(wood,100);
            int oldWood=manager.GetResourceAmount(wood),oldCart=manager.GetResourceAmount(cartType);
            await Click(Ref<Button>(cart,"buyButton"));Check(manager.GetResourceAmount(wood)==oldWood-cart.WoodCost&&manager.GetResourceAmount(cartType)==oldCart+1,"cart spends and produces exact amounts");
            Check(Ref<Text>(cart,"cartAmountText").text==(oldCart+1).ToString(),"cart stock label refreshes");
            foreach(var languageCode in new[]{"ru","en"})
            {
                LocalizationService.Instance.SetLanguage(languageCode);await Task.Delay(250);
                foreach(var text in view.GetComponentsInChildren<Button>().Where(b=>b.name!="Close").Select(b=>b.GetComponentInChildren<Text>()))
                    Check(text.preferredWidth<=text.rectTransform.rect.width,"button label fits "+languageCode+": "+text.text);
                foreach(var image in view.GetComponentsInChildren<Image>().Where(i=>i.name.EndsWith("Icon")&&!i.name.Contains("Close")))
                    Check(image.rectTransform.sizeDelta==image.sprite.rect.size*2,"native icon scale "+image.name);
                ScreenCapture.CaptureScreenshot(Folder+"craft-"+languageCode+".png");await Task.Delay(350);
            }
            LocalizationService.Instance.SetLanguage("ru");
            manager.TrySpendResource(wood,manager.GetResourceAmount(wood));await Task.Delay(200);
            Check(!Ref<Button>(cart,"buyButton").interactable,"unaffordable cart button disables");
            ScreenCapture.CaptureScreenshot(Folder+"craft-disabled.png");await Task.Delay(350);
            manager.AddResource(wood,100);controller.CloseBlacksmith();await Task.Delay(1200);Canvas.ForceUpdateCanvases();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Experience Star.asset");
            var stars=controller.transform.root.GetComponentsInChildren<RisingLabelStars>().Where(s=>s.Emit).ToArray();Check(stars.Length>0,"availability particles active");
            foreach(var star in stars)
            {
                Check(Ref<Sprite>(star,"starSprite")==sprite&&star.mainTexture==sprite.texture,"same soldier-level star sprite and texture");
                Check(!ShaderUtil.ShaderHasError(star.material.shader),"star tint shader compiles");
                var mesh=star.canvasRenderer.GetMesh();Check(mesh&&mesh.vertexCount>0&&mesh.vertexCount%4==0,"textured star quads rendered");
                var vertices=mesh.vertices;Check(Mathf.Abs(vertices[2].x-vertices[0].x-sprite.rect.width*2)<.01f,"star rendered at pixel size x2");
            }
            ScreenCapture.CaptureScreenshot(Folder+"stars.png");await Task.Delay(350);File.AppendAllText(Folder+"report.txt","COMPLETED\n");
        }
        finally{LocalizationService.Instance?.SetLanguage(language);}
    }
}
