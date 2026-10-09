#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using GameFoundation.Quests;
using GameFoundation.Saves;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class QuestDialogueValidation
{
    const string Key = "QuestDialogueValidation";
    static IEnumerator test;
    static readonly System.Collections.Generic.List<string> checks = new();
    static QuestDialogueValidation() { EditorApplication.playModeStateChanged += Changed; }
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Exit Play Mode first");
        SessionState.SetString(Key+".scene",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Base.unity");
        SessionState.SetBool(Key,true); BuildingUpgradeValidation.Begin();
        SaveSlotPrefs.SetInt("foundation.quest.royal_magic_tribute.accepted",1);
        SaveSlotPrefs.SetInt("foundation.quest.royal_magic_tribute.deadline",31);
        SaveSlotPrefs.Save();
    }
    static void Changed(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){checks.Clear();test=Test();EditorApplication.update+=Tick;}
        if(state==PlayModeStateChange.EnteredEditMode){EditorApplication.update-=Tick;EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Key+".scene",""));BuildingUpgradeValidation.Restore();SessionState.SetBool(Key,false);}
    }
    static void Tick()
    {
        try{if(test.MoveNext())return;checks.Add("COMPLETE");}catch(Exception e){checks.Add("FAIL "+e);}
        Directory.CreateDirectory("Temp");File.WriteAllLines("Temp/QuestDialogueValidation.txt",checks);EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();
    }
    static void Check(bool b,string s){if(!b)throw new Exception(s);checks.Add("PASS "+s);}
    static IEnumerator Test()
    {
        yield return null; yield return null;
        var catalog=AssetDatabase.LoadAssetAtPath<QuestCatalog>("Assets/Resources/Quests/Quest Catalog.asset");
        var first=catalog.quests[0];var view=UnityEngine.Object.FindFirstObjectByType<QuestDialogueView>();
        var panel=UnityEngine.Object.FindObjectsByType<QuestPanel>(FindObjectsSortMode.None).First(p=>!RoyalTributeSetup.IsRoyal(p));
        Check(view!=null,"dialogue installed in Base");Check(QuestProgress.Current(catalog)==null,"quest hidden before acceptance");
        Check(!QuestProgress.TryComplete(first,GlobalResourceManager.Instance),"unaccepted quest cannot complete");
        Check(panel.GetComponent<CanvasGroup>().alpha==0,"HUD hidden before acceptance");
        var next=view.GetComponentInChildren<Button>();Check(next.GetComponentInChildren<TMP_Text>().text=="…","typing button shows ellipsis");
        view.gameObject.SetActive(false);
        Check(!QuestProgress.IsAccepted(first),"interrupted conversation does not accept quest");
        view.ActivateIfNeeded();
        Check(view.gameObject.activeInHierarchy,"interrupted dialogue reactivates when needed");
        next=view.GetComponentInChildren<Button>();
        Check(next.GetComponentInChildren<TMP_Text>().text=="…","interrupted dialogue restarts with ellipsis");
        var audio=GameFoundation.Audio.GameAudioController.Instance;
        Check(audio!=null,"audio controller available");
        int ticks=0;
        System.Action<GameFoundation.Audio.GameAudioCue> onCue=c=>{if(c==GameFoundation.Audio.GameAudioCue.DialogueWord)ticks++;};
        audio.CuePlayed+=onCue;
        var text=(TMP_Text)new SerializedObject(view).FindProperty("dialogue").objectReferenceValue;
        Check(view.IsTyping && text.maxVisibleCharacters<text.textInfo.characterCount,"dialogue begins with partial word reveal");
        int visible=text.maxVisibleCharacters;float previousScale=Time.timeScale;Time.timeScale=0;
        float deadline=Time.realtimeSinceStartup+.4f;while(Time.realtimeSinceStartup<deadline)yield return null;
        Time.timeScale=previousScale;
        Check(text.maxVisibleCharacters>visible && ticks>0,"words and sound continue while game time paused");
        next.onClick.Invoke();Check(!view.IsTyping && !QuestProgress.IsAccepted(first) && next.GetComponentInChildren<TMP_Text>().text=="Далее","first click finishes text without skipping line or accepting quest");
        int stoppedTicks=ticks;deadline=Time.realtimeSinceStartup+.2f;while(Time.realtimeSinceStartup<deadline)yield return null;
        Check(ticks==stoppedTicks,"word audio stops after reveal completion");audio.CuePlayed-=onCue;
        var dimmer=view.transform.Find("Dialogue Screen Dimmer");
        Check(dimmer!=null && dimmer.gameObject.activeInHierarchy,"screen dimmer active during dialogue");
        var image=dimmer.GetComponent<UnityEngine.UI.Image>();
        Check(image.raycastTarget && image.color.a>.7f,"dimmer darkens and blocks raycasts");
        var eventSystem=UnityEngine.EventSystems.EventSystem.current;
        var pointer=new UnityEngine.EventSystems.PointerEventData(eventSystem);
        var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        foreach(var p in new[]{new Vector2(1,1),new Vector2(Screen.width-1,Screen.height-1)})
        {
            pointer.position=p;hits.Clear();eventSystem.RaycastAll(pointer,hits);
            Check(hits.Count>0 && hits[0].gameObject==dimmer.gameObject,"screen corner blocked by dialogue dimmer");
        }
        var canvas=next.GetComponentInParent<Canvas>().rootCanvas;
        pointer.position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,next.transform.TransformPoint(((RectTransform)next.transform).rect.center));
        hits.Clear();eventSystem.RaycastAll(pointer,hits);
        Check(hits.Count>0 && hits[0].gameObject.GetComponentInParent<Button>()==next,"dialogue button remains clickable above dimmer");
        var hold=next.GetComponent<DialogueHoldButton>();
        next.onClick.Invoke();Check(!view.IsTyping,"ordinary click cannot advance printed line");
        hold.OnPointerDown(pointer);
        deadline=Time.realtimeSinceStartup+.5f;while(Time.realtimeSinceStartup<deadline)yield return null;
        Check(hold.Progress>.2f && hold.Progress<.5f,"hold fill advances with elapsed time");
        hold.OnPointerUp(pointer);Check(hold.Progress==0,"early release resets progress");
        hold.OnPointerDown(pointer);
        deadline=Time.realtimeSinceStartup+1.6f;while(Time.realtimeSinceStartup<deadline)yield return null;
        hold.OnPointerUp(pointer);next.onClick.Invoke();
        Check(view.IsTyping && next.GetComponentInChildren<TMP_Text>().text=="…","hold advances once and release does not skip next typing");
        next.onClick.Invoke();Check(!QuestProgress.IsAccepted(first),"last-line reveal cannot accidentally accept quest");
        Check(next.GetComponentInChildren<TMP_Text>().text=="Взять задание","printed last line offers acceptance");
        hold.OnPointerDown(pointer);deadline=Time.realtimeSinceStartup+1.6f;while(Time.realtimeSinceStartup<deadline)yield return null;
        Check(QuestProgress.IsAccepted(first),"full hold accepts and persists quest");
        Check(!dimmer.gameObject.activeInHierarchy,"screen dimmer closes with dialogue");
        Check(QuestProgress.Current(catalog)==first,"accepted quest becomes current");
        yield return null;
        var copy=GameObject.Find("Accepted Quest Presentation");Check(copy!=null,"expanded quest presentation exists");
        var rt=(RectTransform)copy.transform;Check(rt.anchoredPosition.sqrMagnitude<1,"presentation starts at screen centre");
        Check(rt.localScale.x>1.3f,"presentation starts enlarged");
        float until=Time.realtimeSinceStartup+2.5f;while(Time.realtimeSinceStartup<until)yield return null;
        Check(copy!=null && rt.anchoredPosition.sqrMagnitude<1,"presentation waits for confirmation at centre");
        var expansion=copy.GetComponent<QuestPanelExpansion>();
        expansion.OnPointerExit(pointer);
        yield return null;
        var details=copy.transform.Find("Hover Details").GetComponent<RectTransform>();
        Check(details.rect.height>100 && details.GetComponent<CanvasGroup>().alpha>.99f,"presentation details stay fully expanded after pointer exit");
        var ok=GameObject.Find("Quest Presentation OK").GetComponent<Button>();
        Check(ok.interactable && ok.GetComponentInChildren<TMP_Text>().text=="ОК","confirmation button is enabled and labelled");
        Check(((RectTransform)ok.transform).position.y<rt.position.y,"confirmation is below quest");
        pointer.position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,ok.transform.position);
        hits.Clear();eventSystem.RaycastAll(pointer,hits);
        Check(hits.Count>0 && hits[0].gameObject.GetComponentInParent<Button>()==ok,"confirmation receives pointer above modal blocker");
        ok.onClick.Invoke();
        until=Time.realtimeSinceStartup+1.2f;while(Time.realtimeSinceStartup<until)yield return null;
        Check(GameObject.Find("Accepted Quest Presentation")==null,"flight cleans up visual copy");
        Check(panel.GetComponent<CanvasGroup>().alpha==1,"quest arrives in HUD");
        Check(SaveSlotPrefs.GetInt("foundation.quest."+first.id+".accepted",0)==1,"acceptance saved in selected slot");
        view.Evaluate();Check(view.GetComponentInChildren<Button>()==null,"accepted dialogue does not reopen");
        view.gameObject.SetActive(false);view.ActivateIfNeeded();
        Check(!view.gameObject.activeSelf,"accepted quest does not reactivate disabled dialogue");
        var plainGo=new GameObject("Temporary plain conversation");
        var plain=plainGo.AddComponent<QuestDialogueDefinition>();plain.id="validation_plain_conversation";plain.speakerName="Старик Пью";plain.lines=new[]{"Хорошего пути, рыцарь."};
        var config=new SerializedObject(view);var conversations=config.FindProperty("conversations");conversations.arraySize=1;conversations.GetArrayElementAtIndex(0).objectReferenceValue=plain;config.ApplyModifiedPropertiesWithoutUndo();
        int closes=0;System.Action<GameFoundation.Audio.GameAudioCue> onClose=c=>{if(c==GameFoundation.Audio.GameAudioCue.DialogueClose)closes++;};audio.CuePlayed+=onClose;
        view.ActivateIfNeeded();next=view.GetComponentInChildren<Button>();
        deadline=Time.realtimeSinceStartup+1;while(Time.realtimeSinceStartup<deadline)yield return null;
        Check(!view.IsTyping && text.maxVisibleCharacters==int.MaxValue,"word typing finishes automatically");
        Check(next.GetComponentInChildren<TMP_Text>().text=="Закрыть","printed plain conversation offers Close");
        hold.OnPointerDown(pointer);deadline=Time.realtimeSinceStartup+1.6f;while(Time.realtimeSinceStartup<deadline)yield return null;
        Check(!dimmer.gameObject.activeInHierarchy && closes==1,"held plain conversation closes with sound and removes blocker");
        view.gameObject.SetActive(false);view.ActivateIfNeeded();Check(!view.gameObject.activeSelf,"completed plain conversation does not repeat");
        audio.CuePlayed-=onClose;UnityEngine.Object.Destroy(plainGo);
    }
}
#endif
