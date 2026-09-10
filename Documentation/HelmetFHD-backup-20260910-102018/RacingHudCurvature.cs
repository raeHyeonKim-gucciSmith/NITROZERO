using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

/// <summary>Render the editable UIDocument through a subtle curved-screen projection.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
public sealed class RacingHudCurvature : MonoBehaviour
{
    [Range(0f, .12f)] public float curvature = .035f;
    public Shader curvedShader;
    UIDocument document;
    PanelSettings originalSettings, runtimeSettings;
    RenderTexture surface;
    Material material;
    GameObject overlay;
    RawImage image;
    int width, height;

    void OnEnable() { document = GetComponent<UIDocument>(); }
    void LateUpdate()
    {
        if (document == null || !document.enabled || document.panelSettings == null) return;
        if (runtimeSettings == null) Initialize();
        if (runtimeSettings == null) return;
        int w = Mathf.Max(64, Screen.width), h = Mathf.Max(64, Screen.height);
        if (surface == null || w != width || h != height) Resize(w, h);
        material.SetFloat("_Curvature", curvature);
    }

    void Initialize()
    {
        if (curvedShader == null) curvedShader = Shader.Find("UI/Racing HUD Curved");
        if (curvedShader == null) return;
        originalSettings = document.panelSettings;
        runtimeSettings = Instantiate(originalSettings);
        runtimeSettings.name = "Racing HUD curved surface (temporary)";
        runtimeSettings.hideFlags = HideFlags.HideAndDontSave;
        runtimeSettings.clearColor = true;
        runtimeSettings.colorClearValue = Color.clear;
        material = new Material(curvedShader) { hideFlags = HideFlags.HideAndDontSave };
        overlay = new GameObject("Racing HUD curved display", typeof(RectTransform), typeof(Canvas));
        overlay.hideFlags = HideFlags.HideAndDontSave;
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(originalSettings.sortingOrder), -32768, 32767);
        var display = new GameObject("HUD", typeof(RectTransform), typeof(RawImage));
        display.hideFlags = HideFlags.HideAndDontSave;
        display.transform.SetParent(overlay.transform, false);
        image = display.GetComponent<RawImage>();
        image.raycastTarget = false;
        image.material = material;
        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Resize(Mathf.Max(64, Screen.width), Mathf.Max(64, Screen.height));
        document.panelSettings = runtimeSettings;
    }

    void Resize(int w, int h)
    {
        var old = surface;
        width = w; height = h;
        surface = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
        {
            name = "Racing HUD surface", hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
        };
        surface.Create();
        runtimeSettings.targetTexture = surface;
        image.texture = surface;
        if (old != null) { old.Release(); Release(old); }
    }

    void OnDisable()
    {
        if (document != null && document.panelSettings == runtimeSettings) document.panelSettings = originalSettings;
        if (surface != null) { surface.Release(); Release(surface); }
        Release(overlay); Release(material); Release(runtimeSettings);
        surface = null; overlay = null; material = null; runtimeSettings = null; image = null;
    }

    static void Release(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
    }
}
