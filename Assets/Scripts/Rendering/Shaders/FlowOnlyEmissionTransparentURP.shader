// URP port of the imported Xuqi Flow_OnlyEmission_Transparent shader.
// 元のプロパティ名とSimplexノイズを維持し、既存マテリアルの設定を引き継ぎます。
// ASEグラフではなく手書きURPシェーダーです。元ファイルは変更しません。
Shader "NEON DETONATOR/URP/Flow Only Emission Transparent"
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
		[Toggle]_UseAlphaGradient("使用透明度变化", Float) = 1
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
	}

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "FlowUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_TextureEmission); SAMPLER(sampler_TextureEmission);
            TEXTURE2D(_TextureColorRamp); SAMPLER(sampler_TextureColorRamp);
            TEXTURE2D(_TextureNoise1); SAMPLER(sampler_TextureNoise1);

            // すべてのマテリアル値を同じCBUFFERにまとめ、SRP Batcherに対応します。
            CBUFFER_START(UnityPerMaterial)
                float4 _EmissionColor;
                float4 _TillingXY;
                float4 _SpeedXY;
                float4 _Noise1TillingXY;
                float4 _Noise1SpeedXY;
                float _EmissionColorIntensity;
                float _UseAlphaOrRGB;
                float _UseGradientColor;
                float _GradientColorSpeed;
                float _LeftRamp;
                float _RightRamp;
                float _UseAlphaGradient;
                float _UseSinOrTexture;
                float _AlphaGradientSpeed;
                float _AlphaMin;
                float _AlphaMax;
                float _TextureNoise1ValueMin;
                float _TextureNoise1ValueMax;
                float _UseCustomNoise;
                float _TextureNoiseValue1Scale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

		float3 FlowMod289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float2 FlowMod289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float3 FlowPermute( float3 x ) { return FlowMod289( ( ( x * 34.0 ) + 1.0 ) * x ); }

		float FlowNoise( float2 v )
		{
			const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
			float2 i = floor( v + dot( v, C.yy ) );
			float2 x0 = v - i + dot( i, C.xx );
			float2 i1;
			i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
			float4 x12 = x0.xyxy + C.xxzz;
			x12.xy -= i1;
			i = FlowMod289( i );
			float3 p = FlowPermute( FlowPermute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
			float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
			m = m * m;
			m = m * m;
			float3 x = 2.0 * frac( p * C.www ) - 1.0;
			float3 h = abs( x ) - 0.5;
			float3 ox = floor( x + 0.5 );
			float3 a0 = x - ox;
			m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
			float3 g;
			g.x = a0.x * x0.x + h.x * x0.y;
			g.yz = a0.yz * x12.xz + h.yz * x12.yw;
			return 130.0 * dot( m, g );
		}



            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // 元の実装と同じUV式。TextureのSTではなくTillingXYで繰り返しを指定します。
                float2 uv = input.uv * _TillingXY.xy + _Time.y * _SpeedXY.xy;
                float4 pattern = SAMPLE_TEXTURE2D(_TextureEmission, sampler_TextureEmission, uv);
                float3 tint = _EmissionColor.rgb;
                if (_UseGradientColor > 0.5)
                {
                    float ramp = lerp(_LeftRamp, _RightRamp, sin(_Time.y * _GradientColorSpeed) * 0.5 + 0.5);
                    tint = SAMPLE_TEXTURE2D(_TextureColorRamp, sampler_TextureColorRamp, float2(ramp, 0.5)).rgb;
                }

                // 周期的点滅、任意ノイズ画像、元のSimplexノイズの3種類を切り替えます。
                float modulation = 1.0;
                if (_UseAlphaGradient > 0.5)
                {
                    float2 noiseUV = input.uv * _Noise1TillingXY.xy + _Time.y * _Noise1SpeedXY.xy;
                    if (_UseSinOrTexture > 0.5)
                        modulation = lerp(_AlphaMin, _AlphaMax, sin(_Time.y * _AlphaGradientSpeed) * 0.5 + 0.5);
                    else if (_UseCustomNoise > 0.5)
                    {
                        float n = SAMPLE_TEXTURE2D(_TextureNoise1, sampler_TextureNoise1, noiseUV).r;
                        modulation = lerp(_TextureNoise1ValueMin, _TextureNoise1ValueMax, n);
                    }
                    else
                        modulation = FlowNoise(noiseUV) * 0.5 + 0.5;
                    modulation *= _TextureNoiseValue1Scale;
                }

                float alpha = _UseAlphaOrRGB > 0.5 ? pattern.a : saturate(dot(pattern.rgb, float3(0.3333, 0.3333, 0.3333)));
                float3 emission = tint * pattern.rgb * modulation * _EmissionColorIntensity;
                // HDR発光は保持し、アルファだけ正常範囲に制限します。にじみはBloom側で処理します。
                return half4(MixFog(emission, input.fogFactor), saturate(alpha * modulation));
            }
            ENDHLSL
        }
        // 装飾用の半透明発光なので、Built-inのディザ影パスは移植しません。
    }
    Fallback Off
}

