Shader "Custom/ShineRainbow"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _Center ("Rainbow Center (UV)", Vector) = (0.5,0.5,0,0)
        _Radius ("Base Radius (UV)", Range(0,1)) = 0.25
        _Thickness ("Ring Thickness", Range(0.001,0.3)) = 0.03
        _Rings ("Rings (1-4)", Range(1,4)) = 3
        _Spacing ("Ring Spacing", Range(0.01,0.5)) = 0.06
        _Intensity ("Intensity", Range(0,4)) = 1.6
        _Saturation ("Saturation", Range(0,2)) = 1.0
        _Speed ("Hue Speed", Range(-2,2)) = 0.25
        _Softness ("Edge Softness", Range(0.0,1.0)) = 0.15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _Center;
            float _Radius;
            float _Thickness;
            float _Rings;
            float _Spacing;
            float _Intensity;
            float _Saturation;
            float _Speed;
            float _Softness;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            // HSV to RGB (h in [0,1], s in [0,1], v in [0,1])
            float3 HSVtoRGB(float3 c)
            {
                float h = c.x;
                float s = c.y;
                float v = c.z;
                float3 rgb = float3(0,0,0);
                if (s <= 0.0001)
                {
                    rgb = float3(v,v,v);
                }
                else
                {
                    h = frac(h) * 6.0;
                    int i = (int)floor(h);
                    float f = h - i;
                    float p = v * (1.0 - s);
                    float q = v * (1.0 - s * f);
                    float t = v * (1.0 - s * (1.0 - f));
                    if (i == 0) rgb = float3(v,t,p);
                    else if (i == 1) rgb = float3(q,v,p);
                    else if (i == 2) rgb = float3(p,v,t);
                    else if (i == 3) rgb = float3(p,q,v);
                    else if (i == 4) rgb = float3(t,p,v);
                    else rgb = float3(v,p,q);
                }
                return rgb;
            }

            float smoothRing(float dist, float ringRadius, float thickness)
            {
                float d = abs(dist - ringRadius);
                float edge = thickness * (1.0 - _Softness);
                // soft transition
                return 1.0 - smoothstep(edge, thickness + edge, d);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseCol = tex2D(_MainTex, i.uv) * _Color;

                // Only show effect where sprite has some alpha
                float baseAlpha = baseCol.a;

                float2 uv = i.uv;
                float2 center = _Center.xy;

                // Direction from center; works in UV space. If sprite UVs are stretched, user can adjust _Radius accordingly.
                float2 dir = uv - center;
                float dist = length(dir);

                float2 norm = dir / max(dist, 1e-6);
                float angle = atan2(dir.y, dir.x); // -PI..PI

                // time-driven hue shift
                float t = _Time.y * _Speed;

                float accumA = 0.0;
                float3 accumRGB = float3(0,0,0);

                // For up to 4 rings, sum
                int rings = (int)round(_Rings);
                for (int r = 0; r < rings; ++r)
                {
                    float ringRadius = _Radius + r * _Spacing;
                    float mask = smoothRing(dist, ringRadius, _Thickness);

                    // slight falloff by distance to keep outer rings softer
                    float falloff = saturate(1.0 - (dist / (_Radius + (_Rings-1)*_Spacing + 0.001)));

                    float hue = frac((angle / 6.2831853) + t + r * 0.18);
                    float sat = saturate(_Saturation);
                    float val = 1.0;

                    float3 rgb = HSVtoRGB(float3(hue, sat, val));

                    float contribution = mask * falloff * _Intensity * baseAlpha;

                    accumRGB += rgb * contribution;
                    accumA += contribution * 0.6; // reduce alpha contribution so base sprite still visible
                }

                // Mix additive rainbow with base color
                float3 outRGB = baseCol.rgb + accumRGB;
                float outA = baseAlpha;

                // Prevent HDR runaway
                outRGB = saturate(outRGB);

                return fixed4(outRGB, outA);
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
