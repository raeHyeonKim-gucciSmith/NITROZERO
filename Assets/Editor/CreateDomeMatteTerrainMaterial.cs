using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CreateDomeMatteTerrainMaterial
{
    private const string ShaderName = "NITRO ZERO/Terrain/Matte Lunar";
    private const string MaterialPath = "Assets/Terrain/Dome_MatteLunarTerrain.mat";
    private const string TerrainPath = "Assets/Terrain/DomeInTheMoon_Basin.asset";
    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/domeInTheMoon.unity",
        "Assets/01RAEHYEON/RHScenes/RH_domeInTheMoon.unity",
        "Assets/04HYUNWOOK/Scenes/HW_domeInTheMoon.unity"
    };

    [MenuItem("NITRO ZERO/Moon Terrain/Create And Apply Dome Matte Shader")]
    public static void Apply()
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null) throw new MissingReferenceException("Shader not imported: " + ShaderName);
        TerrainData targetData = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
        if (targetData == null) throw new MissingReferenceException(TerrainPath);

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "Dome_MatteLunarTerrain" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.shader = shader;
        material.SetFloat("_EnableHeightBlend", 0f);
        material.SetFloat("_EnableInstancedPerPixelNormal", 1f);
        material.DisableKeyword("_TERRAIN_BLEND_HEIGHT");
        EditorUtility.SetDirty(material);

        foreach (string path in ScenePaths)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool close = !scene.IsValid() || !scene.isLoaded;
            if (close) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            bool changed = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Terrain terrain in root.GetComponentsInChildren<Terrain>(true))
                {
                    if (terrain.terrainData != targetData) continue;
                    terrain.materialTemplate = material;
                    EditorUtility.SetDirty(terrain);
                    changed = true;
                }
            }
            if (!changed) throw new MissingReferenceException("Dome terrain not found in " + path);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (close) EditorSceneManager.CloseScene(scene, true);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[NITRO ZERO] Matte lunar terrain shader applied to all three dome scenes.");
    }
}
