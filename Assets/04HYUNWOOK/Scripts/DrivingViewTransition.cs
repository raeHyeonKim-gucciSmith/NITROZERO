using UnityEngine;
using UnityEngine.UI;

/// <summary>Fade through opaque black without screen capture or UI transforms.</summary>
public sealed class DrivingViewTransition : MonoBehaviour
{
    public bool IsReady { get; private set; }
    public float Opacity { get; private set; }
    GameObject overlay;
    Image image;
    public void Prepare()
    {
        Cancel();
        overlay = new GameObject("Driving View Black Fade", typeof(RectTransform), typeof(Canvas));
        overlay.hideFlags = HideFlags.HideAndDontSave;
        overlay.transform.SetParent(transform, false);
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        var cover = new GameObject("Black cover", typeof(RectTransform), typeof(Image));
        cover.transform.SetParent(overlay.transform, false);
        image = cover.GetComponent<Image>();
        image.raycastTarget = false;
        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Apply(0f, 1f);
        IsReady = true;
    }
    public static Vector3 Envelope(float progress)
    {
        float t = Mathf.Clamp01(progress);
        float opacity = t < .4f ? Mathf.SmoothStep(0f, 1f, t / .4f)
            : t <= .6f ? 1f : Mathf.SmoothStep(1f, 0f, (t - .6f) / .4f);
        return new Vector3(0f, 0f, opacity);
    }
    public void Apply(float progress, float direction)
    {
        Opacity = Envelope(progress).z;
        if (image == null) return;
        image.color = new Color(0f, 0f, 0f, Opacity);
        image.enabled = Opacity > 0f;
    }
    public void Cancel()
    {
        IsReady = false; Opacity = 0f;
        if (overlay != null) {
            overlay.SetActive(false);
            if (Application.isPlaying) Destroy(overlay); else DestroyImmediate(overlay);
        }
        overlay = null; image = null;
    }
    void OnDisable() => Cancel();
}
