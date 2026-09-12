Shader "NEON DETONATOR/Effects/Death Voxels"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half3 color : COLOR; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // 面ごとの明暗で小さな立方体の形を見せます。大量のLightは使いません。
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float shade = 0.5 + 0.5 * saturate(dot(normalWS, normalize(float3(-0.4, 1, -0.3))));
                output.color = input.color.rgb * shade;
                return output;
            }
            half4 frag(Varyings input) : SV_Target { return half4(input.color, 1); }
            ENDHLSL
        }
    }
    Fallback Off
}
