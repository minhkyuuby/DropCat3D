Shader "CatDrop3D/GridGroundCutout"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _CutoutEnabled("Cutout Enabled", Float) = 1
        _MaskTex("Boundary Mask", 2D) = "white" {}
        _MaskEnabled("Mask Enabled", Float) = 1
        _GridMin("Grid Min (local)", Vector) = (-0.5, 0, -0.5, 0)
        _GridMax("Grid Max (local)", Vector) = (0.5, 0, 0.5, 0)
        _GridSize("Grid Size", Vector) = (1, 1, 0, 0)
        _CellSize("Cell Size", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _CutoutEnabled;
                float _MaskEnabled;
                float4 _GridMin;
                float4 _GridMax;
                float4 _GridSize;
                float _CellSize;
                float4x4 _GridWorldToLocal;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            void ApplyGridCutout(float3 positionWS)
            {
                if (_CutoutEnabled > 0.5 && _MaskEnabled > 0.5)
                {
                    float3 localPos = mul(_GridWorldToLocal, float4(positionWS, 1.0)).xyz;
                    bool insideX = localPos.x >= _GridMin.x && localPos.x <= _GridMax.x;
                    bool insideZ = localPos.z >= _GridMin.z && localPos.z <= _GridMax.z;
                    if (insideX && insideZ)
                    {
                        float2 cell = floor((localPos.xz - _GridMin.xz) / _CellSize);
                        float2 uv = (cell + 0.5) / _GridSize.xy;
                        half mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv).r;
                        if (mask > 0.5)
                        {
                            clip(-1);
                        }
                    }
                }
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionWS = positionWS;
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                ApplyGridCutout(IN.positionWS);

                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _CutoutEnabled;
                float _MaskEnabled;
                float4 _GridMin;
                float4 _GridMax;
                float4 _GridSize;
                float _CellSize;
                float4x4 _GridWorldToLocal;
            CBUFFER_END

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            void ApplyGridCutout(float3 positionWS)
            {
                if (_CutoutEnabled > 0.5 && _MaskEnabled > 0.5)
                {
                    float3 localPos = mul(_GridWorldToLocal, float4(positionWS, 1.0)).xyz;
                    bool insideX = localPos.x >= _GridMin.x && localPos.x <= _GridMax.x;
                    bool insideZ = localPos.z >= _GridMin.z && localPos.z <= _GridMax.z;
                    if (insideX && insideZ)
                    {
                        float2 cell = floor((localPos.xz - _GridMin.xz) / _CellSize);
                        float2 uv = (cell + 0.5) / _GridSize.xy;
                        half mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv).r;
                        if (mask > 0.5)
                        {
                            clip(-1);
                        }
                    }
                }
            }

            float4 GetShadowPositionHClip(float3 positionWS, float3 normalWS)
            {
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                positionCS = ApplyShadowClamping(positionCS);
                return positionCS;
            }

            ShadowVaryings ShadowVert(ShadowAttributes IN)
            {
                ShadowVaryings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionCS = GetShadowPositionHClip(positionWS, normalWS);
                OUT.positionWS = positionWS;
                return OUT;
            }

            half4 ShadowFrag(ShadowVaryings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                ApplyGridCutout(IN.positionWS);

                return 0;
            }
            ENDHLSL
        }
    }
}
