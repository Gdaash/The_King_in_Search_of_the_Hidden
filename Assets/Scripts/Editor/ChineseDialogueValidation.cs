using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameFoundation.Localization;
using GameFoundation.Quests;

public static class ChineseDialogueValidation
{
    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Validation requires Edit Mode");
        var singleton = typeof(LocalizationService).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        var language = typeof(LocalizationService).GetField("<Language>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        var previous = singleton.GetValue(null);
        var service = new GameObject("QA language").AddComponent<LocalizationService>();
        singleton.SetValue(null, service);
        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        service.SetTable(table);
        var camera = new GameObject("QA camera").AddComponent<Camera>();
        var canvas = new GameObject("QA canvas", typeof(Canvas)).GetComponent<Canvas>();
        var rt = new RenderTexture(1100,650,24);
        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            camera.targetTexture = rt; camera.cullingMask = 1 << 30; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.09f,.07f,.11f);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Dialogue/Quest Dialogue.prefab");
            var view = prefab.GetComponent<QuestDialogueView>();
            var button = (Button)new SerializedObject(view).FindProperty("nextButton").objectReferenceValue;
            var clone = UnityEngine.Object.Instantiate(button.gameObject,canvas.transform,false);
            clone.SetActive(true); var rect = (RectTransform)clone.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
            rect.anchoredPosition = new Vector2(0,-160);
            var hold = clone.GetComponent<DialogueHoldButton>(); hold.Configure(()=>true,()=>{});
            var pointer = new UnityEngine.EventSystems.PointerEventData(null);
            foreach(string code in table.languages)
            {
                language.SetValue(service,code); hold.OnPointerEnter(pointer);
                var hint = clone.transform.Find("Hold LMB Tooltip/Hint").GetComponent<TMP_Text>();
                if(hint.text != table.Get("dialogue.hold",code)) throw new Exception("Hint not translated: "+code);
                if(hint.GetPreferredValues(hint.text).x > hint.rectTransform.rect.width+1) throw new Exception("Hint clipped: "+code);
            }
            language.SetValue(service,"zh-Hans"); hold.OnPointerEnter(pointer);
            var bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Chinese/Noto Sans CJK Bold UI.asset");
            var thin = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Pixel UI/Chinese UI.asset");
            string sample = "欢迎来到传送门之地！\n我是 Stike Pew。我会帮助你重建这片土地。\n居民只能在照亮的区域工作，\n搬运工会把资源送回传送门。";
            if(!bold.HasCharacters(sample,out uint[] missing,true,true)) throw new Exception("Missing bold glyphs: "+string.Join(",",missing));
            void Label(string text,TMP_FontAsset font,Vector2 pos,Vector2 size,int fs)
            {
                var item = new GameObject("QA text",typeof(RectTransform),typeof(TextMeshProUGUI));
                item.transform.SetParent(canvas.transform,false);var label=item.GetComponent<TextMeshProUGUI>();
                label.font=font;label.text=text;label.fontSize=fs;label.color=new Color(.94f,.91f,.82f);
                label.rectTransform.sizeDelta=size;label.rectTransform.anchoredPosition=pos;
            }
            Label("Before",thin,new Vector2(-270,260),new Vector2(480,40),26);
            Label("After",bold,new Vector2(270,260),new Vector2(480,40),26);
            Label(sample,thin,new Vector2(-270,110),new Vector2(480,220),28);
            Label(sample,bold,new Vector2(270,110),new Vector2(480,220),28);
            var legacyObject = new GameObject("Legacy sample",typeof(RectTransform),typeof(Text));
            legacyObject.transform.SetParent(canvas.transform,false);var legacy=legacyObject.GetComponent<Text>();
            legacy.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pixellari Cyrillic/Pixellari-Cyrillic.ttf");
            legacy.fontSize=28;legacy.text=sample;legacy.color=Color.white;
            legacy.rectTransform.sizeDelta=new Vector2(960,140);legacy.rectTransform.anchoredPosition=new Vector2(0,-90);
            foreach(var transform in canvas.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=30;
            Canvas.ForceUpdateCanvases(); camera.Render();
            var old=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(1100,650);image.ReadPixels(new Rect(0,0,1100,650),0,0);image.Apply();
            System.IO.Directory.CreateDirectory("Art/ChineseDialogueQA");
            System.IO.File.WriteAllBytes("Art/ChineseDialogueQA/comparison.png",image.EncodeToPNG());
            RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);
            System.IO.File.WriteAllText(".ai/localization-eight-languages/current.json",JsonUtility.ToJson(table));
            string Escape(string value)=>"\""+value.Replace("\"","\"\"")+"\"";
            System.IO.File.WriteAllText("Docs/LocalizationEightLanguages.csv","key,"+string.Join(",",table.languages)+"\n"+
                string.Join("\n",table.entries.Select(e=>Escape(e.key)+","+string.Join(",",e.values.Select(Escape)))));
            return "PASS: eight translated hold hints fit; Chinese bold glyphs render in TMP and legacy UI; comparison exported";
        }
        finally
        {
            singleton.SetValue(null,previous);camera.targetTexture=null;
            UnityEngine.Object.DestroyImmediate(canvas.gameObject);UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(service.gameObject);UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
