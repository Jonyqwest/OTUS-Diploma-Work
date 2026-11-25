Shader "Hidden/SuperTextureCombine"
{
    Properties
    {
        _Src00 ("Src00", 2D) = "white" {}
        _Src01 ("Src01", 2D) = "white" {}
        _Src10 ("Src10", 2D) = "white" {}
        _Src11 ("Src11", 2D) = "white" {}
        _InvertY ("Invert Y (web tiles)", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _Src00;
            sampler2D _Src01;
            sampler2D _Src10;
            sampler2D _Src11;
            float _InvertY;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 SampleTex(sampler2D tex, float2 uv) {
                return tex2D(tex, uv);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                // uv in [0..1] over output texture
                // split into 4 quadrants:
                // 0: left-top, 1: right-top, 2: left-bottom, 3: right-bottom
                // Convention: _Src00 = top-left, _Src10 = top-right, _Src01 = bottom-left, _Src11 = bottom-right
                // But web tiles often invert Y; _InvertY allows flipping when needed (1 - y).
                float2 uvScaled = uv * 2.0;
                if (uv.x < 0.5)
                {
                    if (uv.y >= 0.5)
                    {
                        // top-left quadrant (uv.x 0..0.5, uv.y 0.5..1)
                        float2 local = float2(uv.x * 2.0, (uv.y - 0.5) * 2.0);
                        if (_InvertY > 0.5) local.y = 1.0 - local.y;
                        return SampleTex(_Src00, local);
                    }
                    else
                    {
                        // bottom-left quadrant (uv.x 0..0.5, uv.y 0..0.5)
                        float2 local = float2(uv.x * 2.0, uv.y * 2.0);
                        if (_InvertY > 0.5) local.y = 1.0 - local.y;
                        return SampleTex(_Src01, local);
                    }
                }
                else
                {
                    if (uv.y >= 0.5)
                    {
                        // top-right quadrant
                        float2 local = float2((uv.x - 0.5) * 2.0, (uv.y - 0.5) * 2.0);
                        if (_InvertY > 0.5) local.y = 1.0 - local.y;
                        return SampleTex(_Src10, local);
                    }
                    else
                    {
                        // bottom-right quadrant
                        float2 local = float2((uv.x - 0.5) * 2.0, uv.y * 2.0);
                        if (_InvertY > 0.5) local.y = 1.0 - local.y;
                        return SampleTex(_Src11, local);
                    }
                }
            }
            ENDCG
        }
    }
}
