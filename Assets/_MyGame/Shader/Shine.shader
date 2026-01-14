Shader "Custom/ShiningRainbowStar"
{
    Properties
    {
        _Intensity ("Intensity", Range(0,5)) = 2
        _Glow ("Glow", Range(0,2)) = 0.6
        _RingSize ("Ring Size", Range(0,1)) = 0.45
        _RingWidth ("Ring Width", Range(0.01,0.3)) = 0.12
        _SparkPower ("Spark Power", Range(1,10)) = 4
        _TimeSpeed ("Time Speed", Range(0,5)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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

            float _Intensity, _Glow;
            float _RingSize, _RingWidth;
            float _SparkPower, _TimeSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * 2 - 1;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float dist = length(uv);

                // ===== Rainbow Ring =====
                float ring = smoothstep(_RingSize + _RingWidth, _RingSize, dist)
                           * smoothstep(_RingSize - _RingWidth, _RingSize, dist);

                float hue = atan2(uv.y, uv.x) / 6.28318 + _Time.y * 0.1 * _TimeSpeed;
                float3 rainbow = saturate(abs(frac(hue + float3(0,0.33,0.66)) * 6 - 3) - 1);

                // ===== Star Spark =====
                float cross = pow(abs(uv.x * uv.y), _SparkPower);
                float spark = saturate(1 - cross * 30) * (1 - dist * 1.5);

                // ===== Glow =====
                float glow = exp(-dist * 4) * _Glow;

                float3 color =
                      ring * rainbow * _Intensity
                    + spark * float3(1,1,1) * 2
                    + glow;

                return float4(color, 1);
            }
            ENDCG
        }
    }
}
