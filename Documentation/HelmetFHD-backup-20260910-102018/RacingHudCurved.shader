Shader "UI/Racing HUD Curved"
{
    Properties
    {
        [PerRendererData] _MainTex ("HUD", 2D) = "white" {}
        _Curvature ("Curvature", Range(0, 0.12)) = 0.035
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest Always
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Curvature;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p=i.uv*2-1;
                float2 curved=p*float2(1+_Curvature*.35*p.y*p.y, 1+_Curvature*p.x*p.x);
                float2 uv=curved*.5+.5;
                if(any(uv<0)||any(uv>1)) return 0;
                fixed4 c=tex2D(_MainTex,uv);
                return fixed4(c.rgb*i.color.rgb*i.color.a, c.a*i.color.a);
            }
            ENDCG
        }
    }
}
