using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Lets a separate caption use the existing button and its feedback.</summary>
public sealed class ButtonPointerRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public GameObject target;
    private bool hovered;
    private Vector3 baseScale;
    void Awake()=>baseScale=transform.localScale;
    void Update(){var b=target?target.GetComponent<UnityEngine.UI.Button>():null;transform.localScale=Vector3.Lerp(transform.localScale,baseScale*(hovered && b && b.interactable?1.08f:1),1-Mathf.Exp(-16*Time.unscaledDeltaTime));}
    void OnDisable(){hovered=false;transform.localScale=baseScale;}
    public void OnPointerEnter(PointerEventData e) { hovered=true;if(target) ExecuteEvents.Execute(target,e,ExecuteEvents.pointerEnterHandler); }
    public void OnPointerExit(PointerEventData e) { hovered=false;if(target) ExecuteEvents.Execute(target,e,ExecuteEvents.pointerExitHandler); }
    public void OnPointerDown(PointerEventData e) { if(target) ExecuteEvents.Execute(target,e,ExecuteEvents.pointerDownHandler); }
    public void OnPointerUp(PointerEventData e) { if(target) ExecuteEvents.Execute(target,e,ExecuteEvents.pointerUpHandler); }
    public void OnPointerClick(PointerEventData e) { if(target) ExecuteEvents.Execute(target,e,ExecuteEvents.pointerClickHandler); }
}
