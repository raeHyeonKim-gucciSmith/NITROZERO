using UnityEngine;
using UnityEngine.UIElements;

namespace RacingUI
{
    /// <summary>Editable perspective view of the current straight road, with live vehicle motion.</summary>
    [UxmlElement]
    public partial class NavigationMap : VisualElement
    {
        float roadWidthValue = .55f, horizonValue = .22f, hazeValue = 1f;
        float lateral, heading;
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
            MarkDirtyRepaint();
        }
        Vector2 Project(float side, float depth)
        {
            Rect r = contentRect;
            // Both boundaries and the road center meet at the same vanishing point.
            float near = Mathf.Clamp01(depth);
            float center = Mathf.Lerp(.5f - heading * .002f, .5f - lateral * .6f, near);
            float width = roadWidthValue * near;
            return new Vector2((center + side * width) * r.width,
                Mathf.Lerp(horizonValue, 1.1f, depth) * r.height);
        }
        static void Quad(Painter2D p, Color color, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            p.fillColor = color; p.BeginPath(); p.MoveTo(a); p.LineTo(b);
            p.LineTo(c); p.LineTo(d); p.ClosePath(); p.Fill();
        }
        void DrawRoad(MeshGenerationContext context)
        {
            // Shared vertices avoid the antialiasing seams between separately painted strips.
            const int steps = 80, columns = 4;
            var mesh = context.Allocate((steps + 1) * columns, steps * 18);
            for (int i = 0; i <= steps; i++)
            {
                float depth = i / (float)steps;
                float visibility = Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(0f, Mathf.Lerp(.18f, .46f, hazeValue), depth));
                float shade = Mathf.Lerp(.44f, .56f, depth);
                for (int column = 0; column < columns; column++)
                {
                    float side = column == 0 ? -.516f : column == 1 ? -.5f : column == 2 ? .5f : .516f;
                    Vector2 point = Project(side, depth);
                    mesh.SetNextVertex(new Vertex {
                        position = new Vector3(point.x, point.y, Vertex.nearZ),
                        tint = new Color(shade, shade * 1.01f, shade * .98f,
                            column == 0 || column == 3 ? 0f : visibility),
                        uv = Vector2.zero
                    });
                }
            }
            for (int row = 0; row < steps; row++)
                for (int column = 0; column < columns - 1; column++)
                {
                    ushort a = (ushort)(row * columns + column), b = (ushort)(a + 1);
                    ushort d = (ushort)(a + columns), c = (ushort)(d + 1);
                    mesh.SetNextIndex(a); mesh.SetNextIndex(b); mesh.SetNextIndex(c);
                    mesh.SetNextIndex(c); mesh.SetNextIndex(d); mesh.SetNextIndex(a);
                }
        }
        void DrawLaneDashes(Painter2D painter)
        {
            // Equal road-space intervals project to shorter, tighter dashes in the distance.
            // Use the road projection for every corner so both lanes share its vanishing point.
            for (int lane = -1; lane <= 1; lane += 2)
            {
                float side = lane / 6f;
                for (int dash = 0; dash < 36; dash++)
                {
                    float distance = 1f + dash * .65f;
                    float near = 1f / distance;
                    float far = 1f / (distance + .32f);
                    float visibility = Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(0f, Mathf.Lerp(.18f, .46f, hazeValue), (near + far) * .5f));
                    Quad(painter, new Color(.86f, .87f, .85f, visibility * .8f),
                        Project(side - .007f, far), Project(side + .007f, far),
                        Project(side + .007f, near), Project(side - .007f, near));
                }
            }
        }
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
            DrawRoad(context);
            DrawLaneDashes(p);
            Vector2 at=new Vector2(r.width*.5f,r.height*.78f);
            float size=Mathf.Min(r.width,r.height)*.075f;
            Quad(p,new Color(0,0,0,.6f),at+new Vector2(-size*1.25f,size*.9f),
                at+new Vector2(0,-size*1.4f),at+new Vector2(size*1.25f,size*.9f),at+new Vector2(0,size*.45f));
            Quad(p,new Color(1f,.2f,.15f,1),at+new Vector2(-size,size*.65f),
                at+new Vector2(0,-size),at+new Vector2(size,size*.65f),at+new Vector2(0,size*.25f));
        }
    }
}
