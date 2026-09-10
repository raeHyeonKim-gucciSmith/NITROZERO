using UnityEngine;
using UnityEngine.UIElements;

namespace RacingUI
{
    /// <summary>Independently editable artwork crop; reuses the original texture without copying pixels.</summary>
    [UxmlElement]
    public partial class HudArtworkPart : Image
    {
        Texture2D sourceTexture;
        float x, y, w = 1, h = 1;
        [UxmlAttribute] public Texture2D artwork { get => sourceTexture; set { sourceTexture = value; image = value; RefreshCrop(); } }
        [UxmlAttribute] public float sourceX { get => x; set { x = value; RefreshCrop(); } }
        [UxmlAttribute] public float sourceY { get => y; set { y = value; RefreshCrop(); } }
        [UxmlAttribute] public float sourceWidth { get => w; set { w = value; RefreshCrop(); } }
        [UxmlAttribute] public float sourceHeight { get => h; set { h = value; RefreshCrop(); } }
        public HudArtworkPart() { pickingMode = PickingMode.Position; scaleMode = ScaleMode.StretchToFill; }
        void RefreshCrop()
        {
            if (sourceTexture != null) sourceRect = new Rect(x, y, Mathf.Max(1,w), Mathf.Max(1,h));
            MarkDirtyRepaint();
        }
    }
}
