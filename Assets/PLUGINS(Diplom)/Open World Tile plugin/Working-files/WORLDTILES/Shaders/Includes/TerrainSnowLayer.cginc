#ifndef TERRAIN_SNOW_LAYER_CGINC
#define TERRAIN_SNOW_LAYER_CGINC

TerrainOverlayLayer BuildTerrainSnowLayer(
    half3 satelliteColor,
    half slope,
    float worldHeight)
{
    half whiteMask = TerrainWhiteMask(
        satelliteColor,
        _SnowThreshold,
        _SnowSoftness);
    half slopeMask = TerrainSlopeMask(slope, _SlopeLimit);
    half altitudeMask = smoothstep(
        _SnowAltitude,
        _SnowAltitude + max(_AltitudeFade, 0.001),
        worldHeight);
    half snowMask = whiteMask * slopeMask * altitudeMask;

    TerrainOverlayLayer layer;
    layer.color = _SnowColor.rgb;
    layer.alpha = smoothstep(0.3h, 0.6h, snowMask) * _SnowColor.a;
    return layer;
}

#endif
