using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RacingUI
{
    /// <summary>Precise texture mesh of the approved rim; neither screenshot background nor baked text is drawn.</summary>
    [UxmlElement]
    public partial class HudReferenceFrame : VisualElement
    {
#pragma warning disable CS0649
        [Serializable] class MeshData { public float[] vertices; public int[] triangles; }
        [Serializable] class FrameData { public float width, height; public MeshData rim, fill, grid, outside; public bool black; }
#pragma warning restore CS0649
        sealed class CachedFrame { public string source; public FrameData frame; }
        static readonly Dictionary<TextAsset, CachedFrame> Cache = new Dictionary<TextAsset, CachedFrame>();
        TextAsset dataAsset; Texture2D texture; FrameData data;
        [UxmlAttribute] public TextAsset geometry { get => dataAsset; set {
            dataAsset = value; data = null;
            RefreshGeometry();
            MarkDirtyRepaint();
        } }
        [UxmlAttribute] public Texture2D artwork { get => texture; set { texture = value; MarkDirtyRepaint(); } }
        public bool IsReady => data != null && texture != null && data.width > 0 && data.height > 0 && HasMesh(data.rim);
        static bool HasMesh(MeshData shape)
        {
            // JsonUtility can create a missing optional mesh with null array fields.
            return shape?.vertices != null && shape.triangles != null &&
                shape.vertices.Length >= 12 && shape.vertices.Length % 4 == 0 &&
                shape.triangles.Length >= 3 && shape.triangles.Length % 3 == 0;
        }
        public HudReferenceFrame()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
#if UNITY_EDITOR
            RegisterCallback<AttachToPanelEvent>(_ => {
                UnityEditor.EditorApplication.projectChanged -= OnProjectChanged;
                UnityEditor.EditorApplication.projectChanged += OnProjectChanged;
                OnProjectChanged();
            });
            RegisterCallback<DetachFromPanelEvent>(_ => UnityEditor.EditorApplication.projectChanged -= OnProjectChanged);
#endif
        }
        void RefreshGeometry()
        {
            if (dataAsset == null) { data = null; return; }
            string source = dataAsset.text;
            if (!Cache.TryGetValue(dataAsset, out var entry) || entry.source != source) {
                entry = new CachedFrame { source = source, frame = JsonUtility.FromJson<FrameData>(source) };
                Cache[dataAsset] = entry;
            }
            data = entry.frame;
        }
#if UNITY_EDITOR
        void OnProjectChanged() { RefreshGeometry(); MarkDirtyRepaint(); }
#endif
        void Draw(MeshGenerationContext ctx)
        {
#if UNITY_EDITOR
            // Reimports preserve the TextAsset identity; compare its contents, not only its key.
            RefreshGeometry();
#endif
            if (!IsReady || contentRect.width <= 0 || contentRect.height <= 0) return;
            DrawMesh(ctx, data.outside, false, solidBlack: true);
            DrawMesh(ctx, data.fill, false);
            DrawMesh(ctx, data.grid, false, true);
            DrawMesh(ctx, data.rim, true);
        }
        void DrawMesh(MeshGenerationContext ctx, MeshData shape, bool rim, bool grid = false, bool solidBlack = false)
        {
            if (!HasMesh(shape)) return;
            var mesh = ctx.Allocate(shape.vertices.Length / 4, shape.triangles.Length, rim ? texture : null);
            for (int i = 0; i < shape.vertices.Length; i += 4)
            {
                float x = shape.vertices[i], y = shape.vertices[i + 1];
                // Mild navy glass depth without a baked label or checkerboard underneath live content.
                float light = Mathf.Sin(Mathf.Clamp01(y / data.height) * Mathf.PI);
                Color fill = solidBlack || data.black ? Color.black : Color.Lerp(new Color(.002f,.024f,.044f,1), new Color(.003f,.066f,.112f,1), light);
                mesh.SetNextVertex(new Vertex {
                    position = new Vector3(x / data.width * contentRect.width, y / data.height * contentRect.height, Vertex.nearZ),
                    tint = rim ? Color.white : grid ? new Color(.075f,.22f,.30f,.12f) : fill,
                    uv = new Vector2(shape.vertices[i + 2], shape.vertices[i + 3])
                });
            }
            foreach (int index in shape.triangles) mesh.SetNextIndex((ushort)index);
        }
    }
}
