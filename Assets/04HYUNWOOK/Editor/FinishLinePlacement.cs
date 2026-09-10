using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class FinishLinePlacement
{
    static FinishLinePlacement() { EditorApplication.delayCall += RemoveOldLines; }
    static void RemoveOldLines()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name != "Finish Line (automatic safe braking distance)" || !t.gameObject.scene.IsValid()) continue;
            var scene = t.gameObject.scene;
            Object.DestroyImmediate(t.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
