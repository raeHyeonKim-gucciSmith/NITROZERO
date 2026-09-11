using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace RacingUI {
[UxmlElement] public partial class AmberHudPanel : VisualElement {
 string shapeValue="8,0 92,0 100,15 100,85 92,100 8,100 0,85 0,15";
 float opacityValue=.28f;
 bool helmetValue;
 [UxmlAttribute] public float cornerRadius {get;set;}
 [UxmlAttribute] public string points {get=>shapeValue;set{shapeValue=value;MarkDirtyRepaint();}}
 [UxmlAttribute] public float glassOpacity {get=>opacityValue;set{opacityValue=Mathf.Clamp01(value);MarkDirtyRepaint();}}
 [UxmlAttribute] public bool helmet {get=>helmetValue;set{helmetValue=value;MarkDirtyRepaint();}}
 public AmberHudPanel(){pickingMode=PickingMode.Ignore;generateVisualContent+=Draw;}
 void Draw(MeshGenerationContext ctx){
 var r=contentRect;if(r.width<=0||r.height<=0)return;
 var parts=shapeValue.Split(' ');var v=new Vector2[parts.Length];
 for(int i=0;i<v.Length;i++){var xy=parts[i].Split(',');if(xy.Length!=2)return;
 if(!float.TryParse(xy[0],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float x)||!float.TryParse(xy[1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float y))return;
 v[i]=new Vector2(x*r.width/100,y*r.height/100);}
 var p=ctx.painter2D;
 // Rounded joins prevent acute panel corners from producing long miter spikes.
 p.lineJoin=LineJoin.Round;
 p.lineCap=LineCap.Round;
 void Path(float inset){
 p.BeginPath();
 for(int i=0;i<v.Length;i++){
 var a=Vector2.Lerp(v[i],r.center,inset);
 var prev=Vector2.Lerp(v[(i+v.Length-1)%v.Length],r.center,inset);
 var next=Vector2.Lerp(v[(i+1)%v.Length],r.center,inset);
 float radius=Mathf.Min(cornerRadius,Mathf.Min(Vector2.Distance(prev,a),Vector2.Distance(next,a))*.35f);
 var enter=a+(prev-a).normalized*radius;var leave=a+(next-a).normalized*radius;
 if(i==0)p.MoveTo(enter);else p.LineTo(enter);
 if(radius>0){
 // Explicit samples avoid Painter2D curve-join artifacts on narrow corners.
 for(int step=1;step<=8;step++){
 float t=step/8f,u=1-t;
 p.LineTo(u*u*enter+2*u*t*a+t*t*leave);
 }
 }else p.LineTo(a);
 }p.ClosePath();}
 if(!helmetValue){Path(0);p.fillColor=new Color(.09f,.12f,.15f,opacityValue);p.Fill();}
 // Thin layered illumination preserves a sharp core instead of a thick polygon border.
 foreach(float width in new[]{7f,4f,1.1f}){Path(.002f);p.lineWidth=width;p.strokeColor=new Color(1,.46f,.08f,width>4?.035f:width>2?.09f:.85f);p.Stroke();}
 Path(.025f);p.lineWidth=.65f;p.strokeColor=new Color(1,.65f,.2f,.37f);p.Stroke();
 for(int i=0;i<v.Length;i++){
 var a=v[i];var b=v[(i+1)%v.Length];float len=Vector2.Distance(a,b);if(len<12)continue;
 var d=(b-a).normalized;var n=new Vector2(-d.y,d.x);
 p.BeginPath();p.MoveTo(a+d*5+n*2);p.LineTo(a+d*Mathf.Min(24,len*.3f)+n*2);
 p.lineWidth=helmetValue?2.6f:1.8f;p.strokeColor=new Color(1,.65f,.22f,.9f);p.Stroke();
 if(len>100){for(int k=1;k<4;k++){float t=.42f+k*.024f;p.BeginPath();p.MoveTo(Vector2.Lerp(a,b,t)+n*3);p.LineTo(Vector2.Lerp(a,b,t)+n*6);p.lineWidth=.7f;p.strokeColor=new Color(1,.55f,.12f,.45f);p.Stroke();}}
 }
 }
}
}
namespace RacingUI {
[UnityEngine.UIElements.UxmlElement] public partial class AmberHudIcon : UnityEngine.UIElements.VisualElement {
 [UnityEngine.UIElements.UxmlAttribute] public string symbol {get;set;}="fuel";
 public AmberHudIcon(){pickingMode=UnityEngine.UIElements.PickingMode.Ignore;generateVisualContent+=ctx=>{
 var p=ctx.painter2D;p.strokeColor=new UnityEngine.Color(1,.61f,.21f,1);p.lineWidth=1.5f;
 void Line(params float[] pts){p.BeginPath();for(int i=0;i<pts.Length;i+=2){var v=new UnityEngine.Vector2(pts[i]*contentRect.width/24,pts[i+1]*contentRect.height/24);if(i==0)p.MoveTo(v);else p.LineTo(v);}p.Stroke();}
 if(symbol=="engine"){Line(3,8,8,8,8,5,16,5,16,8,21,8,21,18,5,18,5,15,3,15,3,8);Line(10,2,15,2);Line(12,2,12,5);Line(1,10,1,16);}
 else{Line(3,21,3,3,14,3,14,21,2,21);Line(5,6,12,6,12,11,5,11,5,6);Line(14,9,17,12,17,19,20,19,20,7,17,4);}
 };}
}
}
