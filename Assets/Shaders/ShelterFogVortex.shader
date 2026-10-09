Shader "Game/Shelter Fog Vortex"
{
    Properties
    {
        [PerRendererData] _MainTex ("Original fog sprite / silhouette", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Center ("Vortex centre (sprite UV)", Vector) = (0.5,0.5,0,0)
        _Ellipse ("Vortex ellipse radius (UV)", Vector) = (0.5,0.5,0,0)
        _WindSpeed ("Rotation speed (radians / second)", Range(-1.5,1.5)) = 0.48
        _SpiralTightness ("Spiral winding", Range(0,12)) = 2.4
        _CloudScale ("Cloud mass detail", Range(1,8)) = 3.0
        _MotionPixels ("Turbulence (source pixels)", Range(0,64)) = 26
        _Density ("Cloud density", Range(0,3)) = 1.65
        _Contrast ("Cloud volume contrast", Range(0.5,3)) = 1.65
        _ShadowColor ("Cloud shadow", Color) = (0.07,0.085,0.11,1)
        _LightColor ("Cloud light", Color) = (0.34,0.39,0.47,1)
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
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Center, _Ellipse;
            fixed4 _Color, _ShadowColor, _LightColor;
            float _WindSpeed, _SpiralTightness, _CloudScale, _MotionPixels;
            float _Density, _Contrast, _PreviewTime;
            struct input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            output vert(input v)
            {
                output o;
                o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color;
                return o;
            }
            fixed4 Pixel(float2 uv)
            {
                fixed4 c=tex2D(_MainTex,saturate(uv)); c.rgb*=c.a; return c;
            }
            fixed4 Fog(float2 uv)
            {
                // Interpolate this atmospheric layer only; the source keeps its Point import.
                float2 pixel=uv*_MainTex_TexelSize.zw-0.5;
                float2 f=frac(pixel);
                float2 origin=(floor(pixel)+0.5)*_MainTex_TexelSize.xy;
                return lerp(lerp(Pixel(origin),Pixel(origin+float2(_MainTex_TexelSize.x,0)),f.x),
                            lerp(Pixel(origin+float2(0,_MainTex_TexelSize.y)),Pixel(origin+_MainTex_TexelSize.xy),f.x),f.y);
            }
            float Hash(float2 p)
            {
                float3 q=frac(float3(p.xyx)*0.1031);
                q+=dot(q,q.yzx+33.33);
                return frac((q.x+q.y)*q.z);
            }
            float Noise(float2 p)
            {
                float2 cell=floor(p), f=frac(p);
                f=f*f*(3-2*f);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),
                            lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
            }
            float Clouds(float2 p)
            {
                float n=0;
                n+=0.55*Noise(p); p=mul(float2x2(1.6,-1.2,1.2,1.6),p)+13.7;
                n+=0.27*Noise(p); p=mul(float2x2(1.6,-1.2,1.2,1.6),p)+8.3;
                n+=0.13*Noise(p); p=mul(float2x2(1.6,-1.2,1.2,1.6),p)+7.1;
                return n+0.05*Noise(p);
            }
            float2 Rotate(float2 p,float a)
            {
                float s,c; sincos(a,s,c);
                return float2(c*p.x-s*p.y,s*p.x+c*p.y);
            }
            fixed4 frag(output i):SV_Target
            {
                fixed4 original=Fog(i.uv);
                if(original.a<0.001) return 0;
                float time=_PreviewTime>=0 ? _PreviewTime : _Time.y;
                float2 p=(i.uv-_Center.xy)/max(_Ellipse.xy,float2(0.01,0.01));
                float2 rolling=Rotate(p,-time*_WindSpeed);
                float2 billow=float2(Noise(rolling*2.2+3.7),Noise(rolling*2.2+17.8))-0.5;
                rolling+=billow*0.20;
                float radius=length(rolling);
                // Fixed radial winding prevents indefinitely tightening spirals during long sessions.
                // Rotating the cloud coordinates advects the masses continuously around the shelter.
                float2 spiral=Rotate(rolling,radius*_SpiralTightness);
                float2 cloudUV=spiral*_CloudScale;
                float2 warp=float2(Noise(cloudUV*0.7+float2(time*0.09,11.2)),
                                   Noise(cloudUV*0.7+float2(19.8,-time*0.07)))-0.5;
                cloudUV+=warp*(_MotionPixels/16.0);
                float mass=Clouds(cloudUV);
                float curl=Clouds(rolling*7+billow+float2(4.7,time*0.04));
                float volume=saturate((mass*0.78+curl*0.22-0.48)*_Contrast+0.5);
                float light=smoothstep(0.15,0.85,volume);
                float3 painted=original.rgb/max(original.a,0.001);
                float3 rgb=lerp(_ShadowColor.rgb,_LightColor.rgb,light);
                rgb=lerp(rgb,painted,0.16);
                // The mask is always sampled at the ORIGINAL UV: neither rotation nor turbulence
                // can spill into the clear centre or outside the painted cloud silhouette.
                float alpha=saturate(original.a*_Density*lerp(0.72,1.15,volume))*i.color.a;
                return fixed4(rgb*i.color.rgb*alpha,alpha);
            }
            ENDCG
        }
    }
}
