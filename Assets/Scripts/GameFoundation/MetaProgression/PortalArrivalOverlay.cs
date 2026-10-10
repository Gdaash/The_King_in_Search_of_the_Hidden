using UnityEngine;
using UnityEngine.Rendering;

namespace GameFoundation.MetaProgression
{
    [DefaultExecutionOrder(-200)]
    public sealed class PortalArrivalOverlay : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer overlay;
        private float elapsed;
        private bool playing;
        private bool firstFramePending;
        private double startedAt;
        public float Elapsed => elapsed;
        public const float TotalDuration=2.4f;
        private void OnEnable()
        {
            elapsed=0;
            firstFramePending=true;
            RenderPipelineManager.endCameraRendering+=OnCameraRendered;
            playing=overlay!=null && gameObject.scene.name=="World";
            if(overlay==null)return;
            overlay.enabled=playing;
            SetAlpha(playing?1:0);
        }
        private void Update()
        {
            if(!playing)return;
            // Begin after the game camera has actually rendered the opaque overlay.
            // Scene loading and Unity's stale first-frame delta must not consume it.
            if(firstFramePending)return;
            elapsed=(float)(Time.realtimeSinceStartupAsDouble-startedAt);
            SetAlpha(AlphaAt(elapsed));
            if(elapsed>=TotalDuration){playing=false;overlay.enabled=false;}
        }
        // The fade clock pauses during both flashes, so 2 s of fading + 0.4 s of flashes = 2.2 s.
        public static float AlphaAt(float time)
        {
            if(time>=TotalDuration)return 0;
            if((time>=1.4f && time<1.6f) || (time>=1.9f && time<2.1f))return .7f;
            float fadeTime=time-Mathf.Clamp(time-1.4f,0,.1f)-Mathf.Clamp(time-1.9f,0,.1f);
            return 1-Mathf.Clamp01(fadeTime/2);
        }
        private void OnCameraRendered(ScriptableRenderContext context,Camera camera)
        {
            if(!playing || !firstFramePending || camera.cameraType!=CameraType.Game)return;
            firstFramePending=false;
            startedAt=Time.realtimeSinceStartupAsDouble;
            RenderPipelineManager.endCameraRendering-=OnCameraRendered;
        }
        private void SetAlpha(float alpha){var color=overlay.color;color.a=alpha;overlay.color=color;}
        private void OnDisable(){RenderPipelineManager.endCameraRendering-=OnCameraRendered;playing=false;if(overlay!=null)overlay.enabled=false;}
    }
}
