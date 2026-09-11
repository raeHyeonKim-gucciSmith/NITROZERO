Shader "NITRO ZERO/High Quality Asphalt"
{
    Properties
    {
        _BaseMap("Asphalt Color", 2D) = "white" {}
        _BumpMap("Asphalt Normal", 2D) = "bump" {}
        _RoughnessMap("Asphalt Roughness", 2D) = "white" {}
        _OcclusionMap("Asphalt Occlusion", 2D) = "white" {}
        _BaseMapB("Asphalt 004 Color", 2D) = "white" {}
        _BumpMapB("Asphalt 004 Normal", 2D) = "bump" {}
        _RoughnessMapB("Asphalt 004 Roughness", 2D) = "white" {}
        _OcclusionMapB("Asphalt 004 Occlusion", 2D) = "white" {}
        _BaseMapC("Asphalt 006 Color", 2D) = "white" {}
        _BumpMapC("Asphalt 006 Normal", 2D) = "bump" {}
        _RoughnessMapC("Asphalt 006 Roughness", 2D) = "white" {}
        _OcclusionMapC("Asphalt 006 Occlusion", 2D) = "white" {}
        _BaseColor("Tint", Color) = (0.78, 0.78, 0.78, 1)
        _BlendBTint("Asphalt 004 Tint", Color) = (0.43, 0.44, 0.45, 1)
        _BlendCTint("Asphalt 006 Tint", Color) = (0.48, 0.49, 0.50, 1)
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
            TEXTURE2D(_BaseMapB); SAMPLER(sampler_BaseMapB);
            TEXTURE2D(_BumpMapB); SAMPLER(sampler_BumpMapB);
            TEXTURE2D(_RoughnessMapB); SAMPLER(sampler_RoughnessMapB);
            TEXTURE2D(_OcclusionMapB); SAMPLER(sampler_OcclusionMapB);
            TEXTURE2D(_BaseMapC); SAMPLER(sampler_BaseMapC);
            TEXTURE2D(_BumpMapC); SAMPLER(sampler_BumpMapC);
            TEXTURE2D(_RoughnessMapC); SAMPLER(sampler_RoughnessMapC);
            TEXTURE2D(_OcclusionMapC); SAMPLER(sampler_OcclusionMapC);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _BlendBTint, _BlendCTint;
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

            float2 Hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash22(i).x;
                float b = Hash22(i + float2(1, 0)).x;
                float c = Hash22(i + float2(0, 1)).x;
                float d = Hash22(i + float2(1, 1)).x;
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float MacroNoise(float2 p)
            {
                return ValueNoise(p) * 0.58 + ValueNoise(RotateUV(p, 0.73) * 2.07 + 13.7) * 0.29
                    + ValueNoise(RotateUV(p, -0.41) * 4.11 + 37.2) * 0.13;
            }

            half4 SampleBaseBroken(float2 uv, float2 cells)
            {
                float2 id = floor(cells), f = frac(cells);
                float2 w = f * f * (3.0 - 2.0 * f);
                half4 a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + Hash22(id) * 19.7);
                half4 b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + Hash22(id + float2(1, 0)) * 19.7);
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + Hash22(id + float2(0, 1)) * 19.7);
                half4 d = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + Hash22(id + float2(1, 1)) * 19.7);
                return lerp(lerp(a, b, w.x), lerp(c, d, w.x), w.y);
            }

            half4 SampleNormalBroken(float2 uv, float2 cells)
            {
                float2 id = floor(cells), f = frac(cells);
                float2 w = f * f * (3.0 - 2.0 * f);
                half4 a = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv + Hash22(id) * 23.1);
                half4 b = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv + Hash22(id + float2(1, 0)) * 23.1);
                half4 c = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv + Hash22(id + float2(0, 1)) * 23.1);
                half4 d = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv + Hash22(id + float2(1, 1)) * 23.1);
                return lerp(lerp(a, b, w.x), lerp(c, d, w.x), w.y);
            }

            half SampleRoughnessBroken(float2 uv, float2 cells)
            {
                float2 id = floor(cells), f = frac(cells);
                float2 w = f * f * (3.0 - 2.0 * f);
                half a = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uv + Hash22(id) * 17.3).r;
                half b = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uv + Hash22(id + float2(1, 0)) * 17.3).r;
                half c = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uv + Hash22(id + float2(0, 1)) * 17.3).r;
                half d = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uv + Hash22(id + float2(1, 1)) * 17.3).r;
                return lerp(lerp(a, b, w.x), lerp(c, d, w.x), w.y);
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
                float2 breakCellsA = worldXZ / 22.0;
                float2 breakCellsB = RotateUV(worldXZ, 0.6847) / 39.0;
                float2 uvB = RotateUV(worldXZ, 0.438) / 7.8 + float2(31.7, 8.4);
                float2 uvC = RotateUV(worldXZ, -0.724) / 11.3 + float2(6.2, 43.9);

                half3 baseA = SampleBaseBroken(baseUV, breakCellsA).rgb;
                half3 baseB = SAMPLE_TEXTURE2D(_BaseMapB, sampler_BaseMapB, uvB).rgb * _BlendBTint.rgb;
                half3 baseC = SAMPLE_TEXTURE2D(_BaseMapC, sampler_BaseMapC, uvC).rgb * _BlendCTint.rgb;
                half noiseB = MacroNoise(worldXZ / 34.0);
                half noiseC = MacroNoise(RotateUV(worldXZ, 0.91) / 57.0 + 19.4);
                half weightB = lerp(0.18h, 0.38h, smoothstep(0.18h, 0.82h, noiseB));
                half weightC = lerp(0.14h, 0.32h, smoothstep(0.22h, 0.78h, noiseC));
                half weightA = max(0.30h, 1.0h - weightB - weightC);
                half weightSum = weightA + weightB + weightC;
                weightA /= weightSum; weightB /= weightSum; weightC /= weightSum;
                half macroSample = MacroNoise(worldXZ / 71.0 + 5.8);
                half3 albedo = baseA * _BaseColor.rgb * weightA + baseB * weightB + baseC * weightC;
                albedo *= 1.0h + (macroSample - 0.5h) * (2.0h * _MacroVariation);

                half3 normalA = UnpackNormal(SampleNormalBroken(baseUV, breakCellsA));
                half3 normalDetail = UnpackNormal(SampleNormalBroken(detailUV, RotateUV(worldXZ, -0.417) / 13.0));
                half3 normalB = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMapB, sampler_BumpMapB, uvB));
                half3 normalC = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMapC, sampler_BumpMapC, uvC));
                half2 normalXY = (normalA.xy * weightA + normalB.xy * weightB + normalC.xy * weightC) * _BumpStrength
                    + normalDetail.xy * _DetailStrength * 0.55h;
                half3 normalWS = normalize(half3(normalXY.x, sqrt(saturate(1.0h - dot(normalXY, normalXY))), normalXY.y));

                half roughA = SampleRoughnessBroken(baseUV, breakCellsA);
                half roughB = SAMPLE_TEXTURE2D(_RoughnessMapB, sampler_RoughnessMapB, uvB).r;
                half roughC = SAMPLE_TEXTURE2D(_RoughnessMapC, sampler_RoughnessMapC, uvC).r;
                half roughness = saturate((roughA * weightA + roughB * weightB + roughC * weightC) * _RoughnessStrength + 0.08h);
                half aoA = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, baseUV).r;
                half aoB = SAMPLE_TEXTURE2D(_OcclusionMapB, sampler_OcclusionMapB, uvB).r;
                half aoC = SAMPLE_TEXTURE2D(_OcclusionMapC, sampler_OcclusionMapC, uvC).r;
                half occlusion = lerp(1.0h, aoA * weightA + aoB * weightB + aoC * weightC, _OcclusionStrength);

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
