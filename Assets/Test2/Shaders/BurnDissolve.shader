Shader "Custom/BurnDissolve"
{
    Properties
    {
        [MainTexture] _MainTex ("Main Texture", 2D) = "white" {}
        _NoiseTex ("Burn Noise Texture", 2D) = "white" {}
        _DisolveAmount ("Dissolve Amount", Range(0,1)) = 0

        [Header(Fire Edge)]
        _FireColor1 ("Hot Color (inner tip)", Color) = (1.0, 1.0, 0.6, 1)
        _FireColor2 ("Fire Color (mid)", Color) = (1.0, 0.3, 0.0, 1)
        _FireColor3 ("Ember Color (outer)", Color) = (0.25, 0.02, 0.0, 1)
        _EdgeWidth ("Fire Width", Range(0.001, 0.3)) = 0.08
        _FireIntensity ("Fire Intensity", Float) = 2.5

        [Header(Ash Scorch)]
        _AshColor ("Ash Color", Color) = (0.07, 0.05, 0.04, 1)
        _AshWidth ("Ash Width", Range(0.001, 0.3)) = 0.06
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

        // ── URP 2D Renderer ────────────────────────────────────────────────────
        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);   SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);  SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                float  _DisolveAmount;
                half4  _FireColor1;
                half4  _FireColor2;
                half4  _FireColor3;
                float  _EdgeWidth;
                float  _FireIntensity;
                half4  _AshColor;
                float  _AshWidth;
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
                float4 color      : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = IN.uv;
                OUT.color      = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 base  = SAMPLE_TEXTURE2D(_MainTex,  sampler_MainTex,
                                               TRANSFORM_TEX(IN.uv, _MainTex));
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex,
                                               TRANSFORM_TEX(IN.uv, _NoiseTex)).r;
                base *= IN.color;

                // diff > 0 = visible, diff <= 0 = dissolved
                float diff = noise - _DisolveAmount;
                clip(diff);

                // ── Fire zone [0, _EdgeWidth] ──────────────────────────────────
                // fireMask = 1 at threshold (hottest), fades to 0 at EdgeWidth
                float fireMask = saturate(1.0 - diff / _EdgeWidth);

                // Three-point gradient: ember → orange → white-hot
                half3 fireColor = lerp(_FireColor3.rgb,
                                       lerp(_FireColor2.rgb, _FireColor1.rgb, fireMask),
                                       fireMask);

                // ── Ash/scorch zone [_EdgeWidth, _EdgeWidth + _AshWidth] ───────
                // ashMask = 1 at fire boundary, 0 at outer ash edge (0 everywhere else)
                float ashNorm = saturate((diff - _EdgeWidth) / _AshWidth);
                float ashMask = (1.0 - ashNorm) * step(_EdgeWidth, diff);

                // ── Combine ────────────────────────────────────────────────────
                half3 rgb = lerp(base.rgb, _AshColor.rgb, ashMask);
                // Squared falloff keeps the glow concentrated right at the burn edge
                rgb += fireColor * (_FireIntensity * fireMask * fireMask);

                return half4(rgb, base.a);
            }
            ENDHLSL
        }

        // ── URP Forward (editor preview / 3D renderer fallback) ─────────────
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);   SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);  SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                float  _DisolveAmount;
                half4  _FireColor1;
                half4  _FireColor2;
                half4  _FireColor3;
                float  _EdgeWidth;
                float  _FireIntensity;
                half4  _AshColor;
                float  _AshWidth;
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
                float4 color      : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = IN.uv;
                OUT.color      = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 base  = SAMPLE_TEXTURE2D(_MainTex,  sampler_MainTex,
                                               TRANSFORM_TEX(IN.uv, _MainTex));
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex,
                                               TRANSFORM_TEX(IN.uv, _NoiseTex)).r;
                base *= IN.color;

                float diff = noise - _DisolveAmount;
                clip(diff);

                float fireMask = saturate(1.0 - diff / _EdgeWidth);
                half3 fireColor = lerp(_FireColor3.rgb,
                                       lerp(_FireColor2.rgb, _FireColor1.rgb, fireMask),
                                       fireMask);

                float ashNorm = saturate((diff - _EdgeWidth) / _AshWidth);
                float ashMask = (1.0 - ashNorm) * step(_EdgeWidth, diff);

                half3 rgb = lerp(base.rgb, _AshColor.rgb, ashMask);
                rgb += fireColor * (_FireIntensity * fireMask * fireMask);

                return half4(rgb, base.a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}
