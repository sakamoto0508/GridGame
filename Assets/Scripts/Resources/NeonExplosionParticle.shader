// 頂点色のAlphaで減衰する加算発光。Particle Mesh/Billboard共用でライト不要。
Shader "NEON DETONATOR/Effects/Explosion Additive"
{
    Properties { [HDR] _TintColor("Glow Color", Color) = (0.1,1,1.5,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _TintColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _TintColor;
                return output;
            }
            half4 frag(Varyings input) : SV_Target { return input.color; }
            ENDHLSL
        }
    }
    Fallback Off
}
