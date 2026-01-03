Shader "Custom/URP/ParticleAdditiveBloom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color (HDR)", Color) = (2,2,2,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ParticleAdditive"
            Blend One One        // 🔥 ADDITIVE
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color; // vertex color * HDR
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 tex = tex2D(_MainTex, IN.uv);
                return tex * IN.color; // 👉 giá trị >1 → Bloom
            }
            ENDHLSL
        }
    }
}
