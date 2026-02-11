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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
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
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            float4x4 _GridWorldToLocal;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionWS = positionWS;
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                if (_CutoutEnabled > 0.5 && _MaskEnabled > 0.5)
                {
                    float3 localPos = mul(_GridWorldToLocal, float4(IN.positionWS, 1.0)).xyz;
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

                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                return col;
            }
            ENDHLSL
        }
    }
}
