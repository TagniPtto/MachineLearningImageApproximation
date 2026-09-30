Shader "Unlit/ConvolutionFilter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // Unity provides this automatically

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // float kernel[9] = {
                //     -0.5f, 0, 0.5f,
                //     -0.5f, 0, 0.5f,
                //     -0.5f, 0, 0.5f
                // };
                float kernel[9] = {
                    -1, -1, -1,
                    0, 0, 0,
                    1,1,1
                };

                int2 offset[9] = {
                    int2(-1, -1), int2(0, -1), int2(1, -1),
                    int2(-1,  0), int2(0,  0), int2(1,  0),
                    int2(-1,  1), int2(0,  1), int2(1,  1)
                };

                float2 texelSize = _MainTex_TexelSize.xy;

                fixed4 result = fixed4(0, 0, 0, 0);

                for (int j = 0; j < 9; ++j)
                {
                    float2 sampleUV = i.uv + float2(offset[j]) * texelSize;
                    result += tex2D(_MainTex, sampleUV) * kernel[j];
                }

                float grayness = result.r + result.g + result.b; 
                if(grayness  > 0.01f)
                {
                    return fixed4(1,0,0,1);
                }
                if(grayness < 0.01f)
                {
                    return fixed4(0,1,0,1);
                }
                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}
