Shader "00Desalination/EquipmentRim"
{
    // 설비 하이라이트용 프레넬 림 글로우. 원본 머티리얼 위에 한 패스 더 얹어 가장자리만 빛나게 한다.
    // 애디티브 블렌드 + ZWrite Off 라 형상은 안 가리고 테두리 광채만 더해진다. URP(Universal) 전용.
    Properties
    {
        _RimColor     ("Rim Color", Color)          = (0.35, 0.8, 1, 1)
        _RimPower     ("Rim Power", Range(0.5, 8))   = 3
        _RimIntensity ("Rim Intensity", Range(0, 6)) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Rim"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _RimColor;
                float  _RimPower;
                float  _RimIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = pos.positionCS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS   = GetWorldSpaceViewDir(pos.positionWS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                float3 v = normalize(IN.viewDirWS);
                float  fres = pow(saturate(1.0 - saturate(dot(n, v))), _RimPower);
                half3  col  = _RimColor.rgb * fres * _RimIntensity;
                return half4(col, fres);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
