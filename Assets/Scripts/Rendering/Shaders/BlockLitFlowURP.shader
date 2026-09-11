// 不透明なライティング付き本体と流れる発光を1マテリアルで描画します。
Shader "NEON DETONATOR/URP/Block Lit Flow"
{
    Properties
	{
		_TextureEmission("TextureEmission", 2D) = "white" {}
		_EmissionColorIntensity("EmissionColorIntensity", Range( 0 , 10)) = 1
		[HDR]_EmissionColor("EmissionColor", Color) = (0.7605247,0.658375,0.597202,1)
		_TillingXY("TillingXY", Vector) = (1,1,0,0)
		[Toggle]_UseAlphaOrRGB("UseAlphaOrRGB", Float) = 0
		_SpeedXY("SpeedXY", Vector) = (0,0,0,0)
		_TextureColorRamp("TextureColorRamp", 2D) = "white" {}
		[Toggle]_UseGradientColor("UseGradientColor", Float) = 0
		_GradientColorSpeed("GradientColorSpeed", Range( 0 , 10)) = 1
		_LeftRamp("LeftRamp", Range( 0 , 1)) = 0
		_RightRamp("RightRamp", Range( 0 , 1)) = 1
		[Toggle]_UseAlphaGradient("Animate Emission Brightness", Float) = 0
		[Toggle]_UseSinOrTexture("勾选-时间透明变化，不勾-贴图采样透明变化", Float) = 0
		_AlphaGradientSpeed("AlphaGradientSpeed透明度变化速度", Range( 0 , 10)) = 1
		_AlphaMin("AlphaMin最小值", Range( 0 , 1)) = 0
		_AlphaMax("AlphaMax最大值", Range( 0 , 1)) = 1
		_TextureNoise1("TextureNoise1", 2D) = "white" {}
		_TextureNoise1ValueMin("TextureNoise1ValueMin", Range( 0 , 1)) = 0
		_TextureNoise1ValueMax("TextureNoise1ValueMax", Range( 0 , 1)) = 1
		_Noise1TillingXY("Noise1TillingXY", Vector) = (1,1,0,0)
		_Noise1SpeedXY("Noise1SpeedXY", Vector) = (0,0,0,0)
		[Toggle]_UseCustomNoise("勾选_自定义透明度流动图 不勾_默认噪声", Float) = 0
		_TextureNoiseValue1Scale("TextureNoiseValue1Scale", Range( 0.1 , 10)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1

        [MainTexture] _BaseMap("Base Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Body Color", Color) = (0.045,0.065,0.085,1)
        _Metallic("Metallic", Range(0,1)) = 0.2
        _Smoothness("Smoothness", Range(0,1)) = 0.35
        [Toggle] _UseProceduralPattern("Use Built-in Neon Lines", Float) = 1
        _LineWidth("Neon Line Width", Range(0.005,0.15)) = 0.025
        _PulseSpeed("Neon Pulse Speed", Range(0,5)) = 0.6
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
        ZWrite On
        Blend One Zero
        Pass
        {
            Name "BlockForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex BlockVertex
            #pragma fragment BlockFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include "NeonBlockFlowInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct BlockAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct BlockVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half4 fogAndVertexLight : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            BlockVaryings BlockVertex(BlockAttributes input)
            {
                BlockVaryings output = (BlockVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fogAndVertexLight = half4(ComputeFogFactor(pos.positionCS.z), VertexLighting(pos.positionWS, output.normalWS));
                return output;
            }
            half4 BlockFragment(BlockVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.positionCS = input.positionCS;
                lighting.normalWS = NormalizeNormalPerPixel(input.normalWS);
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    lighting.shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
                #else
                    lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                lighting.fogCoord = input.fogAndVertexLight.x;
                lighting.vertexLighting = input.fogAndVertexLight.yzw;
                // 動的Block向け。ライトマップではなく環境のSHを使います。
                lighting.bakedGI = SampleSH(lighting.normalWS);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.shadowMask = half4(1,1,1,1);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(input.uv, _BaseMap)).rgb * _BaseColor.rgb;
                surface.metallic = _Metallic;
                surface.smoothness = _Smoothness;
                surface.normalTS = half3(0,0,1);
                surface.occlusion = 1;
                surface.emission = EvaluateBlockEmission(input.uv);
                surface.alpha = 1; // 発光や画像のAlphaが0でも、本体は必ず不透明。
                half4 color = UniversalFragmentPBR(lighting, surface);
                color.rgb = MixFog(color.rgb, lighting.fogCoord);
                return half4(color.rgb, 1);
            }
            ENDHLSL
        }
        // URP同梱Passを同じ定数バッファで使い、影・深度・法線出力を揃えます。
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            ZTest LEqual
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "NeonBlockFlowInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "NeonBlockFlowInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "NeonBlockFlowInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
    Fallback Off
}
