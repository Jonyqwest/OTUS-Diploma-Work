#ifndef TERRAIN_LIGHTING_CGINC
#define TERRAIN_LIGHTING_CGINC

// Lightweight Lambert lighting. Fog is applied by the core because Unity's
// fog macro needs the interpolator declared in v2f.

float3 ApplyTerrainLighting(float3 albedo, float3 worldNormal, float3 worldPosition)
{
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(worldPosition));
    float diffuse = saturate(dot(normalize(worldNormal), mainLight.direction));
    float directLighting = diffuse * mainLight.shadowAttenuation;
    float3 lightFactor = (0.65 + 0.35 * directLighting) * mainLight.color;
    return albedo * lightFactor;
}

#endif
