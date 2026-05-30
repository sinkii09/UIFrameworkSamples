Shader "Custom/2D_Holo_Gleam"
{
    Properties
    {
        _MainTex ("Card Artwork", 2D) = "white" {}
        _MaskTex ("Foil Mask (R)", 2D) = "white" {}
        _GleamSpeed ("Gleam Speed/Scale", Vector) = (1, 1, 0, 0)
        _HoloScale ("Holo Scale", Float) = 5.0
        _Intensity ("Gleam & Holo Intensity", Vector) = (1.0, 1.0, 0, 0) // X = Gleam, Y = Holo
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            float4 _GleamSpeed;
            float _HoloScale;
            float4 _Intensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            // Simple rainbow generator based on input value
            half3 spectral_hue(float x) {
                return saturate(float3(abs(x * 6.0 - 3.0) - 1.0, 2.0 - abs(x * 6.0 - 2.0), 2.0 - abs(x * 6.0 - 4.0)));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate Screen space UVs
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                
                // Base Textures
                fixed4 baseColor = tex2D(_MainTex, i.uv);
                fixed4 mask = tex2D(_MaskTex, i.uv);

                // 1. Diagonal Gleam Logic
                float gleamCoord = screenUV.x * _GleamSpeed.x + screenUV.y * _GleamSpeed.y;
                float gleam = saturate(1.0 - abs(frac(gleamCoord) - 0.5) * 10.0); // Creates a sharp line

                // 2. Holo Foil Logic
                float holoCoord = screenUV.x * _HoloScale + screenUV.y * _HoloScale;
                half3 rainbow = spectral_hue(frac(holoCoord));

                // Combine and apply mask
                half3 effect = (gleam * _Intensity.x) + (rainbow * _Intensity.y);
                baseColor.rgb += effect * mask.r;

                return baseColor;
            }
            ENDCG
        }
    }
}