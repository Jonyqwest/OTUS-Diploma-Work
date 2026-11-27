Shader "Custom/Terrain/TerrainUnified"
{
    Properties
    {
        [Header(Slope Detail Triplanar)]
        _DetailTex("Detail (Triplanar) Texture", 2D) = "gray" {}
        _MainTexScale("Main Tex Scale", Float) = 1.0
        _DetailScale("Detail Scale", Float) = 50.0
        _BlendSharpness("Blend Sharpness", Range(1,8)) = 4
        _DetailStrength("Detail Strength", Range(0,1)) = 0.6
        _SlopeThreshold("Slope Start Threshold", Range(0.0, 0.5)) = 0.08
        _SlopeSmoothness("Slope Smoothness", Range(0.01, 0.5)) = 0.12
        _TriplanarMaxDistance("Triplanar Max Distance (m)", Float) = 400.0
        _TriplanarFadeRange("Triplanar Fade Range (m)", Float) = 80.0

        [Header(Global Normal Maps)]
        _NormalMap("Global Normal Map", 2D) = "bump" {}
        _NormalScale("Global Normal Scale", Float) = 5.0
        _NormalStrength("Global Normal Strength", Range(0,1)) = 0.5
        _TriplanarNormal("Triplanar Normal Map", 2D) = "bump" {}
        _TriplanarNormalScale("Triplanar Normal Scale", Float) = 10.0
        _TriplanarNormalStrength("Triplanar Normal Strength", Range(0,1)) = 0.6

        [Header(Custom Paint Layer)]
        [Toggle(_CUSTOM_PAINT_ON)] _EnableCustomPaint("Enable Custom Paint Layer", Float) = 0
        _PaintTex("Paint Control Map", 2D) = "black" {}
        _PathTex("Path Texture", 2D) = "gray" {}
        _GrassTex("Grass Texture", 2D) = "gray" {}
        _DirtTex("Dirt Texture", 2D) = "gray" {}
        _WaterTex("Water Texture", 2D) = "blue" {}
        _PaintTileSize("Paint Detail Tile Size (world units)", Float) = 10

        [Header(Snow Layer)]
        [Toggle(_SNOW_LAYER_ON)] _EnableSnowLayer("Enable Snow Layer", Float) = 0
        _SnowColor("Snow Color", Color) = (0.95, 0.97, 1.0, 1.0)
        _SnowThreshold("Snow Brightness Threshold", Range(0.0, 1.0)) = 0.65
        _SnowSoftness("Snow Softness", Range(0.01, 0.5)) = 0.15
        _SlopeLimit("Snow Max Slope Limit", Range(0.0, 1.0)) = 0.60
        _SnowAltitude("Snow Altitude Start (m)", Float) = 100.0
        _AltitudeFade("Snow Altitude Fade Range (m)", Float) = 50.0

        [Header(Distance Blur)]
        _NearBlurDistance("Near Distance", Float) = 40.0
        _FarBlurDistance("Far Distance", Float) = 100.0
        _BlurStrength("Blur Strength", Range(0, 20)) = 0.0

        [Header(Satellite Data)]
        _LowTex("Low Satellite Texture", 2D) = "white" {}
        [Toggle] _DebugOnlyLowTex("Force Low Tex Only", Float) = 0
        _MainTex("Main Satellite Texture", 2D) = "white" {}
        _LowTexMinDistance("Low Tex Min Distance (Full Main)", Float) = 250.0
        _LowTexMaxDistance("Low Tex Max Distance (Full Low)", Float) = 400.0

        _SuperTex("Super Satellite Texture", 2D) = "white" {}
        _BlendValue("Super Blend Value", Range(0,1)) = 0
        _BlendSpeed("Super Blend Speed", Range(0.1, 5.0)) = 1.0
        _BlendStartTime("Super Blend Start Time", Float) = 0.0
        _BlendDirection("Super Blend Direction", Float) = 0.0
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
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma shader_feature_local _CUSTOM_PAINT_ON
            #pragma shader_feature_local _SNOW_LAYER_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "../Prefab/Includes/HybridTerrainCommon.cginc"
            #include "../Prefab/Includes/SatelliteTerrain.cginc"
            #include "../Prefab/Includes/TriplanarTerrain.cginc"
            #include "Includes/TerrainCustomPaintModule.cginc"
            #include "Includes/TerrainSnowModule.cginc"
            #include "../Prefab/Includes/TerrainLighting.cginc"

            v2f vert(appdata input)
            {
                return BuildTerrainVaryings(input);
            }

            float4 frag(v2f input) : SV_Target
            {
                float3 sourceNormal = normalize(input.worldNormal);
                float slope = TerrainSlope(sourceNormal);

                // 1. Base Satellite Texture
                float3 satelliteColor = SampleSatelliteTerrain(input.uv, input.worldPos);

                // 2. Optimized Triplanar Rock Detail on Slopes
                float3 color = ApplyTerrainSlopeDetail(
                    satelliteColor,
                    input.worldPos,
                    sourceNormal,
                    slope);

                // 3. Optional Custom Paint Layer (Paths, Grass, Dirt, Water) in 1-Pass!
#if defined(_CUSTOM_PAINT_ON)
                color = ApplyCustomPaintModule(color, input.uv, input.worldPos);
#endif

                // 4. Optional Snow & Altitude Layer in 1-Pass!
#if defined(_SNOW_LAYER_ON)
                color = ApplySnowModule(color, satelliteColor, slope, input.worldPos.y);
#endif

                // 5. Normal Mapping & URP Lighting
                float3 terrainNormal = BuildTerrainNormal(input.worldPos, sourceNormal, slope);
                float3 litColor = ApplyTerrainLighting(color, terrainNormal, input.worldPos);

                float4 outputColor = float4(litColor, 1.0);
                outputColor.rgb = MixFog(outputColor.rgb, input.fogFactor);
                return outputColor;
            }
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
