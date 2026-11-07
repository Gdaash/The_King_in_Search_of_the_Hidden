using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using GameFoundation.Localization;
namespace GameFoundation.MetaProgression
{
 public sealed class WorldCasualtyPopup:MonoBehaviour
 {
  public Font font;
  public Sprite panelSprite,buttonSprite;
  private RectTransform window;
  private Button okay;
  private readonly List<CanvasGroup> icons=new();
  private Action completed;
  public static float RevealInterval(int count)=>count>0?Mathf.Min(.5f,2f/count):0;
  public static int DisplayedIcons(int count)=>count<10?Mathf.Max(0,count):1;
  private RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 position)
  {var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=size;rect.anchoredPosition=position;return rect;}
  private Text Text(Transform parent,string text,Vector2 size,Vector2 position,int sizeFont=26)
  {var rect=Rect(parent,"Label",size,position);var label=rect.gameObject.AddComponent<Text>();label.font=font;label.fontSize=sizeFont;label.text=text;label.color=new Color(.94f,.91f,.82f);label.alignment=TextAnchor.MiddleCenter;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.raycastTarget=false;GameFoundation.UI.QuestReadingText.Ensure(label);return label;}
  public void Show(IReadOnlyDictionary<ResourceType,int> dead,IReadOnlyDictionary<ResourceType,int> missing,Action onComplete)
  {
   StopAllCoroutines();if(window!=null){window.gameObject.SetActive(false);Destroy(window.gameObject);}icons.Clear();completed=onComplete;
   gameObject.SetActive(true);transform.SetAsLastSibling();
   var backdrop=GetComponent<Image>();backdrop.color=new Color(0,0,0,.75f);backdrop.raycastTarget=true;
   window=Rect(transform,"Losses Window",new Vector2(780,200),Vector2.zero);window.anchorMin=window.anchorMax=window.pivot=new Vector2(.5f,.5f);var background=window.gameObject.AddComponent<Image>();background.sprite=panelSprite;background.type=Image.Type.Sliced;
   float top=28;Block(dead,"world.losses.dead","Погибли в бою",ref top);Block(missing,"world.losses.missing","Не вернулись в портал",ref top);
   float height=top+80;window.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
   var buttonRect=Rect(window,"OK",new Vector2(280,48),new Vector2(0,-top-12));var image=buttonRect.gameObject.AddComponent<Image>();image.sprite=Resources.Load<GameFoundation.UI.ButtonVisualTheme>("UI/ButtonVisualTheme")?.textButtonSprite ?? buttonSprite;image.type=Image.Type.Sliced;okay=buttonRect.gameObject.AddComponent<Button>();okay.targetGraphic=image;okay.transition=Selectable.Transition.None;image.color=Color.white;buttonRect.gameObject.AddComponent<GameFoundation.UI.UnifiedButtonFeedback>();
   var label=Text(buttonRect,Tr("world.losses.ok","OK"),new Vector2(248,40),new Vector2(0,-4),24);buttonRect.gameObject.AddComponent<GameFoundation.UI.ActionButtonLabelColor>().Configure(label);okay.onClick.AddListener(Acknowledge);okay.gameObject.SetActive(false);
   StartCoroutine(Reveal());
  }
  private static string Tr(string key,string fallback){var value=LocalizationService.Instance?.Get(key);return string.IsNullOrEmpty(value)||value==key?fallback:value;}
  private void Block(IReadOnlyDictionary<ResourceType,int> losses,string key,string fallback,ref float top)
  {
   var values=losses.Where(pair=>pair.Key!=null&&pair.Value>0).OrderBy(pair=>pair.Key.isHumanResource?0:1).ThenBy(pair=>pair.Key.Id,StringComparer.Ordinal).ToArray();if(values.Length==0)return;
   var heading=Text(window,Tr(key,fallback),new Vector2(708,42),new Vector2(0,-top),30);heading.color=key.EndsWith("dead")?new Color(1,.49f,.49f):new Color(1,.84f,.45f);top+=54;
   float x=-342,rowHeight=0;
   foreach(var pair in values)
   {
    bool grouped=pair.Value>=10;int number=DisplayedIcons(pair.Value);
    for(int i=0;i<number;i++)
    {
     Vector2 size=pair.Key.resourceIcon!=null?pair.Key.resourceIcon.rect.size*2:new Vector2(48,48);float width=grouped?Mathf.Max(148,size.x+92):Mathf.Max(60,size.x+12);float height=Mathf.Max(56,size.y+8);
     if(x+width>354){top+=rowHeight+12;x=-342;rowHeight=0;}
     var cell=Rect(window,"Lost "+pair.Key.Id,new Vector2(width,height),new Vector2(x+width*.5f,-top));var group=cell.gameObject.AddComponent<CanvasGroup>();group.alpha=0;icons.Add(group);
     var iconRect=Rect(cell,"Icon",size,new Vector2(grouped?-width*.5f+size.x*.5f+4:0,-(height-size.y)*.5f));var icon=iconRect.gameObject.AddComponent<Image>();ResourceIconSizing.Apply(icon,pair.Key.resourceIcon);icon.raycastTarget=false;
     if(grouped){var count=Text(cell,"×"+pair.Value,new Vector2(width-size.x-12,height),new Vector2(size.x*.5f+2,0),26);count.resizeTextForBestFit=true;count.resizeTextMinSize=16;count.resizeTextMaxSize=26;}
     x+=width+6;rowHeight=Mathf.Max(rowHeight,height);
    }
   }
   top+=rowHeight+24;
  }
  private IEnumerator Reveal()
  {
   float interval=RevealInterval(icons.Count);
   foreach(var icon in icons){icon.alpha=1;yield return new WaitForSecondsRealtime(interval);}
   okay.gameObject.SetActive(true);
  }
  private void Acknowledge(){var callback=completed;completed=null;gameObject.SetActive(false);callback?.Invoke();}
 }
}
