using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wraps each cruise vehicle in a grounding rig and saves it as a reusable prefab.
/// Probe origins are placed from the live wheel pivots, so radii and offsets are measured, not guessed.
/// </summary>
public static class TrailerGroundingInstaller
{
    private const string ResultPath = "Documentation/TrailerGrounding-install-result.txt";
    private static readonly string[] Corners = { "FL", "FR", "RL", "RR" };

    [MenuItem("Tools/Trailer/Install Wheel Grounding + Save Car Prefabs (All)")]
    public static void InstallAll()
    {
        Run(UnityEngine.Object.FindObjectsByType<TrailerCruiseMotion>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OrderBy(c => c.name, StringComparer.Ordinal).ToArray(),
            "No TrailerCruiseMotion found in the open scene.");
    }

    [MenuItem("Tools/Trailer/Install Wheel Grounding + Save Car Prefabs (Selected)")]
    public static void InstallSelected()
    {
        var picked = new List<TrailerCruiseMotion>();
        foreach (var go in Selection.gameObjects)
        {
            var car = go.GetComponent<TrailerCruiseMotion>() ?? go.GetComponentInChildren<TrailerCruiseMotion>(true);
            if (car != null && !picked.Contains(car)) picked.Add(car);
        }
        Run(picked.OrderBy(c => c.name, StringComparer.Ordinal).ToArray(),
            "Select the vehicle roots that carry TrailerCruiseMotion, then run this again.");
    }

    private static void Run(TrailerCruiseMotion[] cars, string emptyMessage)
    {
        if (cars.Length == 0)
        {
            EditorUtility.DisplayDialog("Trailer grounding", emptyMessage, "OK");
            return;
        }

        var log = new StringBuilder();
        log.AppendLine("Scene: " + SceneManager.GetActiveScene().path);
        int made = 0;

        try
        {
            foreach (var car in cars)
            {
                BuildRig(car, log);
                made++;
            }
        }
        catch (Exception error)
        {
            log.AppendLine("ABORTED: " + error.Message);
            Debug.LogError("TrailerGroundingInstaller: " + error.Message);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        log.AppendLine(made + " of " + cars.Length + " vehicles rigged.");
        Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
        File.WriteAllText(ResultPath, log.ToString());
        Debug.Log("TrailerGroundingInstaller:\n" + log);
    }

    private static void BuildRig(TrailerCruiseMotion car, StringBuilder log)
    {
        Transform root = car.transform;
        string name = root.name;

        Transform[] wheels = car.Wheels;
        Require(wheels != null && wheels.Length == 4 && wheels.All(w => w != null),
            name + ": four wheel pivots are required.");

        float[] radii = new float[4];
        bool perCorner = car.WheelRadii != null && car.WheelRadii.Length == 4;
        for (int i = 0; i < 4; i++)
        {
            radii[i] = perCorner ? car.WheelRadii[i] : car.wheelRadius;
            Require(radii[i] > 0.03f && radii[i] < 3f, name + ": invalid tyre radius " + radii[i]);
        }

        // World matrices are the contract: reparenting must not move a single wheel.
        var before = wheels.ToDictionary(w => w, w => w.localToWorldMatrix);

        var existing = root.GetComponentInParent<TrailerWheelGrounding>();
        Transform rig, pivot;
        if (existing != null)
        {
            rig = existing.transform;
            pivot = rig.Find("Ground");
            Require(pivot != null, name + ": existing rig has no Ground pivot.");
            foreach (string corner in Corners)
            {
                Transform old = rig.Find("Probe_" + corner);
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            }
        }
        else
        {
            var outer = new GameObject("CarRig_" + name);
            SceneManager.MoveGameObjectToScene(outer, root.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(outer, "Create grounding rig");
            rig = outer.transform;
            rig.SetPositionAndRotation(root.position, root.rotation);
            rig.localScale = Vector3.one;

            var ground = new GameObject("Ground");
            Undo.RegisterCreatedObjectUndo(ground, "Create ground pivot");
            pivot = ground.transform;
            pivot.SetParent(rig, false);
            pivot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            Undo.SetTransformParent(root, pivot, "Move vehicle under ground pivot");
        }

        // Probes sit at the resting wheel centres and stay outside every rotating transform.
        var probes = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            var probe = new GameObject("Probe_" + Corners[i]);
            Undo.RegisterCreatedObjectUndo(probe, "Create ground probe");
            probes[i] = probe.transform;
            probes[i].SetParent(rig, false);
            probes[i].position = wheels[i].position;
            probes[i].rotation = rig.rotation;
        }

        var grounding = rig.GetComponent<TrailerWheelGrounding>();
        if (grounding == null) grounding = Undo.AddComponent<TrailerWheelGrounding>(rig.gameObject);
        Undo.RecordObject(grounding, "Bind grounding rig");
        grounding.ConfigureRig(pivot, probes, radii);
        EditorUtility.SetDirty(grounding);

        foreach (var pair in before)
        {
            Matrix4x4 now = pair.Key.localToWorldMatrix;
            for (int k = 0; k < 16; k++)
                Require(Mathf.Abs(now[k] - pair.Value[k]) < 0.003f,
                    name + ": rigging moved wheel " + pair.Key.name);
        }

        Vector3 fl = probes[0].localPosition, fr = probes[1].localPosition;
        Vector3 rl = probes[2].localPosition, rr = probes[3].localPosition;
        log.AppendLine(string.Format(
            "{0}: radii(m)={1}; wheelbase={2:0.000}m; track F/R={3:0.000}/{4:0.000}m; rig={5}",
            name, string.Join(", ", radii.Select(r => r.ToString("0.000"))),
            Mathf.Abs((fl.z + fr.z) * 0.5f - (rl.z + rr.z) * 0.5f),
            Mathf.Abs(fr.x - fl.x), Mathf.Abs(rr.x - rl.x), rig.name));
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
