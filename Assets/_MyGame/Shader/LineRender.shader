Shader "Custom/MovingDissolveLine_Arc_Blur_Glow"
{
    Properties
    {
        _Color ("Line Color", Color) = (1, 0.8, 0.3, 1)

        _NoiseScale ("Noise Scale", Float) = 6
        _NoiseSpeed ("Noise Speed", Float) = 1.2

        _Dissolve ("Dissolve Amount", Range(0,1)) = 0.45
        _EdgeSoftness ("Edge Softness", Range(0.01,0.3)) = 0.12
        _LineScale ("Arc Length Scale", Float) = 0.12

        _BlurStrength ("Blur Strength", Range(0,0.05)) = 0.015

        _GlowColor ("Glow Color", Color) = (1, 0.9, 0.5, 1)
        _GlowWidth ("Glow Width", Range(0.01,0.2)) = 0.08
        _GlowIntensity ("Glow Intensity", Float) = 2.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
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
                float2 arc : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float arc : TEXCOORD1;
            };

            float4 _Color;
            float _NoiseScale, _NoiseSpeed;
            float _Dissolve, _EdgeSoftness, _LineScale;
            float _BlurStrength;

            float4 _GlowColor;
            float _GlowWidth, _GlowIntensity;

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float SampleNoise(float2 uv, float offset)
            {
                return hash21(uv + offset);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.arc = v.arc.x;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ===== ARC-LENGTH NOISE UV =====
                float2 baseUV;
                baseUV.x = i.arc * _LineScale - _Time.y * _NoiseSpeed;
                baseUV.y = i.uv.y;
                baseUV *= _NoiseScale;

                // ===== BLUR NOISE =====
                float n0 = SampleNoise(baseUV, 0);
                float n1 = SampleNoise(baseUV, _BlurStrength);
                float n2 = SampleNoise(baseUV, -_BlurStrength);
                float noise = (n0 + n1 + n2) / 3.0;

                // ===== DISSOLVE MASK =====
                float dissolveMask = smoothstep(
                    _Dissolve,
                    _Dissolve + _EdgeSoftness,
                    noise
                );

                if (dissolveMask < 0.01)
                    discard;

                // ===== GLOW EDGE =====
                float edge = abs(noise - _Dissolve);
                float glowMask = smoothstep(_GlowWidth, 0, edge);

                fixed4 col = _Color;
                col.a *= dissolveMask;

                // add glow (emissive-style)
                col.rgb += _GlowColor.rgb * glowMask * _GlowIntensity;

                return col;
            }
            ENDCG
        }
    }
}
