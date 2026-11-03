Shader "RoboticMuseum/StereoPresenter"
{
    Properties
    {
        _LeftTex ("Left eye", 2D) = "black" {}
        _RightTex ("Right eye", 2D) = "black" {}
        _Mode ("Mode (1 side-by-side, 2 anaglyph)", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "StereoComposite"
            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_LeftTex);
            SAMPLER(sampler_LeftTex);
            TEXTURE2D(_RightTex);
            SAMPLER(sampler_RightTex);
            float _Mode;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                if (_Mode < 1.5)
                {
                    if (input.uv.x < 0.5)
                    {
                        float2 uv = float2(input.uv.x * 2.0, input.uv.y);
                        return SAMPLE_TEXTURE2D(_LeftTex, sampler_LeftTex, uv);
                    }

                    float2 uv = float2((input.uv.x - 0.5) * 2.0, input.uv.y);
                    return SAMPLE_TEXTURE2D(_RightTex, sampler_RightTex, uv);
                }

                float3 leftRgb = SAMPLE_TEXTURE2D(_LeftTex, sampler_LeftTex, input.uv).rgb;
                float3 rightRgb = SAMPLE_TEXTURE2D(_RightTex, sampler_RightTex, input.uv).rgb;
                float leftLuma = dot(leftRgb, float3(0.299, 0.587, 0.114));
                float rightLuma = dot(rightRgb, float3(0.299, 0.587, 0.114));
                return half4(leftLuma, rightLuma, rightLuma, 1.0);
            }
            ENDHLSL
        }
    }
}
