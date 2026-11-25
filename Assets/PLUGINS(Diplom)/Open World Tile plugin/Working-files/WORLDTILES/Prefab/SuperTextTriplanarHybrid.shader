Shader "Custom/HybridTriplanarSuperTexture"
{
    Properties
    {
        // Основные спутниковые текстуры
        _MainTex("Satellite Texture", 2D) = "white" {}
        _SuperTex("Super Satellite Texture", 2D) = "white" {}

        _BlendValue("Blend Value", Range(0,1)) = 0         // для отладки
        _BlendSpeed("Blend Speed", Range(0.1, 5.0)) = 1.0  // скорость перехода
        _BlendStartTime("Blend Start Time", Float) = 0.0   // время начала перехода
        _BlendDirection("Blend Direction", Float) = 1.0    // 1 = к супер, -1 = обратно

        // Детализация
        _DetailTex("Detail (Triplanar) Texture", 2D) = "gray" {}
        _MainTexScale("Main Tex Scale", Float) = 1.0
        _DetailScale("Detail Scale", Float) = 50.0
        _BlendSharpness("Blend Sharpness", Range(1,8)) = 4
        _DetailStrength("Detail Strength", Range(0,1)) = 0.6
        _SlopeThreshold("Slope Start Threshold", Range(0.0, 0.5)) = 0.08
        _SlopeSmoothness("Slope Smoothness", Range(0.01, 0.5)) = 0.12
        _TriplanarMaxDistance("Triplanar Max Distance (m)", Float) = 400.0
        _TriplanarFadeRange("Triplanar Fade Range (m)", Float) = 80.0

        // Глобальная карта нормалей
        _NormalMap("Global Normal Map", 2D) = "bump" {}
        _NormalScale("Global Normal Scale", Float) = 5.0
        _NormalStrength("Global Normal Strength", Range(0,1)) = 0.5

        // Трипланерная нормаль
        _TriplanarNormal("Triplanar Normal Map", 2D) = "bump" {}
        _TriplanarNormalScale("Triplanar Normal Scale", Float) = 10.0
        _TriplanarNormalStrength("Triplanar Normal Strength", Range(0,1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

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
            };

            // --- Параметры для смешивания текстур ---
            sampler2D _MainTex;
            sampler2D _SuperTex;
            float _BlendValue;
            float _BlendSpeed;
            float _BlendStartTime;
            float _BlendDirection;

            // --- Остальные карты ---
            sampler2D _DetailTex;
            float _MainTexScale;
            float _DetailScale;
            float _BlendSharpness;
            float _DetailStrength;

            sampler2D _NormalMap;
            float _NormalScale;
            float _NormalStrength;

            sampler2D _TriplanarNormal;
            float _TriplanarNormalScale;
            float _TriplanarNormalStrength;

            float3 UnpackNormal_Custom(float4 packedNormal)
            {
                float3 n;
                n.xy = packedNormal.xy * 2.0 - 1.0;
                n.z = sqrt(saturate(1.0 - dot(n.xy, n.xy)));
                return n;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv * _MainTexScale; // UV для спутниковых текстур
                return o;
            }

            float3 SampleTriplanar(sampler2D tex, float3 worldPos, float3 normal, float scale)
            {
                normal = normalize(normal);
                float3 blend = pow(abs(normal), _BlendSharpness);
                blend /= (blend.x + blend.y + blend.z + 1e-5);

                float2 uvX = worldPos.yz / scale;
                float2 uvY = worldPos.xz / scale;
                float2 uvZ = worldPos.xy / scale;

                float3 xProj = tex2D(tex, uvX).rgb;
                float3 yProj = tex2D(tex, uvY).rgb;
                float3 zProj = tex2D(tex, uvZ).rgb;

                return xProj * blend.x + yProj * blend.y + zProj * blend.z;
            }

            float3 SampleTriplanarNormal(sampler2D tex, float3 worldPos, float3 normal, float scale)
            {
                normal = normalize(normal);
                float3 blend = pow(abs(normal), _BlendSharpness);
                blend /= (blend.x + blend.y + blend.z + 1e-5);

                float2 uvX = worldPos.yz / scale;
                float2 uvY = worldPos.xz / scale;
                float2 uvZ = worldPos.xy / scale;

                float3 nX = UnpackNormal_Custom(tex2D(tex, uvX));
                float3 nY = UnpackNormal_Custom(tex2D(tex, uvY));
                float3 nZ = UnpackNormal_Custom(tex2D(tex, uvZ));

                float3 blended = normalize(nX * blend.x + nY * blend.y + nZ * blend.z);
                return blended;
            }

            float4 frag(v2f i) : SV_Target
            {
                // --- Плавное вычисление blend внутри шейдера ---
                float timePassed = _Time.y - _BlendStartTime;
                float fade = saturate(timePassed * _BlendSpeed);
                float dynamicBlend = (_BlendDirection > 0) ? fade : (1.0 - fade);

                // можно оставить отладочный _BlendValue (например, принудительно задавать его)
                float blendValue = lerp(_BlendValue, dynamicBlend, 0.9);

                float3 normal = normalize(i.worldNormal);
                float3 up = float3(0,1,0);
                float slope = 1.0 - saturate(dot(normal, up));

                // --- Основные спутниковые текстуры ---
                float3 mainColA = tex2D(_MainTex, i.uv).rgb;
                float3 mainColB = tex2D(_SuperTex, i.uv).rgb;
                float3 mainCol = lerp(mainColA, mainColB, blendValue);

                // --- Детализация ---
                float3 detailCol = SampleTriplanar(_DetailTex, i.worldPos, normal, _DetailScale);
                float3 finalCol = lerp(mainCol, detailCol, pow(slope, _BlendSharpness) * _DetailStrength);

                // --- Нормали ---
                float2 normalUV = i.worldPos.xz / _NormalScale;
                float3 globalNormal = UnpackNormal_Custom(tex2D(_NormalMap, normalUV));
                float3 triNormal = SampleTriplanarNormal(_TriplanarNormal, i.worldPos, normal, _TriplanarNormalScale);
                float3 blendedNormal = normalize(lerp(globalNormal, triNormal, slope * _TriplanarNormalStrength));
                normal = normalize(lerp(normal, blendedNormal, _NormalStrength));

                // --- Простое освещение ---
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(normal, lightDir));
                float3 litColor = finalCol * (0.3 + 0.7 * NdotL);

                return float4(litColor, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
