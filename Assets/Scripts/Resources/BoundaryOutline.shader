Shader "GridBomber/BoundaryOutline"
{
    Properties { _Color ("Color", Color) = (0.4, 0.75, 1, 1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            struct Input { float4 vertex : POSITION; };
            struct Output { float4 position : SV_POSITION; };
            Output vert(Input v) { Output o; o.position = UnityObjectToClipPos(v.vertex); return o; }
            float4 frag(Output i) : SV_Target { return _Color; }
            ENDHLSL
        }
    }
}
