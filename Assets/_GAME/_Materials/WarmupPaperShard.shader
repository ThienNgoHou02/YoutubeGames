Shader "Game YT/Warmup/Paper Shard"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _EdgeColor ("Paper Edge", Color) = (0.82,0.84,0.88,1)
        _EdgeWidth ("Edge Width", Range(0,0.08)) = 0.018
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _EdgeColor;
            float _EdgeWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.uv) * _Color;
                float2 edgeDistance = min(input.uv, 1.0 - input.uv);
                float edge = 1.0 - smoothstep(
                    0.0,
                    _EdgeWidth,
                    min(edgeDistance.x, edgeDistance.y));
                color.rgb = lerp(color.rgb, _EdgeColor.rgb, edge * 0.45);
                return color;
            }
            ENDCG
        }
    }

    Fallback "Unlit/Transparent"
}
