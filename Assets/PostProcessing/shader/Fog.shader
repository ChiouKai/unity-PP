Shader "Hidden/PP/Fog"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
    }

    HLSLINCLUDE

        #pragma multi_compile __ FOG_LINEAR FOG_EXP FOG_EXP2
        #include "Assets/PostProcessing/shader/Copy/URP/ShaderLibrary/Core.hlsl"
        #include "Assets/PostProcessing/shader/Copy/URP/Shaders/PostProcessing/Common.hlsl"

        #define SKYBOX_THREASHOLD_VALUE 0.9999


        TEXTURE2D_X(_CameraDepthTexture);
        TEXTURE2D_X(_MainTex);
        half4 _FogColor;
        half4 _FogParams;
        #define _Density _FogParams.x
        #define _Start _FogParams.y
        #define _End _FogParams.z
        #define _IncludeSkybox _FogParams.w
        half ComputeFog(float z)
        {
            half fog = 0.0;
        #if FOG_LINEAR
            fog = (_End - z) / (_End - _Start);
        #elif FOG_EXP
            fog = exp2(-_Density * z);
        #else // FOG_EXP2
            fog = _Density * z;
            fog = exp2(-fog * fog);
        #endif
            return saturate(fog);
        }

        float ComputeDistance(float depth)
        {
            float dist = depth * _ProjectionParams.z;
            dist -= _ProjectionParams.y;
            return dist;
        }

        half4 FragFog(Varyings i) : SV_Target
        {
            half4 color = SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv);
            half brightness = saturate(max(color.r,max(color.g,color.b))-0.7);
            float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_PointClamp, i.uv);
            depth = Linear01Depth(depth, _ZBufferParams);
            float dist = ComputeDistance(depth);
            half x = ComputeFog(dist);
            half fog = 1.0 - x -brightness * x ;
            float skybox = depth < SKYBOX_THREASHOLD_VALUE ? 1 : _IncludeSkybox;
            return lerp(color, _FogColor, saturate(fog) * skybox);
        }


    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex FullscreenVert
                #pragma fragment FragFog

            ENDHLSL
        }
    }
}
