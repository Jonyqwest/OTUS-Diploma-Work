#ifndef TERRAIN_SURFACE_MASKS_CGINC
#define TERRAIN_SURFACE_MASKS_CGINC

// Color classification is kept separate from rendering so the same masks can
// later drive instanced grass, snow particles or gameplay data.

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

half TerrainGreenMask(half3 satelliteColor, half thresholdGR, half thresholdBG, half softness, half darknessThreshold)
{
    half maxChannel = max(satelliteColor.r, max(satelliteColor.g, satelliteColor.b));
    half minChannel = min(satelliteColor.r, min(satelliteColor.g, satelliteColor.b));
    half saturation = (maxChannel - minChannel) / max(maxChannel, 0.001h);
    
    half diffGR = satelliteColor.g - satelliteColor.r;
    half diffBG = satelliteColor.g - satelliteColor.b;
    
    half maskGR = smoothstep(thresholdGR - softness, thresholdGR + softness, diffGR);
    half maskBG = smoothstep(thresholdBG - softness, thresholdBG + softness, diffBG);
    half hueMask = maskGR * maskBG;
    
    half luminance = TerrainSurfaceLuminance(satelliteColor);
    half saturationMask = smoothstep(0.08h, 0.25h, saturation);
    half visibleDarkMask = smoothstep(darknessThreshold, darknessThreshold + 0.12h, luminance);
    half visibleBrightMask = 1.0h - smoothstep(0.80h, 0.98h, luminance);

    return hueMask * saturationMask * visibleDarkMask * visibleBrightMask;
}

#endif
