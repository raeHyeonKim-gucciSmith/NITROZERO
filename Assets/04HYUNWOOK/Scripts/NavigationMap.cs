using UnityEngine;
using UnityEngine.UIElements;

namespace RacingUI
{
    /// <summary>Editable perspective view of the current straight road, with live vehicle motion.</summary>
    [UxmlElement]
    public partial class NavigationMap : VisualElement
    {
        float roadWidthValue = .78f, horizonValue = .22f, hazeValue = .9f;
        float lateral, heading, travelled;
        [UxmlAttribute]
        public float roadWidth { get => roadWidthValue; set { roadWidthValue = Mathf.Clamp(value, .3f, 1.2f); MarkDirtyRepaint(); } }
        [UxmlAttribute]
        public float horizon { get => horizonValue; set { horizonValue = Mathf.Clamp(value, .1f, .45f); MarkDirtyRepaint(); } }
        [UxmlAttribute]
        public float haze { get => hazeValue; set { hazeValue = Mathf.Clamp01(value); MarkDirtyRepaint(); } }

        public NavigationMap()
        {
            pickingMode = PickingMode.Position;
            generateVisualContent += Draw;
            RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
        }
        public void SetVehicle(float lane, float angle, float distance)
        {
            lateral = Mathf.Clamp(lane, -.65f, .65f);
            heading = Mathf.Clamp(angle, -45f, 45f);
            travelled = distance;
            MarkDirtyRepaint();
        }
        Vector2 Project(float side, float depth)
        {
            Rect r = contentRect;
            float near = depth * depth;
            float center = Mathf.Lerp(.5f - heading * .002f, .5f - lateral * .6f, near);
            float width = Mathf.Lerp(.045f, roadWidthValue, near);
            return new Vector2((center + side * width) * r.width,
                Mathf.Lerp(horizonValue, 1.1f, depth) * r.height);
        }
        static void Quad(Painter2D p, Color color, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            p.fillColor = color; p.BeginPath(); p.MoveTo(a); p.LineTo(b);
            p.LineTo(c); p.LineTo(d); p.ClosePath(); p.Fill();
        }
        void Strip(Painter2D p, Color color, float left, float right, float from, float to)
        { Quad(p, color, Project(left, from), Project(right, from), Project(right, to), Project(left, to)); }
        void Draw(MeshGenerationContext context)
        {
            Rect r = contentRect;
            if (r.width <= 0 || r.height <= 0) return;
            var p = context.painter2D;
            // Layered dark haze has no world imagery: only the road remains distinct.
            for (int i = 0; i < 40; i++)
            {
                float a = i / 40f, b = (i + 1) / 40f;
                float mist = .025f + .055f * Mathf.Exp(-Mathf.Pow((a - horizonValue) / .23f, 2));
                Quad(p, new Color(mist, mist * 1.08f, mist * 1.15f, Mathf.Lerp(.75f, 1f, hazeValue)),
                    new Vector2(0, a*r.height), new Vector2(r.width, a*r.height),
                    new Vector2(r.width, b*r.height+.5f), new Vector2(0, b*r.height+.5f));
            }
            for (int i = 0; i < 24; i++)
            {
                float a=i/24f, b=(i+1)/24f;
                float shade=Mathf.Lerp(.10f,.23f,b);
                Strip(p,new Color(shade,shade,shade*1.04f,1),-.5f,.5f,a,b);
            }
            Strip(p,new Color(.83f,.86f,.89f,.8f),-.505f,-.487f,0,1);
            Strip(p,new Color(.83f,.86f,.89f,.8f),.487f,.505f,0,1);
            // Soft red route glow, then the crisp guidance line.
            for (int i=6;i>=1;i--)
                Strip(p,new Color(1f,.08f,.08f,.035f),-.012f-i*.012f,.012f+i*.012f,0,.88f);
            Strip(p,new Color(1f,.18f,.18f,.9f),-.015f,.015f,0,.88f);
            float phase=Mathf.Repeat(travelled/100f,1f);
            for(int i=0;i<10;i++)
            {
                float a=Mathf.Repeat(i/10f+phase,1f),b=Mathf.Min(1f,a+.038f);
                Strip(p,new Color(.93f,.94f,.95f,.75f),-.26f,-.245f,a,b);
                Strip(p,new Color(.93f,.94f,.95f,.75f),.245f,.26f,a,b);
            }
            Vector2 at=new Vector2(r.width*.5f,r.height*.78f);
            float size=Mathf.Min(r.width,r.height)*.075f;
            Quad(p,new Color(0,0,0,.6f),at+new Vector2(-size*1.25f,size*.9f),
                at+new Vector2(0,-size*1.4f),at+new Vector2(size*1.25f,size*.9f),at+new Vector2(0,size*.45f));
            Quad(p,new Color(1f,.2f,.15f,1),at+new Vector2(-size,size*.65f),
                at+new Vector2(0,-size),at+new Vector2(size,size*.65f),at+new Vector2(0,size*.25f));
        }
    }
}
