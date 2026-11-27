#ifndef TERRAIN_SURFACE_OVERLAY_CORE_CGINC
#define TERRAIN_SURFACE_OVERLAY_CORE_CGINC

#pragma vertex TerrainOverlayVert
#pragma fragment TerrainOverlayFrag
#pragma target 3.0
#pragma multi_compile_fog
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _SHADOWS_SOFT

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "TerrainOverlayCommon.cginc"
#include "TerrainSurfaceMasks.cginc"
#include "TerrainSnowLayer.cginc"
#include "TerrainGrassLayer.cginc"

TerrainOverlayVaryings TerrainOverlayVert(TerrainOverlayVertex input)
{
    return BuildTerrainOverlayVaryings(input);
}

half4 TerrainOverlayFrag(TerrainOverlayVaryings input) : SV_Target
{
    half3 satelliteColor = tex2D(_MainTex, input.uv).rgb;
    half slope = TerrainSurfaceSlope(input.worldNormal);

    TerrainOverlayLayer snow = BuildTerrainSnowLayer(
        satelliteColor,
        slope,
        input.worldPos.y);
    TerrainOverlayLayer grass = BuildTerrainGrassLayer(
        satelliteColor,
        slope);

    // Snow has priority on ambiguous bright-green pixels.
    grass.alpha *= 1.0h - snow.alpha;
    half combinedAlpha = snow.alpha + grass.alpha;
    clip(combinedAlpha - _AlphaClip);

    half3 combinedColor =
        (snow.color * snow.alpha + grass.color * grass.alpha) /
        max(combinedAlpha, 0.001h);

    Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.worldPos));
    half diffuse = saturate(dot(normalize(input.worldNormal), mainLight.direction));
    half directLighting = diffuse * mainLight.shadowAttenuation;
    combinedColor *= (0.3h + 0.7h * directLighting) * mainLight.color;

    half4 outputColor = half4(combinedColor, saturate(combinedAlpha));
    outputColor.rgb = MixFog(outputColor.rgb, input.fogFactor);
    return outputColor;
}

#endif
