using GameFoundation.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.MetaProgression
{
    /// <summary>Canvas XP flight; unscaled time keeps it moving during upgrade selection.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PortalExperienceCrystal : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float flightDuration = .85f;
        [SerializeField, Min(0)] private float arcHeight = .8f;
        [SerializeField, Min(0)] private float scatterRadius = .35f;
        private RectTransform destination, canvasRect, visual;
        private Canvas canvas;
        private Vector2 origin, scatter;
        private float elapsed, delay;
        private float destinationFill;
        private bool arrivalSound;
        private System.Action arrived;
        public void Launch(Vector3 position, PortalTowerExperienceBar bar, float launchDelay, bool playArrivalSound, System.Action onArrival=null, float targetFill=1f)
        {
            if(bar==null || bar.fill==null){Destroy(gameObject);return;}
            arrived=onArrival;
            destinationFill=Mathf.Clamp01(targetFill);
            destination=bar.fill.rectTransform;
            canvas=bar.GetComponentInParent<Canvas>()?.rootCanvas;
            if(canvas==null){Destroy(gameObject);return;}
            canvasRect=canvas.transform as RectTransform;
            var source=GetComponent<SpriteRenderer>();source.enabled=false;
            var go=new GameObject("Flying XP Crystal",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            visual=go.GetComponent<RectTransform>();visual.SetParent(canvasRect,false);
            visual.anchorMin=visual.anchorMax=visual.pivot=new Vector2(.5f,.5f);
            visual.localScale=Vector3.one;
            var image=go.GetComponent<Image>();image.sprite=source.sprite;image.color=source.color;
            image.raycastTarget=false;image.preserveAspect=true;
            visual.sizeDelta=source.sprite!=null?source.sprite.rect.size*2:Vector2.one*24;
            Vector2 screen=Camera.main!=null?(Vector2)Camera.main.WorldToScreenPoint(position):RectTransformUtility.WorldToScreenPoint(UiCamera,destination.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screen,UiCamera,out origin);
            visual.anchoredPosition=origin;
            delay=launchDelay;arrivalSound=playArrivalSound;
            scatter=Random.insideUnitCircle*scatterRadius*32;
        }
        private Camera UiCamera=>canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        private void Update()
        {
            if(destination==null || visual==null || canvas==null){Destroy(gameObject);return;}
            elapsed+=Time.unscaledDeltaTime;
            float t=Mathf.Clamp01((elapsed-delay)/flightDuration);
            float travel=t*t*(3-2*t);
            var rect=destination.rect;
            var center=destination.TransformPoint(new Vector3(Mathf.Lerp(rect.xMin,rect.xMax,destinationFill),rect.center.y,0));
            var screen=RectTransformUtility.WorldToScreenPoint(UiCamera,center);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screen,UiCamera,out Vector2 end);
            Vector2 control=(origin+end)*.5f+Vector2.up*arcHeight*32+scatter;
            visual.anchoredPosition=(1-travel)*(1-travel)*origin+2*(1-travel)*travel*control+travel*travel*end;
            float scale=t<.12f?Mathf.Lerp(.65f,1,t/.12f):t>.8f?Mathf.Lerp(1,.15f,(t-.8f)/.2f):1;
            visual.localScale=Vector3.one*scale;
            if(t<1)return;
            if(arrivalSound)GameAudioController.PlayAt(GameAudioCue.ResourceGain,transform.position,.3f,1.1f,1.2f,.1f);
            var callback=arrived;arrived=null;callback?.Invoke();
            Destroy(gameObject);
        }
        private void OnDestroy(){if(visual!=null)Destroy(visual.gameObject);}
    }
}
