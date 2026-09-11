using UnityEngine;
using UnityEngine.UIElements;
namespace RacingUI {
[UxmlElement] public partial class AmberMapFrame : VisualElement {
 [UxmlAttribute] public bool footer {get;set;} = true;
 public AmberMapFrame(){pickingMode=PickingMode.Ignore;generateVisualContent+=Draw;}
 void Draw(MeshGenerationContext ctx){
  float w=contentRect.width,h=contentRect.height;if(w<=0||h<=0)return;
  var p=ctx.painter2D;
  void Line(Vector2[] a,float width,Color color){p.BeginPath();p.MoveTo(a[0]);for(int i=1;i<a.Length;i++)p.LineTo(a[i]);p.lineWidth=width;p.strokeColor=color;p.Stroke();}
  p.BeginPath();p.MoveTo(new Vector2(1,1));p.LineTo(new Vector2(w-1,1));p.LineTo(new Vector2(w-1,h-1));p.LineTo(new Vector2(1,h-1));p.ClosePath();p.fillColor=new Color(.035f,.12f,.19f,.20f);p.Fill();
  Line(new[]{new Vector2(1,1),new Vector2(w-1,1),new Vector2(w-1,h-1),new Vector2(1,h-1),new Vector2(1,1)},.65f,new Color(1,.61f,.20f,.65f));
  if(footer) Line(new[]{new Vector2(1,h-29),new Vector2(w-1,h-29)},.65f,new Color(1,.61f,.20f,.30f));
  foreach(float x in new[]{1f,w-1})foreach(float y in new[]{1f,h-1}){
   float dx=x<w/2?1:-1,dy=y<h/2?1:-1;
   var corner=new[]{new Vector2(x,y+dy*8),new Vector2(x,y),new Vector2(x+dx*8,y)};
   Line(corner,8,new Color(1,.57f,.10f,.04f));Line(corner,4,new Color(1,.66f,.20f,.13f));Line(corner,1.7f,new Color(1,.73f,.30f,1));
  }
 }
}}
