using UnityEngine;
using UnityEngine.UIElements;
namespace RacingUI
{
    /// <summary>Open helmet rim: side curves stop at the instrument instead of crossing it.</summary>
    [UxmlElement]
    public partial class FpsAmberFrame : VisualElement
    {
        public FpsAmberFrame() { pickingMode = PickingMode.Ignore; generateVisualContent += Draw; }
        void Draw(MeshGenerationContext ctx)
        {
            if (contentRect.width <= 0 || contentRect.height <= 0) return;
            var p = ctx.painter2D;
            Vector2 V(float x, float y) => new Vector2(x * contentRect.width / 1672f, y * contentRect.height / 941f);
            void Stroke(Vector2[] points, float width, Color color)
            {
                p.BeginPath(); p.MoveTo(points[0]);
                for (int i = 1; i < points.Length; i++) p.LineTo(points[i]);
                p.lineWidth = width; p.strokeColor = color; p.Stroke();
            }
            // Dense smooth samples keep the long visor edges curved at every resolution.
            var left = new System.Collections.Generic.List<Vector2> { V(418,735.4f), V(319,825), V(110,825), V(76,756), V(119,711) };
            // A single cubic joins the lower shoulder to the upper visor corner.
            // Its tangents avoid the pinched joins of the previous sine/straight sections.
            for(int i=1;i<=128;i++) {
                float t=i/128f, u=1-t;
                left.Add(V(u*u*u*119+3*u*u*t*24+3*u*t*t*17+t*t*t*87,
                    u*u*u*711+3*u*u*t*554+3*u*t*t*420+t*t*t*269));
            }
            left.Add(V(20,202));left.Add(V(31,71));left.Add(V(91,28));
            var right = new Vector2[left.Count];
            for(int i=0;i<right.Length;i++) right[i]=new Vector2(contentRect.width-left[i].x,left[i].y);
            foreach(var line in new[]{left.ToArray(),right}) {
                Stroke(line,9,new Color(1,.46f,.06f,.035f));
                Stroke(line,4,new Color(1,.48f,.09f,.10f));
                Stroke(line,1.15f,new Color(1,.55f,.13f,.75f));
                var inset=new Vector2[line.Length];
                for(int i=0;i<inset.Length;i++) {
                    Vector2 tangent = (line[Mathf.Min(i+1,line.Length-1)]-line[Mathf.Max(i-1,0)]).normalized;
                    Vector2 normal = new Vector2(-tangent.y,tangent.x);
                    if(Vector2.Dot(normal,contentRect.center-line[i])<0) normal=-normal;
                    inset[i]=line[i]+normal*(9f*contentRect.width/1672f);
                }
                Stroke(inset,.6f,new Color(1,.57f,.16f,.28f));
            }
            Stroke(new[]{V(91,28),V(429,46),V(451,39),V(1221,39),V(1243,46),V(1581,28)},.7f,new Color(1,.51f,.12f,.25f));
            void Lit(float ax,float ay,float bx,float by,int count) {
                for(int i=0;i<count;i++) {
                    var a=V(Mathf.Lerp(ax,bx,(i+.04f)/count),Mathf.Lerp(ay,by,(i+.04f)/count));
                    var b=V(Mathf.Lerp(ax,bx,(i+.85f)/count),Mathf.Lerp(ay,by,(i+.85f)/count));
                    Stroke(new[]{a,b},18,new Color(1,.43f,.03f,.035f));
                    Stroke(new[]{a,b},12,new Color(1,.48f,.04f,.12f));
                    // Angled ends match the original visor's luminous tiles.
                    Vector2 d=(b-a).normalized, n=new Vector2(-d.y,d.x);
                    float half=3.4f*contentRect.height/941f;
                    p.BeginPath();p.MoveTo(a-n*half+d*2);p.LineTo(b-n*half+d*2);
                    p.LineTo(b+n*half-d*2);p.LineTo(a+n*half-d*2);p.ClosePath();
                    p.fillColor=new Color(1,.59f,.15f,.97f);p.Fill();
                }
            }
            Lit(38,221,79,257,3);Lit(1634,221,1593,257,3);
            Lit(76,756,113,822,2);Lit(113,822,319,822,7);Lit(319,822,411,748,4);
            Lit(1596,756,1559,822,2);Lit(1559,822,1353,822,7);Lit(1353,822,1261,748,4);
            Lit(508,720,552,720,4);Lit(1120,720,1164,720,4);
            // The instrument owns the shoulder rails; do not draw a second floating outline here.
        }
    }
}
