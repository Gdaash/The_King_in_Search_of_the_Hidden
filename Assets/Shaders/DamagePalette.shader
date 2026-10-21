Shader "ThoseUnderHex/Damage Palette"
{
 Properties {
  _MainTex ("Sprite Texture",2D)="white" {}
  _Shadow ("Shadow",Color)=(.1,.02,.2,1)
  _Mid ("Midtone",Color)=(.65,.2,1,1)
  _Highlight ("Highlight",Color)=(.95,.85,1,1)
  [HideInInspector] _Color ("Tint",Color)=(1,1,1,1)
  [HideInInspector] _RendererColor ("RendererColor",Color)=(1,1,1,1)
  [HideInInspector] _AlphaTex ("External Alpha",2D)="white" {}
  [HideInInspector] _EnableExternalAlpha ("Enable External Alpha",Float)=0
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True"}
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
  Pass {
   HLSLPROGRAM
   #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE
   struct Attributes { COMMON_2D_INPUTS half4 color:COLOR; UNITY_SKINNED_VERTEX_INPUTS };
   struct Varyings { COMMON_2D_OUTPUTS half4 color:COLOR; };
   #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _Color,_Shadow,_Mid,_Highlight;
   CBUFFER_END
   Varyings vert(Attributes input){
    UNITY_SKINNED_VERTEX_COMPUTE(input);
    SetUpSpriteInstanceProperties();
    input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
    Varyings o=CommonUnlitVertex(input);o.color=input.color*_Color*unity_SpriteColor;return o;
   }
   half4 frag(Varyings i):SV_Target {
    half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
    half brightness=max(c.r,max(c.g,c.b));
    half3 rgb=brightness<.6?lerp(_Shadow.rgb,_Mid.rgb,brightness/.6):lerp(_Mid.rgb,_Highlight.rgb,(brightness-.6)/.4);
    return half4(rgb*i.color.rgb,c.a*i.color.a);
   }
   ENDHLSL
  }
 }
}
