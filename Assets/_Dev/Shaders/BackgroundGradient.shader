Shader "Game/BackgroundGradient"
{
    Properties
    {
        _BottomColor ("Bottom Color", Color) = (0.125, 0.173, 0.259, 1)
        _TopColor ("Top Color", Color) = (0.224, 0.275, 0.4, 1)
        _TopWeight ("Top Color Weight", Range(0, 1)) = 0.5
        _Softness ("Blend Softness", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Background"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "BackgroundGradient"

            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BottomColor;
                float4 _TopColor;
                float _TopWeight;
                float _Softness;
            CBUFFER_END

            Varyings Vertex(Attributes input)
            {
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);

                Varyings output;
                output.positionCS = positions.positionCS;
                output.screenPos = positions.positionNDC;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float height = saturate(input.screenPos.y / input.screenPos.w);

                // The blend midpoint sits below the top color's share of the screen,
                // so _TopWeight is how much of the height the top color dominates.
                float midpoint = 1.0 - _TopWeight;
                float softness = max(_Softness, 1e-4);
                float blend = saturate((height - midpoint) / softness + 0.5);

                return half4(lerp(_BottomColor.rgb, _TopColor.rgb, blend), 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
