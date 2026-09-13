Shader "DAMIN/Reusable Air Refraction"
{
 Properties {
  _AirBackground("Clean background",2D)="black"{}
  _Strength("Strength",Float)=1
  _Pixels("Pixel displacement",Float)=22
  _Mask("Mask",Float)=1
  _NoiseScale("Noise Scale",Float)=4
  _NoiseSpeed("Noise Speed",Float)=4
  _FineDetail("Fine Detail",Float)=.6
  _Clock("Clock",Float)=0
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass {
   Name "AirRefraction"
   Tags {"LightMode"="SRPDefaultUnlit"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_AirBackground);SAMPLER(sampler_AirBackground);
   CBUFFER_START(UnityPerMaterial)
   float _Strength,_Pixels,_Mask,_NoiseScale,_NoiseSpeed,_FineDetail,_Clock;
   CBUFFER_END
   struct Attributes{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct Varyings{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 screen:TEXCOORD1;};
   Varyings Vert(Attributes i){Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.screen=ComputeScreenPos(o.positionCS);return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
   float field(float2 p){return noise(p)*.6+noise(p*2.17+float2(3.7,9.2))*.28+noise(p*4.31-5.1)*.12;}
   half4 Frag(Varyings i):SV_Target {
    float d=1-i.uv.x,y=i.uv.y*2-1;
    float t=_Clock*_NoiseSpeed*.14;
    float radius=.06+.92*pow(saturate(d/.78),.58);
    float wave=(field(float2(d*7-t,y*2+t*.37))-.5)*.12*smoothstep(.12,.55,d);
    float q=abs(y-wave)/radius;
    float footprint=(1-smoothstep(.84,1,q))*smoothstep(0,.045,d)*(1-smoothstep(.78,1,d));
    float band=exp(-pow((q-.69)/.15,2));
    float wake=smoothstep(.13,.36,d)*(1-smoothstep(.68,1,d));
    float2 p=float2(d*_NoiseScale*3.1+t,y*_NoiseScale*1.7-t*.23);
    float e=.07;
    float2 g=float2(field(p+float2(e,0))-field(p-float2(e,0)),field(p+float2(0,e))-field(p-float2(0,e)))/(2*e);
    float2 small=float2(noise(p*3.3+5)-.5,noise(p.yx*3.7-7)-.5);
    float outer=exp(-pow((q-.84)/.065,2));
    float2 bend=float2(-.22,sign(y-wave)*1.25)*(band-outer*.55);
    bend+=float2(g.y,-g.x)*wake*.28+small*wake*_FineDetail*.18;
    bend+=float2(-.1,y*.18)*(1-smoothstep(.3,.95,q))*(1-d);
    float2 screen=i.screen.xy/i.screen.w;
    float2 dx=ddx(screen),dy=ddy(screen),ux=ddx(i.uv),uy=ddy(i.uv);
    float det=ux.x*uy.y-ux.y*uy.x;float safeDet=abs(det)>1e-8?det:(det<0?-1e-8:1e-8);
    float2 along=normalize(((dx*uy.y-dy*ux.y)/safeDet)*_ScaledScreenParams.xy+float2(1e-6,0));
    float2 across=normalize(((-dx*uy.x+dy*ux.x)/safeDet)*_ScaledScreenParams.xy+float2(0,1e-6));
    float2 offset=(along*bend.x+across*bend.y)*_Pixels*_Strength*footprint/_ScaledScreenParams.xy;
    float2 sampleUV=clamp(screen+offset,1/_ScaledScreenParams.xy,1-1/_ScaledScreenParams.xy);
    float3 color=SAMPLE_TEXTURE2D(_AirBackground,sampler_AirBackground,sampleUV).rgb;
    return half4(color,saturate(footprint*5)*_Mask*step(.00001,_Strength*_Pixels));
   }
   ENDHLSL
  }
 }
}
