#ifndef TERRAIN_OVERLAY_COMMON_CGINC
#define TERRAIN_OVERLAY_COMMON_CGINC

struct TerrainOverlayVertex
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
};

struct TerrainOverlayVaryings
{
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 worldNormal : TEXCOORD1;
    float3 worldPos : TEXCOORD2;
    half fogFactor : TEXCOORD3;
};

struct TerrainOverlayLayer
{
    half3 color;
    half alpha;
};

sampler2D _MainTex;

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    half _AlphaClip;

    // Snow parameters
    half4 _SnowColor;
    half _SnowThreshold;
    half _SnowSoftness;
    half _SlopeLimit;
    float _SnowAltitude;
    float _AltitudeFade;

    // Grass parameters
    half _GrassEnabled;
    half _GrassThresholdGR;
    half _GrassThresholdBG;
    half _GrassSoftness;
    half _GrassSlopeLimit;
    half _GrassDarknessThreshold;

    half4 _GrassLocationColor;
    half _GrassLocationOpacity;
CBUFFER_END

TerrainOverlayVaryings BuildTerrainOverlayVaryings(TerrainOverlayVertex input)
{
    TerrainOverlayVaryings output;
    output.position = TransformObjectToHClip(input.vertex.xyz);
    output.uv = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
    output.worldNormal = TransformObjectToWorldNormal(input.normal);
    output.worldPos = TransformObjectToWorld(input.vertex.xyz);
    output.fogFactor = ComputeFogFactor(output.position.z);
    return output;
}

half TerrainSurfaceSlope(float3 worldNormal)
{
    return 1.0h - abs(dot(normalize(worldNormal), float3(0.0, 1.0, 0.0)));
}

half TerrainSurfaceLuminance(half3 color)
{
    return dot(color, half3(0.299h, 0.587h, 0.114h));
}

half TerrainSlopeMask(half slope, half slopeLimit)
{
    return 1.0h - smoothstep(0.0h, max(slopeLimit, 0.001h), slope);
}

#endif
