Shader "Custom/TerrainOverlays/CustomPaintOverlay"
{
    Properties
    {
        _PaintTex("Paint Control Map", 2D) = "black" {}
        _PathTex("Path Texture", 2D) = "gray" {}
        _GrassTex("Grass Texture", 2D) = "gray" {}
        _DirtTex("Dirt Texture", 2D) = "gray" {}
        _WaterTex("Water Texture", 2D) = "blue" {}
        _TileSize("Global Detail Tile Size (world units)", Float) = 10
    }

    SubShader
    {
        Tags
        {
            "Queue"="Geometry+1"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }
        ZTest LEqual
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        Offset -1, -1

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex PaintOverlayVert
            #pragma fragment PaintOverlayFrag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _PaintTex;
            sampler2D _PathTex;
            sampler2D _GrassTex;
            sampler2D _DirtTex;
            sampler2D _WaterTex;

            CBUFFER_START(UnityPerMaterial)
                float _TileSize;
                float4 _PathTex_ST;
                float4 _GrassTex_ST;
                float4 _DirtTex_ST;
                float4 _WaterTex_ST;
            CBUFFER_END

            Varyings PaintOverlayVert(Attributes input)
            {
                Varyings output;
                output.position = TransformObjectToHClip(input.vertex.xyz);
                output.uv = input.uv;
                output.worldPos = TransformObjectToWorld(input.vertex.xyz);
                return output;
            }

            half4 PaintOverlayFrag(Varyings input) : SV_Target
            {
                half4 control = tex2D(_PaintTex, input.uv);

                half waterWeight = 1.0h - control.a;
                half landWeight = control.r + control.g + control.b;

                half alpha = saturate(max(waterWeight, landWeight));
                clip(alpha - 0.001h);

                float2 baseWorldUV = input.worldPos.xz / max(_TileSize, 0.001f);

                float2 pathUV  = baseWorldUV * _PathTex_ST.xy + _PathTex_ST.zw;
                float2 grassUV = baseWorldUV * _GrassTex_ST.xy + _GrassTex_ST.zw;
                float2 dirtUV  = baseWorldUV * _DirtTex_ST.xy + _DirtTex_ST.zw;
                float2 waterUV = baseWorldUV * _WaterTex_ST.xy + _WaterTex_ST.zw;

                if (waterWeight > landWeight)
                {
                    half3 water = tex2D(_WaterTex, waterUV).rgb;
                    return half4(water, saturate(waterWeight));
                }

                half3 path = tex2D(_PathTex, pathUV).rgb;
                half3 grass = tex2D(_GrassTex, grassUV).rgb;
                half3 dirt = tex2D(_DirtTex, dirtUV).rgb;

                half3 color = (path * control.r + grass * control.g + dirt * control.b) / max(landWeight, 0.001h);

                return half4(color, saturate(landWeight));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
