using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class HellaFlushWheelInstaller
{
    private const string ScenePath = "Assets/01RAEHYEON/RaehyeonCar.unity";
    private const string Type9PrefabPath = "Assets/Simple Drift Tyres/Prefabs/Tyre_Type_9.prefab";
    private const string Type3PrefabPath = "Assets/Simple Drift Tyres/Prefabs/Tyre_Type_3.prefab";
    private const string Type9VisualName = "SimpleDriftTyre_Type9_Visual";
    private const string Type3VisualName = "SimpleDriftTyre_Type3_Visual";
    private static readonly string[] WheelNames = { "FL", "FR", "RL", "RR" };
    private static readonly string[] Racer5WheelNames =
        { "FL wheel ", "FR wheel ", "RL wheel ", "RR wheel " };

    [DidReloadScripts]
    private static void QueueSimpleDriftTyreInstall()
    {
        EditorApplication.delayCall += InstallSimpleDriftTyres;
    }

    [MenuItem("Tools/Raehyeon Car/Install Simple Drift Tyres")]
    public static void InstallSimpleDriftTyres()
    {
        InstallIntoVehicle("RedCar", WheelNames, Type9PrefabPath, Type9VisualName);
        InstallIntoVehicle("RACER 5", Racer5WheelNames, Type3PrefabPath, Type3VisualName);
    }

    private static void InstallIntoVehicle(
        string vehicleName,
        string[] wheelNames,
        string prefabPath,
        string visualName)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool closeSceneAfterInstall = !scene.IsValid() || !scene.isLoaded;
        if (closeSceneAfterInstall)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        GameObject targetVehicle = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == vehicleName && candidate.gameObject.activeInHierarchy)
                {
                    targetVehicle = candidate.gameObject;
                    break;
                }
            }

            if (targetVehicle != null)
                break;
        }

        GameObject wheelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (targetVehicle == null || wheelPrefab == null)
        {
            if (closeSceneAfterInstall && scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
            return;
        }

        Transform[] descendants = targetVehicle.GetComponentsInChildren<Transform>(true);
        var pivots = new List<Transform>(4);
        foreach (string wheelName in wheelNames)
        {
            Transform pivot = null;
            foreach (Transform candidate in descendants)
            {
                if (candidate.name == wheelName)
                {
                    pivot = candidate;
                    break;
                }
            }

            if (pivot == null || pivot.Find(visualName) != null)
            {
                if (closeSceneAfterInstall)
                    EditorSceneManager.CloseScene(scene, true);
                return;
            }

            pivots.Add(pivot);
        }

        Undo.RegisterFullObjectHierarchyUndo(targetVehicle, "Install Simple Drift Tyres");

        foreach (Transform pivot in pivots)
        {
            Renderer[] oldRenderers = pivot.GetComponentsInChildren<Renderer>(true);
            if (!TryGetBounds(oldRenderers, out Bounds oldBounds))
                continue;

            foreach (Renderer oldRenderer in oldRenderers)
            {
                Undo.RecordObject(oldRenderer, "Disable Old Wheel Renderer");
                oldRenderer.enabled = false;
            }

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(wheelPrefab, scene);
            Undo.RegisterCreatedObjectUndo(visual, "Create HellaFlush Wheel Visual");
            visual.name = visualName;
            visual.transform.SetParent(pivot, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            visual.transform.localScale = Vector3.one;

            Renderer[] newRenderers = visual.GetComponentsInChildren<Renderer>(true);
            if (!TryGetBounds(newRenderers, out Bounds newBounds))
                continue;

            float oldDiameter = Mathf.Max(oldBounds.size.y, oldBounds.size.z);
            float newDiameter = Mathf.Max(newBounds.size.y, newBounds.size.z);
            if (newDiameter > 0.0001f)
                visual.transform.localScale = Vector3.one * (oldDiameter / newDiameter);

            if (TryGetBounds(newRenderers, out newBounds))
                visual.transform.position += oldBounds.center - newBounds.center;
        }

        EditorUtility.SetDirty(targetVehicle);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[SimpleDriftTyreInstaller] {vehicleName}의 바퀴 교체를 완료했습니다.", targetVehicle);
        if (closeSceneAfterInstall)
            EditorSceneManager.CloseScene(scene, true);
    }

    private static bool TryGetBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }
}
