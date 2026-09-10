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
