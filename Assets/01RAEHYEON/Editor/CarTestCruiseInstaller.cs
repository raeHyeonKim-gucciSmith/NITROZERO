using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

// Operates on the loaded scene, including the user's unsaved unpacked objects.
[InitializeOnLoad]
public static class CarTestCruiseInstaller
{
    private const string ScenePath = "Assets/Scenes/CarTest.unity";
    private const string Request = "Documentation/CarTestCruise.install-request";
    private const string Report = "Documentation/CarTestCruise-install-result.txt";
    private static readonly string[] Names = { "Red_Car_Final", "Blue_Car_Final", "Green_Car_Final", "extraCar1_Final", "extraCar2_Final", "extraCar3_Final" };
    private static readonly string[] Corners = { "FL", "FR", "RL", "RR" };
    private static double nextCheck;

    static CarTestCruiseInstaller() { EditorApplication.update += CheckRequest; }
    private static void CheckRequest()
    {
        if (EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 2;
        string requestPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), Request);
        if (!File.Exists(requestPath) || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) return;
        File.Delete(requestPath);
        Install();
    }

    [MenuItem("Tools/Trailer/Install and Validate CarTest Cruise")]
    public static void Install()
    {
        var log = new StringBuilder();
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        int undoGroup = -1;
        try
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            Require(scene.IsValid() && scene.isLoaded && !EditorApplication.isPlaying, "Open CarTest in Edit mode first.");
            GameObject[] roots = Names.Select(n => scene.GetRootGameObjects().Single(g => g.name == n)).ToArray();
            string backupDir = "Documentation/CarTestCruiseBackup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(Path.Combine(projectRoot, backupDir));
            Require(EditorSceneManager.SaveScene(scene), "Could not save the current unpacked scene before backup.");
            File.Copy(Path.Combine(projectRoot, ScenePath), Path.Combine(projectRoot, backupDir, "CarTest-before.unity"));
            log.AppendLine("Backup: " + backupDir + "/CarTest-before.unity");
            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Configure six trailer cars");
            var cars = new List<TrailerCruiseMotion>();
            for (int i = 0; i < roots.Length; i++)
                cars.Add(Configure(roots[i], i, log));
            foreach (var car in cars) Validate(car, log);
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene), "Could not save configured CarTest.");
            Undo.CollapseUndoOperations(undoGroup);
            log.AppendLine("PASS: six scene cars configured and validated. No prefab assets created or modified.");
            File.WriteAllText(Path.Combine(projectRoot, Report), log.ToString());
            Debug.Log(log.ToString());
        }
        catch (Exception e)
        {
            if (undoGroup >= 0) Undo.RevertAllDownToGroup(undoGroup);
            log.AppendLine("FAILED (scene changes rolled back): " + e);
            Directory.CreateDirectory(Path.Combine(projectRoot, "Documentation"));
            File.WriteAllText(Path.Combine(projectRoot, Report), log.ToString());
            Debug.LogException(e);
        }
    }

    private static TrailerCruiseMotion Configure(GameObject original, int seed, StringBuilder log)
    {
        var existing = original.GetComponent<TrailerCruiseMotion>();
        if (existing != null && existing.WheelSpinRoots.Length == 4)
        { log.AppendLine(original.name + ": rig already installed"); return existing; }
        var matrices = original.GetComponentsInChildren<MeshRenderer>(true).ToDictionary(r => r, r => r.localToWorldMatrix);
        Undo.RegisterFullObjectHierarchyUndo(original, "Prepare trailer rig");
        // Only unpack instances belonging to these scene cars; never apply changes to assets.
        foreach (var t in original.GetComponentsInChildren<Transform>(true))
            if (t != null && PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                PrefabUtility.UnpackPrefabInstance(t.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        Transform[] oldWheels = new Transform[4];
        for (int i = 0; i < 4; i++) oldWheels[i] = FindWheel(original.transform, i);
        string name = original.name;
        Transform root = original.transform;
        Transform body;
        TrailerCruiseMotion car;
        if (existing != null)
        {
            existing.RestorePose();
            car = existing;
            body = existing.BodyMotionRoot;
        }
        else
        {
            // Preserve the complete original model and all transformation scripts/references.
            // Their direct child paths stay intact inside this model object.
            var outer = new GameObject(name);
            SceneManager.MoveGameObjectToScene(outer, original.scene);
            Undo.RegisterCreatedObjectUndo(outer, "Vehicle movement root");
            outer.transform.SetPositionAndRotation(root.position, root.rotation);
            outer.transform.localScale = root.localScale;
            body = CreatePivot("CruiseBodyMotion", outer.transform, outer.transform.position, outer.transform.rotation);
            Undo.SetTransformParent(root, body, "Preserve model under body motion");
            Undo.RecordObject(original, "Name preserved model");
            original.name = name + "_Model";
            root = outer.transform;
            car = Undo.AddComponent<TrailerCruiseMotion>(outer);
        }
        foreach (var behaviour in original.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null) continue;
            string type = behaviour.GetType().Name;
            if (type == "RaehyeonCarWheelSpin" || type == "VehicleWheelSpinController" ||
                type == "VehicleDriftMotionController" || type == "TaurusWheelSpinTest")
            {
                Undo.RecordObject(behaviour, "Use unified wheel control");
                behaviour.enabled = false;
            }
        }
        var hubs = new Transform[4];
        var rolls = new Transform[4];
        var radii = new float[4];
        var pivot = Vector3.zero;
        for (int i = 0; i < 4; i++)
        {
            Transform wheel = oldWheels[i];
            Bounds bounds = WheelBounds(wheel);
            Vector3 center = bounds.center;
            // World bounds are reliable here: the scene cars face +/- world Z.
            radii[i] = bounds.size.y * 0.5f;
            Require(radii[i] > 0.03f && radii[i] < 2f, name + ": invalid tire radius " + radii[i]);
            hubs[i] = CreatePivot(Corners[i] + "_Steering", body, center, root.rotation);
            rolls[i] = CreatePivot(Corners[i] + "_Rolling", hubs[i], center, root.rotation);
            // Calipers follow steering and suspension, but never rolling rotation.
            foreach (var part in wheel.GetComponentsInChildren<Transform>(true).Where(t => t.name.IndexOf("caliper", StringComparison.OrdinalIgnoreCase) >= 0).ToArray())
                Undo.SetTransformParent(part, hubs[i], "Keep brake caliper stationary");
            if (name == "extraCar2_Final")
            {
                string[] brakeNames = { "RMCar26_BrakeFrontLeft", "RMCar26_BrakeFrontRight", "RMCar26_BrakeRearLeft", "RMCar26_BrakeRearRight" };
                Transform brake = original.GetComponentsInChildren<Transform>(true).SingleOrDefault(t => t.name == brakeNames[i]);
                if (brake != null) Undo.SetTransformParent(brake, hubs[i], "Brake assembly follows steering");
            }
            Undo.SetTransformParent(wheel, rolls[i], "Separate rolling wheel visual");
            pivot += root.InverseTransformPoint(center) * 0.25f;
        }
        Undo.RecordObject(car, "Bind cruise controls");
        car.ConfigureRig(body, hubs, rolls, radii, pivot);
        car.motionSeed = 17 + seed * 19;
        car.wheelRadius = radii.Average();
        if (existing == null)
        {
            float scale = Mathf.Max(0.001f, root.lossyScale.y);
            car.heaveAmplitude = 0.001f / scale;
            car.vibrationAmplitude = 0.00025f / scale;
        }
        EditorUtility.SetDirty(car);
        foreach (var pair in matrices)
            for (int k = 0; k < 16; k++)
                Require(Mathf.Abs(pair.Key.localToWorldMatrix[k] - pair.Value[k]) < 0.003f,
                    name + ": reparent changed mesh placement: " + pair.Key.name);
        log.AppendLine(name + ": radii(m)=" + string.Join(", ", radii.Select(r => r.ToString("0.000"))) +
            "; 4 steering/rolling pairs; mesh placement preserved; existing transformation components retained.");
        return car;
    }

    private static Transform FindWheel(Transform root, int corner)
    {
        string[] rm = { "RMCar26_WheelFrontLeft", "RMCar26_WheelFrontRight", "RMCar26_WheelRearLeft", "RMCar26_WheelRearRight" };
        string[] names = { Corners[corner], Corners[corner] + " wheel", rm[corner] };
        var candidates = root.GetComponentsInChildren<Transform>(true)
            .Where(t => names.Contains(t.name.Trim()) && t.gameObject.activeInHierarchy).ToArray();
        Require(candidates.Length == 1, root.name + ": expected one active " + Corners[corner] + ", found " + candidates.Length);
        return candidates[0];
    }

    private static Bounds WheelBounds(Transform wheel)
    {
        var all = wheel.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
        var tires = all.Where(r => r.name.IndexOf("rubber", StringComparison.OrdinalIgnoreCase) >= 0 ||
            r.name.IndexOf("tire_base", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        if (tires.Length > 0) all = tires;
        Require(all.Length > 0, "No visible wheel geometry: " + wheel.name);
        Bounds bounds = all[0].bounds;
        foreach (var r in all.Skip(1)) bounds.Encapsulate(r.bounds);
        return bounds;
    }

    private static Transform CreatePivot(string name, Transform parent, Vector3 position, Quaternion rotation)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create wheel/body pivot");
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, rotation);
        return go.transform;
    }

    private static void Validate(TrailerCruiseMotion car, StringBuilder log)
    {
        float speed = car.wheelSpeedKph, handle = car.steeringWheelAngle, weight = car.manualSteeringWeight;
        var positions = car.Wheels.Select(w => w.position).ToArray();
        try
        {
            car.wheelSpeedKph = 600;
            car.manualSteeringWeight = 1;
            car.steeringWheelAngle = 0;
            car.EvaluateAtTime(0);
            var baseRotations = car.Wheels.Select(w => w.rotation).ToArray();
            car.steeringWheelAngle = 360;
            car.EvaluateAtTime(0);
            var hubRotations = car.Wheels.Select(w => w.rotation).ToArray();
            var spinRotations = car.WheelSpinRoots.Select(w => w.rotation).ToArray();
            car.EvaluateAtTime(0.001);
            for (int i = 0; i < 4; i++)
            {
                Require(Quaternion.Angle(hubRotations[i], car.Wheels[i].rotation) < 0.05f, "Steering hub rotated with tire");
                Require(Quaternion.Angle(spinRotations[i], car.WheelSpinRoots[i].rotation) > 1, "Rolling visual did not spin");
                Require(Mathf.Abs(Quaternion.Angle(baseRotations[i], hubRotations[i]) - (i < 2 ? car.wheelAngleAt360 : 0)) < 0.1f,
                    "Incorrect front/rear steering");
                Require(Vector3.Distance(positions[i], car.Wheels[i].position) < 0.0003f, "Contact moved");
            }
            car.EvaluateAtTime(0.75);
            var sample = car.WheelSpinRoots.Select(w => w.rotation).ToArray();
            car.EvaluateAtTime(0.1);
            car.EvaluateAtTime(0.75);
            for (int i = 0; i < 4; i++) Require(Quaternion.Angle(sample[i], car.WheelSpinRoots[i].rotation) < 0.05f, "Seek mismatch");
            log.AppendLine(car.name + ": PASS steering, stationary hubs/brakes, rolling, contact, repeatable sampling.");
        }
        finally
        {
            car.RestorePose();
            car.wheelSpeedKph = speed;
            car.steeringWheelAngle = handle;
            car.manualSteeringWeight = weight;
        }
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
