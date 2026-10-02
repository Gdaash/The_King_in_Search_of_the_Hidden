#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using GameFoundation.Base;
public static class BaseWorldVariantSetup
{
 static Vector2 Position(string n)=>n switch {
 "Magic Library"=>new(-3.5f,3.4f),"Laboratory"=>new(-7.1f,2.2f),"Castle"=>new(0,2.35f),"Blacksmith"=>new(-4.6f,.3f),"Portal"=>new(0,-1.65f),"Fort"=>new(5.4f,2.5f),"Archery Range"=>new(5.4f,-.6f),"Warehouse"=>new(4,-4.7f),"Housing"=>new(-6,-2.9f),"Square"=>new(-1.9f,-4.8f),"Refugees"=>new(-.45f,-7.4f),_=>Vector2.zero};
 static string[] Layers(string n)=>n switch {"Housing"=>new[]{"House 1","House 2","House 3","House 4","House 5","House 6","House 7"},"Square"=>new[]{"Market"},"Refugees"=>new[]{"Refugee Camp"},"Magic Library"=>new[]{"Magic Library","Magic Library Glow"},"Archery Range"=>new[]{"Archery Range","Archery Range Annex"},"Warehouse"=>new[]{"Warehouse","Warehouse Barrels and Crates"},_=>new[]{n}};
 public static void Run(){
 if(EditorApplication.isPlaying || GameObject.Find("Base - Location Variant")!=null)throw new System.Exception("Wrong state or already configured");
 System.IO.Directory.CreateDirectory("Temp/BaseVariantBackup");System.IO.File.Copy("Assets/Scenes/Base.unity","Temp/BaseVariantBackup/Base.unity",true);
 var ui=Object.FindFirstObjectByType<BaseUIController>();var hud=PrefabUtility.GetOutermostPrefabInstanceRoot(ui.gameObject);
 var backup=new GameObject("Base - Islands Backup (disabled)");backup.SetActive(false);var clone=Object.Instantiate(hud,backup.transform);clone.name="Islands HUD";
 var active=new GameObject("Base - Location Variant");hud.transform.SetParent(active.transform,true);PrefabUtility.UnpackPrefabInstance(hud,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
 var art=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.name=="Base Scene Artwork");art.SetParent(active.transform,true);art.gameObject.SetActive(true);PrefabUtility.RecordPrefabInstancePropertyModifications(art.gameObject);
 var panel=ui.transform.Find("Base Panel");panel.GetComponent<UnityEngine.UI.Image>().enabled=false;
 var go=new GameObject("Building Buttons - World Space",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.GraphicRaycaster));go.transform.SetParent(active.transform,false);var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=Camera.main;canvas.sortingOrder=100;var rt=(RectTransform)go.transform;rt.sizeDelta=new Vector2(2000,1600);rt.localScale=Vector3.one/80;rt.position=new Vector3(0,0,-1);
 var su=new SerializedObject(ui);su.FindProperty("worldBuildingButtons").objectReferenceValue=go.transform;su.ApplyModifiedPropertiesWithoutUndo();
 foreach(var island in panel.GetComponentsInChildren<CanvasBuildingIsland>(true).ToArray()){
 var b=island.gameObject;var si=new SerializedObject(island);var mat=si.FindProperty("unbuiltTint").objectReferenceValue;
 b.transform.SetParent(go.transform,false);((RectTransform)b.transform).anchorMin=((RectTransform)b.transform).anchorMax=new Vector2(.5f,.5f);b.transform.localScale=Vector3.one;b.transform.position=new Vector3(Position(b.name).x,Position(b.name).y,-1);
 b.transform.Find("Hex").gameObject.SetActive(false);b.transform.Find("Building").gameObject.SetActive(false);Object.DestroyImmediate(island);
 var v=b.AddComponent<WorldBuildingButton>();var sv=new SerializedObject(v);sv.FindProperty("manuallyPositioned").boolValue=true;sv.FindProperty("unbuiltMaterial").objectReferenceValue=mat;sv.FindProperty("construction").objectReferenceValue=b.GetComponent<BaseBuildingConstruction>();var a=sv.FindProperty("artworkLayers");var names=Layers(b.name);a.arraySize=names.Length;for(int i=0;i<names.Length;i++)a.GetArrayElementAtIndex(i).stringValue=names[i];sv.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(b.transform);
 }
 foreach(var ray in art.GetComponentsInChildren<WorldBuildingRaycaster>(true))ray.enabled=false;
 const string folder="Assets/Prefabs/Base/Variants";if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Prefabs/Base","Variants");
 PrefabUtility.SaveAsPrefabAssetAndConnect(backup,folder+"/Base Islands Backup.prefab",InteractionMode.AutomatedAction);
 PrefabUtility.SaveAsPrefabAssetAndConnect(active,folder+"/Base Location Variant.prefab",InteractionMode.AutomatedAction);
 EditorSceneManager.MarkSceneDirty(active.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();Debug.Log("Both base variants saved; location active, islands disabled.");
 }
}
#endif
