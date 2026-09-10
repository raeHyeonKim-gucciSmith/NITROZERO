using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RacingUI
{
    // DS-Digital outlines use the approved preview's metrics, avoiding the
    // legacy alpha-only font atlas. Text, color and font size stay editable.
    static class HudGlyphRenderer
    {
        // These fields are populated by JsonUtility from the bundled font outlines.
#pragma warning disable CS0649
        [Serializable] class Polygon { public Vector2[] points; }
        [Serializable] class Glyph { public int code; public float advance; public float[] bounds; public Polygon[] contours; }
        [Serializable] class FontData { public float units; public Glyph[] glyphs; }
#pragma warning restore CS0649
        static FontData data;
        static Dictionary<char, Glyph> glyphs;

        static bool Load()
        {
            if (glyphs != null) return true;
            var asset = Resources.Load<TextAsset>("RacingHud/DS-Digital-Glyphs");
            if (asset == null) return false;
            data = JsonUtility.FromJson<FontData>(asset.text);
            glyphs = new Dictionary<char, Glyph>();
            foreach (var glyph in data.glyphs) glyphs[(char)glyph.code] = glyph;
            return data.units > 0;
        }

        public static void Draw(MeshGenerationContext context, Rect area,
            string text, float fontSize, Color color, bool fit)
        {
            if (string.IsNullOrEmpty(text) || area.width <= 0 || area.height <= 0 ||
                float.IsNaN(fontSize) || float.IsInfinity(fontSize) || fontSize <= 0 || !Load()) return;
            float xMin = float.PositiveInfinity, yMin = xMin;
            float xMax = float.NegativeInfinity, yMax = xMax, advance = 0;
            foreach (char c in text)
            {
                if (!glyphs.TryGetValue(c, out var glyph)) glyph = glyphs['?'];
                if (glyph.bounds.Length == 4)
                {
                    xMin = Mathf.Min(xMin, advance + glyph.bounds[0]); xMax = Mathf.Max(xMax, advance + glyph.bounds[2]);
                    yMin = Mathf.Min(yMin, glyph.bounds[1]); yMax = Mathf.Max(yMax, glyph.bounds[3]);
                }
                advance += glyph.advance;
            }
            if (!(xMax > xMin) || !(yMax > yMin)) return;
            float scale = fontSize / data.units;
            if (fit) scale = Mathf.Min(scale, Mathf.Min(Mathf.Max(0, area.width - 4) / (xMax - xMin),
                Mathf.Max(0, area.height - 4) / (yMax - yMin)));
            float left = area.center.x - (xMin + xMax) * .5f * scale;
            float baseline = area.center.y + (yMin + yMax) * .5f * scale;
            var painter = context.painter2D;
            painter.BeginPath(); advance = 0;
            foreach (char c in text)
            {
                if (!glyphs.TryGetValue(c, out var glyph)) glyph = glyphs['?'];
                foreach (var contour in glyph.contours)
                {
                    for (int i = 0; i < contour.points.Length; i++)
                    {
                        var p = contour.points[i];
                        var position = new Vector2(left + (advance + p.x) * scale, baseline - p.y * scale);
                        if (i == 0) painter.MoveTo(position); else painter.LineTo(position);
                    }
                    painter.ClosePath();
                }
                advance += glyph.advance;
            }
            painter.fillColor = color; painter.Fill(FillRule.OddEven);
        }
    }
}
