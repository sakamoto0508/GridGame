Shader "NEON DETONATOR/City Background"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex : POSITION; float4 color : COLOR; };
            struct Output { float4 position : SV_POSITION; float4 color : COLOR; };
            Output vert(Input v)
            {
                Output o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }
            half4 frag(Output i) : SV_Target { return half4(i.color.rgb, 1); }
            ENDHLSL
        }
    }
}
