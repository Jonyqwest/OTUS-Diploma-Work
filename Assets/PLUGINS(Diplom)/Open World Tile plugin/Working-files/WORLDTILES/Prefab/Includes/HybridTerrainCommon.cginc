#ifndef HYBRID_TERRAIN_COMMON_CGINC
#define HYBRID_TERRAIN_COMMON_CGINC

// Shared vertex data, CBUFFER and math helpers used by all terrain modules.

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 pos : SV_POSITION;
    float3 worldPos : TEXCOORD0;
    float3 worldNormal : TEXCOORD1;
    float2 uv : TEXCOORD2;
    half fogFactor : TEXCOORD3;
};

CBUFFER_START(UnityPerMaterial)
    float _MainTexScale;

    float _LowTexMinDistance;
    float _LowTexMaxDistance;

    float _BlendValue;
    float _BlendSpeed;
    float _BlendStartTime;
    float _BlendDirection;

    float _NearBlurDistance;
    float _FarBlurDistance;
    float _BlurStrength;
    float _DebugOnlyLowTex;

    float _DetailScale;
    float _BlendSharpness;
    float _DetailStrength;
    float _SlopeThreshold;
    float _SlopeSmoothness;
    float _TriplanarMaxDistance;
    float _TriplanarFadeRange;

    float _NormalScale;
    float _NormalStrength;
    float _TriplanarNormalScale;
    float _TriplanarNormalStrength;

    float4 _PathTex_ST;
    float4 _GrassTex_ST;
    float4 _DirtTex_ST;
    float4 _WaterTex_ST;
    float _PaintTileSize;

    float4 _SnowColor;
    float _SnowThreshold;
    float _SnowSoftness;
    float _SlopeLimit;
    float _SnowAltitude;
    float _AltitudeFade;
CBUFFER_END

float3 UnpackTerrainNormal(float4 packedNormal)
{
    float3 normal;
    normal.xy = packedNormal.xy * 2.0 - 1.0;
    normal.z = sqrt(saturate(1.0 - dot(normal.xy, normal.xy)));
    return normal;
}

float TerrainSlope(float3 worldNormal)
{
    return 1.0 - saturate(dot(normalize(worldNormal), float3(0.0, 1.0, 0.0)));
}

v2f BuildTerrainVaryings(appdata input)
{
    v2f output;
    output.pos = TransformObjectToHClip(input.vertex.xyz);
    output.worldPos = TransformObjectToWorld(input.vertex.xyz);
    output.worldNormal = TransformObjectToWorldNormal(input.normal);
    output.uv = input.uv * _MainTexScale;
    output.fogFactor = ComputeFogFactor(output.pos.z);
    return output;
}

#endif
