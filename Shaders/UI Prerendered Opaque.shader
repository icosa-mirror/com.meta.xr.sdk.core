﻿Shader "UI/Prerendered Opaque"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags {"Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Opaque"}

        Blend One Zero, Zero Zero

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ WITH_CLIP
            #pragma multi_compile _ EXPENSIVE

            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                half2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert(appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                #if !WITH_CLIP
                    if (_Color.x == 0 && _Color.y == 0 && _Color.z == 0)
                    {
                        return _Color;
                    }
                #endif
                #if EXPENSIVE
                // perform 4x multitap sample
                float2 dx = 0.25 * ddx(i.texcoord);
                float2 dy = 0.25 * ddy(i.texcoord);
                fixed4 col = 0.25 * (
                    tex2D(_MainTex, i.texcoord + dx + dy) +
                    tex2D(_MainTex, i.texcoord - dx + dy) +
                    tex2D(_MainTex, i.texcoord + dx - dy) +
                    tex2D(_MainTex, i.texcoord - dx - dy));
                #else
                fixed4 col = tex2D(_MainTex, i.texcoord);
                #endif

                col *= _Color;

                #if WITH_CLIP
                    clip(col.a - 0.5);
                #endif
                return col;
            }
            ENDCG
        }
    }
}
