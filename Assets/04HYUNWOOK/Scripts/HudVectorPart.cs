using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace RacingUI
{
    /// <summary>A selectable, resolution independent HUD part. No texture or glow pass.</summary>
    [UxmlElement]
    public partial class HudVectorPart : VisualElement
    {
        struct Command
        {
            public char kind;
            public Vector2 a, b, c;
        }

        static readonly Regex Tokens = new Regex(@"[MLCZ]|[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?");
        readonly List<Command> commands = new List<Command>();
        string data = "";
        float designWidth = 100, designHeight = 100, weight = 2.5f;
        Color stroke = new Color32(255, 52, 52, 255), fill = Color.clear;

        // Absolute SVG commands: M x y, L x y, C x1 y1 x2 y2 x y, Z.
        // Each segment starts with its command. Multiple contours use an even-odd fill.
        [UxmlAttribute] public string pathData { get => data; set { data = value ?? ""; Parse(); MarkDirtyRepaint(); } }
        [UxmlAttribute] public float viewWidth { get => designWidth; set { designWidth = Positive(value, 100); MarkDirtyRepaint(); } }
        [UxmlAttribute] public float viewHeight { get => designHeight; set { designHeight = Positive(value, 100); MarkDirtyRepaint(); } }
        [UxmlAttribute] public float strokeWidth { get => weight; set { weight = float.IsNaN(value) || float.IsInfinity(value) ? 2.5f : Mathf.Max(0, value); MarkDirtyRepaint(); } }
        [UxmlAttribute] public Color strokeColor { get => stroke; set { stroke = value; MarkDirtyRepaint(); } }
        [UxmlAttribute] public Color fillColor { get => fill; set { fill = value; MarkDirtyRepaint(); } }
        public bool IsPathValid { get; private set; }

        public HudVectorPart()
        {
            pickingMode = PickingMode.Position;
            generateVisualContent += Draw;
            RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
        }

        static float Positive(float value, float fallback) =>
            float.IsNaN(value) || float.IsInfinity(value) || value <= 0 ? fallback : value;

        void Parse()
        {
            commands.Clear();
            IsPathValid = false;
            if (string.IsNullOrWhiteSpace(data)) return;
            // Reject unsupported commands instead of partially drawing a broken contour.
            if (Regex.Replace(Tokens.Replace(data, ""), @"[\s,]", "").Length != 0) return;
            var tokens = Tokens.Matches(data);
            int i = 0;
            bool hasStart = false;
            try
            {
                while (i < tokens.Count)
                {
                    var token = tokens[i++].Value;
                    if (token.Length != 1) throw new FormatException();
                    var command = new Command { kind = token[0] };
                    if (command.kind == 'M') { command.a = Point(); hasStart = true; }
                    else if (!hasStart) throw new FormatException();
                    else if (command.kind == 'L') command.a = Point();
                    else if (command.kind == 'C') { command.a = Point(); command.b = Point(); command.c = Point(); }
                    else if (command.kind != 'Z') throw new FormatException();
                    commands.Add(command);
                }
                IsPathValid = hasStart;
            }
            catch (FormatException) { commands.Clear(); }

            Vector2 Point() => new Vector2(Number(), Number());
            float Number()
            {
                if (i >= tokens.Count || !float.TryParse(tokens[i++].Value, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float value) || float.IsNaN(value) || float.IsInfinity(value))
                    throw new FormatException();
                return value;
            }
        }

        void Draw(MeshGenerationContext context)
        {
            Rect r = contentRect;
            if (!IsPathValid || r.width <= 0 || r.height <= 0) return;
            Vector2 scale = new Vector2(r.width / designWidth, r.height / designHeight);
            Vector2 Map(Vector2 point) => r.position + Vector2.Scale(point, scale);
            var p = context.painter2D;
            p.BeginPath();
            foreach (var command in commands)
            {
                switch (command.kind)
                {
                    case 'M': p.MoveTo(Map(command.a)); break;
                    case 'L': p.LineTo(Map(command.a)); break;
                    case 'C': p.BezierCurveTo(Map(command.a), Map(command.b), Map(command.c)); break;
                    case 'Z': p.ClosePath(); break;
                }
            }
            if (fill.a > 0) { p.fillColor = fill; p.Fill(FillRule.OddEven); }
            if (weight > 0 && stroke.a > 0)
            {
                p.strokeColor = stroke;
                p.lineWidth = weight * Mathf.Min(scale.x, scale.y);
                p.lineJoin = LineJoin.Miter;
                p.miterLimit = 3;
                p.Stroke();
            }
        }
    }
}
