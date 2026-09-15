using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

/// <summary>
/// Lays one editable spline per vehicle along the road of an RHScenes shot.
/// Knots are converted to Mirrored tangents so every one of them exposes a
/// draggable bezier handle in the scene view.
/// Road extents are measured from the live renderers, never assumed.
/// </summary>
public static class RoadSplineInstaller
{
    private const string RootName = "Road Splines";

    /// <summary>Metres of clearance kept between the outermost lane and the road edge.</summary>
    private const float EdgeMargin = 3f;

    /// <summary>Handles per path. Raise it for finer control, lower it for softer curves.</summary>
    private const int KnotsPerPath = 20;

    private sealed class ShotConfig
    {
        public string Scene;
        public int CarCount;
        public string GuidePrefix;      // straight corridor shots
        public string RoadPrefix;       // width source / fallback centerline
        public string PointPrefix;      // shots whose centerline is already authored
    }

    private static readonly ShotConfig[] Shots =
    {
        new ShotConfig { Scene = "RH_boostMode",     CarCount = 2, GuidePrefix = "Boost Road Guide",     RoadPrefix = "Boost On Asphalt Road" },
        new ShotConfig { Scene = "RH_avoidMissile",  CarCount = 3, GuidePrefix = "Road Placement Guide", RoadPrefix = "Avoid Missile Asphalt Road" },
        new ShotConfig { Scene = "RH_domeInTheMoon", CarCount = 6, GuidePrefix = "Dome Exit Road Guide", RoadPrefix = "Dome Exit Asphalt Road" },
        new ShotConfig { Scene = "RH_racing",        CarCount = 6, PointPrefix = "Road Point",           RoadPrefix = "Racing Asphalt Road Preview" },
    };

    [MenuItem("Tools/Trailer/Install Road Splines (Current Scene)")]
    public static void InstallCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        ShotConfig shot = Shots.FirstOrDefault(s =>
            string.Equals(s.Scene, scene.name, StringComparison.Ordinal));

        if (shot == null)
        {
            EditorUtility.DisplayDialog("Road Splines",
                "'" + scene.name + "' is not one of the four shots.\n\n" +
                string.Join("\n", Shots.Select(s => "  " + s.Scene)),
                "OK");
            return;
        }

        try
        {
            string report = Install(shot);
            Debug.Log(report);
            EditorUtility.DisplayDialog("Road Splines", report, "OK");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Road Splines", e.Message, "OK");
            Debug.LogException(e);
        }
    }

    [MenuItem("Tools/Trailer/Bind Selected Cars To Road Splines")]
    public static void BindSelectedCars()
    {
        GameObject[] cars = Selection.gameObjects
            .OrderBy(g => g.name, StringComparer.Ordinal).ToArray();

        if (cars.Length == 0)
        {
            EditorUtility.DisplayDialog("Road Splines",
                "Select the vehicle roots in the hierarchy, then run this again.\n" +
                "They are bound to the paths in name order.", "OK");
            return;
        }

        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            EditorUtility.DisplayDialog("Road Splines",
                "No '" + RootName + "' in this scene. Install the road splines first.", "OK");
            return;
        }

        SplineContainer[] paths = root.GetComponentsInChildren<SplineContainer>(true)
            .OrderBy(c => c.name, StringComparer.Ordinal).ToArray();

        var log = new StringBuilder();
        log.AppendLine("Bound " + Math.Min(cars.Length, paths.Length) + " car(s):");

        for (int i = 0; i < cars.Length; i++)
        {
            if (i >= paths.Length)
            {
                log.AppendLine("  " + cars[i].name + " -> no path left (" + paths.Length + " available)");
                continue;
            }

            SplineCarFollower follower = cars[i].GetComponent<SplineCarFollower>();
            if (follower == null)
                follower = Undo.AddComponent<SplineCarFollower>(cars[i]);

            Undo.RecordObject(follower, "Bind Road Spline");
            follower.spline = paths[i];
            follower.splineIndex = 0;
            EditorUtility.SetDirty(follower);

            log.AppendLine("  " + cars[i].name + " -> " + paths[i].name);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("Road Splines", log.ToString(), "OK");
    }

    private static string Install(ShotConfig shot)
    {
        Vector3[] centerline = shot.PointPrefix != null
            ? SampledCenterline(shot)
            : CorridorCenterline(shot);

        float width = MeasureWidth(shot);
        float usable = Mathf.Max(0f, width * 0.5f - EdgeMargin);
        float[] lanes = LaneOffsets(shot.CarCount, usable);

        GameObject old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Install Road Splines");

        float pathLength = 0f;
        for (int i = 0; i < shot.CarCount; i++)
        {
            Vector3[] lane = OffsetLaterally(centerline, lanes[i]);
            Vector3[] knots = Resample(lane, KnotsPerPath);

            var go = new GameObject("Car " + (i + 1).ToString("00", CultureInfo.InvariantCulture) + " Path");
            Undo.RegisterCreatedObjectUndo(go, "Install Road Splines");
            go.transform.SetParent(root.transform, true);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.transform.localScale = Vector3.one;

            var container = go.AddComponent<SplineContainer>();
            Spline spline = container.Spline;
            spline.Clear();

            // AutoSmooth first, so tangents are fitted to the road shape...
            foreach (Vector3 knot in knots)
                spline.Add((float3)knot, TangentMode.AutoSmooth);

            // ...then bake them into Mirrored handles the user can grab and twist.
            for (int k = 0; k < spline.Count; k++)
                spline.SetTangentMode(k, TangentMode.Mirrored);

            pathLength = spline.GetLength();
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = root;

        var report = new StringBuilder();
        report.AppendLine(shot.Scene + ": " + shot.CarCount + " path(s) under '" + RootName + "'.");
        report.AppendLine("  road width measured : " + width.ToString("0.0", CultureInfo.InvariantCulture) + " m");
        report.AppendLine("  path length         : " + pathLength.ToString("0", CultureInfo.InvariantCulture) + " m");
        report.AppendLine("  handles per path    : " + KnotsPerPath + " (Mirrored bezier)");
        report.AppendLine("  lane offsets        : " +
            string.Join(", ", lanes.Select(l => l.ToString("0.0", CultureInfo.InvariantCulture))) + " m");
        report.AppendLine("  scene is dirty but not saved - save it yourself once the paths look right.");
        return report.ToString();
    }

    /// <summary>Straight corridor: measure the guide's world bounds and run down its middle.</summary>
    private static Vector3[] CorridorCenterline(ShotConfig shot)
    {
        Bounds bounds = MeasureBounds(shot.GuidePrefix ?? shot.RoadPrefix, shot);
        float y = bounds.max.y;
        float z = bounds.center.z;

        // Sampled rather than just the two ends, so the lateral offset pass has a real polyline.
        const int samples = 64;
        var line = new Vector3[samples];
        for (int i = 0; i < samples; i++)
        {
            float u = i / (float)(samples - 1);
            line[i] = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, u), y, z);
        }
        return line;
    }

    /// <summary>Racing shot: the centerline is already authored as Road Point NN.</summary>
    private static Vector3[] SampledCenterline(ShotConfig shot)
    {
        List<Transform> points = UnityEngine.Object
            .FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t.name.StartsWith(shot.PointPrefix, StringComparison.Ordinal))
            .OrderBy(t => t.name, StringComparer.Ordinal)
            .ToList();

        if (points.Count < 2)
            throw new InvalidOperationException(
                shot.Scene + ": found " + points.Count + " '" + shot.PointPrefix +
                " NN' objects, need at least 2.");

        return points.Select(t => t.position).ToArray();
    }

    private static float MeasureWidth(ShotConfig shot)
    {
        // A bending road's AABB says nothing useful about its width, so use the authored corridor spec.
        if (shot.PointPrefix != null) return 20f;

        Bounds b = MeasureBounds(shot.GuidePrefix ?? shot.RoadPrefix, shot);
        return Mathf.Min(b.size.x, b.size.z);
    }

    private static Bounds MeasureBounds(string namePrefix, ShotConfig shot)
    {
        GameObject go = UnityEngine.Object
            .FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(g => g.name.StartsWith(namePrefix, StringComparison.Ordinal)
                                 && g.GetComponent<Renderer>() != null);

        if (go == null)
            throw new InvalidOperationException(
                shot.Scene + ": no renderer found on an object named '" + namePrefix + "...'.");

        return go.GetComponent<Renderer>().bounds;
    }

    private static float[] LaneOffsets(int count, float usable)
    {
        var lanes = new float[count];
        if (count < 2) return lanes;
        for (int i = 0; i < count; i++)
            lanes[i] = Mathf.Lerp(-usable, usable, i / (float)(count - 1));
        return lanes;
    }

    private static Vector3[] OffsetLaterally(Vector3[] line, float offset)
    {
        var result = new Vector3[line.Length];
        for (int i = 0; i < line.Length; i++)
        {
            int a = Mathf.Max(0, i - 1);
            int b = Mathf.Min(line.Length - 1, i + 1);
            Vector3 forward = line[b] - line[a];
            forward.y = 0f;
            Vector3 right = forward.sqrMagnitude > 1e-6f
                ? Vector3.Cross(Vector3.up, forward.normalized)
                : Vector3.right;
            result[i] = line[i] + right * offset;
        }
        return result;
    }

    /// <summary>Even arc-length resample, so handles sit at regular distances down the road.</summary>
    private static Vector3[] Resample(Vector3[] line, int count)
    {
        if (count < 2) throw new ArgumentOutOfRangeException(nameof(count));

        var cumulative = new float[line.Length];
        for (int i = 1; i < line.Length; i++)
            cumulative[i] = cumulative[i - 1] + Vector3.Distance(line[i - 1], line[i]);

        float total = cumulative[cumulative.Length - 1];
        if (total <= 0.0001f)
            throw new InvalidOperationException("The centerline has zero length.");

        var result = new Vector3[count];
        int cursor = 0;
        for (int i = 0; i < count; i++)
        {
            float target = total * i / (count - 1);
            while (cursor < line.Length - 2 && cumulative[cursor + 1] < target) cursor++;

            float span = cumulative[cursor + 1] - cumulative[cursor];
            float u = span > 0.0001f ? (target - cumulative[cursor]) / span : 0f;
            result[i] = Vector3.Lerp(line[cursor], line[cursor + 1], u);
        }
        return result;
    }
}
