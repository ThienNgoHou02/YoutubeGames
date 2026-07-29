Shader "GameYT/Warmup Speed Lines"
{
    Properties
    {
        _Softness ("Line Softness", Range(0.2, 3.0)) = 1.2
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha One
        ColorMask RGB
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            float _Softness;

            struct AppData
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct VertexToFragment
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            VertexToFragment Vert(AppData input)
            {
                VertexToFragment output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            fixed4 Frag(VertexToFragment input) : SV_Target
            {
                float horizontalFade =
                    pow(saturate(sin(input.uv.x * UNITY_PI)), _Softness);
                float verticalFade =
                    pow(saturate(sin(input.uv.y * UNITY_PI)), 0.65);
                float alpha =
                    input.color.a * horizontalFade * verticalFade;
                return fixed4(input.color.rgb, alpha);
            }
            ENDCG
        }
    }
}
