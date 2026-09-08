using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CoverHinge))]
public sealed class CoverHingeEditor : Editor
{
    private bool adjustHinge;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        adjustHinge = EditorGUILayout.Toggle("Adjust Hinge Only", adjustHinge);
        EditorGUILayout.HelpBox("Enable to move the Scene handle without moving the cover. Disable to test rotation with the normal Rotate tool.", MessageType.Info);
    }

    private void OnSceneGUI()
    {
        if (!adjustHinge || Application.isPlaying) return;
        Transform pivot = ((CoverHinge)target).transform;
        EditorGUI.BeginChangeCheck();
        Vector3 next = Handles.PositionHandle(pivot.position, pivot.rotation);
        if (!EditorGUI.EndChangeCheck()) return;
        var objects = new Object[pivot.childCount + 1];
        var positions = new Vector3[pivot.childCount];
        objects[0] = pivot;
        for (int i = 0; i < pivot.childCount; i++)
        {
            objects[i + 1] = pivot.GetChild(i);
            positions[i] = pivot.GetChild(i).position;
        }
        Undo.RecordObjects(objects, "Move cover hinge only");
        pivot.position = next;
        for (int i = 0; i < pivot.childCount; i++)
            pivot.GetChild(i).position = positions[i];
        foreach (Object item in objects)
            PrefabUtility.RecordPrefabInstancePropertyModifications(item);
    }
}
