Shader "Hidden/ThoseUnderHex/CRT World"
{
    Properties
    {
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.18
        _ScanlineDensity ("Scanlines Per Screen", Range(120, 1600)) = 720
        _ScanlineAngle ("Scanline Angle (0=Horizontal, 90=Vertical)", Range(0, 180)) = 90
        _VignetteIntensity ("Vignette Intensity", Range(0, 1)) = 0.38
        _VignetteWidth ("Vignette Edge Width", Range(0.01, 0.5)) = 0.08
        _VignetteRoundness ("Vignette Shape (2=Round, 8=Rectangular)", Range(1, 16)) = 8
        _Curvature ("Screen Curvature", Range(0, 0.2)) = 0.035
        _ChromaticAberration ("Chromatic Aberration", Range(0, 0.01)) = 0.0015
        _Flicker ("Flicker", Range(0, 0.12)) = 0.018
        _Glow ("Highlight Glow", Range(0, 0.5)) = 0.08
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            Name "CRT"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _ScanlineIntensity;
            float _ScanlineDensity;
            float _ScanlineAngle;
            float _VignetteIntensity;
            float _VignetteWidth;
            float _VignetteRoundness;
            float _Curvature;
            float _ChromaticAberration;
            float _Flicker;
            float _Glow;

            float2 Curve(float2 uv)
            {
                float2 centered = uv * 2.0 - 1.0;
                centered *= 1.0 + dot(centered, centered) * _Curvature;
                return centered * 0.5 + 0.5;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = Curve(input.texcoord);
                float inside = step(0.0, uv.x) * step(0.0, uv.y) * step(uv.x, 1.0) * step(uv.y, 1.0);
                float2 chroma = float2(_ChromaticAberration, 0.0);
                float3 color;
                color.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + chroma).r;
                color.g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).g;
                color.b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - chroma).b;

                float angleRadians = radians(_ScanlineAngle);
                float scanlineAxis = dot(input.texcoord, float2(sin(angleRadians), cos(angleRadians)));
                float scanline = sin((scanlineAxis + _Time.y * 0.0007) * _ScanlineDensity * 6.2831853) * 0.5 + 0.5;
                color *= 1.0 - scanline * _ScanlineIntensity;

                float2 centered = input.texcoord * 2.0 - 1.0;
                float edgeDistance = pow(abs(centered.x), _VignetteRoundness) + pow(abs(centered.y), _VignetteRoundness);
                float vignette = smoothstep(1.0 - _VignetteWidth, 1.0, edgeDistance);
                color *= 1.0 - vignette * _VignetteIntensity;
                color += max(color - 0.78, 0.0) * _Glow;
                color *= 1.0 - sin(_Time.y * 60.0) * _Flicker;
                return half4(color * inside, 1.0);
            }
            ENDHLSL
        }
    }
}
