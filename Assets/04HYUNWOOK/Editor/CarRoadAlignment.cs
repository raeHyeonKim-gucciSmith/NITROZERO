using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CarRoadAlignment
{
    const string Request = "Library/CarRoadAlignment.request";
    static CarRoadAlignment() { EditorApplication.update += CheckRequest; }
    static void CheckRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        Align();
    }
    [MenuItem("Tools/HYUNWOOK/Align Car With Road")]
    public static void Align()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/04HYUNWOOK/Scenes/Test.unity")
        { File.WriteAllText("Library/CarRoadAlignment-result.txt", "Deferred: open 04HYUNWOOK/Scenes/Test.unity."); return; }
        var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
        var road = GameObject.Find("Road");
        if (car == null || road == null) throw new InvalidOperationException("Car or Road missing.");
        bool found = false; Bounds bounds = new Bounds();
        foreach (var filter in road.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null) continue;
            Bounds mesh = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 v = mesh.center + Vector3.Scale(mesh.extents, new Vector3((i&1)==0?-1:1, (i&2)==0?-1:1, (i&4)==0?-1:1));
                v = road.transform.InverseTransformPoint(filter.transform.TransformPoint(v));
                if (!found) { bounds = new Bounds(v, Vector3.zero); found = true; } else bounds.Encapsulate(v);
            }
        }
        if (!found) throw new InvalidOperationException("Road mesh bounds missing.");
        string before = "Car before: " + car.transform.position.ToString("F6") + ", rotation " + car.transform.eulerAngles.ToString("F6");
        Undo.RecordObject(car.transform, "Align car with road");
        Vector3 local = road.transform.InverseTransformPoint(car.transform.position);
        local.x = bounds.center.x;
        Vector3 forward = Vector3.ProjectOnPlane(road.transform.forward, Vector3.up).normalized;
        car.transform.SetPositionAndRotation(road.transform.TransformPoint(local), Quaternion.LookRotation(forward, Vector3.up));
        PrefabUtility.RecordPrefabInstancePropertyModifications(car.transform);
        if (car.spawnPoint != null)
        {
            Undo.RecordObject(car.spawnPoint, "Align respawn with road");
            car.spawnPoint.SetPositionAndRotation(car.transform.position, car.transform.rotation);
            PrefabUtility.RecordPrefabInstancePropertyModifications(car.spawnPoint);
        }
        var camera = car.GetComponentInChildren<CarCinemachineSetup>(true);
        if (camera != null)
        {
            Undo.RecordObject(camera, "Center driving cameras");
            camera.followOffset.x = camera.firstPersonOffset.x = camera.lookAtOffset.x = 0;
            PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        string report = before + "\nCar after: " + car.transform.position.ToString("F6") + ", rotation " + car.transform.eulerAngles.ToString("F6") +
            "\nRoad local bounds: " + bounds + "\nCar center error: " + Mathf.Abs(road.transform.InverseTransformPoint(car.transform.position).x-bounds.center.x) +
            "\nForward alignment: " + Vector3.Dot(car.transform.forward,forward) + "\nScene saved.";
        File.WriteAllText("Library/CarRoadAlignment-result.txt", report);
        Debug.Log(report, car);
    }
}
