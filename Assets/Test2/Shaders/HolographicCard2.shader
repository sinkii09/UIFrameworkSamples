Shader "Custom/HolographicCard2"
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
        _SweepSpeed("Sweep Speed", Range(0, 3)) = 1
        _SweepWidth("Sweep Width", Range(0.01, 0.3)) = 0.1
        _SweepStrength("Sweep Strength", Range(0, 1)) = 0.5

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
                float4 _MainTex_ST;
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

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
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

                // === LIGHT SWEEP ===
                float sweep = (uv.x + uv.y) * 0.5;  // Diagonal
                sweep = frac(sweep - _Time.y * _SweepSpeed);  // Animate

                // Create sharp light bar
                float halfWidth = _SweepWidth * 0.5;
                float sweepMask = smoothstep(0.5 - halfWidth - 0.02, 0.5 - halfWidth, sweep)
                                * smoothstep(0.5 + halfWidth + 0.02, 0.5 + halfWidth, sweep);

                finalColor += _SweepColor.rgb * sweepMask * _SweepStrength;

                // === SPARKLES ===
                float2 sparkleUV = floor(uv * _SparkleScale);
                float sparkleRand = Hash(sparkleUV);

                // Animate sparkle
                float sparklePhase = sparkleRand * 6.28318;  // Random phase
                float sparkle = sin(_Time.y * _SparkleSpeed + sparklePhase);
                sparkle = smoothstep(0.9, 1.0, sparkle);  // Sharp threshold

                // Only some cells sparkle
                sparkle *= step(0.95, sparkleRand);

                finalColor += _SparkleColor.rgb * sparkle * _SparkleStrength;

                return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }
    }
}