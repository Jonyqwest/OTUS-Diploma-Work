#ifndef SNOW_OVERLAY_CGINC
#define SNOW_OVERLAY_CGINC

#pragma vertex vert_snow
#pragma fragment frag_snow
#pragma target 3.0
#include "UnityCG.cginc"

struct appdata_snow
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
};

struct v2f_snow
{
    float4 pos : SV_POSITION;
    float3 worldPos : TEXCOORD0;
    float3 worldNormal : TEXCOORD1;
    float2 uv : TEXCOORD2;
    float snowCoverage : TEXCOORD3;
};

sampler2D _MainTex;
float _MainTexScale;

sampler2D _SnowTex;
float4 _SnowColor;
float _SnowThreshold;
float _SnowMaxSlope;
float _SnowScale;

float3 _DeformOrigin;
float _DeformRadius;
float _DeformDepth;

v2f_snow vert_snow(appdata_snow v)
{
    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    float dist = distance(worldPos.xz, _DeformOrigin.xz);
    float deform = smoothstep(_DeformRadius, 0, dist) * _DeformDepth;

    v.vertex.y -= deform;

    v2f_snow o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.worldNormal = UnityObjectToWorldNormal(v.normal);
    o.uv = v.uv * _MainTexScale;
    o.snowCoverage = 1.0 - saturate(deform / max(_DeformDepth, 0.001));

    return o;
}

float4 frag_snow(v2f_snow i) : SV_Target
{
    return float4(1, 0, 0, 1);
}

#endif
