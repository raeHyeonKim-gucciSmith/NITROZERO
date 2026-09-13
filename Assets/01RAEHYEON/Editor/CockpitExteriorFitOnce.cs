using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CockpitExteriorFitOnce
{
    static CockpitExteriorFitOnce() => EditorApplication.delayCall += Fit;

    static void Fit()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/01RAEHYEON/Cockpit.unity") return;
        var report = "Assets/01RAEHYEON/RepairedAssets/CockpitExteriorFit/fit.txt";
        if (File.Exists(report))
        {
            if (File.Exists(report + ".height")) return;
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                var child = root.transform.Find(root.name == "RedCarCockpit" ? "v1_exterior" : root.name == "BlueCarCockpit" ? "v2_exterior" : root.name == "GreenCarCockpit" ? "v3_exterior" : "");
                if (child) child.localPosition = new Vector3(0f, 2f, -10.5f);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText(report + ".height", "Exterior Y=2: wheels above ground, roof over roll cage.\n");
            return;
        }

        GameObject FindRoot(string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        var source = FindRoot("v1_exterior");
        var red = FindRoot("RedCarCockpit");
        var blue = FindRoot("BlueCarCockpit");
        var green = FindRoot("GreenCarCockpit");
        if (!source || !red || !blue || !green) return;
        if (red.transform.Find("v1_exterior") || blue.transform.Find("v2_exterior") || green.transform.Find("v3_exterior")) return;

        var blueExterior = UnityEngine.Object.Instantiate(source);
        var greenExterior = UnityEngine.Object.Instantiate(source);
        Place(source, red, "v1_exterior");
        Place(blueExterior, blue, "v2_exterior");
        Place(greenExterior, green, "v3_exterior");
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save Cockpit scene");
        Directory.CreateDirectory(Path.GetDirectoryName(report));
        File.WriteAllText(report, "v1_exterior -> RedCarCockpit\nv2_exterior -> BlueCarCockpit\nv3_exterior -> GreenCarCockpit\nlocalPosition=(0, 1.85, -10.5)\nlocalRotation=(0,0,0)\nlocalScale=(89,75,75)\n");
        Debug.Log("Fitted three cockpit exteriors and saved scene.");
    }

    static void Place(GameObject exterior, GameObject cockpit, string name)
    {
        exterior.name = name;
        exterior.transform.SetParent(cockpit.transform, false);
        exterior.transform.localPosition = new Vector3(0f, 1.85f, -10.5f);
        exterior.transform.localRotation = Quaternion.identity;
        exterior.transform.localScale = new Vector3(89f, 75f, 75f);
    }
}
