#ifndef TERRAIN_CUSTOM_PAINT_MODULE_CGINC
#define TERRAIN_CUSTOM_PAINT_MODULE_CGINC

// Custom paint overlay module: path, grass, dirt, water texture blending

sampler2D _PaintTex;
sampler2D _PathTex;
sampler2D _GrassTex;
sampler2D _DirtTex;
sampler2D _WaterTex;

float3 ApplyCustomPaintModule(float3 baseColor, float2 uv, float3 worldPos)
{
    half4 control = tex2D(_PaintTex, uv);

    half waterWeight = 1.0h - control.a;
    half landWeight = control.r + control.g + control.b;
    half totalWeight = saturate(max(waterWeight, landWeight));

    [branch]
    if (totalWeight <= 0.001h)
        return baseColor;

    float tileSize = max(_PaintTileSize, 0.001f);
    float2 baseWorldUV = worldPos.xz / tileSize;

    float2 pathUV  = baseWorldUV * _PathTex_ST.xy + _PathTex_ST.zw;
    float2 grassUV = baseWorldUV * _GrassTex_ST.xy + _GrassTex_ST.zw;
    float2 dirtUV  = baseWorldUV * _DirtTex_ST.xy + _DirtTex_ST.zw;
    float2 waterUV = baseWorldUV * _WaterTex_ST.xy + _WaterTex_ST.zw;

    float3 paintColor;
    if (waterWeight > landWeight)
    {
        paintColor = tex2D(_WaterTex, waterUV).rgb;
    }
    else
    {
        float3 path = tex2D(_PathTex, pathUV).rgb;
        float3 grass = tex2D(_GrassTex, grassUV).rgb;
        float3 dirt = tex2D(_DirtTex, dirtUV).rgb;
        paintColor = (path * control.r + grass * control.g + dirt * control.b) / max(landWeight, 0.001h);
    }

    return lerp(baseColor, paintColor, totalWeight);
}

#endif
