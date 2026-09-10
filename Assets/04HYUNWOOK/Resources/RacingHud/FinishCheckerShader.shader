Shader "Racing/Finish Checker"
{
 Properties { _MainTex ("Checker", 2D) = "white" {} }
 SubShader
 {
  Tags { "RenderType"="Opaque" "Queue"="Geometry+10" }
  Cull Off ZWrite On Offset -1, -1
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex; float4 _MainTex_ST;
   struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
   v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.texcoord,_MainTex); return o; }
   fixed4 frag(v2f i):SV_Target { return tex2D(_MainTex,i.uv); }
   ENDCG
  }
 }
}
