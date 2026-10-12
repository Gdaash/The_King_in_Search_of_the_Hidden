using UnityEngine;

[ExecuteAlways]
public sealed class QuestSummarySpacing : MonoBehaviour
{
    void LateUpdate()
    {
        var rect=(RectTransform)transform;
        int count=0;
        float width=0;
        foreach(RectTransform child in rect) if(child.gameObject.activeSelf){child.sizeDelta=new Vector2(104,80);count++;width+=104;}
        if(count==0)return;
        float gap=(rect.rect.width+32-width)/(count+1);
        float x=gap-16;
        foreach(RectTransform child in rect)
        {
            if(!child.gameObject.activeSelf)continue;
            child.anchorMin=child.anchorMax=new Vector2(0,1);
            child.pivot=new Vector2(.5f,1);
            child.anchoredPosition=new Vector2(x+child.rect.width*.5f,0);
            x+=child.rect.width+gap;
        }
    }
}
