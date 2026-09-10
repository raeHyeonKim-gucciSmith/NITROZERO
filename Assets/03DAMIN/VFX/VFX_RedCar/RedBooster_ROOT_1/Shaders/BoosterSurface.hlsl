#ifndef NITRO_ZERO_BOOSTER_SURFACE_INCLUDED
#define NITRO_ZERO_BOOSTER_SURFACE_INCLUDED
#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#endif

void BoosterGlow_float(float2 UV, float3 Tint, float Intensity, float Halo, out float3 Color, out float Alpha)
{
    float radius=length((UV-0.5)*2);
    float hot=saturate(1-radius);
    float radial=radius<0.25 ? 1 : radius<0.55 ? lerp(1,0.45,(radius-0.25)/0.30) : lerp(0.45,0,saturate((radius-0.55)/0.45));
    float3 middle=lerp(Tint,1.0.xxx,0.42);
    float3 chroma=radius<0.2 ? lerp(1.0.xxx,middle,radius/0.2) : lerp(middle,Tint,saturate((radius-0.2)/0.4));
    float3 hotChroma=lerp(Tint,1.0.xxx,saturate(1-radius*1.8));
    Color=lerp(hotChroma,chroma,saturate(Halo))*Intensity;
    Alpha=lerp(radial,pow(hot,2.6)*0.22,saturate(Halo));
}
void BoosterDistortion_float(float2 UV, float4 ScreenUV, float Strength, float NoiseSpeed, float NoiseScale, out float3 Color, out float Alpha)
{
    float time=0;
#ifndef SHADERGRAPH_PREVIEW
    time=_Time.y;
#endif
    float2 p=UV*NoiseScale*6.2831853;
    float2 n=float2(sin(p.y*1.37+time*NoiseSpeed)*cos(p.x*1.91-time*NoiseSpeed*.73),
                    cos(p.x*1.61+time*NoiseSpeed*.83)*sin(p.y*2.13+time*NoiseSpeed*.61));
    float across=pow(saturate(1-abs(UV.x*2-1)),2);
    // Axial billboard's +Y points into local -Z: UV.y is the distance down the plume.
    float tail=1-smoothstep(0,1,UV.y);
    float nozzle=smoothstep(0,.08,UV.y);
    Alpha=across*tail*nozzle*.65;
#ifdef SHADERGRAPH_PREVIEW
    Color=0.25.xxx;
#else
    float2 screen=ScreenUV.xy;
    float2 offset=n*Strength*.015*Alpha;
    Color=SampleSceneColor(saturate(screen+offset));
#endif
}
#endif
