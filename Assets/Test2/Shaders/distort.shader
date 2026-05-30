Shader "Custom/Distort"
{
    Properties
    {
        _Noise("Noise", 2D) = "white" {}
        _StrengthFilter("Strength Filter", 2D) = "white" {}
        _Strength("Distort Strength", float) = 1.0
        _Speed("Distort Speed", float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "DisableBatching" = "True"
        }

        Pass
        {
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Noise);                 SAMPLER(sampler_Noise);
            TEXTURE2D(_StrengthFilter);        SAMPLER(sampler_StrengthFilter);
            TEXTURE2D_X(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);

            CBUFFER_START(UnityPerMaterial)
                float _Strength;
                float _Speed;
            CBUFFER_END

            struct Attributes
            {
                float4 vertex   : POSITION;
                float3 texCoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 pos       : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float3 texCoord  : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                // billboard: place at object world-origin in view space, offset vertex X/Z as view-space X/Y
                float4 pos = input.vertex;
                pos = mul(UNITY_MATRIX_P,
                      mul(UNITY_MATRIX_MV, float4(0, 0, 0, 1))
                          + float4(pos.x, pos.z, 0, 0));
                output.pos      = pos;
                output.screenPos = ComputeScreenPos(pos);
                output.texCoord = input.texCoord;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.screenPos.xy / input.screenPos.w;

                float  noise = SAMPLE_TEXTURE2D(_Noise, sampler_Noise, input.texCoord.xy).r;
                // R channel scales X distortion, G channel scales Y — preserves original per-axis control
                float2 filt  = SAMPLE_TEXTURE2D(_StrengthFilter, sampler_StrengthFilter, input.texCoord.xy).rg;

                uv.x += cos(noise * _Time.x * _Speed) * filt.x * _Strength;
                uv.y += sin(noise * _Time.x * _Speed) * filt.y * _Strength;

                // Requires "Opaque Texture" enabled on the URP asset (UniversalRP.asset).
                // If the texture appears black, check that no camera overrides it via UniversalAdditionalCameraData.
                return SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
            }

            ENDHLSL
        }
    }
}
