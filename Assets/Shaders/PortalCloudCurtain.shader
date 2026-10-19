Shader "UI/PortalCloudCurtain"
{
    Properties
    {
        [PerRendererData] _MainTex ("Fullscreen cloud painting", 2D) = "white" {}
        _Progress ("Cloud coverage", Range(0,1)) = 0
        _CloudDarkness ("Dark overlay above clouds", Range(0,1)) = .4
        _FlowTime ("Unscaled flow", Float) = 0
        _ViewSize ("Canvas dimensions", Vector) = (1920,1080,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Progress, _FlowTime, _CloudDarkness;
            float4 _ViewSize;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            float hash21(float2 p)
            {
                float3 q=frac(float3(p.xyx)*float3(.1031,.1030,.0973));
                q+=dot(q,q.yzx+33.33); return frac((q.x+q.y)*q.z);
            }
            float noise(float2 p)
            {
                float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash21(i),hash21(i+float2(1,0)),f.x),lerp(hash21(i+float2(0,1)),hash21(i+1),f.x),f.y);
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float aspect=_ViewSize.x/max(1,_ViewSize.y);
                float2 centred=(i.uv-.5)*2;
                float2 physical=centred*float2(aspect,1);
                float radius=length(physical)/length(float2(aspect,1));
                // Differential rotation winds the cloud texture into a vortex:
                // the inner clouds turn much farther than the outer clouds.
                float twist=_Progress*3.2-5.0*log(radius+.16);
                float s,c; sincos(twist,s,c);
                float2 rotated=float2(c*physical.x-s*physical.y,s*physical.x+c*physical.y);
                float2 vortex=rotated/length(float2(aspect,1));
                float textureAspect=_MainTex_TexelSize.z/max(1,_MainTex_TexelSize.w);
                float contraction=lerp(.96,.76,_Progress);
                float2 uv=vortex*float2(1/textureAspect,1)*contraction*.46+.5;
                fixed3 colour=tex2D(_MainTex,uv).rgb*(1-_CloudDarkness);
                // A ragged shrinking eye, rather than separate rotating mask blades.
                // Warped noise follows the same spiral as the visible cloud painting.
                float n=noise(vortex*10)*.7+noise(vortex*23+17)*.3;
                float front=(1-_Progress)*1.22-.12;
                float edge=radius+(n-.5)*.12;
                float coverage=smoothstep(front-.075,front+.075,edge);
                coverage*=smoothstep(0,.04,_Progress);
                coverage=lerp(coverage,1,smoothstep(.92,1,_Progress));
                // Composite a fullscreen black veil above both the clouds and their opening.
                // The same progress runs backwards on reveal, so the fade is symmetric.
                float blackout=smoothstep(0,1,saturate(_Progress));
                float alpha=coverage+(1-coverage)*blackout;
                fixed3 composite=colour*coverage*(1-blackout)/max(alpha,.0001);
                return fixed4(composite,alpha)*i.color;
            }
            ENDCG
        }
    }
}
