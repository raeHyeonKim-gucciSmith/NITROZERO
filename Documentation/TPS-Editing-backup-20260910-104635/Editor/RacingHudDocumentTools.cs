using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class RacingHudDocumentTools
{
    const string FpsPath = "Assets/04HYUNWOOK/UI Toolkit/RacingHUD/FPS.uxml";

    [MenuItem("Tools/HYUNWOOK/Open FPS UI Builder")]
    public static void OpenFps()
    {
        var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(FpsPath);
        if (asset == null) { Debug.LogError("FPS.uxml import failed: " + FpsPath); return; }
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
        AssetDatabase.OpenAsset(asset);
    }

    [MenuItem("Tools/HYUNWOOK/Validate FPS UI")]
    public static void ValidateFps()
    {
        var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(FpsPath);
        if (asset == null) throw new System.InvalidOperationException("FPS UXML did not import.");
        var root = asset.CloneTree();
        var speed = root.Q<RacingUI.DigitalReadout>("speed-value");
        var gear = root.Q<RacingUI.DigitalReadout>("gear-value");
        var map = root.Q<RacingUI.NavigationMap>("navigation-road");
        if (speed == null || gear == null || map == null)
            throw new System.InvalidOperationException("Missing UXML custom element registration.");
        if (speed.pickingMode != PickingMode.Position || gear.pickingMode != PickingMode.Position)
            throw new System.InvalidOperationException("Digital readouts cannot be picked.");
        Debug.Log("FPS UI: UXML/custom elements and editable readouts PASS.", asset);
    }
}
