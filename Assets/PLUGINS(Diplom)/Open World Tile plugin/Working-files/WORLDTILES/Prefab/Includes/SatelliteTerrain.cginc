#ifndef SATELLITE_TERRAIN_CGINC
#define SATELLITE_TERRAIN_CGINC

// Satellite imagery, distance-based LOD transitions, and near-field blur.

sampler2D _LowTex;
sampler2D _MainTex;
sampler2D _SuperTex;
float4 _MainTex_TexelSize;

float AnimatedSuperBlend()
{
    [branch]
    if (_BlendStartTime <= 0.001f)
        return _BlendValue;

    float fade = saturate((_Time.y - _BlendStartTime) * _BlendSpeed);
    float animatedValue = _BlendDirection > 0.0 ? fade : (1.0 - fade);
    return lerp(_BlendValue, animatedValue, 0.9);
}

float3 SampleMainSatellite(float2 clampedUV, float distanceToCamera)
{
    float3 mainColor = tex2D(_MainTex, clampedUV).rgb;

    float minDist = (_LowTexMinDistance > 0.0) ? _LowTexMinDistance : 250.0;
    float maxDist = (_LowTexMaxDistance > minDist) ? _LowTexMaxDistance : 400.0;

    [branch]
    if (distanceToCamera <= minDist)
        return mainColor;

    [branch]
    if (distanceToCamera >= maxDist)
        return tex2D(_LowTex, clampedUV).rgb;

    float lowFade = saturate((distanceToCamera - minDist) / max(maxDist - minDist, 1.0));
    float3 lowColor = tex2D(_LowTex, clampedUV).rgb;
    return lerp(mainColor, lowColor, lowFade);
}

float3 SampleNearSatellite(float2 uv, float blurInset, float2 blurStep)
{
    float3 blurredMainColor = (
        tex2D(_MainTex, clamp(uv + float2(-blurStep.x, -blurStep.y), blurInset, 1.0 - blurInset)).rgb +
        tex2D(_MainTex, clamp(uv + float2( blurStep.x, -blurStep.y), blurInset, 1.0 - blurInset)).rgb +
        tex2D(_MainTex, clamp(uv + float2(-blurStep.x,  blurStep.y), blurInset, 1.0 - blurInset)).rgb +
        tex2D(_MainTex, clamp(uv + float2( blurStep.x,  blurStep.y), blurInset, 1.0 - blurInset)).rgb
    ) * 0.25;

    return blurredMainColor;
}

float3 SampleFarSatellite(float2 clampedUV, float distanceToCamera)
{
    float superBlend = AnimatedSuperBlend();

    float3 mainColor = SampleMainSatellite(clampedUV, distanceToCamera);

    [branch]
    if (superBlend <= 0.001)
        return mainColor;

    float3 superColor = tex2D(_SuperTex, clampedUV).rgb;
    return lerp(mainColor, superColor, superBlend);
}

float3 SampleSatelliteTerrain(float2 uv, float3 worldPos)
{
    float blurInset = max(_MainTex_TexelSize.x, 0.0001);
    float2 clampedUV = clamp(uv, blurInset, 1.0 - blurInset);

    [branch]
    if (_DebugOnlyLowTex > 0.5)
        return tex2D(_LowTex, clampedUV).rgb;

    float nearBlur = (_FarBlurDistance > 0.01) ? _NearBlurDistance : 40.0;
    float farBlur = (_FarBlurDistance > 0.01) ? _FarBlurDistance : 100.0;
    float blurStr = (_FarBlurDistance > 0.01) ? _BlurStrength : 0.0;

    float distanceToCamera = distance(worldPos, _WorldSpaceCameraPos);

    float distanceBlend = saturate(
        (distanceToCamera - nearBlur) /
        max(farBlur - nearBlur, 0.01));

    [branch]
    if (distanceBlend >= 0.999 || blurStr <= 0.001)
        return SampleFarSatellite(clampedUV, distanceToCamera);

    float currentBlur = blurStr * (1.0 - distanceBlend);
    float2 blurStep = _MainTex_TexelSize.xy * currentBlur;
    float3 nearColor = SampleNearSatellite(uv, blurInset, blurStep);

    [branch]
    if (distanceBlend <= 0.001)
        return nearColor;

    float3 farColor = SampleFarSatellite(clampedUV, distanceToCamera);
    return lerp(nearColor, farColor, distanceBlend);
}

#endif
