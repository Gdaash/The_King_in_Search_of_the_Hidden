using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Individual recalled residents, with a count for large populations.</summary>
public sealed class ReturnedPeopleView : MonoBehaviour
{
    [SerializeField] private TMP_Text countLabel;
    [SerializeField] private Color awayColor=new Color(.78f,.36f,.39f,1);
    readonly List<UnityEngine.UI.Image> icons=new();
    public void Present(int returned,int total,ResourceType human)
    {
        if(!human || !human.resourceIcon)return;
        total=Mathf.Max(0,total);returned=Mathf.Clamp(returned,0,total);
        bool counted=total>10;int needed=counted?1:total;
        Vector2 size=human.resourceIcon.rect.size*2;
        while(icons.Count<needed)
        {
            var go=new GameObject("Returned Resident",typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(transform,false);
            var image=go.GetComponent<UnityEngine.UI.Image>();image.raycastTarget=false;icons.Add(image);
        }
        var panel=(RectTransform)transform;
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,counted?180:Mathf.Max(80,needed*size.x+32));
        for(int i=0;i<icons.Count;i++)
        {
            var image=icons[i];image.gameObject.SetActive(i<needed);if(i>=needed)continue;
            image.sprite=human.resourceIcon;image.color=(counted?returned==total:i<returned)?Color.white:awayColor;
            var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,.5f);rect.anchoredPosition=new Vector2(16+i*size.x,4);rect.sizeDelta=size;
        }
        if(countLabel)
        {
            countLabel.gameObject.SetActive(counted);countLabel.text=returned+" / "+total;
            var rect=countLabel.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,.5f);rect.anchoredPosition=new Vector2(16+size.x+8,4);rect.sizeDelta=new Vector2(90,30);
        }
    }
}
