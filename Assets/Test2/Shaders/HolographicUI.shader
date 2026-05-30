Shader "Custom/HolographicUI"
{
    Properties
    {
        [MainTexture] _MainTex("Texture", 2D) = "white" {}
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

        // UI Stencil properties
        [HideInInspector] _StencilComp("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        // UI Stencil for masking
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _ClipRect;

            float _HoloStrength;
            float _HoloScale;
            float _HoloSpeed;

            fixed4 _FresnelColor;
            float _FresnelPower;
            float _FresnelStrength;

            fixed4 _SweepColor;
            float _SweepSpeed;
            float _SweepWidth;
            float _SweepStrength;
            float _SweepFrequency;
            float _SweepDuration;

            fixed4 _SparkleColor;
            float _SparkleScale;
            float _SparkleSpeed;
            float _SparkleStrength;

            float _TiltX;
            float _TiltY;

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
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash(i);
                float b = Hash(i + float2(1.0, 0.0));
                float c = Hash(i + float2(0.0, 1.0));
                float d = Hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Fractal Brownian Motion
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

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 tilt = float2(_TiltX, _TiltY);

                // === BASE COLOR ===
                fixed4 baseColor = tex2D(_MainTex, uv) * i.color;

                // Early out if transparent
                if (baseColor.a < 0.01)
                    discard;

                fixed3 finalColor = baseColor.rgb;

                // === RAINBOW / IRIDESCENCE ===
                float2 rainbowUV = uv * _HoloScale + tilt * 2.0;
                float hue = rainbowUV.x + rainbowUV.y + _Time.y * _HoloSpeed;
                float3 rainbow = HsvToRgb(float3(frac(hue), 0.7, 1.0));

                float luma = dot(baseColor.rgb, float3(0.299, 0.587, 0.114));
                finalColor = lerp(finalColor, finalColor * rainbow, _HoloStrength * luma);

                // === FRESNEL EDGE GLOW ===
                float2 centerDist = abs(uv - 0.5) * 2.0;
                float edgeDist = max(centerDist.x, centerDist.y);
                edgeDist += dot(tilt, uv - 0.5) * 0.5;

                float fresnel = pow(saturate(edgeDist), _FresnelPower);
                finalColor += _FresnelColor.rgb * fresnel * _FresnelStrength;

                // === LIGHT SWEEP (Random Timing) ===
                float cycleLength = 1.0 / _SweepFrequency;
                float currentCycle = floor(_Time.y / cycleLength);
                float cycleTime = frac(_Time.y / cycleLength);

                float sweepRand = Hash(float2(currentCycle, currentCycle * 0.7));
                float shouldSweep = step(0.5, sweepRand);

                float sweepProgress = saturate(cycleTime / _SweepDuration);
                float sweepPos = sweepProgress;

                float sweep = (uv.x + uv.y) * 0.5;
                float halfWidth = _SweepWidth * 0.5;
                float distFromSweep = abs(sweep - sweepPos);
                float sweepMask = smoothstep(halfWidth + 0.02, halfWidth, distFromSweep);

                float sweepActive = step(cycleTime, _SweepDuration) * shouldSweep;
                finalColor += _SweepColor.rgb * sweepMask * _SweepStrength * sweepActive;

                // === SPARKLES (Noise-Based) ===
                float2 sparkleGridUV = uv * _SparkleScale;
                float2 sparkleCell = floor(sparkleGridUV);
                float2 sparkleFrac = frac(sparkleGridUV);

                float sparkleRand = Hash(sparkleCell);
                float sparkleRand2 = Hash(sparkleCell + 1.0);

                float driftSpeed = 0.3;
                float2 noiseDrift = float2(
                    ValueNoise(sparkleCell + _Time.y * driftSpeed) - 0.5,
                    ValueNoise(sparkleCell * 1.7 + _Time.y * driftSpeed) - 0.5
                ) * 0.3;

                float2 sparkleOffset = float2(
                    Hash(sparkleCell * 1.1) * 0.6 + 0.2,
                    Hash(sparkleCell * 1.3) * 0.6 + 0.2
                ) + noiseDrift;

                float sparkleDist = length(sparkleFrac - sparkleOffset);

                float sizeNoise = ValueNoise(sparkleCell * 2.3 + _Time.y * 0.5);
                float sparkleSize = 0.04 + sparkleRand2 * 0.08 + sizeNoise * 0.03;
                float sparklePoint = 1.0 - saturate(sparkleDist / sparkleSize);
                sparklePoint = pow(sparklePoint, 2.0);

                float2 starUV = sparkleFrac - sparkleOffset;
                float star = max(
                    exp(-abs(starUV.x) * 40.0) * exp(-abs(starUV.y) * 8.0),
                    exp(-abs(starUV.y) * 40.0) * exp(-abs(starUV.x) * 8.0)
                );
                sparklePoint = max(sparklePoint, star * 0.5);

                float noiseTime = _Time.y * _SparkleSpeed * 0.3;
                float twinkleNoise = FBM(sparkleCell + noiseTime, 3);
                float shimmer = ValueNoise(sparkleCell * 5.0 + _Time.y * _SparkleSpeed);

                float twinkle = smoothstep(0.3, 0.6, twinkleNoise + shimmer * 0.3);
                twinkle = pow(twinkle, 2.0);

                float visibilityNoise = ValueNoise(sparkleCell * 0.5 + _Time.y * 0.1);
                float sparkleMask = smoothstep(0.55, 0.65, visibilityNoise + sparkleRand * 0.3);

                float sparkle = sparklePoint * twinkle * sparkleMask;
                finalColor += _SparkleColor.rgb * sparkle * _SparkleStrength;

                fixed4 output = fixed4(finalColor, baseColor.a);

                // UI Clipping (for RectMask2D)
                #ifdef UNITY_UI_CLIP_RECT
                output.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(output.a - 0.001);
                #endif

                return output;
            }
            ENDCG
        }
    }
}
