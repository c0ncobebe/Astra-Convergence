Shader "Custom/GlitterSkyboxOptimized"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.1, 0.1, 0.2, 1)
        _GlitterColor ("Glitter Color", Color) = (1, 1, 1, 1)
        _NoiseTexture ("Noise Texture", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Vector) = (1, 1, 0, 0)
        _NoiseRotation ("Noise Rotation Speed", Float) = 0.1
        _GlitterIntensity ("Glitter Intensity", Range(0, 10)) = 2
        _GlitterThreshold ("Glitter Threshold", Range(0, 1)) = 0.7
        _GlitterSize ("Glitter Size", Range(0.001, 0.1)) = 0.01
        _MipBias ("Mip Bias (Anti-Flicker)", Range(-2, 2)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Background" 
            "RenderType"="Background" 
            "PreviewType"="Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            // Mobile optimization
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewDir : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            // Properties
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GlitterColor;
                float2 _NoiseScale;
                half _NoiseRotation;
                half _GlitterIntensity;
                half _GlitterThreshold;
                half _GlitterSize;
                half _MipBias;
            CBUFFER_END
            
            TEXTURE2D(_NoiseTexture);
            SAMPLER(sampler_NoiseTexture);
            
            // Rotation matrix 2D
            float2 Rotate2D(float2 uv, half angle)
            {
                half s = sin(angle);
                half c = cos(angle);
                return float2(
                    uv.x * c - uv.y * s,
                    uv.x * s + uv.y * c
                );
            }
            
            // Convert 3D direction to spherical UV (more stable than raw directions)
            float2 DirToSphericalUV(float3 dir)
            {
                // Normalize to ensure consistency
                dir = normalize(dir);
                
                // Convert to spherical coordinates with better precision
                half phi = atan2(dir.z, dir.x);
                half theta = asin(dir.y);
                
                // Map to UV space [0, 1]
                float2 uv;
                uv.x = phi / (2.0 * PI) + 0.5;
                uv.y = theta / PI + 0.5;
                
                return uv;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                // Transform to clip space
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                
                // View direction in world space (normalized for stability)
                output.viewDir = normalize(input.positionOS.xyz);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Normalize view direction for consistency (prevent precision issues)
                half3 viewDir = normalize(input.viewDir);
                
                // Convert to spherical UV coordinates (more stable than direct sampling)
                float2 baseUV = DirToSphericalUV(viewDir);
                
                // Apply noise scale
                float2 scaledUV = baseUV * _NoiseScale;
                
                // Rotate UV over time (use frac to keep values bounded)
                half timeRotation = frac(_Time.y * _NoiseRotation) * 2.0 * PI;
                float2 rotatedUV = Rotate2D(scaledUV, timeRotation);
                
                // Sample noise texture with explicit LOD bias to prevent flickering
                // Higher mip bias = softer, less flickering but slightly blurrier
                half4 noiseSample = SAMPLE_TEXTURE2D_LOD(_NoiseTexture, sampler_NoiseTexture, rotatedUV, _MipBias);
                
                // Extract noise value (use average for stability)
                half noiseValue = dot(noiseSample.rgb, half3(0.333, 0.333, 0.334));
                
                // Create glitter effect with smooth threshold
                half glitterMask = smoothstep(_GlitterThreshold - _GlitterSize, _GlitterThreshold, noiseValue);
                
                // Apply glitter intensity with saturation to prevent overbright
                half3 glitter = saturate(_GlitterColor.rgb * glitterMask * _GlitterIntensity);
                
                // Combine base color with glitter
                half3 finalColor = _BaseColor.rgb + glitter;
                
                return half4(finalColor, 1.0);
            }
            
            ENDHLSL
        }
    }
    
    FallBack "Skybox/Procedural"
}
