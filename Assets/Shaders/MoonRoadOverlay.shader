Shader "NITRO ZERO/Moon Road Overlay"
{
    Properties
    {
        _BaseMap("Detail Texture", 2D) = "white" {}
        _OpacityMap("Opacity", 2D) = "white" {}
        _Tint("Tint", Color) = (0.65,0.64,0.60,1)
        _RepeatLength("Repeat Length", Float) = 4
        _Opacity("Opacity", Range(0,1)) = 0.8
        _NoiseScale("Wear Scale", Float) = 11
        _Cutoff("Cutoff", Range(0,1)) = 0.12
        _IsDust("Dust Mode", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest+10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_OpacityMap); SAMPLER(sampler_OpacityMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _RepeatLength, _NoiseScale;
                half _Opacity, _Cutoff, _IsDust;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float2 uv:TEXCOORD1; };
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
            }
            Varyings Vert(Attributes i)
            {
                Varyings o; VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS=p.positionCS; o.positionWS=p.positionWS; o.uv=i.uv; return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 uv=float2(i.uv.x, i.uv.y / max(_RepeatLength,0.01));
                half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb;
                half broad=Noise(i.positionWS.xz/max(_NoiseScale,0.01));
                half fine=Noise(i.positionWS.xz/max(_NoiseScale*0.27,0.01)+19.3);
                half wear=smoothstep(0.16h,0.78h,broad*0.7h+fine*0.3h);
                half wander=(broad-0.5h)*0.26h+(fine-0.5h)*0.12h;
                half edgeDust=1.0h-smoothstep(0.19h,0.98h,saturate(i.uv.x+wander));
                half lineEdge=smoothstep(0.0h,0.08h,i.uv.x)*smoothstep(0.0h,0.08h,1.0h-i.uv.x);
                half chips=smoothstep(0.82h,0.95h,Noise(i.positionWS.xz*1.37h+31.7h));
                half lineAlpha=lineEdge*lerp(0.70h,1.0h,wear)*(1.0h-chips*0.78h);
                half scattered=smoothstep(0.66h,0.86h,Noise(i.positionWS.xz/8.5h+47.1h));
                half dustAlpha=saturate(edgeDust*lerp(0.42h,0.82h,wear)+scattered*0.06h);
                half alpha=lerp(lineAlpha,dustAlpha,_IsDust)*_Opacity;
                clip(alpha-_Cutoff);
                half3 albedo=lerp(_Tint.rgb,tex*_Tint.rgb*1.12h,_IsDust);
                Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half lighting=0.28h+0.72h*saturate(light.direction.y)*light.shadowAttenuation;
                half3 dustColor=albedo*light.color*lighting;
                // Painted lines stay pale even when the road sits in a lunar shadow.
                half3 paintColor=lerp(_Tint.rgb*0.88h,dustColor,0.16h);
                return half4(lerp(paintColor,dustColor,_IsDust),alpha);
            }
            ENDHLSL
        }
    }
}
