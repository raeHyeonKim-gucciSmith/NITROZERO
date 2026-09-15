using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Drops the finished car prefabs onto the start of the RH_racing road.
/// Ride height comes from each instance's own renderer bounds, so the wheels
/// sit on the surface without any raycast.
/// </summary>
public static class RacingCarPlacer
{
    private const string SceneName = "RH_racing";
    private const string RootName = "Racing Cars";
    private const string PointPrefix = "Road Point";

    /// <summary>Road corridor width in metres, as authored.</summary>
    private const float RoadWidth = 20f;

    /// <summary>Metres of clearance kept between the outermost car and the road edge.</summary>
    private const float EdgeMargin = 3f;

    /// <summary>The finished cars, in lane order. Cockpit prefabs are deliberately left out.</summary>
    private static readonly string[] CarPrefabs =
    {
        "Assets/Prefabs/Final_Cars/Blue_Car_Final.prefab",
        "Assets/Prefabs/Final_Cars/Green_Car_Final.prefab",
        "Assets/Prefabs/Final_Cars/Red_Car_Final.prefab",
        "Assets/Prefabs/Final_Cars/extraCar2_Final.prefab",
        "Assets/Prefabs/Final_Cars/extraCar3_Final.prefab",
    };

    [MenuItem("Tools/Trailer/Place Final Cars (RH_racing)")]
    public static void PlaceCars()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, SceneName, StringComparison.Ordinal))
        {
            EditorUtility.DisplayDialog("Place Final Cars",
                "Open " + SceneName + " first. The active scene is '" + scene.name + "'.", "OK");
            return;
        }

        try
        {
            string report = Run();
            Debug.Log(report);
            EditorUtility.DisplayDialog("Place Final Cars", report, "OK");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Place Final Cars", e.Message, "OK");
            Debug.LogException(e);
        }
    }

    private static string Run()
    {
        Transform[] points = UnityEngine.Object
            .FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t.name.StartsWith(PointPrefix, StringComparison.Ordinal))
            .OrderBy(t => t.name, StringComparer.Ordinal)
            .ToArray();

        if (points.Length < 2)
            throw new InvalidOperationException(
                "Found " + points.Length + " '" + PointPrefix + " NN' objects, need at least 2.");

        Vector3 start = points[0].position;
        Vector3 forward = points[1].position - points[0].position;
        forward.y = 0f;
        if (forward.sqrMagnitude < 1e-6f)
            throw new InvalidOperationException("The first two road points sit on top of each other.");
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward);
        float surfaceY = start.y;
        float usable = RoadWidth * 0.5f - EdgeMargin;

        GameObject old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Place Final Cars");

        var log = new StringBuilder();
        log.AppendLine(SceneName + ": placed " + CarPrefabs.Length + " car(s) under '" + RootName + "'.");
        log.AppendLine("  start point  : " + Format(start));
        log.AppendLine("  heading      : " + Format(forward));
        log.AppendLine("  surface Y    : " + surfaceY.ToString("0.00", CultureInfo.InvariantCulture));

        var missing = new List<string>();

        for (int i = 0; i < CarPrefabs.Length; i++)
        {
            string path = CarPrefabs[i];
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                missing.Add(path);
                continue;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            Undo.RegisterCreatedObjectUndo(instance, "Place Final Cars");
            instance.transform.SetParent(root.transform, true);

            float lane = CarPrefabs.Length < 2
                ? 0f
                : Mathf.Lerp(-usable, usable, i / (float)(CarPrefabs.Length - 1));

            instance.transform.SetPositionAndRotation(
                start + right * lane,
                Quaternion.LookRotation(forward, Vector3.up));

            // Static lift measured from the instance itself, so each car rests on the road.
            float lift = 0f;
            if (TryMeasure(instance, out Bounds bounds))
            {
                lift = surfaceY - bounds.min.y;
                instance.transform.position += Vector3.up * lift;
            }

            log.AppendLine("  " + instance.name.PadRight(28) +
                           " lane " + lane.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture).PadLeft(5) + " m" +
                           "   lift " + lift.ToString("0.000", CultureInfo.InvariantCulture) + " m");
        }

        if (missing.Count > 0)
        {
            log.AppendLine("  MISSING:");
            foreach (string m in missing) log.AppendLine("    " + m);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        log.AppendLine("  scene is dirty but not saved - save it yourself once the placement looks right.");
        return log.ToString();
    }

    [MenuItem("Tools/Trailer/Add Physics To Placed Cars (RH_racing)")]
    public static void AddPhysics()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            EditorUtility.DisplayDialog("Add Physics",
                "No '" + RootName + "' in this scene. Place the cars first.", "OK");
            return;
        }

        var log = new StringBuilder();
        log.AppendLine("Physics added under '" + RootName + "':");

        foreach (Transform child in root.transform)
        {
            GameObject car = child.gameObject;

            // A non-convex MeshCollider cannot ride on a non-kinematic Rigidbody.
            // The root box below replaces them, so switch the authored ones off.
            int disabled = 0;
            foreach (MeshCollider mc in car.GetComponentsInChildren<MeshCollider>(true))
            {
                if (!mc.enabled || mc.convex) continue;
                Undo.RecordObject(mc, "Add Physics");
                mc.enabled = false;
                EditorUtility.SetDirty(mc);
                disabled++;
            }

            if (!TryMeasureLocal(car, out Vector3 center, out Vector3 size))
            {
                log.AppendLine("  " + car.name.PadRight(28) + " no renderer - skipped");
                continue;
            }

            BoxCollider box = car.GetComponent<BoxCollider>();
            if (box == null) box = Undo.AddComponent<BoxCollider>(car);
            Undo.RecordObject(box, "Add Physics");
            box.center = center;
            box.size = size;
            EditorUtility.SetDirty(box);

            Rigidbody body = car.GetComponent<Rigidbody>();
            if (body == null) body = Undo.AddComponent<Rigidbody>(car);
            Undo.RecordObject(body, "Add Physics");
            body.isKinematic = false;
            body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            EditorUtility.SetDirty(body);

            log.AppendLine("  " + car.name.PadRight(28) +
                           " box " + Format(size) +
                           (disabled > 0 ? "  (" + disabled + " mesh collider(s) disabled)" : ""));
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        log.AppendLine("  physics only runs in play mode - press Play to see them settle.");
        log.AppendLine("  scene is dirty but not saved.");
        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("Add Physics", log.ToString(), "OK");
    }

    [MenuItem("Tools/Trailer/Unpack Placed Cars Completely (RH_racing)")]
    public static void UnpackCars()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            EditorUtility.DisplayDialog("Unpack Placed Cars",
                "No '" + RootName + "' in this scene. Place the cars first.", "OK");
            return;
        }

        // Snapshot the children: unpacking rebuilds the hierarchy underneath them.
        var cars = new List<GameObject>();
        foreach (Transform child in root.transform) cars.Add(child.gameObject);

        var log = new StringBuilder();
        log.AppendLine("Unpack completely under '" + RootName + "':");
        int unpacked = 0;

        foreach (GameObject car in cars)
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(car))
            {
                log.AppendLine("  " + car.name.PadRight(28) + " already a plain object");
                continue;
            }

            GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(car);
            if (instanceRoot == null) instanceRoot = car;

            // UserAction records the undo entry for us.
            PrefabUtility.UnpackPrefabInstance(
                instanceRoot, PrefabUnpackMode.Completely, InteractionMode.UserAction);

            log.AppendLine("  " + car.name.PadRight(28) + " unpacked");
            unpacked++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        log.AppendLine("  " + unpacked + " of " + cars.Count + " unpacked. Scene is dirty but not saved.");
        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("Unpack Placed Cars", log.ToString(), "OK");
    }

    /// <summary>
    /// Renderer bounds expressed in the car's own local space, so a BoxCollider
    /// wraps the body tightly even though the car is rotated onto the road heading.
    /// </summary>
    private static bool TryMeasureLocal(GameObject car, out Vector3 center, out Vector3 size)
    {
        center = Vector3.zero;
        size = Vector3.one;

        Transform t = car.transform;
        Quaternion saved = t.rotation;
        t.rotation = Quaternion.identity;

        bool any = TryMeasure(car, out Bounds bounds);

        if (any)
        {
            center = t.InverseTransformPoint(bounds.center);
            Vector3 scale = t.lossyScale;
            size = new Vector3(
                scale.x != 0f ? bounds.size.x / Mathf.Abs(scale.x) : bounds.size.x,
                scale.y != 0f ? bounds.size.y / Mathf.Abs(scale.y) : bounds.size.y,
                scale.z != 0f ? bounds.size.z / Mathf.Abs(scale.z) : bounds.size.z);
        }

        t.rotation = saved;
        return any;
    }

    /// <summary>Combined world bounds of every renderer under the instance.</summary>
    private static bool TryMeasure(GameObject instance, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);

        bool any = false;
        foreach (Renderer r in renderers)
        {
            // Particle and trail renderers report bounds that have nothing to do with the body.
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;

            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return any;
    }

    private static string Format(Vector3 v)
    {
        return "(" + v.x.ToString("0.0", CultureInfo.InvariantCulture) +
               ", " + v.y.ToString("0.0", CultureInfo.InvariantCulture) +
               ", " + v.z.ToString("0.0", CultureInfo.InvariantCulture) + ")";
    }
}
