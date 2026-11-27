#ifndef TERRAIN_GRASS_LAYER_CGINC
#define TERRAIN_GRASS_LAYER_CGINC

TerrainOverlayLayer BuildTerrainGrassLayer(
    half3 satelliteColor,
    half slope)
{
    TerrainOverlayLayer layer;
    layer.color = half3(0, 0, 0);
    layer.alpha = 0.0h;

    [branch]
    if (_GrassEnabled <= 0.001h)
        return layer;

    half greenMask = TerrainGreenMask(
        satelliteColor,
        _GrassThresholdGR,
        _GrassThresholdBG,
        _GrassSoftness,
        _GrassDarknessThreshold);
    half slopeMask = TerrainSlopeMask(slope, _GrassSlopeLimit);
    half grassMask = greenMask * slopeMask;

    [branch]
    if (grassMask <= 0.001h)
        return layer;

    half finalAlpha = smoothstep(0.15h, 0.65h, grassMask);

    [branch]
    if (_GrassLocationOpacity > 0.001h)
    {
        layer.color = _GrassLocationColor.rgb;
        layer.alpha = finalAlpha * _GrassLocationOpacity;
    }

    return layer;
}

#endif
