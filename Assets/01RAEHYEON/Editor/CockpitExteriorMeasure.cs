using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CockpitExteriorMeasure
{
    static CockpitExteriorMeasure() => EditorApplication.delayCall += Measure;

    static void Measure()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/01RAEHYEON/Cockpit.unity") return;
        var sb = new StringBuilder();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (!root.name.Contains("Cockpit") && root.name != "v1_exterior") continue;
            Append(sb, root, 0);
            foreach (Transform child in root.transform)
                Append(sb, child.gameObject, 1);
        }
        var dir = "Assets/01RAEHYEON/RepairedAssets/CockpitExteriorFit";
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "measure.txt"), sb.ToString());
        Debug.Log("Cockpit exterior measurement written");
    }

    static void Append(StringBuilder sb, GameObject obj, int depth)
    {
        var t = obj.transform;
        sb.AppendLine($"{new string(' ', depth * 2)}{obj.name} pos={t.position:F4} rot={t.eulerAngles:F2} scale={t.lossyScale:F4} local={t.localPosition:F4}/{t.localScale:F4}");
        var renderers = obj.GetComponentsInChildren<Renderer>(true).Where(r => !r.name.StartsWith("SM")).ToArray();
        if (renderers.Length == 0) return;
        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        sb.AppendLine($"{new string(' ', depth * 2)}  renderers={renderers.Length} bounds center={b.center:F4} size={b.size:F4} min={b.min:F4} max={b.max:F4}");
        if (obj.name == "v1_cockpit")
            foreach (var r in renderers)
                sb.AppendLine($"    {r.name} center={r.bounds.center:F3} size={r.bounds.size:F3}");
    }
}
