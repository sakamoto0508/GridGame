// 全Passで同一のマテリアル定数バッファを共有します。
#ifndef NEON_BLOCK_FLOW_INPUT_INCLUDED
#define NEON_BLOCK_FLOW_INPUT_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_TextureEmission); SAMPLER(sampler_TextureEmission);
TEXTURE2D(_TextureColorRamp); SAMPLER(sampler_TextureColorRamp);
TEXTURE2D(_TextureNoise1); SAMPLER(sampler_TextureNoise1);
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
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Metallic;
                float _Smoothness;
                float _UseProceduralPattern;
                float _LineWidth;
                float _PulseSpeed;
            CBUFFER_END


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




float3 EvaluateBlockEmission(float2 meshUV)
{
    float2 uv = meshUV * _TillingXY.xy + _Time.y * _SpeedXY.xy;
    float4 pattern = SAMPLE_TEXTURE2D(_TextureEmission, sampler_TextureEmission, uv);
    if (_UseProceduralPattern > 0.5)
    {
        // メッシュUVの各タイルに枠を描き、その枠上を明るい帯が流れます。
        // テクスチャや子メッシュを追加しなくても、Cubeの各面に模様が出ます。
        float2 tile = frac(meshUV * _TillingXY.xy);
        float edge = min(min(tile.x, 1.0 - tile.x), min(tile.y, 1.0 - tile.y));
        float aa = max(fwidth(edge), 0.001);
        // HLSL予約語との衝突を避け、発光枠のマスクと明示します。
        float neonLineMask = 1.0 - smoothstep(_LineWidth, _LineWidth + aa, edge);
        float pulse = pow(saturate(sin((tile.x + tile.y - _Time.y * _PulseSpeed) * 6.2831853) * 0.5 + 0.5), 6.0);
        pattern = float4((neonLineMask * (0.25 + pulse * 0.75)).xxx, 1);
    }
    float3 tint = _EmissionColor.rgb;
    if (_UseGradientColor > 0.5)
    {
        float ramp = lerp(_LeftRamp, _RightRamp, sin(_Time.y * _GradientColorSpeed) * 0.5 + 0.5);
        tint = SAMPLE_TEXTURE2D(_TextureColorRamp, sampler_TextureColorRamp, float2(ramp, 0.5)).rgb;
    }
    float modulation = 1.0;
    if (_UseAlphaGradient > 0.5)
    {
        float2 noiseUV = meshUV * _Noise1TillingXY.xy + _Time.y * _Noise1SpeedXY.xy;
        if (_UseSinOrTexture > 0.5)
            modulation = lerp(_AlphaMin, _AlphaMax, sin(_Time.y * _AlphaGradientSpeed) * 0.5 + 0.5);
        else if (_UseCustomNoise > 0.5)
            modulation = lerp(_TextureNoise1ValueMin, _TextureNoise1ValueMax,
                SAMPLE_TEXTURE2D(_TextureNoise1, sampler_TextureNoise1, noiseUV).r);
        else
            modulation = FlowNoise(noiseUV) * 0.5 + 0.5;
        modulation *= _TextureNoiseValue1Scale;
    }
    // Alphaは本体を透過させず、任意テクスチャの発光マスクとしてだけ使用します。
    float mask = _UseProceduralPattern > 0.5 ? 1 :
        (_UseAlphaOrRGB > 0.5 ? pattern.a : saturate(dot(pattern.rgb, float3(0.3333,0.3333,0.3333))));
    return tint * pattern.rgb * modulation * mask * _EmissionColorIntensity;
}
#endif
