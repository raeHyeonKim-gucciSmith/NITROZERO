using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BoosterDeploymentController))]
public sealed class BoosterDeploymentControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (Application.isPlaying)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Edit Mode Preview", EditorStyles.boldLabel);

        BoosterDeploymentController controller =
            (BoosterDeploymentController)target;

        if (GUILayout.Button("후방 날개 기본상태 및 피봇 준비"))
            controller.PrepareRearWingPivotSetupInEditor();

        if (GUILayout.Button("전개 전 상태로 저장"))
        {
            Undo.RegisterFullObjectHierarchyUndo(
                controller.transform.root.gameObject,
                "Preview Booster Retracted");
            controller.PreviewRetractedInEditor();
        }

        if (GUILayout.Button("전개 상태로 미리보기"))
        {
            Undo.RegisterFullObjectHierarchyUndo(
                controller.transform.root.gameObject,
                "Preview Booster Deployed");
            controller.PreviewDeployedInEditor();
        }
    }
}

[InitializeOnLoad]
internal static class RearWingPivotSetupInitializer
{
    static RearWingPivotSetupInitializer()
    {
        EditorApplication.delayCall += PrepareCurrentRaehyeonCar;
    }

    private static void PrepareCurrentRaehyeonCar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        foreach (BoosterDeploymentController controller in
                 Resources.FindObjectsOfTypeAll<BoosterDeploymentController>())
        {
            if (!controller.gameObject.scene.IsValid()
                || !controller.gameObject.activeInHierarchy)
                continue;

            if (controller.gameObject.scene.name != "RaehyeonCar")
                continue;

            if (controller.PrepareRearWingPivotSetupInEditor())
                break;
        }
    }
}
