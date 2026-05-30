Shader "Custom/CardHoloFoil"
{
    Properties
    {
        [MainTexture] _MainTex ("Card Artwork", 2D) = "white" {}
        _FoilMask ("Foil Mask (R=foil area)", 2D) = "white" {}

        [Header(Gleam)]
        _GleamAngle ("Gleam Angle (0=horizontal  1=vertical)", Range(0,1)) = 0.5
        _GleamSpeed ("Gleam Speed", Float) = 0.5
        _GleamWidth ("Gleam Sharpness", Float) = 15.0
        _GleamIntensity ("Gleam Intensity", Float) = 1.5

        [Header(Rainbow Foil)]
        _RainbowScale ("Rainbow Scale", Float) = 5.0
        _RainbowIntensity ("Rainbow Intensity", Range(0,2)) = 0.8
        _RainbowSpeed ("Rainbow Speed", Float) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        // ── URP 2D Renderer pass ────────────────────────────────────────────
        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);   SAMPLER(sampler_MainTex);
            TEXTURE2D(_FoilMask);  SAMPLER(sampler_FoilMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _FoilMask_ST;
                float  _GleamAngle;
                float  _GleamSpeed;
                float  _GleamWidth;
                float  _GleamIntensity;
                float  _RainbowScale;
                float  _RainbowIntensity;
                float  _RainbowSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float2 uvFoil     : TEXCOORD1;
                float4 color      : COLOR;
            };

            // Smooth spectral rainbow: maps 0-1 to RGB hue cycle
            half3 SpectralHue(float t)
            {
                return saturate(half3(
                    abs(t * 6.0 - 3.0) - 1.0,
                    2.0 - abs(t * 6.0 - 2.0),
                    2.0 - abs(t * 6.0 - 4.0)
                ));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.uvFoil     = TRANSFORM_TEX(IN.uv, _FoilMask);
                OUT.color      = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 base     = SAMPLE_TEXTURE2D(_MainTex,  sampler_MainTex,  IN.uv);
                half  foilMask = SAMPLE_TEXTURE2D(_FoilMask, sampler_FoilMask, IN.uvFoil).r;

                // Tint from sprite renderer color
                base *= IN.color;

                // ── Diagonal gleam stripe ───────────────────────────────────
                // GleamAngle blends between horizontal (0) and vertical (1) sweep
                float gleamCoord = IN.uv.x * _GleamAngle + IN.uv.y * (1.0 - _GleamAngle);
                float t = frac(gleamCoord + _Time.y * _GleamSpeed);
                // Narrow the band: higher _GleamWidth = thinner, sharper stripe
                float gleam = saturate(1.0 - abs(t - 0.5) * _GleamWidth);
                // Cube for a soft falloff
                gleam = gleam * gleam * gleam;

                // ── Rainbow foil ────────────────────────────────────────────
                // Diagonal UV sum creates iridescent bands at 45 degrees
                float holoCoord = frac((IN.uv.x + IN.uv.y) * _RainbowScale
                                       + _Time.y * _RainbowSpeed);
                half3 rainbow = SpectralHue(holoCoord);

                // ── Combine ─────────────────────────────────────────────────
                // Both effects are additive, masked to foil area, respect alpha
                half3 effect = (gleam * _GleamIntensity + rainbow * _RainbowIntensity) * foilMask;
                base.rgb += effect * base.a;

                return base;
            }
            ENDHLSL
        }

        // ── Fallback: URP Forward (3D renderer / editor preview) ────────────
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);   SAMPLER(sampler_MainTex);
            TEXTURE2D(_FoilMask);  SAMPLER(sampler_FoilMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _FoilMask_ST;
                float  _GleamAngle;
                float  _GleamSpeed;
                float  _GleamWidth;
                float  _GleamIntensity;
                float  _RainbowScale;
                float  _RainbowIntensity;
                float  _RainbowSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float2 uvFoil     : TEXCOORD1;
                float4 color      : COLOR;
            };

            half3 SpectralHue(float t)
            {
                return saturate(half3(
                    abs(t * 6.0 - 3.0) - 1.0,
                    2.0 - abs(t * 6.0 - 2.0),
                    2.0 - abs(t * 6.0 - 4.0)
                ));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.uvFoil     = TRANSFORM_TEX(IN.uv, _FoilMask);
                OUT.color      = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 base     = SAMPLE_TEXTURE2D(_MainTex,  sampler_MainTex,  IN.uv);
                half  foilMask = SAMPLE_TEXTURE2D(_FoilMask, sampler_FoilMask, IN.uvFoil).r;
                base *= IN.color;

                float gleamCoord = IN.uv.x * _GleamAngle + IN.uv.y * (1.0 - _GleamAngle);
                float t = frac(gleamCoord + _Time.y * _GleamSpeed);
                float gleam = saturate(1.0 - abs(t - 0.5) * _GleamWidth);
                gleam = gleam * gleam * gleam;

                float holoCoord = frac((IN.uv.x + IN.uv.y) * _RainbowScale
                                       + _Time.y * _RainbowSpeed);
                half3 rainbow = SpectralHue(holoCoord);

                half3 effect = (gleam * _GleamIntensity + rainbow * _RainbowIntensity) * foilMask;
                base.rgb += effect * base.a;

                return base;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}
