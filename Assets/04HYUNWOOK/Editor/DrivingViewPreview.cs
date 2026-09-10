using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Cinemachine;

/// <summary>Edit-mode camera and HUD layout preview. Runtime startup remains controlled by Start In First Person.</summary>
public sealed class DrivingViewPreview : EditorWindow
{
    CarCinemachineSetup cameraSetup;

    [MenuItem("Tools/HYUNWOOK/Driving View Preview")]
    static void Open() => GetWindow<DrivingViewPreview>("시점 미리보기");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Game 뷰 시점 미리보기", EditorStyles.boldLabel);
        cameraSetup = (CarCinemachineSetup)EditorGUILayout.ObjectField(
            "차량 카메라", cameraSetup, typeof(CarCinemachineSetup), true);
        EditorGUILayout.HelpBox("카메라를 비워두면 현재 씬에서 찾습니다. 플레이를 끈 상태에서 전환하세요. 시작 시점 설정은 바뀌지 않습니다. Ctrl+Z로 미리보기 변경을 되돌릴 수 있습니다.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("1인칭 미리보기", GUILayout.Height(32))) Apply(Resolve(), true);
            if (GUILayout.Button("3인칭 미리보기", GUILayout.Height(32))) Apply(Resolve(), false);
        }
    }

    CarCinemachineSetup Resolve()
    {
        if (cameraSetup == null)
            foreach (var candidate in Object.FindObjectsByType<CarCinemachineSetup>(FindObjectsSortMode.None))
                if (candidate.isActiveAndEnabled && candidate.gameObject.scene == UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene())
                { cameraSetup = candidate; break; }
        return cameraSetup;
    }

    internal static void Apply(CarCinemachineSetup setup, bool firstPerson)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (setup == null || setup.carTarget == null)
        { Debug.LogWarning("현재 씬의 Car Cinemachine Setup과 Car Target을 지정해 주세요."); return; }
        var cam = setup.GetComponent<CinemachineCamera>();
        var follow = setup.GetComponent<CinemachineFollow>();
        var aim = setup.GetComponent<CinemachineRotationComposer>();
        if (cam == null || follow == null || aim == null) return;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Preview driving view");
        Undo.RecordObjects(new Object[] { cam, follow, aim }, "Preview driving view");
        cam.Follow = cam.LookAt = setup.carTarget;
        cam.Lens.FieldOfView = setup.fieldOfView;
        follow.FollowOffset = firstPerson ? setup.firstPersonOffset : setup.followOffset;
        aim.TargetOffset = firstPerson
            ? setup.firstPersonOffset + Vector3.forward * setup.firstPersonLookDistance : setup.lookAtOffset;
        cam.PreviousStateIsValid = false;
        foreach (var component in new Component[] { cam, follow, aim })
        {
            EditorUtility.SetDirty(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        foreach (var hud in Object.FindObjectsByType<RacingHudController>(FindObjectsSortMode.None))
        {
            if (hud.gameObject.scene != setup.gameObject.scene || !hud.isActiveAndEnabled) continue;
            if (hud.car != null && !setup.carTarget.IsChildOf(hud.car.transform)) continue;
            var document = hud.GetComponent<UIDocument>();
            var asset = firstPerson ? hud.fpsDocument : hud.tpsDocument;
            if (document == null || asset == null) continue;
            Undo.RecordObject(document, "Preview driving HUD");
            document.visualTreeAsset = asset;
            EditorUtility.SetDirty(document);
            PrefabUtility.RecordPrefabInstancePropertyModifications(document);
        }
        foreach (var effect in Object.FindObjectsByType<FirstPersonVignette>(FindObjectsSortMode.None))
            if (effect.isActiveAndEnabled && effect.gameObject.scene == setup.gameObject.scene)
                effect.PreviewHelmetEffects(firstPerson);
        Undo.CollapseUndoOperations(group);
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }
}

[InitializeOnLoad]
static class HelmetPreviewCleanup
{
    static HelmetPreviewCleanup()
    {
        AssemblyReloadEvents.beforeAssemblyReload += Restore;
        EditorApplication.quitting += Restore;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) Restore();
        };
    }

    static void Restore()
    {
        foreach (var effect in Object.FindObjectsByType<FirstPersonVignette>(FindObjectsSortMode.None))
            effect.EndHelmetPreview();
    }
}

[CustomEditor(typeof(CarCinemachineSetup))]
public sealed class CarCinemachineSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("1인칭 미리보기")) DrivingViewPreview.Apply((CarCinemachineSetup)target, true);
            if (GUILayout.Button("3인칭 미리보기")) DrivingViewPreview.Apply((CarCinemachineSetup)target, false);
            EditorGUILayout.EndHorizontal();
        }
        DrawDefaultInspector();
    }
}
