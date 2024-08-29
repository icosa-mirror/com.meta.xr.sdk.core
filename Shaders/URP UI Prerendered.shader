﻿Shader "URP/UI/Prerendered"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal": "12.1" // 2021.3+
        }

        Tags {"Queue"="Transparent" "IgnoreProjector"="True"}
        Pass
        {
            Tags {"RenderType"="Transparent"}

            Blend One OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ ALPHA_SQUARED
            #pragma multi_compile _ EXPENSIVE
            #pragma multi_compile _ OVERLAP_MASK

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
                #if OVERLAP_MASK
                // perform 4x multitap sample, selecting min value
                float2 dx = 0.5 * ddx(i.texcoord);
                float2 dy = 0.5 * ddy(i.texcoord);
                // sample the corners of the pixel
                fixed4 col = min(
                    min(
                        tex2D(_MainTex, i.texcoord + dx + dy),
                        tex2D(_MainTex, i.texcoord - dx + dy)),
                    min(
                        tex2D(_MainTex, i.texcoord + dx - dy),
                        tex2D(_MainTex, i.texcoord - dx - dy)));
                #elif EXPENSIVE
                // perform 4x multitap sample
                float2 dx = 0.25 * ddx(i.texcoord);
                float2 dy = 0.25 * ddy(i.texcoord);
                // sample four points inside the pixel
                fixed4 col = 0.25 * (
                    tex2D(_MainTex, i.texcoord + dx + dy) +
                    tex2D(_MainTex, i.texcoord - dx + dy) +
                    tex2D(_MainTex, i.texcoord + dx - dy) +
                    tex2D(_MainTex, i.texcoord - dx - dy));
                #else
                fixed4 col = tex2D(_MainTex, i.texcoord);
                #endif

                #if ALPHA_SQUARED
                // prerended UI will have a = Alpha * SrcAlpha, so we need to sqrt
                // to get the original alpha value
                col.a = sqrt(col.a);
                #endif

                // It should be noted that with Gamma lighting on PC,
                // the blend will result in not correct colors of transparent
                // portions of the overlay

                col *= _Color;
                return col;
            }
            ENDCG
        }

        Pass
        {
            Name "MotionVectors"
            Tags {
                "Queue" = "Opaque"
                "LightMode" = "MotionVectors"
                "RenderType"="Opaque"
                "RenderPipeline" = "UniversalPipeline"
            }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ ALPHA_SQUARED
            #pragma multi_compile _ EXPENSIVE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #ifndef UNITY_MATRIX_PREV_VP
                // If this version of URP does not support motion vectors
                // fall back to current VP matrix
                #define  UNITY_MATRIX_PREV_VP UNITY_MATRIX_VP
            #endif

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                half2 texcoord : TEXCOORD0;
                float4 curPositionCS : TEXCOORD8;
                float4 prevPositionCS : TEXCOORD9;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            v2f vert(appdata_t v) {
                v2f o;
                o.vertex = mul(unity_MatrixVP, mul(unity_ObjectToWorld, v.vertex));
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                // for motion vectors, only apply camera movement
                o.curPositionCS = o.vertex;
                if (unity_MotionVectorsParams.y == 0.0)
                {
                    o.prevPositionCS = mul(UNITY_MATRIX_PREV_VP, mul(unity_ObjectToWorld, v.vertex));
                }
                else
                {
                    o.prevPositionCS = mul(UNITY_MATRIX_PREV_VP, mul(UNITY_PREV_MATRIX_M, v.vertex));
                }
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                #if EXPENSIVE
                // perform 4x multitap sample
                float2 dx = 0.25 * ddx(i.texcoord);
                float2 dy = 0.25 * ddy(i.texcoord);
                float4 col = 0.25 * (
                    tex2D(_MainTex, i.texcoord + dx + dy) +
                    tex2D(_MainTex, i.texcoord - dx + dy) +
                    tex2D(_MainTex, i.texcoord + dx - dy) +
                    tex2D(_MainTex, i.texcoord - dx - dy));
                #else
                float4 col = tex2D(_MainTex, i.texcoord);
                #endif

                // clip at 80% opaque
                #if ALPHA_SQUARED
                // prerended UI will have a = Alpha * SrcAlpha, so we need to square our cutoff
                clip(col.a - 0.8 * 0.8);
                #else
                clip(col.a - 0.8);
                #endif

                float3 screenPos = i.curPositionCS.xyz / i.curPositionCS.w;
                float3 screenPosPrev = i.prevPositionCS.xyz / i.prevPositionCS.w;
                float4 color = (1);
                color.xyz = screenPos - screenPosPrev;
                return color;
            }
            ENDHLSL
        }
    }

    FallBack "UI/Prerendered"
}
