Shader "Custom/HolographicCard"
{
    Properties
    {
        [MainTexture] _MainTex("Card Art", 2D) = "white" {}
        [MainColor] _Color("Tint", Color) = (1,1,1,1)

        [Header(Holographic)]
        _HoloStrength("Holographic Strength", Range(0, 1)) = 0.5
        _HoloScale("Rainbow Scale", Range(1, 10)) = 3
        _HoloSpeed("Rainbow Speed", Range(0, 2)) = 0.5

        [Header(Fresnel)]
        _FresnelColor("Fresnel Color", Color) = (1, 1, 1, 1)
        _FresnelPower("Fresnel Power", Range(1, 10)) = 3
        _FresnelStrength("Fresnel Strength", Range(0, 1)) = 0.3

        [Header(Light Sweep)]
        _SweepColor("Sweep Color", Color) = (1, 1, 1, 1)
        _SweepSpeed("Sweep Speed", Range(0.1, 3)) = 1
        _SweepWidth("Sweep Width", Range(0.01, 0.3)) = 0.1
        _SweepStrength("Sweep Strength", Range(0, 1)) = 0.5
        _SweepFrequency("Sweep Frequency", Range(0.1, 1.0)) = 0.3
        _SweepDuration("Sweep Duration", Range(0.2, 1.0)) = 0.5

        [Header(Sparkle)]
        _SparkleColor("Sparkle Color", Color) = (1, 1, 1, 1)
        _SparkleScale("Sparkle Scale", Range(10, 200)) = 50
        _SparkleSpeed("Sparkle Speed", Range(1, 20)) = 10
        _SparkleStrength("Sparkle Strength", Range(0, 1)) = 0.8

        [Header(Card Tilt)]
        _TiltX("Tilt X", Range(-1, 1)) = 0
        _TiltY("Tilt Y", Range(-1, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;

                float _HoloStrength;
                float _HoloScale;
                float _HoloSpeed;

                half4 _FresnelColor;
                float _FresnelPower;
                float _FresnelStrength;

                half4 _SweepColor;
                float _SweepSpeed;
                float _SweepWidth;
                float _SweepStrength;
                float _SweepFrequency;
                float _SweepDuration;

                half4 _SparkleColor;
                float _SparkleScale;
                float _SparkleSpeed;
                float _SparkleStrength;

                float _TiltX;
                float _TiltY;
            CBUFFER_END

            // HSV to RGB conversion
            float3 HsvToRgb(float3 hsv)
            {
                float3 rgb = saturate(abs(fmod(hsv.x * 6.0 + float3(0.0, 4.0, 2.0), 6.0) - 3.0) - 1.0);
                return hsv.z * lerp(float3(1.0, 1.0, 1.0), rgb, hsv.y);
            }

            // Simple hash for sparkles
            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Value noise for organic randomness
            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);  // Smoothstep interpolation

                float a = Hash(i);
                float b = Hash(i + float2(1.0, 0.0));
                float c = Hash(i + float2(0.0, 1.0));
                float d = Hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Fractal Brownian Motion - layered noise
            float FBM(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;

                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * ValueNoise(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                return value;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float2 tilt = float2(_TiltX, _TiltY);

                // === BASE CARD ART ===
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                baseColor *= _Color;

                // Early out if transparent
                if (baseColor.a < 0.01)
                    discard;

                half3 finalColor = baseColor.rgb;

                // === RAINBOW / IRIDESCENCE ===
                // Use UV + tilt for view-dependent rainbow
                float2 rainbowUV = uv * _HoloScale + tilt * 2.0;
                float hue = rainbowUV.x + rainbowUV.y + _Time.y * _HoloSpeed;
                float3 rainbow = HsvToRgb(float3(frac(hue), 0.7, 1.0));

                // Apply rainbow based on luminance (brighter areas = more holo)
                float luma = dot(baseColor.rgb, float3(0.299, 0.587, 0.114));
                finalColor = lerp(finalColor, finalColor * rainbow, _HoloStrength * luma);

                // === FRESNEL EDGE GLOW ===
                // Simulate edge glow based on UV distance from center
                float2 centerDist = abs(uv - 0.5) * 2.0;  // 0 at center, 1 at edge
                float edgeDist = max(centerDist.x, centerDist.y);

                // Add tilt influence
                edgeDist += dot(tilt, uv - 0.5) * 0.5;

                float fresnel = pow(saturate(edgeDist), _FresnelPower);
                finalColor += _FresnelColor.rgb * fresnel * _FresnelStrength;

                // === LIGHT SWEEP (Random Timing) ===
                // Create time windows based on frequency
                float cycleLength = 1.0 / _SweepFrequency;  // How long between potential sweeps
                float currentCycle = floor(_Time.y / cycleLength);
                float cycleTime = frac(_Time.y / cycleLength);  // 0-1 within current cycle

                // Random value for this cycle determines if sweep happens
                float sweepRand = Hash(float2(currentCycle, currentCycle * 0.7));
                float shouldSweep = step(0.5, sweepRand);  // 50% chance each cycle

                // Sweep animation within the duration window
                float sweepProgress = saturate(cycleTime / _SweepDuration);  // 0-1 during sweep
                float sweepPos = sweepProgress;  // Position of sweep across card

                // Diagonal sweep position
                float sweep = (uv.x + uv.y) * 0.5;  // Diagonal UV position (0-1)

                // Create sharp light bar at current sweep position
                float halfWidth = _SweepWidth * 0.5;
                float distFromSweep = abs(sweep - sweepPos);
                float sweepMask = smoothstep(halfWidth + 0.02, halfWidth, distFromSweep);

                // Only show during active sweep window
                float sweepActive = step(cycleTime, _SweepDuration) * shouldSweep;

                finalColor += _SweepColor.rgb * sweepMask * _SweepStrength * sweepActive;

                // === SPARKLES (Noise-Based) ===
                // Grid cell position and local position within cell
                float2 sparkleGridUV = uv * _SparkleScale;
                float2 sparkleCell = floor(sparkleGridUV);
                float2 sparkleFrac = frac(sparkleGridUV);

                // Base random values per cell
                float sparkleRand = Hash(sparkleCell);
                float sparkleRand2 = Hash(sparkleCell + 1.0);

                // Noise-based drifting offset (sparkles slowly move within cells)
                float driftSpeed = 0.3;
                float2 noiseDrift = float2(
                    ValueNoise(sparkleCell + _Time.y * driftSpeed) - 0.5,
                    ValueNoise(sparkleCell * 1.7 + _Time.y * driftSpeed) - 0.5
                ) * 0.3;

                // Base offset + drift
                float2 sparkleOffset = float2(
                    Hash(sparkleCell * 1.1) * 0.6 + 0.2,
                    Hash(sparkleCell * 1.3) * 0.6 + 0.2
                ) + noiseDrift;

                // Distance from sparkle center
                float sparkleDist = length(sparkleFrac - sparkleOffset);

                // Noise-modulated size (sparkles breathe)
                float sizeNoise = ValueNoise(sparkleCell * 2.3 + _Time.y * 0.5);
                float sparkleSize = 0.04 + sparkleRand2 * 0.08 + sizeNoise * 0.03;
                float sparklePoint = 1.0 - saturate(sparkleDist / sparkleSize);
                sparklePoint = pow(sparklePoint, 2.0);

                // Star/cross shape
                float2 starUV = sparkleFrac - sparkleOffset;
                float star = max(
                    exp(-abs(starUV.x) * 40.0) * exp(-abs(starUV.y) * 8.0),
                    exp(-abs(starUV.y) * 40.0) * exp(-abs(starUV.x) * 8.0)
                );
                sparklePoint = max(sparklePoint, star * 0.5);

                // Organic twinkle using layered noise (FBM)
                float noiseTime = _Time.y * _SparkleSpeed * 0.3;
                float twinkleNoise = FBM(sparkleCell + noiseTime, 3);

                // Add faster shimmer layer
                float shimmer = ValueNoise(sparkleCell * 5.0 + _Time.y * _SparkleSpeed);

                // Combine for organic twinkle (threshold creates on/off effect)
                float twinkle = smoothstep(0.3, 0.6, twinkleNoise + shimmer * 0.3);
                twinkle = pow(twinkle, 2.0);

                // Dynamic visibility - sparkles appear and disappear over time
                float visibilityNoise = ValueNoise(sparkleCell * 0.5 + _Time.y * 0.1);
                float sparkleMask = smoothstep(0.55, 0.65, visibilityNoise + sparkleRand * 0.3);

                // Combine
                float sparkle = sparklePoint * twinkle * sparkleMask;

                // Add to final color
                finalColor += _SparkleColor.rgb * sparkle * _SparkleStrength;

                return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }
    }
}