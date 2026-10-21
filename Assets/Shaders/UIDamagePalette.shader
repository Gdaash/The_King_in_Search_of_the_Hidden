Shader "ThoseUnderHex/UI Damage Palette"
{
 Properties {
  [PerRendererData] _MainTex ("Sprite",2D)="white" {}
  _Color ("Tint",Color)=(1,1,1,1)
  _Shadow ("Shadow",Color)=(.1,.02,.2,1)
  _Mid ("Midtone",Color)=(.65,.2,1,1)
  _Highlight ("Highlight",Color)=(.95,.85,1,1)
  _StencilComp ("Stencil Comparison",Float)=8
  _Stencil ("Stencil ID",Float)=0
  _StencilOp ("Stencil Operation",Float)=0
  _StencilWriteMask ("Stencil Write Mask",Float)=255
  _StencilReadMask ("Stencil Read Mask",Float)=255
  _ColorMask ("Color Mask",Float)=15
  [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip",Float)=0
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" "IgnoreProjector"="True" "PreviewType"="Plane"}
  Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
  Pass {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;float4 world:TEXCOORD1;};
   sampler2D _MainTex;float4 _Shadow,_Mid,_Highlight,_Color,_TextureSampleAdd,_ClipRect;
   v2f vert(appdata v){v2f o;o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   float4 frag(v2f i):SV_Target {
    float4 c=tex2D(_MainTex,i.uv)+_TextureSampleAdd;
    float brightness=max(c.r,max(c.g,c.b));
    float3 rgb=brightness<.6?lerp(_Shadow.rgb,_Mid.rgb,brightness/.6):lerp(_Mid.rgb,_Highlight.rgb,(brightness-.6)/.4);
    float4 result=float4(rgb*i.color.rgb,c.a*i.color.a);
    #ifdef UNITY_UI_CLIP_RECT
     result.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
     clip(result.a-.001);
    #endif
    return result;
   }
   ENDHLSL
  }
 }
}
