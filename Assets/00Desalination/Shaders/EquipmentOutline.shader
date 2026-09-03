Shader "00Desalination/EquipmentOutline"
{
    // 인버티드 헐 외곽선. 뒷면(Cull Front)만 그리고, 정점을 클립 공간에서 노멀 방향으로 밀어
    // 오브젝트 스케일/거리와 무관하게 대략 일정한 두께의 테두리를 만든다. URP(Universal) 전용.
    Properties
    {
        _OutlineColor ("Outline Color", Color)      = (0.35, 0.8, 1, 1)
        _OutlineWidth ("Outline Width (px)", Range(0, 12)) = 3
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+1" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nWS   = TransformObjectToWorldNormal(IN.normalOS);
                float4 posCS = TransformWorldToHClip(posWS);

                // 노멀을 클립 공간으로 근사 투영 → 화면상에서 일정 픽셀만큼 바깥으로.
                float3 nCS = mul((float3x3)GetWorldToHClipMatrix(), nWS);
                float2 offset = normalize(nCS.xy + 1e-5) * (_OutlineWidth / _ScreenParams.y) * 2.0 * posCS.w;
                posCS.xy += offset;

                OUT.positionHCS = posCS;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
