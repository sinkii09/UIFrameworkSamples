Shader "UI/AnimatedGlow"
{
    // Animates a single static glow texture (e.g. show1_glow_00067_tex.png) using
    // domain-warp UV distortion + a breathing pulse, so a lone baked frame reads as a
    // living, roiling glow instead of a static blob. No flipbook required.
    //
    // Technique: sample the texture through UV offset by two independently-scrolling
    // noise fields (different scale/speed/direction each) so the warp never repeats in
    // an obviously periodic way -- this is the standard "domain warping" trick used for
    // fire/smoke/energy/aura shaders industry-wide (see Inigo Quilez's writeup on it).
    //
    // Reviewed 2026-08-29 (code-reviewer, Tier 1 diff gate) -- fixes applied:
    //  - RectMask2D clip now actually clips (premultiply reordered after the clip factor)
    //  - brightness/pulse no longer double-applied through the premultiply step
    //  - half-precision instead of fixed (fixed==lowp overflows past +-2 on GLES/mobile)
    //  - noise time input wrapped to avoid float precision banding on long sessions
    //  - optional _CustomTime input so a driver script can keep this animating through
    //    Time.timeScale = 0 (pause-menu overlays); falls back to _Time.y untouched
    //  - warp layers now scroll on genuinely different directions, not anti-parallel
    //  - hash22 halves the noise cost (one combined lookup per corner, not two)
    //  - CanUseSpriteAtlas off (the UV clamp only holds for a standalone, non-atlased sprite)
    //  - Fallback + target 3.0 for headroom

    Properties
    {
        [PerRendererData] _MainTex ("Glow Texture", 2D) = "white" {}
        _Color ("Tint (UI graying/fade)", Color) = (1,1,1,1)
        _GlowColor ("Glow Recolor", Color) = (0.6, 0.8, 1.0, 1)
        _Brightness ("Brightness", Range(0.5, 4)) = 1.6

        [Header(Domain Warp Layer 1)]
        _DistortScale ("Noise Scale", Range(1, 15)) = 3.0
        _DistortSpeed ("Scroll Speed", Range(0, 2)) = 0.25
        _DistortStrength ("Strength (UV units)", Range(0, 0.3)) = 0.06

        [Header(Domain Warp Layer 2)]
        // different scale/speed/direction than layer 1 so the combined warp never reads
        // as an obviously repeating single sine wave
        _DistortScale2 ("Noise Scale", Range(1, 15)) = 5.0
        _DistortSpeed2 ("Scroll Speed", Range(0, 2)) = 0.52
        _DistortStrength2 ("Strength (UV units)", Range(0, 0.3)) = 0.03

        [Header(Breathing Pulse)]
        _PulseSpeed ("Pulse Speed", Range(0, 4)) = 1.2
        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.12

        [Header(Optional Unscaled Time Driver)]
        // Leave at 0 to animate on _Time.y (default, freezes if Time.timeScale = 0).
        // Drive it yourself with Time.unscaledTime (via MaterialPropertyBlock or
        // material.SetFloat) to keep animating on paused/pause-menu screens.
        _CustomTime ("Custom Time (0 = use _Time.y)", Float) = 0

        // --- standard UI stencil/clip boilerplate, unchanged from UI/Default ---
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
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "False"   // the UV clamp below only holds for a standalone sprite
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
        Blend One One          // true additive: reads as light-emitting, standard for glow/aura UI VFX
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

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
                half4 color     : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color;
            half4 _GlowColor;
            float _Brightness;
            float4 _ClipRect;
            float _CustomTime;

            float _DistortScale, _DistortSpeed, _DistortStrength;
            float _DistortScale2, _DistortSpeed2, _DistortStrength2;
            float _PulseSpeed, _PulseAmount;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            // combined 2-channel hash: same cost as one hash21 call (reuses the same
            // intermediate q for both output channels via two different combinations),
            // so a 4-corner value-noise lookup costs ~4 hashes instead of 8
            float2 hash22(float2 p)
            {
                float2 q = frac(p * float2(123.34, 456.21));
                q += dot(q, q + 45.32);
                return frac(float2(q.x * q.y, q.y * q.x + q.x * 7.1));
            }

            float2 valueNoise2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 a = hash22(i);
                float2 b = hash22(i + float2(1, 0));
                float2 c = hash22(i + float2(0, 1));
                float2 d = hash22(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            half4 frag(v2f IN) : SV_Target
            {
                // resolve time source: custom unscaled driver if set, else built-in _Time.y;
                // wrapped to keep the noise-domain input bounded (avoids float-precision
                // banding after long play sessions -- a harmless ~seamful reset every ~100s,
                // masked by the two-layer warp's visual complexity)
                float t = _CustomTime > 0 ? _CustomTime : _Time.y;
                t = fmod(t, 100.0);

                float2 uv = IN.texcoord;

                // ---- domain-warp the sample UV through two independent scrolling noise fields ----
                // distinct scroll directions per layer (not anti-parallel) so the combined
                // warp doesn't read as one wave cancelling/reinforcing along a single axis
                float2 dir1 = float2(1.0, 0.3);
                float2 dir2 = float2(-0.4, 0.9);
                float2 warp1 = (valueNoise2(uv * _DistortScale + t * _DistortSpeed * dir1) - 0.5) * _DistortStrength;
                float2 warp2 = (valueNoise2(uv * _DistortScale2 + t * _DistortSpeed2 * dir2 + 7.7) - 0.5) * _DistortStrength2;
                float2 distortedUV = uv + warp1 + warp2;
                distortedUV = clamp(distortedUV, 0.001, 0.999); // stay inside the texture, avoid edge artifacts

                half4 tex = tex2D(_MainTex, distortedUV);

                // ---- breathing pulse: subtle brightness/alpha swell, not a hard blink ----
                half pulse = 1.0 + sin(t * _PulseSpeed) * _PulseAmount;

                // raw coverage (no brightness/pulse folded in) is what premultiply and
                // clipping must use -- folding brightness/pulse in first would square
                // both of them once premultiplied, and silently defeat RectMask2D clipping
                // by keeping full rgb visible outside the clip rect under additive blending
                half coverage = tex.a * IN.color.a * _GlowColor.a;

                #ifdef UNITY_UI_CLIP_RECT
                coverage *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                half3 rgb = tex.rgb * IN.color.rgb * _GlowColor.rgb * _Brightness * pulse;
                rgb *= coverage; // premultiply by the (already-clipped) coverage

                half4 color = half4(rgb, coverage);

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
