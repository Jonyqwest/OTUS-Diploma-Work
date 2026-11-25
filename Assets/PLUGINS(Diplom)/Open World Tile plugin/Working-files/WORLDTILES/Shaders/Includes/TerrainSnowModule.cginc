#ifndef TERRAIN_SNOW_MODULE_CGINC
#define TERRAIN_SNOW_MODULE_CGINC

// Snow & Altitude layer parameters for URP Unified Terrain

half TerrainSurfaceLuminance(half3 color)
{
    return dot(color, half3(0.299h, 0.587h, 0.114h));
}

half TerrainWhiteMask(half3 satelliteColor, half threshold, half softness)
{
    half luminance = TerrainSurfaceLuminance(satelliteColor);
    half maxChannel = max(satelliteColor.r, max(satelliteColor.g, satelliteColor.b));
    half minChannel = min(satelliteColor.r, min(satelliteColor.g, satelliteColor.b));
    half saturation = (maxChannel - minChannel) / max(maxChannel, 0.001h);

    half brightnessMask = smoothstep(
        threshold - softness,
        threshold + softness,
        luminance);
    half neutralColorMask = 1.0h - smoothstep(0.10h, 0.35h, saturation);
    return brightnessMask * neutralColorMask;
}

float3 ApplySnowModule(float3 baseColor, float3 satelliteColor, float slope, float worldHeight)
{
    half whiteMask = TerrainWhiteMask(
        satelliteColor,
        _SnowThreshold,
        _SnowSoftness);
    half slopeMask = 1.0h - smoothstep(0.0h, max(_SlopeLimit, 0.001h), slope);
    half altitudeMask = smoothstep(
        _SnowAltitude,
        _SnowAltitude + max(_AltitudeFade, 0.001),
        worldHeight);
    half snowMask = whiteMask * slopeMask * altitudeMask;
    half snowAlpha = smoothstep(0.3h, 0.6h, snowMask) * _SnowColor.a;

    [branch]
    if (snowAlpha <= 0.001h)
        return baseColor;

    return lerp(baseColor, _SnowColor.rgb, snowAlpha);
}

#endif
