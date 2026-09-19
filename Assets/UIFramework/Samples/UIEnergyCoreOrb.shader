Shader "UI/EnergyCoreOrb"
{
    // Fully procedural "energy core" orb for UI item cells -- a swirling plasma sphere with a
    // pulsing hot core, starburst arms, twinkling sparks and an outer halo. No texture sampled.
    //
    // Replaces a 16-frame flipbook (135x135 RGBA per frame, six rarity tiers = 96 PNGs, ~3.2 MB)
    // with one shader whose palette is two colors. Two of the reference tiers are structurally
    // different, so both are parameterised rather than special-cased:
    //   _CoreIntensity = 0   -> hollow ring/wisp look (no filled core)
    //   _Prismatic     = 1   -> iridescent hue sweep instead of a two-color ramp
    //
    // TRADE-OFF, stated honestly: this is MORE GPU than the flipbook was (~150 ALU/px vs one
    // texture fetch). What it buys is atlas size, runtime recolor and resolution independence --
    // not speed. The early-out below is what keeps a full inventory grid affordable.
    //
    // ---------------------------------------------------------------------------------------
    // USAGE CONSTRAINT -- read this before assigning the material.
    // The effect is generated from IN.texcoord and therefore needs a true 0..1 rect UV:
    //   * Image "Source Image" must be EMPTY (a null sprite makes Graphic emit a plain 0..1 quad).
    //   * Image Type = Simple. Sliced/Tiled emit several quads that EACH span 0..1, so you would
    //     get one small orb per slice. An atlased sprite gives atlas sub-rect UVs, not 0..1.
    //   * Leave the material's tiling/offset alone (this shader ignores _MainTex_ST by design).
    //   * Keep the RectTransform SQUARE. There is no aspect correction anywhere -- the orb is
    //     built in raw UV space, so a non-square cell renders an ellipse, not a circle.
    // "CanUseSpriteAtlas" = "False" below is advisory only -- the null sprite is the real guarantee.
    // ---------------------------------------------------------------------------------------
    //
    // Notes for whoever edits this next (each of these is a bug that already shipped once in
    // this project's other UI shaders -- see UIAnimatedGlow.shader / UIBorderTraceFrame.shader):
    //
    //  * NO atan2 IN THE NOISE DOMAIN. Sampling noise on a raw-radian angle cracks along -x:
    //    the jump at a = +-PI is 2*PI*N, never an integer lattice step, and value noise is not
    //    periodic anyway, so no "make the multiplier an integer" trick closes the loop. The swirl
    //    here is a Cartesian ROTATION instead, which has no seam at all. atan2 is used only for
    //    the starburst and the prismatic hue, where cos() handles the wrap.
    //  * NO fmod TIME WRAP, deliberately. The usual precision-banding fix is unnecessary here
    //    because rotation preserves magnitude: the noise input never leaves |p| * _NoiseScale no
    //    matter how large t grows, so the domain is bounded by construction. Adding the wrap back
    //    would REINTRODUCE a pop -- neither the rotation angle nor the sin() oscillators are
    //    continuous across an arbitrary reset.
    //  * The fbm is weight-normalised to 0..1 so the ridge term cannot go negative. pow() of a
    //    negative base is NaN, and NaN under additive blending is a permanent white pixel.
    //  * Coverage is computed and clipped BEFORE rgb is premultiplied by it. Reversing that
    //    silently defeats RectMask2D, because additive blending ignores alpha entirely.

    Properties
    {
        // Declared because uGUI assigns it; never sampled.
        [PerRendererData] _MainTex ("Sprite (unused)", 2D) = "white" {}
        _Color ("Tint (UI graying and fade)", Color) = (1,1,1,1)

        [Header(Palette)]
        _CoreColor ("Core Color", Color) = (1.0, 0.859, 0.451, 1)
        _EdgeColor ("Edge Color", Color) = (0.984, 0.635, 0.259, 1)
        _Brightness ("Brightness", Range(0.25, 4)) = 1.4
        _WhiteHot ("White Hot Center", Range(0, 1)) = 0.6
        // How fast the palette moves from _CoreColor at the middle to _EdgeColor at the rim.
        // Raise it to keep a tier's saturated edge color and stop the middle washing out.
        _ColorFalloff ("Color Falloff", Range(0.25, 6)) = 1.6
        _Prismatic ("Prismatic Hue Spread", Range(0, 1)) = 0

        [Header(Shape)]
        _Radius ("Sphere Radius", Range(0.2, 1)) = 0.78
        _EdgeSoftness ("Edge Softness", Range(0.01, 0.5)) = 0.14
        _HaloRadius ("Halo Radius", Range(0.2, 1.45)) = 1.0
        _HaloStrength ("Halo Strength", Range(0, 2)) = 0.35

        [Header(Filaments)]
        _NoiseScale ("Noise Scale", Range(1, 12)) = 3.4
        _Swirl ("Swirl Twist", Range(0, 8)) = 2.6
        _FilamentSpeed ("Swirl Speed", Range(0, 3)) = 0.5
        _FilamentSharpness ("Filament Sharpness", Range(1, 12)) = 4.0
        _FilamentStrength ("Filament Strength", Range(0, 2)) = 0.9

        [Header(Core)]
        // 0 gives the hollow ring/wisp variant (reference tier qua3).
        _CoreIntensity ("Core Intensity", Range(0, 3)) = 1.2
        _CoreTightness ("Core Tightness", Range(1, 12)) = 3.5
        // Integer: cos(a*N) is value continuous for any N, but its derivative kinks at +-PI
        // unless N is whole, which reads as a crease down one arm.
        [IntRange] _SpikeCount ("Starburst Arms", Range(0, 12)) = 6
        _SpikeAmount ("Starburst Amount", Range(0, 1)) = 0.45
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.6
        _PulseAmount ("Pulse Amount", Range(0, 0.6)) = 0.22

        [Header(Rim Shell)]
        _RimRadius ("Rim Radius", Range(0, 1)) = 0.72
        _RimWidth ("Rim Width", Range(0.01, 0.4)) = 0.1
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.5

        [Header(Sparks)]
        _SparkDensity ("Spark Grid Density", Range(2, 16)) = 7
        _SparkSize ("Spark Size", Range(0.01, 0.45)) = 0.13
        _SparkSpeed ("Spark Twinkle Speed", Range(0, 6)) = 2.2
        _SparkStrength ("Spark Strength", Range(0, 2)) = 0.6

        [Header(Optional Unscaled Time Driver)]
        // Leave at 0 to animate on _Time.y (freezes at Time.timeScale = 0). Drive it with
        // Time.unscaledTime from a script to keep animating on pause-menu screens.
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
            "CanUseSpriteAtlas" = "False"   // procedural UV needs a standalone, non-atlased rect
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
        Blend One One          // additive: the orb is emissive, it never darkens what's behind it
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
                float4 vertex        : SV_POSITION;
                half4 color          : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            half4 _Color;
            float4 _ClipRect;
            float _CustomTime;

            half4 _CoreColor, _EdgeColor;
            float _Brightness, _WhiteHot, _ColorFalloff, _Prismatic;
            float _Radius, _EdgeSoftness, _HaloRadius, _HaloStrength;
            float _NoiseScale, _Swirl, _FilamentSpeed, _FilamentSharpness, _FilamentStrength;
            float _CoreIntensity, _CoreTightness, _SpikeCount, _SpikeAmount;
            float _PulseSpeed, _PulseAmount;
            float _RimRadius, _RimWidth, _RimStrength;
            float _SparkDensity, _SparkSize, _SparkSpeed, _SparkStrength;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                // raw texcoord, NOT TRANSFORM_TEX: the effect is generated in UV space, so a
                // stray tiling/offset on the material would distort the orb rather than a texture
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            float hash21(float2 p)
            {
                float3 q = frac(p.xyx * float3(123.34, 234.34, 345.65));
                q += dot(q, q + 34.45);
                return frac(q.x * q.y * q.z);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float2 rotate(float2 v, float ang)
            {
                float s, c;
                sincos(ang, s, c);
                return float2(v.x * c - v.y * s, v.x * s + v.y * c);
            }

            half4 frag(v2f IN) : SV_Target
            {
                float t = _CustomTime > 0 ? _CustomTime : _Time.y;

                float2 p = (IN.texcoord - 0.5) * 2.0;
                float r = length(p);

                // Early-out before any noise. Under Blend One One returning zero adds nothing, so
                // this is a real skip. Culls the quad corners plus everything past the halo --
                // ~21% of the quad even when the halo fills the inscribed circle. This is what
                // makes a 20-60 cell inventory grid affordable at ~150 ALU/px.
                // Guarded with _Radius so a halo smaller than the sphere cannot clip the sphere.
                float cullRadius = max(_HaloRadius, _Radius);
                if (r > cullRadius)
                    return half4(0, 0, 0, 0);

                // +1e-6 because the exact center IS sampled on an odd-sized rect (the reference
                // art is 135x135) and atan2(0,0) is undefined.
                float a = atan2(p.y, p.x + 1e-6);

                // Sphere remap: compresses the domain toward the rim so filaments bunch at the
                // silhouette and read as wrapped on a ball rather than painted on a flat disc.
                // Clamped to 0.99 because d(asin)/dr diverges at 1 -- at exactly 1 the filament
                // screen-frequency explodes into shimmer along the edge.
                float rs = asin(min(r, 0.99)) * (2.0 / UNITY_PI);

                // ---- filaments: two counter-rotating octaves ----
                // Rotation, not translation, is what animates the noise: |rotate(p,x)| == |p|, so
                // the sample coordinate stays bounded for any t and needs no precision wrap.
                // Two octaves twisting at different rates and opposite directions keeps this from
                // reading as one rigid spin.
                float2 q1 = rotate(p,  _Swirl * rs + t * _FilamentSpeed);
                float2 q2 = rotate(p, -_Swirl * 0.6 * rs - t * _FilamentSpeed * 0.63 + 2.1);

                // weights sum to 1, so n is guaranteed 0..1 and the ridge below cannot go negative
                float n = valueNoise(q1 * _NoiseScale) * 0.6667
                        + valueNoise(q2 * _NoiseScale * 2.03 + 11.7) * 0.3333;

                float ridge = saturate(1.0 - abs(2.0 * n - 1.0));
                float filaments = pow(ridge, _FilamentSharpness) * _FilamentStrength;

                // ---- hot core with starburst arms ----
                float pulse = 1.0 + sin(t * _PulseSpeed) * _PulseAmount;
                // _SpikeAmount is capped at 1, so this stays >= 0
                float starburst = 1.0 + _SpikeAmount * cos(a * _SpikeCount);
                float core = pow(saturate(1.0 - r), _CoreTightness)
                           * _CoreIntensity * starburst * pulse;

                // ---- rim shell ----
                // Both band edges are pulled inside _Radius: a band sitting at or past the
                // silhouette is dead code, the gate below would erase it whatever the slider says.
                float rimHalf = min(_RimWidth, _Radius * 0.5);
                float rimR = clamp(_RimRadius, rimHalf, _Radius - rimHalf);
                float rim = smoothstep(rimR - rimHalf, rimR, r)
                          * (1.0 - smoothstep(rimR, rimR + rimHalf, r))
                          * _RimStrength;

                // ---- halo: a separate term, deliberately NOT gated by the silhouette ----
                float haloT = saturate(1.0 - r / _HaloRadius);
                float halo = haloT * haloT * _HaloStrength;

                // ---- sparks: one hashed point per grid cell ----
                float2 sp = p * _SparkDensity;
                float2 cell = floor(sp);
                float2 fp = frac(sp);
                // inset the point by its own radius so it never straddles a cell border -- we
                // don't sample neighbouring cells, so a point on the edge would be cut in half
                float inset = saturate(_SparkSize);
                float2 pt = lerp(float2(inset, inset), float2(1.0 - inset, 1.0 - inset),
                                 float2(hash21(cell), hash21(cell + 37.7)));
                float phase = hash21(cell + 91.3) * UNITY_TWO_PI;
                float twinkle = saturate(sin(t * _SparkSpeed + phase));
                float spark = (1.0 - smoothstep(0.0, _SparkSize, length(fp - pt)))
                            * twinkle * _SparkStrength * haloT;

                // ---- composite ----
                float silhouette = 1.0 - smoothstep(_Radius - _EdgeSoftness, _Radius, r);
                float energy = (filaments + rim + core) * silhouette + spark + halo;

                // ---- palette ----
                // The ramp has to be a BOUNDED radial term, never `energy`. energy is unbounded
                // -- filaments + core + halo routinely sum past 1 -- so saturate(energy) pins to
                // 1 across most of the disc, the lerp lands on _CoreColor everywhere, and every
                // tier washes out toward white with its edge color never appearing at all.
                // Intensity and hue are separate concerns here: energy drives brightness below,
                // this drives only where we are between the two palette colors.
                float ramp = pow(saturate(1.0 - r / _Radius), _ColorFalloff);
                half3 rgb = lerp(_EdgeColor.rgb, _CoreColor.rgb, ramp);

                // Iridescent variant (reference tier qua7): cosine palette, cheaper than an
                // HSV round-trip. a/(2*PI) means the hue closes over one full turn, so the
                // +-PI wrap is continuous.
                float hue = rs * 1.3 + a / UNITY_TWO_PI + t * 0.05;
                half3 prism = 0.5 + 0.5 * cos(UNITY_TWO_PI * (hue + float3(0.0, 0.33, 0.67)));
                rgb = lerp(rgb, prism, _Prismatic);

                // blow the very center toward white without washing out the palette elsewhere
                rgb = lerp(rgb, half3(1, 1, 1), saturate(core * _WhiteHot));

                // Blend One One ignores alpha, so the ONLY thing that may scale rgb is the fade
                // and the clip rect. Shape must NOT be folded in here: `energy` above already
                // carries silhouette, halo and spark, so multiplying rgb by a shape term again
                // squares it -- that drove the outer halo to ~1e-4 and made the outer sparks
                // die whenever _HaloStrength went to 0, coupling two unrelated sliders.
                // The clip factor still multiplies rgb directly, which is what makes RectMask2D
                // actually clip under additive blending.
                half fade = IN.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                fade *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                half3 outRgb = rgb * energy * _Brightness * IN.color.rgb * fade;

                // alpha carries the shape, for RectMask2D and the alpha-clip path below
                half coverage = saturate(silhouette + halo) * fade;

                half4 col = half4(outRgb, coverage);

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
