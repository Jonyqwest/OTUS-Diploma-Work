Shader "Custom/HybridTriplanarSuperTexture_TripleBlend"
{
    Properties
    {
        _LowTex("Low Texture", 2D) = "black" {}
        _MainTex("Satellite Texture", 2D) = "white" {}
        _SuperTex("Super Satellite Texture", 2D) = "white" {}

        _MainBlendValue("Main Blend Value", Range(0,1)) = 0
        _MainBlendSpeed("Main Blend Speed", Range(0.1, 5.0)) = 1.0
        _MainBlendStartTime("Main Blend Start Time", Float) = 0.0
        _MainBlendDirection("Main Blend Direction", Float) = 1.0

        _BlendValue("Super Blend Value", Range(0,1)) = 0
        _BlendSpeed("Super Blend Speed", Range(0.1, 5.0)) = 1.0
        _BlendStartTime("Super Blend Start Time", Float) = 0.0
        _BlendDirection("Main Blend Direction", Float) = 1.0

        [Space(20)]
        [Header(Distance Blur)]
        _NearBlurDistance("Near Distance", Float) = 40
        _FarBlurDistance("Far Distance", Float) = 100
        _BlurStrength("Blur Strength", Range(0, 20)) = 6

        _DetailTex("Detail (Triplanar) Texture", 2D) = "gray" {}
        _MainTexScale("Main Tex Scale", Float) = 1.0
        _DetailScale("Detail Scale", Float) = 50.0
        _BlendSharpness("Blend Sharpness", Range(1,8)) = 4
        _DetailStrength("Detail Strength", Range(0,1)) = 0.6
        _SlopeThreshold("Slope Start Threshold", Range(0.0, 0.5)) = 0.08
        _SlopeSmoothness("Slope Smoothness", Range(0.01, 0.5)) = 0.12
        _TriplanarMaxDistance("Triplanar Max Distance (m)", Float) = 400.0
        _TriplanarFadeRange("Triplanar Fade Range (m)", Float) = 80.0

        _NormalMap("Global Normal Map", 2D) = "bump" {}
        _NormalScale("Global Normal Scale", Float) = 5.0
        _NormalStrength("Global Normal Strength", Range(0,1)) = 0.5

        _TriplanarNormal("Triplanar Normal Map", 2D) = "bump" {}
        _TriplanarNormalScale("Triplanar Normal Scale", Float) = 10.0
        _TriplanarNormalStrength("Triplanar Normal Strength", Range(0,1)) = 0.6

    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 300

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #include_with_pragmas "Includes/HybridTriplanarCore.cginc"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct DepthAppdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthVaryings DepthVert(DepthAppdata input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.vertex.xyz);
                return output;
            }

            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct ShadowAppdata
            {
                float4 vertex   : POSITION;
                float3 normal   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 _LightDirection;

            ShadowVaryings ShadowVert(ShadowAppdata input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.vertex.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normal);

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
