#ifndef DAMIN_BULLET_AIR_REFRACTION
#define DAMIN_BULLET_AIR_REFRACTION
#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#endif
// Pure background resampling. No smoke texture, color tint, emission, or additive light.
void BulletAirRefraction_float(float4 UV, float4 Data, float4 ScreenUV,
    float DistortionStrength, float NoiseScale, float NoiseSpeed,
    float RefractionOffset, float OpacityMask, float EffectTime,
    out float3 Color, out float Alpha)
{
    float2 p=UV.xy*2-1;
    float phase=EffectTime*NoiseSpeed;
    float2 n=float2(sin(UV.x*NoiseScale*6.283+phase)*cos(UV.y*8.3-phase*.6),
                    sin(UV.x*NoiseScale*9.13-phase*.8)*cos(UV.y*6.1+phase*.7));
    float mask=0;float2 bend=0;
    if(Data.x<.5){
        float r=length(p);
        mask=(1-smoothstep(.35,1,r))*smoothstep(0,.15,r);
        bend=-p*.8+n*.09;
    }else if(Data.x<1.5){
        float edge=1-smoothstep(.2,1,abs(p.y));
        float tail=smoothstep(0,.65,UV.x);
        float head=1-smoothstep(.94,1,UV.x);
        mask=edge*tail*head;
        bend=float2(n.x*.16,p.y*.22+n.y*.34);
    }else{
        // Thin pressure arc. Still refraction only, never a colored ring.
        float r=length(float2(p.x*.95,p.y));
        mask=(1-smoothstep(.025,.10,abs(r-.65)))*smoothstep(-.55,.15,p.x);
        bend=normalize(p+float2(.0001,0))*.55;
    }
    float power=max(0,DistortionStrength)*saturate(Data.y);
    Alpha=saturate(mask*OpacityMask)*step(.00001,power*RefractionOffset);
#ifdef SHADERGRAPH_PREVIEW
    Color=.3.xxx;Alpha=0;
#else
    float2 screen=ScreenUV.xy;
    float2 dx=ddx(screen),dy=ddy(screen),ux=ddx(UV.xy),uy=ddy(UV.xy);
    float det=ux.x*uy.y-ux.y*uy.x;
    float safeDet=abs(det)>.00000001?det:(det<0?-.00000001:.00000001);
    float2 along=(dx*uy.y-dy*ux.y)/safeDet;
    float2 across=(-dx*uy.x+dy*ux.x)/safeDet;
    float2 pixels=_ScaledScreenParams.xy;
    along=normalize(along*pixels+float2(.000001,0));
    across=normalize(across*pixels+float2(0,.000001));
    float2 offset=(along*bend.x+across*bend.y)*RefractionOffset*power*mask/pixels;
    float border=min(min(screen.x,1-screen.x),min(screen.y,1-screen.y));
    Alpha*=smoothstep(0,.008,border);
    Color=SampleSceneColor(clamp(screen+offset,1/pixels,1-1/pixels));
#endif
}
#endif
