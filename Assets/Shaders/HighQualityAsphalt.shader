Shader "NITRO ZERO/High Quality Asphalt"
{
    Properties
    {
        _BaseMap("Asphalt Color", 2D) = "white" {}
        _BumpMap("Asphalt Normal", 2D) = "bump" {}
        _RoughnessMap("Asphalt Roughness", 2D) = "white" {}
        _OcclusionMap("Asphalt Occlusion", 2D) = "white" {}
        _BaseColor("Tint", Color) = (0.78, 0.78, 0.78, 1)
        _BaseWorldSize("Base Pattern Size", Float) = 5.5
        _SecondaryWorldSize("Broken Pattern Size", Float) = 17
        _DetailWorldSize("Micro Normal Size", Float) = 1.05
        _MacroWorldSize("Macro Variation Size", Float) = 47
        _SecondaryBlend("Broken Pattern Blend", Range(0, 0.5)) = 0.26
        _BumpStrength("Main Normal Strength", Range(0, 3)) = 1.25
        _DetailStrength("Micro Normal Strength", Range(0, 2)) = 0.52
        _MacroVariation("Macro Brightness Variation", Range(0, 0.2)) = 0.075
        _RoughnessStrength("Roughness Strength", Range(0, 1)) = 0.88
        _OcclusionStrength("Occlusion Strength", Range(0, 1)) = 0.78
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_RoughnessMap); SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _BaseWorldSize, _SecondaryWorldSize, _DetailWorldSize, _MacroWorldSize;
                half _SecondaryBlend, _BumpStrength, _DetailStrength, _MacroVariation;
                half _RoughnessStrength, _OcclusionStrength;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half fogFactor : TEXCOORD1; };

            float2 RotateUV(float2 uv, float angle)
            {
                float s = sin(angle), c = cos(angle);
                return float2(c * uv.x - s * uv.y, s * uv.x + c * uv.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 worldXZ = input.positionWS.xz;
                float2 baseUV = worldXZ / max(_BaseWorldSize, 0.01);
                float2 secondaryUV = RotateUV(worldXZ, 0.6847) / max(_SecondaryWorldSize, 0.01) + float2(17.31, 9.73);
                float2 detailUV = RotateUV(worldXZ, -0.417) / max(_DetailWorldSize, 0.01) + float2(3.19, 21.67);
                float2 macroUV = RotateUV(worldXZ, 0.193) / max(_MacroWorldSize, 0.01) + float2(41.2, 7.6);

                half3 baseA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV).rgb;
                half3 baseB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, secondaryUV).rgb;
                half macroSample = dot(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, macroUV).rgb, half3(0.299, 0.587, 0.114));
                half irregularMask = smoothstep(0.28, 0.72, macroSample);
                half3 albedo = lerp(baseA, baseB, _SecondaryBlend * irregularMask);
                albedo *= 1.0h + (macroSample - 0.5h) * (2.0h * _MacroVariation);
                albedo *= _BaseColor.rgb;

                half3 normalA = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, baseUV));
                half3 normalDetail = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, detailUV));
                half2 normalXY = normalA.xy * _BumpStrength + normalDetail.xy * _DetailStrength;
                half3 normalWS = normalize(half3(normalXY.x, sqrt(saturate(1.0h - dot(normalXY, normalXY))), normalXY.y));

                half roughA = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, baseUV).r;
                half roughB = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, secondaryUV).r;
                half roughness = saturate(lerp(roughA, roughB, irregularMask * 0.35h) * _RoughnessStrength + 0.08h);
                half occlusion = lerp(1.0h, SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, baseUV).r, _OcclusionStrength);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 ambient = SampleSH(normalWS) * albedo;
                half3 diffuse = albedo * mainLight.color * ndotl * mainLight.shadowAttenuation;
                half3 viewDir = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half3 halfDir = SafeNormalize(mainLight.direction + viewDir);
                half smoothness = 1.0h - roughness;
                half specular = pow(saturate(dot(normalWS, halfDir)), lerp(8.0h, 96.0h, smoothness)) * smoothness * 0.14h;
                half3 color = (ambient + diffuse + mainLight.color * specular) * occlusion;
                return half4(MixFog(color, input.fogFactor), 1.0h);
            }
            ENDHLSL
        }
    }
}
