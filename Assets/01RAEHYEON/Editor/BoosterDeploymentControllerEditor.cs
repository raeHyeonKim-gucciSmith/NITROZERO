using UnityEditor;

[CustomEditor(typeof(BoosterDeploymentController))]
public sealed class BoosterDeploymentControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool changed = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();

        if (changed && !EditorApplication.isPlaying)
            ((BoosterDeploymentController)target).PreviewSequence();
    }
}
