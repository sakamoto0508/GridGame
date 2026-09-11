// uGUI専用。画像のAlpha・頂点色・CanvasGroup・Stencil Mask・RectMask2Dを維持します。
Shader "NEON DETONATOR/UI/Flow Glow"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _FlowRect("Local Rect", Vector) = (-100,-50,200,100)
        _FlowSpeed("Flow Speed", Float) = 0.25
        _FlowStrength("Flow Strength", Float) = 0.8
        _FlowWidth("Flow Width", Range(0.02,0.5)) = 0.18
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct Attributes
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 localPosition : TEXCOORD1;
                half4 mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            sampler2D _MainTex;
            float4 _Color, _TextureSampleAdd, _ClipRect, _FlowRect;
            float _FlowSpeed, _FlowStrength, _FlowWidth;
            float _UIMaskSoftnessX, _UIMaskSoftnessY;
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.localPosition = input.vertex.xy;
                output.uv = input.uv;
                output.color = input.color * _Color;
                float2 pixelSize = output.vertex.w;
                pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 clipRect = clamp(_ClipRect, -2e10, 2e10);
                output.mask = half4(input.vertex.xy * 2 - clipRect.xy - clipRect.zw,
                    0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // 模様の位置はUVではなくローカル座標。9-sliceやAtlasで光が途切れません。
                float2 position = (input.localPosition - _FlowRect.xy) / max(_FlowRect.zw, float2(0.001,0.001)) * 2 - 1;
                float phase = atan2(position.y, position.x) / 6.2831853 + 0.5;
                float distanceToHead = abs(frac(phase - _Time.y * _FlowSpeed + 0.5) - 0.5);
                float pulse = 1 - smoothstep(0, max(_FlowWidth, 0.001), distanceToHead);
                half4 color = (tex2D(_MainTex, input.uv) + _TextureSampleAdd) * input.color;
                // ぼかした発光Sprite自体は維持。暗部を少し残し、明るい帯だけ巡回させます。
                color.rgb *= 1 + pulse * _FlowStrength;
                color.a *= lerp(1, 0.55 + pulse * 0.45, saturate(_FlowStrength));
                #ifdef UNITY_UI_CLIP_RECT
                    half2 mask = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
                    color.a *= mask.x * mask.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}
