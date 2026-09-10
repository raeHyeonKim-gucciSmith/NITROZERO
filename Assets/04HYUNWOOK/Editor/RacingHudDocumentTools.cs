using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class RacingHudDocumentTools
{
    const string TpsPath = "Assets/04HYUNWOOK/UI Toolkit/RacingHUD/TPS.uxml";
    const string FpsPath = "Assets/04HYUNWOOK/UI Toolkit/RacingHUD/FPS.uxml";

    [MenuItem("Tools/HYUNWOOK/Open FPS UI Builder")]
    public static void OpenFps()
    {
        OpenDocument(FpsPath);
    }

    [MenuItem("Tools/HYUNWOOK/Open TPS UI Builder")]
    public static void OpenTps() => OpenDocument(TpsPath);

    static void OpenDocument(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
        if (asset == null) { Debug.LogError("UXML import failed: " + path); return; }
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
        AssetDatabase.OpenAsset(asset);
    }

    [MenuItem("Tools/HYUNWOOK/Validate FPS UI")]
    public static void ValidateFps() => ValidateDocument(FpsPath);

    [MenuItem("Tools/HYUNWOOK/Validate TPS UI")]
    public static void ValidateTps() => ValidateDocument(TpsPath);

    static void ValidateDocument(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
        if (asset == null) throw new System.InvalidOperationException("UXML did not import: " + path);
        var root = asset.CloneTree();
        var speed = root.Q<RacingUI.DigitalReadout>("speed-value");
        var gear = root.Q<RacingUI.DigitalReadout>("gear-value");
        var map = root.Q<RacingUI.NavigationMap>("navigation-road");
        if (speed == null || gear == null || map == null)
            throw new System.InvalidOperationException("Missing UXML custom element registration.");
        if (speed.pickingMode != PickingMode.Position || gear.pickingMode != PickingMode.Position)
            throw new System.InvalidOperationException("Digital readouts cannot be picked.");
        var artwork = root.Q<RacingUI.HudVectorPart>("art-timer-frame");
        if (artwork == null || !artwork.IsPathValid || artwork.viewWidth <= 1f)
            throw new System.InvalidOperationException("Vector artwork did not deserialize.");
        root.Query<RacingUI.HudVectorPart>().ForEach(part => {
            if (!part.IsPathValid) throw new System.InvalidOperationException("Invalid vector part: " + part.name);
        });
        var rpmTitle = root.Q<Label>("rpm-title");
        if (rpmTitle == null || rpmTitle.pickingMode != PickingMode.Position)
            throw new System.InvalidOperationException("RPM title is not independently selectable.");
        Debug.Log("UI: UXML/custom elements and editable readouts PASS: " + path, asset);
    }
}
