Shader "Custom/TerrainOverlays/GpuGrassBlades"
{
    Properties
    {
        [Header(Detection)]
        _GrassThresholdGR("GR Threshold (Green-Red)", Range(-0.5, 0.5)) = 0.04
        _GrassThresholdBG("BG Threshold (Green-Blue)", Range(-0.5, 0.5)) = 0.04
        _GrassSoftness("Green Edge Softness", Range(0.001, 0.25)) = 0.035
        _GrassDarknessThreshold("Darkness Threshold", Range(0.0, 0.15)) = 0.03
        _GrassSlopeLimit("Grass Slope Limit", Range(0.001, 1)) = 0.65

        [Header(Geometry In Real Meters)]
        _GrassRenderDistance("Render Distance", Range(1, 500)) = 300
        _GrassFadeStart("Distance Fade Start", Range(1, 150)) = 10
        _GrassMinHeight("Minimum Height", Range(0.02, 20)) = 1
        _GrassMaxHeight("Maximum Height", Range(0.05, 50)) = 20
        _GrassWidth("Blade Width", Range(0.01, 2)) = 0.4
        _GrassRootLift("Root Lift", Range(0, 0.2)) = 0.015
        _GrassDensity("Grass Clusters Per Square Meter", Range(1, 20)) = 5

        [Header(Appearance)]
        _BaseColor("Base Color", Color) = (0.24, 0.48, 0.14, 1)
        _GrassTipBlend("Tip Blend", Range(0, 1)) = 0.5
        _WindStrength("Wind Strength", Range(0, 0.5)) = 0.08
        _WindSpeed("Wind Speed", Range(0, 5)) = 1.4
    }

    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest+10"
            "RenderType"="TransparentCutout"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "GPU Grass Blades"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex GrassBladeVertex
            #pragma fragment GrassBladeFragment
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "../Includes/TerrainGrassGpuCommon.cginc"

            StructuredBuffer<TerrainGrassBlade> _GrassBlades;
            half4 _BaseColor;
            half _GrassTipBlend;
            float _GrassRenderDistanceWorld;
            float _GrassFadeStartWorld;
            float _WindStrength;
            float _WindSpeed;

            struct GrassBladeVaryings
            {
                float4 positionCS : SV_POSITION;
                half4 color : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            GrassBladeVaryings GrassBladeVertex(uint vertexId : SV_VertexID)
            {
                GrassBladeVaryings output;
                uint bladeIndex = vertexId / 3u;
                uint corner = vertexId - bladeIndex * 3u;
                TerrainGrassBlade blade = _GrassBlades[bladeIndex];

                float3 normalWS = normalize(blade.normalWS.xyz);
                float3 viewDirection = _WorldSpaceCameraPos.xyz - blade.rootWS.xyz;
                viewDirection -= normalWS * dot(viewDirection, normalWS);
                float viewLength = length(viewDirection);
                viewDirection = viewLength > 0.0001
                    ? viewDirection / viewLength
                    : normalize(cross(normalWS, float3(1.0, 0.0, 0.0)) + float3(0.0, 0.0, 0.001));
                float3 sideWS = normalize(cross(normalWS, viewDirection));

                float baseSide = corner == 0u ? -1.0 : (corner == 1u ? 1.0 : 0.0);
                float tipFactor = corner == 2u ? 1.0 : 0.0;
                float windPhase = dot(blade.rootWS.xz, float2(0.173, 0.317)) +
                    blade.data.x * 6.2831853 + _Time.y * _WindSpeed;
                float3 windOffset = float3(sin(windPhase), 0.0, cos(windPhase * 0.83)) *
                    (_WindStrength * blade.rootWS.w * tipFactor);

                float distanceToCamera = distance(_WorldSpaceCameraPos.xz, blade.rootWS.xz);
                half distanceFade = 1.0h - smoothstep(
                    _GrassFadeStartWorld,
                    max(_GrassRenderDistanceWorld, _GrassFadeStartWorld + 0.0001),
                    distanceToCamera);

                // Shrinking at the edge gives a stable opaque fade without
                // transparent-triangle sorting artifacts.
                float3 positionWS = blade.rootWS.xyz +
                    sideWS * (baseSide * blade.normalWS.w * distanceFade) +
                    normalWS * (blade.rootWS.w * tipFactor * distanceFade) +
                    windOffset * distanceFade;

                output.positionCS = mul(UNITY_MATRIX_VP, float4(positionWS, 1.0));
                half4 baseColor = half4(blade.groundColor.xyz, 1.0);

                half groundLuma = dot(baseColor.rgb, half3(0.299h, 0.587h, 0.114h));
                half baseLuma = dot(_BaseColor.rgb, half3(0.299h, 0.587h, 0.114h));

                half4 baseFinal, tipFinal;
                if (groundLuma > baseLuma)
                {
                    baseFinal = lerp(_BaseColor, half4(0.0h, 0.0h, 0.0h, 1.0h), 0.25h);
                    tipFinal = _BaseColor;
                }
                else
                {
                    baseFinal = baseColor;
                    tipFinal = lerp(baseColor, _BaseColor, _GrassTipBlend);
                }
                output.color = lerp(baseFinal, tipFinal, tipFactor);
                output.color.a = lerp(0.0h, 1.0h, tipFactor);
                output.color.a *= distanceFade;
                UNITY_TRANSFER_FOG(output, output.positionCS);
                return output;
            }

            half4 GrassBladeFragment(GrassBladeVaryings input) : SV_Target
            {
                clip(input.color.a - 0.02h);
                half4 color = input.color;
                UNITY_APPLY_FOG(input.fogCoord, color);
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
