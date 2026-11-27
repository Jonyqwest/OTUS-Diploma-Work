#ifndef HYBRID_TRIPLANAR_CORE_CGINC
#define HYBRID_TRIPLANAR_CORE_CGINC

// Entry point and execution order for the terrain shader.

#pragma vertex vert
#pragma fragment frag
#pragma target 3.0
#pragma multi_compile_fog
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _SHADOWS_SOFT

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "HybridTerrainCommon.cginc"
#include "SatelliteTerrain.cginc"
#include "TriplanarTerrain.cginc"
#include "TerrainLighting.cginc"

v2f vert(appdata input)
{
    return BuildTerrainVaryings(input);
}

float4 frag(v2f input) : SV_Target
{
    float3 sourceNormal = normalize(input.worldNormal);
    float slope = TerrainSlope(sourceNormal);

    float3 satelliteColor = SampleSatelliteTerrain(input.uv, input.worldPos);
    float3 terrainColor = ApplyTerrainSlopeDetail(
        satelliteColor,
        input.worldPos,
        sourceNormal,
        slope);

    float3 terrainNormal = BuildTerrainNormal(input.worldPos, sourceNormal, slope);
    float3 litColor = ApplyTerrainLighting(terrainColor, terrainNormal, input.worldPos);

    float4 outputColor = float4(litColor, 1.0);
    outputColor.rgb = MixFog(outputColor.rgb, input.fogFactor);
    return outputColor;
}

#endif
