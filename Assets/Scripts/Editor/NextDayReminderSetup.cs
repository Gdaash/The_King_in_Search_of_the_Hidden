#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class NextDayReminderSetup
{
    static void Configure(GameObject root)
    {
        foreach(var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            if(b.name!="Next Day"||b.transform.parent.name!="Day Panel")continue;
            var panel=b.transform.parent;var existing=panel.Find("Next Day Reminder");GameObject hint;
            if(existing)hint=existing.gameObject;
            else
            {
                hint=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Tooltip.prefab"),panel,false);hint.name="Next Day Reminder";
                var manager=hint.GetComponent<TooltipManager>();if(manager)Object.DestroyImmediate(manager);
            }
            var text=hint.GetComponentInChildren<UnityEngine.UI.Text>(true);text.fontSize=Mathf.Max(16,Mathf.RoundToInt(text.fontSize*text.transform.localScale.x));text.transform.localScale=Vector3.one;text.resizeTextForBestFit=false;text.text="Начни следующий день";
            text.color=AssetDatabase.LoadAssetAtPath<GameFoundation.UI.ButtonVisualTheme>("Assets/Resources/UI/ButtonVisualTheme.asset").positiveLabelColor;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;text.alignment=TextAnchor.MiddleCenter;
            var tr=text.rectTransform;tr.anchorMin=tr.anchorMax=tr.pivot=new Vector2(.5f,.5f);tr.anchoredPosition=Vector2.zero;tr.sizeDelta=new Vector2(text.preferredWidth,text.preferredHeight);
            var rect=(RectTransform)hint.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,.5f);rect.pivot=new Vector2(1,.5f);rect.anchoredPosition=new Vector2(-12,4);rect.sizeDelta=tr.sizeDelta+new Vector2(32,24);rect.localScale=Vector3.one;
            foreach(var graphic in hint.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))graphic.raycastTarget=false;
            var group=hint.GetComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;group.interactable=false;
            var reminder=b.GetComponent<NextDayReminder>()??b.gameObject.AddComponent<NextDayReminder>();var so=new SerializedObject(reminder);so.FindProperty("hint").objectReferenceValue=group;so.ApplyModifiedPropertiesWithoutUndo();
            var starTransform=b.transform.Find("Rising Stars");
            if(!starTransform)starTransform=hint.transform.Find("Rising Stars");
            if(!starTransform)starTransform=new GameObject("Rising Stars",typeof(RectTransform),typeof(CanvasRenderer),typeof(GameFoundation.UI.RisingLabelStars)).transform;
            starTransform.SetParent(b.transform,false);
            var starRect=(RectTransform)starTransform;starRect.anchorMin=Vector2.zero;starRect.anchorMax=Vector2.one;starRect.offsetMin=Vector2.zero;starRect.offsetMax=Vector2.zero;
            var stars=starTransform.GetComponent<GameFoundation.UI.RisingLabelStars>();stars.raycastTarget=false;stars.color=text.color;
            var starSettings=new SerializedObject(stars);starSettings.FindProperty("label").objectReferenceValue=b.transform;starSettings.FindProperty("starSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Experience Star.asset");starSettings.ApplyModifiedPropertiesWithoutUndo();
            so.FindProperty("stars").objectReferenceValue=stars;so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    public static void Apply()
    {
        foreach(var p in new[]{"Assets/Prefabs/Base/Base Panel.prefab","Assets/Prefabs/Base/Variants/Base Islands Backup.prefab","Assets/Prefabs/Base/Variants/Base Location Variant.prefab"}){var g=PrefabUtility.LoadPrefabContents(p);try{Configure(g);PrefabUtility.SaveAsPrefabAsset(g,p);}finally{PrefabUtility.UnloadPrefabContents(g);}}
        foreach(var g in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())Configure(g);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
    }
}
#endif
