using UnityEngine;
using UnityEngine.UIElements;
namespace RacingUI {
[UxmlElement] public partial class AmberTimerFrame : VisualElement {
 public AmberTimerFrame(){pickingMode=PickingMode.Ignore;generateVisualContent+=ctx=>{
 var p=ctx.painter2D;float w=contentRect.width,h=contentRect.height;if(w<=0||h<=0)return;
 void Line(float width,Color color,params Vector2[] a){p.BeginPath();p.MoveTo(a[0]);for(int i=1;i<a.Length;i++)p.LineTo(a[i]);p.lineWidth=width;p.strokeColor=color;p.Stroke();}
 Vector2 V(float x,float y)=>new Vector2(x*w,y*h);
 var rail=new[]{V(0,.15f),V(.12f,.88f),V(.17f,.96f),V(.83f,.96f),V(.88f,.88f),V(1,.15f)};
 Line(4,new Color(1,.6f,.2f,.05f),rail);Line(.8f,new Color(1,.65f,.25f,.7f),rail);
 for(int side=0;side<2;side++){
 Vector2 M(float x,float y)=>V(side==0?x:1-x,y);
 Line(1,new Color(1,.65f,.25f,.7f),M(.02f,.06f),M(.11f,.61f),M(.14f,.60f));
 Line(.6f,new Color(1,.65f,.25f,.38f),M(.065f,.16f),M(.13f,.51f));
 }
 Line(.7f,new Color(1,.6f,.2f,.5f),V(.43f,.02f),V(.57f,.02f));
 };}
}}
