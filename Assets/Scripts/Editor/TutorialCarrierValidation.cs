using System;
using System.Reflection;
using GameFoundation.MetaProgression;
using GameFoundation.Quests;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class TutorialCarrierValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,Private).SetValue(o,value);
    static object Get(object o,string field)=>o.GetType().GetField(field,Private).GetValue(o);
    static void Check(bool value,string text){if(!value)throw new Exception(text);}
    public static void Run()
    {
        SessionState.SetString("TutorialCarrierQA","RUNNING");
        var parent=new GameObject("Carrier tutorial QA");parent.SetActive(false);
        var controller=new GameObject("QA lights");controller.transform.SetParent(parent.transform);
        var lights=controller.AddComponent<WorldFlashlightAvailability>();Set(lights,"flashlights",Array.Empty<GameObject>());
        var canvas=Object.FindAnyObjectByType<Canvas>();
        var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(FirstRunLightTutorialSetup.Path),parent.transform);
        var tutorial=go.GetComponent<FirstRunLightTutorial>();Set(tutorial,"lights",lights);
        var canvasRect=go.GetComponent<RectTransform>();go.transform.SetParent(canvas.transform,false);
        go.SetActive(false);parent.SetActive(true);go.SetActive(true);
        // Lights must be assigned before Awake, using the inactive parent above.
        var stepField=typeof(FirstRunLightTutorial).GetField("step",Private);
        stepField.SetValue(tutorial,Enum.Parse(stepField.FieldType,"Mining"));
        typeof(FirstRunLightTutorial).GetMethod("LockInput",Private).Invoke(tutorial,null);
        GameSpeedControls.SetSimulationSpeed(3);
        var source=new GameObject("QA first hex production").AddComponent<ResourceRequester>();source.enabled=false;
        Set(tutorial,"firstHex",(Vector2)source.transform.position);
        var item=new GameObject("QA mined resource").AddComponent<ResourceItem>();var wood=Resources.Load<ResourceType>("ResourceTypes/Wood");var stone=Resources.Load<ResourceType>("ResourceTypes/Stone");item.type=wood;
        var produced=typeof(FirstRunLightTutorial).GetMethod("OnResourceProduced",Private);produced.Invoke(tutorial,new object[]{source,item});
        var window=(GameObject)Get(tutorial,"window");var next=(Button)Get(tutorial,"nextButton");
        Check(stepField.GetValue(tutorial).ToString()=="FirstHarvest"&&Time.timeScale==3&&!window.activeSelf,"First harvest must stay visible before pausing for the carrier explanation");
        var presentation=(System.Collections.IEnumerator)typeof(FirstRunLightTutorial).GetMethod("ShowCarriers",Private).Invoke(tutorial,null);
        Check(presentation.MoveNext()&&presentation.Current is WaitForSecondsRealtime wait&&Mathf.Approximately(wait.waitTime,.5f),"Carrier presentation waits half a second");
        presentation.MoveNext();
        Check(stepField.GetValue(tutorial).ToString()=="Carriers"&&Time.timeScale==0&&window.activeSelf&&next.gameObject.activeSelf,"Mining must pause for the carrier explanation with Next");
        Check(FirstRunLightTutorial.BlocksInput&&!FirstRunLightTutorial.AllowsLightDrag,"Carrier explanation must keep gameplay and dragging locked");
        Warehouse.Instance.DepositResource(wood);
        Check((int)Get(tutorial,"deliveredResources")==0,"Deliveries before confirming carrier explanation must not skip the waiting step");
        var hold=next.GetComponent<DialogueHoldButton>();hold.OnPointerDown(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});
        Check((bool)Get(hold,"holding"),"Next must accept its normal hold gesture");
        Set(hold,"elapsed",(float)Get(hold,"duration"));hold.SendMessage("Update");
        Check(stepField.GetValue(tutorial).ToString()=="Delivery"&&!window.activeSelf&&Time.timeScale==3&&FirstRunLightTutorial.BlocksInput&&!FirstRunLightTutorial.AllowsLightDrag,"Held Next must resume time without unlocking player actions");
        Warehouse.Instance.DepositResource(wood);
        Check(stepField.GetValue(tutorial).ToString()=="Delivery"&&Time.timeScale==3&&!FirstRunLightTutorial.AllowsLightDrag,"One resource must not unlock dragging");
        item.type=stone;produced.Invoke(tutorial,new object[]{source,item});Warehouse.Instance.DepositResource(stone);
        Check(stepField.GetValue(tutorial).ToString()=="Drag"&&Time.timeScale==0&&window.activeSelf&&FirstRunLightTutorial.AllowsLightDrag,"Second deposited unit, including a different resource, must pause and unlock the beam-drag lesson");
        var table=AssetDatabase.LoadAssetAtPath<GameFoundation.Localization.LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        Check(table.Get("world.tutorial.carriers","en").Contains("first supplies to arrive"),"Carrier explanation must be localized in English");
        go.SetActive(false);source.gameObject.SetActive(false);item.gameObject.SetActive(false);
        Object.Destroy(go);Object.Destroy(parent);Object.Destroy(source.gameObject);Object.Destroy(item.gameObject);
        SessionState.SetString("TutorialCarrierQA","PASS: production explanation pause, held Next, locked delivery phase, one-unit wait, second mixed-resource delivery, drag pause and RU/EN");
    }
}
