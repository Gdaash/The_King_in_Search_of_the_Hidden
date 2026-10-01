Shader "UI/ThoseUnderHex/CRT Overlay"
{
    Properties
    {
        // RawImage always binds its texture through this standard UI property.
        _MainTex ("UI Texture", 2D) = "white" {}
        _ScanlineOpacity ("Scanline Opacity", Range(0, 1)) = 0.11
        _ScanlineDensity ("Scanlines Per Screen", Range(120, 1600)) = 720
        _ScanlineAngle ("Scanline Angle (0=Horizontal, 90=Vertical)", Range(0, 180)) = 90
        _VignetteOpacity ("Vignette Opacity", Range(0, 1)) = 0.25
        _VignetteWidth ("Vignette Edge Width", Range(0.01, 0.5)) = 0.06
        _VignetteRoundness ("Vignette Shape (2=Round, 8=Rectangular)", Range(1, 16)) = 8
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off Lighting Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            float _ScanlineOpacity, _ScanlineDensity, _ScanlineAngle, _VignetteOpacity, _VignetteWidth, _VignetteRoundness;
            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float angleRadians = radians(_ScanlineAngle);
                float scanlineAxis = dot(i.uv, float2(sin(angleRadians), cos(angleRadians)));
                float scan = sin((scanlineAxis + _Time.y * 0.0007) * _ScanlineDensity * 6.2831853) * 0.5 + 0.5;
                float2 centered = i.uv * 2.0 - 1.0;
                float edgeDistance = pow(abs(centered.x), _VignetteRoundness) + pow(abs(centered.y), _VignetteRoundness);
                float edge = smoothstep(1.0 - _VignetteWidth, 1.0, edgeDistance);
                return fixed4(0, 0, 0, saturate(scan * _ScanlineOpacity + edge * _VignetteOpacity));
            }
            ENDHLSL
        }
    }
}
