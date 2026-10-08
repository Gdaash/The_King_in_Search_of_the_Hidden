Shader "Game/Shelter Fog Flow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Fog sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _MotionPixels ("Motion distance (source pixels)", Range(0,32)) = 16
        _WindSpeed ("Wind speed", Range(0,1)) = 0.28
        _Density ("Density", Range(0,1.5)) = 1
        [HideInInspector] _PreviewTime ("Preview time", Float) = -1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off ZWrite Off ZTest LEqual
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _MotionPixels, _WindSpeed, _Density, _PreviewTime;
            struct input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            output vert(input v)
            {
                output o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            fixed4 Pixel(float2 uv)
            {
                fixed4 c=tex2D(_MainTex,saturate(uv));
                c.rgb*=c.a;
                return c;
            }
            // Only fog is interpolated, keeping the source's Point import and all scene pixel art intact.
            fixed4 Fog(float2 uv)
            {
                float2 pixel=uv*_MainTex_TexelSize.zw-0.5;
                float2 f=frac(pixel);
                float2 origin=(floor(pixel)+0.5)*_MainTex_TexelSize.xy;
                return lerp(lerp(Pixel(origin),Pixel(origin+float2(_MainTex_TexelSize.x,0)),f.x),
                            lerp(Pixel(origin+float2(0,_MainTex_TexelSize.y)),Pixel(origin+_MainTex_TexelSize.xy),f.x),f.y);
            }
            fixed4 frag(output i):SV_Target
            {
                float t=(_PreviewTime>=0 ? _PreviewTime : _Time.y)*_WindSpeed;
                float2 uv=i.uv;
                float2 flow=float2(sin(uv.y*13+t)+0.4*sin(uv.x*17-0.63*t),
                                   cos(uv.x*11-0.79*t)+0.35*sin(uv.y*19+0.47*t));
                float edge=min(min(uv.x,1-uv.x),min(uv.y,1-uv.y));
                flow*=_MotionPixels*_MainTex_TexelSize.xy*smoothstep(0,0.035,edge);
                fixed4 original=Fog(uv);
                fixed4 moving=Fog(uv+flow);
                // Keep the painted clear centre clear; movement stays within the authored fog belt.
                fixed4 c=lerp(original,moving,0.85);
                float density=_Density*(0.97+0.03*sin(t*0.71+uv.x*7+uv.y*9));
                c*=density*saturate(original.a*12);
                c.rgb*=i.color.rgb*i.color.a;
                c.a*=i.color.a;
                return c;
            }
            ENDCG
        }
    }
}
