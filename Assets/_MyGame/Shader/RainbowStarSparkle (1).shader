Shader "Custom/RainbowStarSparkle"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        _CoreIntensity ("Core Intensity", Range(0, 10)) = 2.0
        _StarSharpness ("Star Sharpness", Range(1, 20)) = 8.0
        _StarPoints ("Star Points", Range(2, 8)) = 4
        _StarRotation ("Star Rotation", Range(0, 360)) = 45
        
        [Header(RGB Ring Settings)]
        _RainbowIntensity ("Ring Intensity", Range(0, 2)) = 0.8
        _RainbowWidth ("Ring Width", Range(0.01, 0.5)) = 0.08
        _RainbowOffset ("Ring Radius", Range(0, 1)) = 0.35
        _ChromaticSpread ("RGB Separation", Range(0, 0.15)) = 0.04
        _RedStrength ("Red Strength", Range(0, 2)) = 1.0
        _GreenStrength ("Green Strength", Range(0, 2)) = 1.0
        _BlueStrength ("Blue Strength", Range(0, 2)) = 1.0
        
        [Header(Eclipse Settings)]
        _EclipseAmount ("Eclipse Amount", Range(0, 1)) = 0.0
        _EclipseAngle ("Eclipse Angle", Range(0, 360)) = 0
        _EclipseSoftness ("Eclipse Softness", Range(0.01, 1)) = 0.3
        
        [Header(Pulse Animation)]
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2.0
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.3
        _PulseMin ("Pulse Minimum", Range(0, 1)) = 0.7
        
        [Header(Secondary Star)]
        _SecondaryStarEnabled ("Enable Secondary Star", Float) = 1
        _SecondaryStarScale ("Secondary Star Scale", Range(0.1, 2)) = 0.6
        _SecondaryStarRotationOffset ("Secondary Rotation Offset", Range(0, 90)) = 22.5
        _SecondaryStarSharpness ("Secondary Sharpness", Range(1, 20)) = 4.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        
        Blend One One // Additive blending for glow effect
        ZWrite Off
        Cull Off
        
        Pass
        {
            Name "StarSparkle"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _CoreColor;
                float _CoreIntensity;
                float _StarSharpness;
                float _StarPoints;
                float _StarRotation;
                float _RainbowIntensity;
                float _RainbowWidth;
                float _RainbowOffset;
                float _ChromaticSpread;
                float _RedStrength;
                float _GreenStrength;
                float _BlueStrength;
                float _EclipseAmount;
                float _EclipseAngle;
                float _EclipseSoftness;
                float _PulseSpeed;
                float _PulseAmount;
                float _PulseMin;
                float _SecondaryStarEnabled;
                float _SecondaryStarScale;
                float _SecondaryStarRotationOffset;
                float _SecondaryStarSharpness;
            CBUFFER_END
            
            // Attempt GPU instancing for per-instance properties
            #ifdef UNITY_INSTANCING_ENABLED
                UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_INSTANCING_BUFFER_END(Props)
            #endif
            
            // Rotate UV coordinates
            float2 RotateUV(float2 uv, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float2(
                    uv.x * c - uv.y * s,
                    uv.x * s + uv.y * c
                );
            }
            
            // Star shape function
            float StarShape(float2 uv, float points, float sharpness, float rotation)
            {
                float2 centeredUV = uv - 0.5;
                float2 rotatedUV = RotateUV(centeredUV, rotation * PI / 180.0);
                
                float angle = atan2(rotatedUV.y, rotatedUV.x);
                float dist = length(centeredUV);
                
                // Create star pattern
                float starPattern = cos(angle * points) * 0.5 + 0.5;
                starPattern = pow(starPattern, sharpness);
                
                // Combine with radial falloff
                float radialFalloff = 1.0 - saturate(dist * 2.0);
                radialFalloff = pow(radialFalloff, 2.0);
                
                // Star rays
                float rays = starPattern * radialFalloff;
                
                // Add core glow
                float core = pow(1.0 - saturate(dist * 4.0), 3.0);
                
                return saturate(rays + core);
            }
            
            // RGB chromatic aberration ring - red outside, green middle, blue inside
            // Individual control over each color channel strength
            float3 ChromaticRing(float2 uv, float intensity, float width, float offset, float separation, float3 colorStrengths)
            {
                float2 centeredUV = uv - 0.5;
                float dist = length(centeredUV);
                
                // Separate RGB channels at different radii
                float redDist = abs(dist - (offset + separation));
                float greenDist = abs(dist - offset);
                float blueDist = abs(dist - (offset - separation));
                
                // Create soft rings for each channel
                float redRing = 1.0 - saturate(redDist / width);
                float greenRing = 1.0 - saturate(greenDist / width);
                float blueRing = 1.0 - saturate(blueDist / width);
                
                // Smooth falloff
                redRing = pow(redRing, 2.0);
                greenRing = pow(greenRing, 2.0);
                blueRing = pow(blueRing, 2.0);
                
                // Apply individual color strengths
                float3 finalRing = float3(
                    redRing * colorStrengths.r,
                    greenRing * colorStrengths.g,
                    blueRing * colorStrengths.b
                );
                
                return finalRing * intensity;
            }
            
            // Angular eclipse fade - fades part of the ring based on angle
            float EclipseFade(float2 uv, float eclipseAmount, float eclipseAngle, float softness)
            {
                if (eclipseAmount <= 0.0) return 1.0;
                
                float2 centeredUV = uv - 0.5;
                
                // Get angle of current pixel (0 to 2PI)
                float pixelAngle = atan2(centeredUV.y, centeredUV.x);
                
                // Convert eclipse angle to radians and offset
                float eclipseRad = eclipseAngle * PI / 180.0;
                
                // Calculate angular distance from eclipse center
                float angleDiff = pixelAngle - eclipseRad;
                
                // Wrap to -PI to PI range
                angleDiff = fmod(angleDiff + 3.0 * PI, 2.0 * PI) - PI;
                
                // Eclipse covers an arc based on eclipseAmount (0 = no eclipse, 1 = full circle hidden)
                float eclipseArc = eclipseAmount * PI; // Max arc is PI (half circle)
                
                // Create smooth fade based on angular distance
                float fade = smoothstep(eclipseArc - softness, eclipseArc + softness, abs(angleDiff));
                
                return fade;
            }
            
            // Chromatic aberration on star
            float3 ChromaticStar(float2 uv, float points, float sharpness, float rotation, float spread)
            {
                float2 center = float2(0.5, 0.5);
                float2 dir = normalize(uv - center);
                
                float r = StarShape(uv + dir * spread, points, sharpness, rotation);
                float g = StarShape(uv, points, sharpness, rotation);
                float b = StarShape(uv - dir * spread, points, sharpness, rotation);
                
                return float3(r, g, b);
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                float2 uv = input.uv;
                
                // Pulse animation
                float pulse = _PulseMin + _PulseAmount * (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5);
                
                // Main star with chromatic aberration
                float3 mainStar = ChromaticStar(uv, _StarPoints, _StarSharpness, _StarRotation, _ChromaticSpread * 0.5);
                
                // Secondary rotated star (for that classic sparkle look)
                float3 secondaryStar = float3(0, 0, 0);
                if (_SecondaryStarEnabled > 0.5)
                {
                    float2 scaledUV = (uv - 0.5) / _SecondaryStarScale + 0.5;
                    secondaryStar = ChromaticStar(scaledUV, _StarPoints, _SecondaryStarSharpness, 
                        _StarRotation + _SecondaryStarRotationOffset, _ChromaticSpread * 0.3) * 0.5;
                }
                
                // Combine stars
                float3 stars = mainStar + secondaryStar;
                
                // RGB chromatic ring effect
                float3 colorStrengths = float3(_RedStrength, _GreenStrength, _BlueStrength);
                float3 rgbRing = ChromaticRing(uv, _RainbowIntensity, _RainbowWidth, _RainbowOffset, _ChromaticSpread, colorStrengths);
                
                // Apply eclipse angular fade
                float eclipseMask = EclipseFade(uv, _EclipseAmount, _EclipseAngle, _EclipseSoftness);
                rgbRing *= eclipseMask;
                
                // Final color
                float3 finalColor = stars * _CoreColor.rgb * _CoreIntensity + rgbRing;
                finalColor *= pulse;
                
                // Alpha based on overall brightness
                float alpha = saturate(max(max(finalColor.r, finalColor.g), finalColor.b));
                
                return float4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
