Shader "Hidden/CropUV"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _CropUV ("Crop UV", Vector) = (0, 0, 1, 1) // x, y, w, h в UV
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _CropUV; // x, y, w, h

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // пересчёт UV в границы кропа
                o.uv = v.uv * _CropUV.zw + _CropUV.xy;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
