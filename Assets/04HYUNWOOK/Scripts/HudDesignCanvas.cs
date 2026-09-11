using UnityEngine;
using UnityEngine.UIElements;

namespace RacingUI
{
    /// <summary>One authored coordinate space, uniformly fitted to any screen or UI Builder viewport.</summary>
    [UxmlElement]
    public partial class HudDesignCanvas : VisualElement
    {
        readonly VisualElement surface;
        public override VisualElement contentContainer => surface ?? this;
        public const float DesignWidth = 1672f, DesignHeight = 941f;
        public HudDesignCanvas()
        {
            pickingMode = PickingMode.Ignore;
            surface = new VisualElement { name = "design-surface", pickingMode = PickingMode.Ignore };
            surface.style.position = Position.Absolute;
            surface.style.width = DesignWidth; surface.style.height = DesignHeight;
            surface.style.transformOrigin = new TransformOrigin(0, 0);
            hierarchy.Add(surface);
            RegisterCallback<GeometryChangedEvent>(_ => Fit());
            generateVisualContent += DrawOutsideMargins;
        }
        void DrawOutsideMargins(MeshGenerationContext context)
        {
            if (!ClassListContains("fps-document")) return;
            float w = contentRect.width, h = contentRect.height;
            float scale = Mathf.Min(w / DesignWidth, h / DesignHeight);
            if (!(scale > 0)) return;
            float x = (w - DesignWidth * scale) * .5f, y = (h - DesignHeight * scale) * .5f;
            var painter = context.painter2D;
            painter.fillColor = Color.black;
            void Fill(float left, float top, float width, float height)
            {
                if (width <= 0 || height <= 0) return;
                painter.BeginPath(); painter.MoveTo(new Vector2(left, top));
                painter.LineTo(new Vector2(left + width, top));
                painter.LineTo(new Vector2(left + width, top + height));
                painter.LineTo(new Vector2(left, top + height)); painter.ClosePath(); painter.Fill();
            }
            Fill(0, 0, x, h); Fill(w - x, 0, x, h);
            Fill(x, 0, w - 2 * x, y); Fill(x, h - y, w - 2 * x, y);
        }
        void Fit()
        {
            float scale = Mathf.Min(contentRect.width / DesignWidth, contentRect.height / DesignHeight);
            if (!(scale > 0) || float.IsInfinity(scale)) return;
            surface.style.scale = new Scale(new Vector3(scale, scale, 1));
            surface.style.left = (contentRect.width - DesignWidth * scale) * .5f;
            surface.style.top = (contentRect.height - DesignHeight * scale) * .5f;
            MarkDirtyRepaint();
        }
    }
}
