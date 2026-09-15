using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Damin.VFX.TireSmoke.Progressive;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

/// <summary>
/// Replaces the six parallel paths with one shared racing line and wires the pack
/// into RaceChoreography: the drift order, the booster overtake, the lane weaving
/// and the tyre smoke.
/// </summary>
public static class RaceChoreographyInstaller
{
    private const string SceneName = "RH_racing";
    private const string OldSplineRoot = "Road Splines";
    private const string LineName = "Racing Line";
    private const string RigName = "Race Choreography";
    private const string CarRoot = "Racing Cars";
    private const string PointPrefix = "Road Point";
    private const string SmokePrefab = "Assets/Prefabs/VFX/PF_TireSmoke_Progressive.prefab";

    private const int KnotsOnLine = 24;
    private const float Spacing = 8f;          // metres between running-order places
    private const float DriftCentreTime = 13f; // the corner lands here in the shot

    /// <summary>Colour, object name, place at the start, place after the booster.</summary>
    private static readonly (string Label, string Car, int From, int To)[] Order =
    {
        ("파랑",  "Blue_Car_RH",   0, 1),
        ("카키",  "Green_Car_RH",  1, 2),
        ("흰색",  "extraCar1_RH",  2, 4),
        ("빨강",  "Red_Car_RH",    3, 0),
        ("노랑",  "extraCar2_RH",  4, 5),
        ("연두",  "extraCar3_RH",  5, 3),
    };

    /// <summary>Lane keys at 0, 7, 14, 21 and 30 seconds. -1 left edge, +1 right edge.</summary>
    private static readonly float[][] Lanes =
    {
        new[] {  0.00f, -0.35f,  0.20f, -0.10f,  0.15f }, // 파랑
        new[] {  0.40f,  0.10f, -0.30f,  0.25f, -0.20f }, // 카키
        new[] { -0.40f,  0.30f,  0.45f,  0.10f,  0.50f }, // 흰색
        new[] {  0.20f, -0.20f, -0.45f, -0.50f, -0.35f }, // 빨강 dives to the inside
        new[] { -0.20f,  0.45f,  0.35f,  0.50f,  0.60f }, // 노랑
        new[] {  0.60f, -0.45f,  0.05f, -0.30f, -0.05f }, // 연두
    };

    private static readonly float[] LaneTimes = { 0f, 7f, 14f, 21f, 30f };

    [MenuItem("Tools/Trailer/Setup RH_racing Race Shot")]
    public static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, SceneName, StringComparison.Ordinal))
        {
            EditorUtility.DisplayDialog("Race Choreography",
                "Open " + SceneName + " first. The active scene is '" + scene.name + "'.", "OK");
            return;
        }

        try
        {
            string report = Run();
            Debug.Log(report);
            EditorUtility.DisplayDialog("Race Choreography", report, "OK");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Race Choreography", e.Message, "OK");
            Debug.LogException(e);
        }
    }

    private static string Run()
    {
        Vector3[] centre = RoadPoints();
        Transform apex = Find("Drift Apex");

        // 1. the old parallel paths go
        GameObject old = GameObject.Find(OldSplineRoot);
        bool removed = old != null;
        if (removed) Undo.DestroyObjectImmediate(old);

        GameObject oldLine = GameObject.Find(LineName);
        if (oldLine != null) Undo.DestroyObjectImmediate(oldLine);

        // 2. one shared racing line, with grabbable handles
        var lineGo = new GameObject(LineName);
        Undo.RegisterCreatedObjectUndo(lineGo, "Install Race Choreography");
        lineGo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        lineGo.transform.localScale = Vector3.one;

        var container = lineGo.AddComponent<SplineContainer>();
        Spline spline = container.Spline;
        spline.Clear();

        foreach (Vector3 knot in Resample(centre, KnotsOnLine))
            spline.Add((float3)knot, TangentMode.AutoSmooth);
        for (int k = 0; k < spline.Count; k++)
            spline.SetTangentMode(k, TangentMode.Mirrored);

        float lineLength = spline.GetLength();

        // 3. the rig
        GameObject oldRig = GameObject.Find(RigName);
        if (oldRig != null) Undo.DestroyObjectImmediate(oldRig);

        var rigGo = new GameObject(RigName);
        Undo.RegisterCreatedObjectUndo(rigGo, "Install Race Choreography");
        var rig = rigGo.AddComponent<RaceChoreography>();

        rig.racingLine = container;
        rig.splineIndex = 0;
        rig.duration = 30f;
        rig.baseSpeedKph = 220f;
        rig.roadWidth = 30f;
        rig.edgeMargin = 3f;
        rig.driftApex = apex;
        rig.driftAngle = 35f;
        rig.speedOverTime = SpeedCurve();

        // Start far enough back that the corner arrives at DriftCentreTime.
        float apexDistance = apex != null ? DistanceAlong(centre, apex.position) : lineLength * 0.5f;
        float run = Integrate(rig.speedOverTime, rig.baseSpeedKph / 3.6f, DriftCentreTime);
        rig.startDistance = Mathf.Max(0f, apexDistance - run);

        // 4. the cars
        GameObject carRoot = GameObject.Find(CarRoot);
        var smokeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SmokePrefab);

        var racers = new List<RaceChoreography.Racer>();
        var missing = new List<string>();
        int smokeBound = 0;
        int steeringWired = 0;
        int wheelsBound = 0;

        for (int i = 0; i < Order.Length; i++)
        {
            var (label, carName, from, to) = Order[i];
            Transform car = FindUnder(carRoot, carName) ?? Find(carName);

            if (car == null) { missing.Add(label + " (" + carName + ")"); continue; }

            var racer = new RaceChoreography.Racer
            {
                label = label,
                car = car,
                weavePhase = i / (float)Order.Length,
                gap = GapCurve(from, to, label == "빨강"),
                lane = LaneCurve(Lanes[i]),
                // Measured before the smoke prefab is parented, or its VFX bounds skew the floor.
                rideHeight = MeasureRideHeight(car),
            };

            // The steering these cars already carry was pointing at nothing. Give it the line.
            var cruise = car.GetComponent<TrailerCruiseMotion>()
                         ?? car.GetComponentInChildren<TrailerCruiseMotion>(true);
            if (cruise != null)
            {
                Undo.RecordObject(cruise, "Install Race Choreography");
                cruise.steeringSpline = container;
                cruise.steeringSplineIndex = 0;
                cruise.automaticSteering = true;
                cruise.speedKph = rig.baseSpeedKph;
                cruise.wheelSpeedKph = rig.baseSpeedKph;
                EditorUtility.SetDirty(cruise);
                steeringWired++;
            }

            racer.cruise = cruise;
            racer.wheels = car.GetComponentsInChildren<ContinuousLocalRotation>(true);
            wheelsBound += racer.wheels.Length;

            if (smokeAsset != null)
            {
                var smoke = (GameObject)PrefabUtility.InstantiatePrefab(smokeAsset);
                Undo.RegisterCreatedObjectUndo(smoke, "Install Race Choreography");
                smoke.name = "TireSmoke_" + label;
                smoke.transform.SetParent(car, false);
                smoke.transform.localPosition = Vector3.zero;
                smoke.transform.localRotation = Quaternion.identity;

                var ctrl = smoke.GetComponent<ProgressiveTireSmokeController>()
                           ?? smoke.GetComponentInChildren<ProgressiveTireSmokeController>(true);
                if (ctrl != null)
                {
                    ctrl.vehicleRoot = car;
                    ctrl.smokePower = 0f;
                    racer.smoke = ctrl;
                    smokeBound++;
                    EditorUtility.SetDirty(ctrl);
                }
            }

            racers.Add(racer);
        }

        rig.racers = racers.ToArray();
        rig.InvalidateCache();
        EditorUtility.SetDirty(rig);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = rigGo;

        var log = new StringBuilder();
        log.AppendLine("RH_racing race choreography installed.");
        log.AppendLine("  old parallel paths : " + (removed ? "removed" : "none found"));
        log.AppendLine("  racing line        : " + KnotsOnLine + " Mirrored handles, " +
                       lineLength.ToString("0", CultureInfo.InvariantCulture) + " m");
        log.AppendLine("  apex at            : " + apexDistance.ToString("0", CultureInfo.InvariantCulture) + " m along the line");
        log.AppendLine("  leader starts at   : " + rig.startDistance.ToString("0", CultureInfo.InvariantCulture) + " m");
        log.AppendLine("  cars wired         : " + racers.Count + " of " + Order.Length);
        log.AppendLine("  tyre smoke bound   : " + smokeBound);
        log.AppendLine("  steering wired     : " + steeringWired + " TrailerCruiseMotion -> " + LineName);
        log.AppendLine("  wheel spinners     : " + wheelsBound);
        if (missing.Count > 0) log.AppendLine("  NOT FOUND: " + string.Join(", ", missing));
        log.AppendLine("  drift 35 deg, 30 s shot, 220 kph base.");
        log.AppendLine("  ride height measured per car (pivot to floor):");
        foreach (RaceChoreography.Racer r in rig.racers)
            log.AppendLine("    " + r.label.PadRight(5) + r.car.name.PadRight(16) +
                           r.rideHeight.ToString("0.000", CultureInfo.InvariantCulture) + " m" +
                           (r.cruise != null ? "  steer" : "  NO STEER") +
                           (r.smoke != null ? "  smoke" : "") +
                           (r.wheels.Length > 0 ? "  wheels:" + r.wheels.Length : ""));
        log.AppendLine("  scene is dirty but not saved.");
        return log.ToString();
    }

    /// <summary>Gap in metres behind the leader, keyed from the start order to the finish order.</summary>
    private static AnimationCurve GapCurve(int from, int to, bool booster)
    {
        float a = -from * Spacing;
        float b = -to * Spacing;

        // The booster car makes its move harder and earlier than the pack settles.
        float hold = booster ? 17f : 16f;
        float land = booster ? 20.5f : 21f;

        var curve = new AnimationCurve(
            new Keyframe(0f, a),
            new Keyframe(hold, a),
            new Keyframe(land, b),
            new Keyframe(30f, b));

        for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
        return curve;
    }

    private static AnimationCurve LaneCurve(float[] values)
    {
        var curve = new AnimationCurve();
        for (int i = 0; i < values.Length && i < LaneTimes.Length; i++)
            curve.AddKey(new Keyframe(LaneTimes[i], values[i]));
        for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
        return curve;
    }

    /// <summary>Eases off into the corner, then surges on the booster exit.</summary>
    private static AnimationCurve SpeedCurve()
    {
        var curve = new AnimationCurve(
            new Keyframe(0f, 1.00f),
            new Keyframe(9f, 0.95f),
            new Keyframe(13f, 0.82f),
            new Keyframe(18f, 1.00f),
            new Keyframe(21f, 1.25f),
            new Keyframe(30f, 1.05f));

        for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
        return curve;
    }

    private static float Integrate(AnimationCurve curve, float baseMs, float until)
    {
        const int steps = 256;
        float dt = until / steps;
        float acc = 0f;
        for (int i = 1; i <= steps; i++)
        {
            float v0 = baseMs * Mathf.Max(0f, curve.Evaluate((i - 1) * dt));
            float v1 = baseMs * Mathf.Max(0f, curve.Evaluate(i * dt));
            acc += (v0 + v1) * 0.5f * dt;
        }
        return acc;
    }

    /// <summary>
    /// Distance from the car's pivot down to the lowest point of its body. Adding this
    /// to a point on the road surface puts the wheels on the road instead of under it.
    /// Yaw does not change it, so a drifting car still sits right.
    /// </summary>
    private static float MeasureRideHeight(Transform car)
    {
        bool any = false;
        Bounds bounds = default;

        foreach (Renderer r in car.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }

        return any ? car.position.y - bounds.min.y : 0f;
    }

    private static Vector3[] RoadPoints()
    {
        Transform[] points = UnityEngine.Object
            .FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t.name.StartsWith(PointPrefix, StringComparison.Ordinal))
            .OrderBy(t => t.name, StringComparer.Ordinal)
            .ToArray();

        if (points.Length < 2)
            throw new InvalidOperationException(
                "Found " + points.Length + " '" + PointPrefix + " NN' objects, need at least 2.");

        return points.Select(t => t.position).ToArray();
    }

    /// <summary>Polyline distance from the start to the point nearest the marker.</summary>
    private static float DistanceAlong(Vector3[] line, Vector3 target)
    {
        float best = float.MaxValue, bestAt = 0f, walked = 0f;

        for (int i = 0; i < line.Length; i++)
        {
            if (i > 0) walked += Vector3.Distance(line[i - 1], line[i]);
            float d = Vector3.SqrMagnitude(line[i] - target);
            if (d < best) { best = d; bestAt = walked; }
        }
        return bestAt;
    }

    private static Vector3[] Resample(Vector3[] line, int count)
    {
        var cumulative = new float[line.Length];
        for (int i = 1; i < line.Length; i++)
            cumulative[i] = cumulative[i - 1] + Vector3.Distance(line[i - 1], line[i]);

        float total = cumulative[cumulative.Length - 1];
        if (total <= 0.0001f) throw new InvalidOperationException("The centerline has zero length.");

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

    private static Transform Find(string name)
    {
        return UnityEngine.Object
            .FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => string.Equals(t.name, name, StringComparison.Ordinal));
    }

    private static Transform FindUnder(GameObject root, string name)
    {
        if (root == null) return null;
        foreach (Transform child in root.transform)
            if (string.Equals(child.name, name, StringComparison.Ordinal)) return child;
        return null;
    }
}
