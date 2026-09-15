using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Swaps the window glass on the finished car prefabs to the dark coated material.
/// Headlight glass and cockpit interior glass are deliberately left alone.
/// </summary>
public static class CarGlassTinter
{
    /// <summary>The dark coated glass already authored in 01RAEHYEON/Material.</summary>
    private const string CoatedGlass = "Assets/01RAEHYEON/Material/ExtraCars_OpaqueGlass.mat";

    private static readonly string[] CarPrefabs =
    {
        "Assets/Prefabs/Final_Cars/Green_Car_Final.prefab",
        "Assets/Prefabs/Final_Cars/extraCar3_Final.prefab",
    };

    /// <summary>
    /// The window glass actually used by the two prefabs above, verified against the
    /// objects that carry them. MAT_Bullet_Glass (missile shells), frontlight1 and
    /// mirror1 sit on the same models and are deliberately not listed.
    /// </summary>
    private static readonly string[] WindowMaterials =
    {
        "Assets/ImportedBlenderAsset/Materials/MAT_Taurus_Glass_LightDust_v24.mat",
        "Assets/SportCar/Models/SportCar_1/Materials/glass1.mat",
    };

    [MenuItem("Tools/Trailer/Tint Car Glass (Final Cars)")]
    public static void TintGlass()
    {
        var coated = AssetDatabase.LoadAssetAtPath<Material>(CoatedGlass);
        if (coated == null)
        {
            EditorUtility.DisplayDialog("Tint Car Glass",
                "Coated glass material not found:\n" + CoatedGlass, "OK");
            return;
        }

        var targets = new HashSet<Material>();
        var unresolved = new List<string>();
        foreach (string path in WindowMaterials)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) targets.Add(mat);
            else unresolved.Add(path);
        }

        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("Tint Car Glass",
                "None of the window materials could be loaded.", "OK");
            return;
        }

        bool go = EditorUtility.DisplayDialog("Tint Car Glass",
            "This rewrites " + CarPrefabs.Length + " PREFAB ASSETS on disk.\n" +
            "Other team members share these files and Undo does not cover it.\n\n" +
            "Window glass becomes: " + coated.name + "\n\nContinue?",
            "Rewrite prefabs", "Cancel");
        if (!go) return;

        var log = new StringBuilder();
        log.AppendLine("Glass tinted to '" + coated.name + "':");
        int totalSlots = 0;

        foreach (string prefabPath in CarPrefabs)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            string shortName = System.IO.Path.GetFileNameWithoutExtension(prefabPath);

            if (asset == null)
            {
                log.AppendLine("  " + shortName.PadRight(22) + " MISSING - skipped");
                continue;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            int slots = 0;

            try
            {
                foreach (Renderer r in contents.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] mats = r.sharedMaterials;
                    bool changed = false;

                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null || !targets.Contains(mats[i])) continue;
                        mats[i] = coated;
                        changed = true;
                        slots++;
                    }

                    if (changed) r.sharedMaterials = mats;
                }

                if (slots > 0) PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            log.AppendLine("  " + shortName.PadRight(22) +
                           (slots > 0 ? slots + " slot(s) swapped" : "no window glass found"));
            totalSlots += slots;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (unresolved.Count > 0)
        {
            log.AppendLine("  source materials not found:");
            foreach (string u in unresolved) log.AppendLine("    " + u);
        }

        log.AppendLine("  " + totalSlots + " slot(s) total. Prefab assets were written.");
        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("Tint Car Glass", log.ToString(), "OK");
    }
}
