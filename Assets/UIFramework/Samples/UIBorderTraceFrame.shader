Shader "UI/BorderTraceFrame"
{
    // Neon square frame with two bright highlights (comets) traveling around its edge,
    // 180 degrees apart, moving together. Purely procedural -- ignores _MainTex content
    // entirely, so it drops onto any UI Image regardless of the assigned sprite (even a
    // blank 1x1 white sprite works; _MainTex only exists so the Image has something to
    // raycast/mask against).
    //
    // Border-trace-only rebuild of the earlier UI_QualityGlow_Qua7 scratchpad prototype
    // (orb-fill + sparkle layers dropped per user request -- just the edge-traveling
    // highlights). Carries forward every gotcha caught reviewing UIAnimatedGlow.shader,
    // since the qua7 prototype predates that review and shares the same shader family:
    //  - premultiply happens AFTER the RectMask2D clip factor, not before (the prototype
    //    never premultiplied rgb by alpha at all, which is wrong under the premultiplied
    //    Blend One OneMinusSrcAlpha mode used here -- partial-alpha edges would read too
    //    bright)
    //  - half precision throughout, including the frag return type itself
    //  - optional _CustomTime so a driver script can keep this animating through
    //    Time.timeScale = 0 (pause-menu overlays) via Time.unscaledTime
    //  - Fallback + target 3.0
    //
    // USAGE CONSTRAINT: the Image this material is on must be Image Type = Simple with
    // either no sprite assigned (Source Image = None) or a full, non-atlased, non-9-sliced
    // sprite (its texcoord must span 0..1 across the whole rect). A packed/atlased sprite
    // or Sliced/Tiled Image Type will offset or tile the frame incorrectly, since the
    // effect reads its own coordinate space directly off the mesh's sprite UV.
    //
    // Reviewed again 2026-08-29 (code-reviewer, Tier 1 diff gate, round 2) -- fixes:
    //  - outer ring smoothstep was dead code (ringOuter sat exactly at the UV boundary,
    //    which boxDist never exceeds) -- inset it so the outer glow actually fades instead
    //    of hard-clipping at the quad edge
    //  - comet tail width now clamped so it can never cross the antipodal wrap point and
    //    seam when _TraceWidth is pushed high
    //  - removed the fmod(t,100) time wrap added last round -- appropriate for noise-domain
    //    warp shaders, but wrong here: it introduced a visible position/hue "pop" every
    //    100s because frac(t*speed) isn't continuous across an arbitrary hard reset unless
    //    speed*100 happens to be an integer. This shader has no noise domain to bound, so
    //    plain _Time.y is used unwrapped like UI/Default itself does.
    //  - squarePerimeterParam's atan2 guarded against the (0,0) NaN at the exact rect centre
    //  - perimeter position computed once per pixel and passed into both comet calls,
    //    instead of recomputing atan2 twice
    //
    // Reviewed again 2026-08-29 (code-reviewer, Tier 1 diff gate, round 3) -- replaced the
    // always-on rainbow hue-cycle (_HueSpeed, now removed) with a per-tier _FrameColor plus
    // an optional _PrismaticShimmer blend (highest-tier perk: multi-hue sweep that varies by
    // PERIMETER POSITION, not just time, so several colors are visible around the ring at
    // once). _ShimmerFrequency marked [IntRange] -- a fractional band count seams at the
    // perimeter's atan2 wrap point since frac() isn't continuous across it otherwise.

    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Frame Ring)]
        _FrameColor ("Frame Color (per-tier)", Color) = (0.6, 0.8, 1.0, 1)
        _FrameThickness ("Frame Thickness", Range(0.01, 0.3)) = 0.06
        _FrameSoftness ("Frame Glow Softness", Range(0.001, 0.3)) = 0.05
        _FrameGlowIntensity ("Frame Glow Intensity", Range(0.5, 5)) = 2.0

        [Header(Prismatic Shimmer)]
        // Highest-tier perk: blends the static _FrameColor toward an animated multi-hue
        // sweep traveling around the perimeter (several colors visible at once), instead
        // of a flat color. Leave _PrismaticShimmer at 0 for normal tiers.
        _PrismaticShimmer ("Shimmer Amount (0=static color, 1=full prismatic)", Range(0, 1)) = 0
        // must land on a whole number of bands -- a fractional value seams at the
        // perimeter's atan2 wrap point (the frac() below isn't continuous there otherwise)
        [IntRange] _ShimmerFrequency ("Shimmer Color Bands", Range(1, 8)) = 3
        _ShimmerSpeed ("Shimmer Sweep Speed", Range(-2, 2)) = 0.4

        [Header(Border Trace Comets)]
        // two comets, 180 degrees apart on the perimeter, always moving together
        _TraceIntensity ("Trace Brightness", Range(0, 4)) = 1.5
        _TraceWidth ("Trace Width (0..0.25 of perimeter)", Range(0.01, 0.25)) = 0.08
        _TraceSpeed ("Trace Speed (loops/sec)", Range(-2, 2)) = 0.3
        _TraceTailFalloff ("Trace Tail Sharpness", Range(0.5, 6)) = 2.0

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
            "CanUseSpriteAtlas" = "True"
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
        Blend One OneMinusSrcAlpha   // premultiplied-alpha composite: sits correctly over arbitrary panel art
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
                float4 worldPosition : TEXCOORD1;
                float2 localUV : TEXCOORD0; // sprite UV, see USAGE CONSTRAINT above
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex; // unused for sampling -- declared so the Image's PerRendererData texture still binds cleanly
            half4 _Color;
            half4 _FrameColor;
            float4 _ClipRect;
            float _CustomTime;

            float _FrameThickness, _FrameSoftness, _FrameGlowIntensity;
            float _PrismaticShimmer, _ShimmerFrequency, _ShimmerSpeed;
            float _TraceIntensity, _TraceWidth, _TraceSpeed, _TraceTailFalloff;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.localUV = v.texcoord; // sprite's own UV -- see USAGE CONSTRAINT: must span 0..1 across the rect
                OUT.color = v.color * _Color;
                return OUT;
            }

            half3 hue2rgb(half h)
            {
                half3 rgb = saturate(abs(frac(h + half3(0, 2.0/3.0, 1.0/3.0)) * 6.0 - 3.0) - 1.0);
                return rgb * rgb * (3.0 - 2.0 * rgb);
            }

            // Parameterizes position around a square perimeter as a single 0..1 value
            // (angle-based, not true arc-length -- moves slightly faster through the
            // corners than along the flat edges, which reads fine for a stylized comet
            // trail and is far cheaper than a per-edge branch). +1e-6 on x guards the
            // atan2(0,0) NaN at the exact rect centre (real pixels hit this on
            // odd-pixel-dimension rects).
            float squarePerimeterParam(float2 p)
            {
                return atan2(p.y, p.x + 1e-6) / 6.28318530718 + 0.5; // 0..1
            }

            // Bright comet/highlight that travels around the ring over time, with a soft
            // trailing tail (asymmetric falloff: sharp leading edge, longer tail). `phase`
            // offsets this comet's position around the loop (0 and 0.5 = opposite sides,
            // moving together). Takes the perimeter position pre-computed by the caller so
            // two comets don't each pay for their own atan2.
            half borderTrace(float perim, float phase, float speed, float width, float tailSharpness, float t)
            {
                float lightPos = frac(t * speed + phase);
                float d = perim - lightPos;
                d = d - floor(d + 0.5); // shortest signed wrap-around distance, -0.5..0.5

                // asymmetric width: short/sharp ahead of the comet (d >= 0, not yet lit),
                // long/soft behind it (d < 0, fading tail) -- branchless, cheap.
                float side = step(0.0, d);                 // 1 = ahead, 0 = behind (tail)
                // clamp the tail so it can never cross the antipodal wrap point (d = +-0.5)
                // and seam against the sharp lead width there when _TraceWidth is pushed high
                float tailWidth = min(width * 3.0, 0.49);
                float w = lerp(tailWidth, width, side);
                float shape = saturate(1.0 - abs(d) / w);
                return (half)pow(shape, tailSharpness);
            }

            half4 frag(v2f IN) : SV_Target
            {
                // no fmod wrap here -- this shader has no noise domain to bound (unlike
                // UIAnimatedGlow's warp noise), and wrapping would itself introduce a
                // position/hue pop every wrap period since frac(t*speed) isn't continuous
                // across a hard reset for an arbitrary user-set speed. Plain _Time.y, same
                // as UI/Default.
                float t = _CustomTime > 0 ? _CustomTime : _Time.y;

                float2 uv = IN.localUV - 0.5; // centered, -0.5..0.5
                float perim = squarePerimeterParam(uv); // 0..1 position around the perimeter,
                                                          // shared by the frame color and both comets

                // ---- frame ring: box SDF -> ring -> soft glow ----
                float boxDist = max(abs(uv.x), abs(uv.y)); // 0 center .. 0.5 edge, square metric
                float ringInner = 0.5 - _FrameThickness;
                float ringOuter = 0.5 - _FrameSoftness; // inset so the outer smoothstep actually
                                                         // runs before boxDist hits the quad edge
                                                         // (at 0.5 exactly it was dead code / a hard clip)
                half ringMask = (half)(smoothstep(ringInner - _FrameSoftness, ringInner, boxDist)
                               - smoothstep(ringOuter, ringOuter + _FrameSoftness, boxDist));

                // static per-tier color, blended toward an animated multi-hue sweep for the
                // highest tier. Hue depends on PERIMETER POSITION (not just time), so multiple
                // colors are visible around the ring at once instead of the whole ring flashing
                // one color at a time.
                half shimmerHue = (half)frac(perim * _ShimmerFrequency + t * _ShimmerSpeed);
                half3 shimmerColor = hue2rgb(shimmerHue);
                // _FrameColor.rgb only -- alpha isn't read, this shader has no per-tier transparency control
                half3 frameColor = lerp(_FrameColor.rgb, shimmerColor, saturate((half)_PrismaticShimmer)) * (half)_FrameGlowIntensity;

                // ---- two comets, 180 degrees apart, moving together ----
                half trace1 = borderTrace(perim, 0.0, _TraceSpeed, _TraceWidth, _TraceTailFalloff, t) * ringMask;
                half trace2 = borderTrace(perim, 0.5, _TraceSpeed, _TraceWidth, _TraceTailFalloff, t) * ringMask;
                half trace = max(trace1, trace2); // never double-brighten where a wide trace could overlap

                // raw coverage (no brightness folded in) is what premultiply and clipping
                // must use -- folding brightness in first would square it once premultiplied,
                // and silently defeat RectMask2D clipping by leaving full rgb visible
                // outside the clip rect once the premultiplied blend composites it
                half coverage = saturate(ringMask + trace) * IN.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                coverage *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                half3 highlightColor = lerp(half3(1, 1, 1), frameColor, 0.3) * trace * _TraceIntensity;
                half3 rgb = (frameColor * ringMask + highlightColor) * IN.color.rgb;
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
