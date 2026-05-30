// CardPrismaticUI — Holographic trading card effect for Unity UI (Built-in RP)
//
// Two layered systems:
//   1. PRISMATIC BORDER  — Reflection-based face normals, narrow hue sweep around BaseHue.
//                          Aspect-ratio-corrected so corner diagonal is geometrically correct
//                          on portrait cards (e.g. standard 2.5:3.5 trading card).
//
//   2. HOLOGRAPHIC FOIL  — UV-shift diffraction grating. tiltOffset shifts the phase of each
//                          pixel, so color bands appear to travel across the surface as the
//                          card is tilted. Works in 2D (no camera perspective needed).
//                          Coverage controlled by _HoloMask (R channel).
//
// Drive _TiltX / _TiltY each frame from CardHoloTiltController.cs.
Shader "Custom/CardPrismaticUI"
{
    Properties
    {
        [MainTexture] _MainTex  ("Texture", 2D) = "white" {}
        [MainColor]   _Color    ("Tint", Color) = (1,1,1,1)
        _HoloMask               ("Holo Mask (R=foil area)", 2D) = "white" {}

        [Header(Tilt Input)]
        _TiltX      ("Tilt X",                     Range(-1,  1)) = 0
        _TiltY      ("Tilt Y",                     Range(-1,  1)) = 0
        _CardAspect ("Card Aspect (width/height)", Float)         = 0.714

        [Header(Prismatic Border)]
        _BaseHue         ("Base Hue (0=red 0.5=cyan 0.75=purple)", Range(0, 1))      = 0.75
        _BorderHueRange  ("Hue Sweep Range",                        Range(0, 0.5))    = 0.12
        _BorderWidth     ("Border Width",                           Range(0.01, 0.15)) = 0.06
        _BorderBrightness("Border Brightness",                      Range(0, 3))      = 1.5
        _BorderSpeed     ("Border Slow Drift",                      Range(0, 1))      = 0.04

        [Header(Holographic Foil)]
        _FoilAngle    ("Foil Angle (0=horiz 0.5=diagonal 1=vert)", Range(0, 1))   = 0.5
        _FoilScale    ("Foil Band Scale",                           Range(1, 20))  = 6.0
        _TiltShiftX   ("Tilt Shift X  (band travel per X tilt)",   Range(0, 5))   = 2.0
        _TiltShiftY   ("Tilt Shift Y  (band travel per Y tilt)",   Range(0, 5))   = 2.0
        _FoilStrength ("Foil Blend Strength",                       Range(0, 1))   = 0.7
        _FoilSpeed    ("Foil Auto-drift Speed",                     Range(0, 2))   = 0.1

        // UI stencil — set by the Unity UI system, do not edit in the Inspector
        [HideInInspector] _StencilComp     ("Stencil Comparison",   Float) = 8
        [HideInInspector] _Stencil         ("Stencil ID",           Float) = 0
        [HideInInspector] _StencilOp       ("Stencil Operation",    Float) = 0
        [HideInInspector] _StencilWriteMask("Stencil Write Mask",   Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask",    Float) = 255
        [HideInInspector] _ColorMask       ("Color Mask",           Float) = 15
    }

    SubShader
    {
        Tags
        {
            "RenderType"        = "Transparent"
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull     Off
        ZWrite   Off
        ZTest    [unity_GUIZTestMode]
        ColorMask[_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            // ── Structs ────────────────────────────────────────────────────
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                float2 uv            : TEXCOORD0;
                float4 color         : COLOR;
                float4 worldPosition : TEXCOORD1;
            };

            // ── Uniforms ───────────────────────────────────────────────────
            sampler2D _MainTex;
            float4    _MainTex_ST;
            sampler2D _HoloMask;
            fixed4    _Color;
            float4    _ClipRect;

            float _TiltX;          float _TiltY;         float _CardAspect;
            float _BaseHue;        float _BorderHueRange;
            float _BorderWidth;    float _BorderBrightness; float _BorderSpeed;
            float _FoilAngle;      float _FoilScale;
            float _TiltShiftX;     float _TiltShiftY;
            float _FoilStrength;   float _FoilSpeed;

            // ── Utilities ──────────────────────────────────────────────────

            // HSV hue wheel: red(0)→yellow→green→cyan→blue→magenta→red(1)
            half3 SpectralHue(float t)
            {
                return saturate(half3(
                    abs(t * 6.0 - 3.0) - 1.0,
                    2.0 - abs(t * 6.0 - 2.0),
                    2.0 - abs(t * 6.0 - 4.0)
                ));
            }

            // ── Vertex ─────────────────────────────────────────────────────
            v2f vert(appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex        = UnityObjectToClipPos(v.vertex);
                o.uv            = TRANSFORM_TEX(v.uv, _MainTex);
                o.color         = v.color * _Color;
                return o;
            }

            // ── Fragment ───────────────────────────────────────────────────
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // ── BASE ──────────────────────────────────────────────────
                fixed4 baseColor = tex2D(_MainTex, uv) * i.color;
                if (baseColor.a < 0.01) discard;

                float3 finalColor = (float3)baseColor.rgb;
                float2 tiltVec    = float2(_TiltX, _TiltY);
                float  tiltMag    = length(tiltVec);

                // ── PRISMATIC BORDER ──────────────────────────────────────
                float edgeMin    = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                float borderMask = 1.0 - smoothstep(0.0, _BorderWidth, edgeMin);

                // Scale centered UV by aspect so the left/top face-normal boundary sits
                // at the geometric 90° corner rather than the UV-space 45° midpoint.
                float2 centered      = (uv - 0.5) * float2(_CardAspect, 1.0);
                float2 absCen        = abs(centered);
                float2 outwardNormal = (absCen.x > absCen.y)
                    ? float2(sign(centered.x), 0.0)
                    : float2(0.0, sign(centered.y));

                // reflection: +1 = face points toward tilt (lit), -1 = facing away
                float reflection  = dot(outwardNormal, tiltVec);
                float borderHue   = frac(_BaseHue + reflection * _BorderHueRange + _Time.y * _BorderSpeed);
                half3 borderColor = SpectralHue(borderHue);

                // Brightness ramp + specular hot-spot on the most-lit face
                float reflBright  = pow(saturate((reflection + 1.0) * 0.5), 0.6) * 1.2 + 0.2;
                float specular    = pow(saturate(reflection), 8.0) * saturate(tiltMag * 3.0);
                half3 prismBorder = lerp(borderColor * reflBright, half3(1.6, 1.6, 1.6), specular * 0.5);

                // Soft inner glow that bleeds from border edge into card interior
                float innerGlow      = smoothstep(_BorderWidth * 1.3, 0.0, edgeMin) * 0.25;
                half3 innerGlowColor = SpectralHue(frac(_BaseHue + 0.08));

                finalColor += (float3)(prismBorder    * borderMask * baseColor.a * _BorderBrightness);
                finalColor += (float3)(innerGlowColor * innerGlow * (1.0 - borderMask) * baseColor.a * _BorderBrightness * 0.25);

                // ── HOLOGRAPHIC FOIL ──────────────────────────────────────
                // Diffraction grating simulation: project UV onto the chosen diagonal axis,
                // then add a tilt-driven offset. As tilt changes, tiltOffset shifts, which
                // shifts the phase each pixel samples → color bands appear to travel.
                float  angle      = _FoilAngle * UNITY_PI * 0.5; // 0→0° horiz, 0.5→45° diag, 1→90° vert
                float  diagCoord  = uv.x * cos(angle) + uv.y * sin(angle);
                float  tiltOffset = _TiltX * _TiltShiftX + _TiltY * _TiltShiftY;
                float  phase      = (diagCoord + tiltOffset) * _FoilScale + _Time.y * _FoilSpeed;
                float3 holoColor  = (float3)SpectralHue(frac(phase));

                float foilMask  = tex2D(_HoloMask, uv).r;
                float foilBlend = foilMask * _FoilStrength * baseColor.a;
                finalColor      = lerp(finalColor, holoColor, foilBlend);

                // ── OUTPUT ────────────────────────────────────────────────
                fixed4 output = fixed4(finalColor, baseColor.a);

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
