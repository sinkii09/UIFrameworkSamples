Shader "Custom/ToonSprite2D"
{
    Properties
    {
        [MainTexture] _MainTex("Sprite Texture", 2D) = "white" {}
        [MainColor] _Color("Tint", Color) = (1,1,1,1)

        [Header(Toon Settings)]
        _ColorBands("Color Bands", Range(2, 8)) = 4
        _ShadowColor("Shadow Tint", Color) = (0.7, 0.7, 0.8, 1)
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.5

        [Header(Outline)]
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness("Outline Thickness", Range(0, 10)) = 1
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
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                half4 _Color;
                float _ColorBands;
                half4 _ShadowColor;
                float _ShadowThreshold;
                half4 _OutlineColor;
                float _OutlineThickness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample the sprite texture
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // Early out if transparent
                if (texColor.a < 0.01)
                {
                    // === OUTLINE CHECK ===
                    // Sample neighbors to detect edge
                    float2 texelSize = _MainTex_TexelSize.xy * _OutlineThickness;

                    float alphaSum = 0;
                    alphaSum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(texelSize.x, 0)).a;
                    alphaSum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-texelSize.x, 0)).a;
                    alphaSum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(0, texelSize.y)).a;
                    alphaSum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(0, -texelSize.y)).a;

                    // If any neighbor is opaque, this is an outline pixel
                    if (alphaSum > 0.1)
                    {
                        return _OutlineColor;
                    }

                    discard;
                }

                // === TOON COLOR QUANTIZATION ===
                // Convert to grayscale for luminance-based banding
                float luminance = dot(texColor.rgb, float3(0.299, 0.587, 0.114));

                // Quantize to bands
                float quantized = floor(luminance * _ColorBands) / (_ColorBands - 1.0);

                // Apply shadow tint to dark areas
                half3 toonColor = texColor.rgb;
                if (quantized < _ShadowThreshold)
                {
                    toonColor *= _ShadowColor.rgb;
                }

                // Optional: posterize the colors themselves
                toonColor = floor(toonColor * _ColorBands) / (_ColorBands - 1.0);

                // Apply sprite tint and vertex color
                half4 finalColor = half4(toonColor, texColor.a) * _Color * IN.color;

                return finalColor;
            }
            ENDHLSL
        }
    }
}