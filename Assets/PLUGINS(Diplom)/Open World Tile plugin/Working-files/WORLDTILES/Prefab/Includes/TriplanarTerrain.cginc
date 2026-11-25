#ifndef TRIPLANAR_TERRAIN_CGINC
#define TRIPLANAR_TERRAIN_CGINC

// Rock/detail projection on slopes and the terrain normal-map composition.

sampler2D _DetailTex;
sampler2D _NormalMap;
sampler2D _TriplanarNormal;

float CalculateDistanceFade(float3 worldPos)
{
    float maxDist = (_TriplanarMaxDistance > 1.0) ? _TriplanarMaxDistance : 400.0;
    float fadeRange = (_TriplanarFadeRange > 1.0) ? _TriplanarFadeRange : 80.0;

    float dist = distance(worldPos, _WorldSpaceCameraPos);
    float startFade = maxDist - fadeRange;
    return 1.0 - smoothstep(startFade, maxDist, dist);
}

float3 TerrainTriplanarWeights(float3 worldNormal)
{
    float3 weights = pow(abs(normalize(worldNormal)), _BlendSharpness);
    return weights / (weights.x + weights.y + weights.z + 1e-5);
}

// Bi-Planar (2-sample) projection optimization for mobile VR GPUs
float3 SampleTerrainBiplanar(sampler2D textureSampler, float3 worldPos, float3 worldNormal, float scale)
{
    float3 absNormal = abs(normalize(worldNormal));

    float yWeight = absNormal.y;
    float sideX = absNormal.x;
    float sideZ = absNormal.z;

    float2 uvY = worldPos.xz / scale;
    float2 uvSide = (sideX > sideZ) ? (worldPos.yz / scale) : (worldPos.xy / scale);

    float3 colY = tex2D(textureSampler, uvY).rgb;
    float3 colSide = tex2D(textureSampler, uvSide).rgb;

    float maxSide = max(sideX, sideZ);
    float totalWeight = yWeight + maxSide + 1e-5;
    float wY = yWeight / totalWeight;
    float wSide = maxSide / totalWeight;

    return colY * wY + colSide * wSide;
}

float3 SampleTerrainBiplanarNormal(
    sampler2D textureSampler,
    float3 worldPos,
    float3 worldNormal,
    float scale)
{
    float3 absNormal = abs(normalize(worldNormal));

    float yWeight = absNormal.y;
    float sideX = absNormal.x;
    float sideZ = absNormal.z;

    float2 uvY = worldPos.xz / scale;
    float2 uvSide = (sideX > sideZ) ? (worldPos.yz / scale) : (worldPos.xy / scale);

    float3 normalY = UnpackTerrainNormal(tex2D(textureSampler, uvY));
    float3 normalSide = UnpackTerrainNormal(tex2D(textureSampler, uvSide));

    float maxSide = max(sideX, sideZ);
    float totalWeight = yWeight + maxSide + 1e-5;
    float wY = yWeight / totalWeight;
    float wSide = maxSide / totalWeight;

    return normalize(normalY * wY + normalSide * wSide);
}

float3 ApplyTerrainSlopeDetail(
    float3 satelliteColor,
    float3 worldPos,
    float3 worldNormal,
    float slope)
{
    float distFade = CalculateDistanceFade(worldPos);

    [branch]
    if (distFade <= 0.001)
        return satelliteColor;

    float threshold = max(_SlopeThreshold, 0.02);
    float smoothness = max(_SlopeSmoothness, 0.05);

    float slopeFactor = smoothstep(threshold, threshold + smoothness, slope);
    float detailMask = pow(slopeFactor, _BlendSharpness) * _DetailStrength * distFade;

    // Flat or distant areas use only the satellite image -> 0 triplanar samples!
    [branch]
    if (detailMask <= 0.001)
        return satelliteColor;

    float3 detailColor = SampleTerrainBiplanar(
        _DetailTex,
        worldPos,
        worldNormal,
        _DetailScale);

    // Overlay Detail Blend Mode:
    // Preserves original satellite color, hue, and overall lighting 100%.
    // Breaks up stretched vertical stripes by injecting rock micro-texture without turning sand into dark stone or brightening shadows.
    float3 detailOverlay = satelliteColor * saturate(detailColor * 2.0);
    return lerp(satelliteColor, detailOverlay, detailMask);
}

float3 BuildTerrainNormal(float3 worldPos, float3 worldNormal, float slope)
{
    worldNormal = normalize(worldNormal);

    [branch]
    if (_NormalStrength <= 0.001)
        return worldNormal;

    float2 normalUV = worldPos.xz / _NormalScale;
    float3 globalNormal = UnpackTerrainNormal(tex2D(_NormalMap, normalUV));

    float distFade = CalculateDistanceFade(worldPos);

    [branch]
    if (distFade <= 0.001)
        return normalize(lerp(worldNormal, globalNormal, _NormalStrength));

    float threshold = max(_SlopeThreshold, 0.02);
    float smoothness = max(_SlopeSmoothness, 0.05);
    float triplanarBlend = smoothstep(threshold, threshold + smoothness, slope) * _TriplanarNormalStrength * distFade;

    // On flat or distant areas the global normal is sufficient.
    [branch]
    if (triplanarBlend <= 0.001)
        return normalize(lerp(worldNormal, globalNormal, _NormalStrength));

    float3 triplanarNormal = SampleTerrainBiplanarNormal(
        _TriplanarNormal,
        worldPos,
        worldNormal,
        _TriplanarNormalScale);

    float3 detailNormal = normalize(lerp(
        globalNormal,
        triplanarNormal,
        triplanarBlend));

    return normalize(lerp(worldNormal, detailNormal, _NormalStrength));
}

#endif
