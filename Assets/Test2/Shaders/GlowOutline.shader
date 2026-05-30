Shader "UI/GlowOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0, 10)) = 2

        [Header(Glow)]
        _GlowColor ("Glow Color", Color) = (1,1,0,1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1
        _GlowSize ("Glow Size", Range(0, 20)) = 5

        [Header(Animation)]
        _AnimationSpeed ("Animation Speed", Range(0, 10)) = 2
        _PulseMin ("Pulse Min", Range(0, 1)) = 0.5
        _PulseMax ("Pulse Max", Range(0, 1)) = 1
        _WaveAmplitude ("Wave Amplitude", Range(0, 5)) = 1

        [Header(UI)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            fixed4 _OutlineColor;
            float _OutlineWidth;
            fixed4 _GlowColor;
            float _GlowIntensity;
            float _GlowSize;
            float _AnimationSpeed;
            float _PulseMin;
            float _PulseMax;
            float _WaveAmplitude;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Sample the main texture
                half4 color = tex2D(_MainTex, IN.texcoord);

                // Animation calculations
                float time = _Time.y * _AnimationSpeed;

                // Pulse animation (breathing effect)
                float pulse = lerp(_PulseMin, _PulseMax, (sin(time) + 1.0) * 0.5);

                // Wave animation (radiating effect from center)
                float2 center = float2(0.5, 0.5);
                float distFromCenter = length(IN.texcoord - center);
                float wave = sin(distFromCenter * 10.0 - time * 2.0) * 0.5 + 0.5;
                wave = wave * _WaveAmplitude;

                // Combine animations
                float animationMultiplier = pulse * (1.0 + wave * 0.3);

                // Sample outline
                float outline = 0;
                float glow = 0;

                // Animated outline width
                float animatedOutlineWidth = _OutlineWidth * animationMultiplier;

                // Sample surrounding pixels for outline
                for(float x = -1; x <= 1; x++)
                {
                    for(float y = -1; y <= 1; y++)
                    {
                        if(x == 0 && y == 0) continue;

                        float2 offset = float2(x, y) * _MainTex_TexelSize.xy * animatedOutlineWidth;
                        float sampleAlpha = tex2D(_MainTex, IN.texcoord + offset).a;
                        outline = max(outline, sampleAlpha);
                    }
                }

                // Animated glow size
                float animatedGlowSize = _GlowSize * animationMultiplier;

                // Sample surrounding pixels for glow (larger radius)
                for(float gx = -2; gx <= 2; gx++)
                {
                    for(float gy = -2; gy <= 2; gy++)
                    {
                        float2 glowOffset = float2(gx, gy) * _MainTex_TexelSize.xy * animatedGlowSize;
                        float glowSample = tex2D(_MainTex, IN.texcoord + glowOffset).a;
                        float distance = length(float2(gx, gy)) / 2.82842712475; // sqrt(8)
                        glow += glowSample * (1.0 - distance);
                    }
                }
                glow /= 25.0; // Normalize by number of samples
                glow = saturate(glow * _GlowIntensity * animationMultiplier);

                // Create outline mask (only where original is transparent)
                outline = outline * (1 - color.a);

                // Create glow mask (only where original is transparent)
                glow = glow * (1 - color.a) * (1 - outline);

                // Combine original color, outline, and glow
                fixed4 finalColor = color;
                finalColor.rgb = lerp(finalColor.rgb, _OutlineColor.rgb, outline * _OutlineColor.a);
                finalColor.a = max(finalColor.a, outline);

                // Add glow
                finalColor.rgb += _GlowColor.rgb * glow * _GlowColor.a;
                finalColor.a = max(finalColor.a, glow);

                // Apply tint
                finalColor *= IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (finalColor.a - 0.001);
                #endif

                return finalColor;
            }
        ENDCG
        }
    }
}
