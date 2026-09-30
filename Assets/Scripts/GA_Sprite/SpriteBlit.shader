Shader "Custom/SpriteBlit"
{
    Properties
    {
        _MainTex ("Paper Texture", 2D) = "white" {}
        _SpriteTex ("Sprite Texture", 2D) = "white" {}
        _Position ("Position", Vector) = (0, 0, 0, 0)
        _Scale ("Scale", Vector) = (1, 1, 0, 0)
        _Rotation ("Rotation", Float) = 0
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha // This is the cause of WHY  blitfirst then copy doesnt WORK but copy then blit works
            ZWrite Off
            Cull Off

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _SpriteTex;
            float4 _Position;
            float4 _Scale;
            float _Rotation;
            float4 _Color;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                float2 pos = v.vertex.xy;

                // Offset so that (0,0) is the center (from bottom-left corner)
                pos -= 0.5; // Shift by half so pivot is center

                // Apply scale
                pos *= _Scale.xy;

                // Apply rotation
                float sinTheta = sin(_Rotation);
                float cosTheta = cos(_Rotation);
                pos = float2(
                    pos.x * cosTheta - pos.y * sinTheta,
                    pos.x * sinTheta + pos.y * cosTheta
                );

                // Shift back
                pos += 0.5;

                // Apply position
                pos += _Position.xy;

                o.vertex = UnityObjectToClipPos(float4(pos, 0, 1));
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 sprite_col = tex2D(_SpriteTex, i.uv);
                return sprite_col * _Color;
            }
            ENDCG
        }
    }
}