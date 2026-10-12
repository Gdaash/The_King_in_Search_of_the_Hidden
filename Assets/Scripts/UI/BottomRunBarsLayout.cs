using UnityEngine;

[ExecuteAlways, DefaultExecutionOrder(200)]
public sealed class BottomRunBarsLayout : MonoBehaviour
{
    public RectTransform experience,alarm,controls,returnPanel,autoRepeat;
    readonly Vector3[] corners=new Vector3[4];
    void LateUpdate()
    {
        if(!experience||!alarm||!controls||!returnPanel)return;
        var canvas=(RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform;
        controls.GetWorldCorners(corners);
        float left=canvas.InverseTransformPoint(corners[2]).x-canvas.rect.xMin;
        if(autoRepeat){autoRepeat.GetWorldCorners(corners);left=Mathf.Max(left,canvas.InverseTransformPoint(corners[2]).x-canvas.rect.xMin);}
        returnPanel.GetWorldCorners(corners);
        float right=canvas.InverseTransformPoint(corners[0]).x-canvas.rect.xMin;
        const float middleGap=0;
        float gap=Mathf.Max(12,(right-left-experience.rect.width-alarm.rect.width-middleGap)/2);
        Place(experience,left+gap+experience.rect.width*.5f);
        Place(alarm,experience.anchoredPosition.x+(experience.rect.width+alarm.rect.width)*.5f);
    }
    static void Place(RectTransform rect,float x)
    {rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(Mathf.Round(x/2)*2,42);}
}
