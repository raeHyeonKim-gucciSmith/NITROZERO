using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MoonSceneAsphaltRoadGenerator
{
    private const string AsphaltFolder = "Assets/01RAEHYEON/Material/Asphalt025C_4K-JPG/";
    private const string AsphaltPath = AsphaltFolder + "highQualityAsphalt.mat";
    private const string ColorPath = AsphaltFolder + "Asphalt025C_4K-JPG_Color.jpg";
    private const string NormalPath = AsphaltFolder + "Asphalt025C_4K-JPG_NormalGL.jpg";
    private const string RoughnessPath = AsphaltFolder + "Asphalt025C_4K-JPG_Roughness.jpg";
    private const string OcclusionPath = AsphaltFolder + "Asphalt025C_4K-JPG_AmbientOcclusion.jpg";
    private const string HeightPath = AsphaltFolder + "Asphalt025C_4K-JPG_Displacement.jpg";
    private const float TileWorldSize = 4f;
    private const float PlacedDomeX = -1123f;
    private const float PlacedDomeZ = 32f;

    [MenuItem("NITRO ZERO/Moon Terrain/Apply Asphalt Roads To All Moon Scenes")]
    public static void ApplyAll()
    {
        Material asphalt = CreateHighQualityAsphalt();
        BuildStraightRoad("Assets/Scenes/avoidMissile.unity", "Avoid Missile Asphalt Road",
            "Assets/Terrain/AvoidMissile_AsphaltRoad.asset", -2000f, 2000f, 0f, -29.65f, 100f, asphalt);
        BuildStraightRoad("Assets/Scenes/boostOn.unity", "Boost On Asphalt Road",
            "Assets/Terrain/BoostOn_AsphaltRoad.asset", -3000f, 3000f, 0f, -23.65f, 140f, asphalt);
        BuildDomeRoad(asphalt);
        ApplyRacingRoad(asphalt);

        AssetDatabase.SaveAssets();
        Debug.Log("[NITRO ZERO] highQualityAsphalt applied to all four moon-road scenes.");
    }

    private static Material CreateHighQualityAsphalt()
    {
        ConfigureTexture(ColorPath, false, true);
        ConfigureTexture(NormalPath, true, false);
        ConfigureTexture(RoughnessPath, false, false);
        ConfigureTexture(OcclusionPath, false, false);
        ConfigureTexture(HeightPath, false, false);

        Material material = AssetDatabase.LoadAssetAtPath<Material>(AsphaltPath);
        Shader shader = Shader.Find("NITRO ZERO/High Quality Asphalt");
        if (shader == null) throw new MissingReferenceException("High Quality Asphalt shader was not found.");
        if (material == null)
        {
            material = new Material(shader) { name = "highQualityAsphalt" };
            AssetDatabase.CreateAsset(material, AsphaltPath);
        }
        else material.shader = shader;

        Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(ColorPath);
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
        Texture2D roughness = AssetDatabase.LoadAssetAtPath<Texture2D>(RoughnessPath);
        Texture2D occlusion = AssetDatabase.LoadAssetAtPath<Texture2D>(OcclusionPath);
        material.SetTexture("_BaseMap", color);
        material.SetTexture("_BumpMap", normal);
        material.SetTexture("_RoughnessMap", roughness);
        material.SetTexture("_OcclusionMap", occlusion);
        material.SetColor("_BaseColor", new Color(0.78f, 0.78f, 0.78f, 1f));
        material.SetFloat("_BaseWorldSize", 5.5f);
        material.SetFloat("_SecondaryWorldSize", 17f);
        material.SetFloat("_DetailWorldSize", 1.05f);
        material.SetFloat("_MacroWorldSize", 47f);
        material.SetFloat("_SecondaryBlend", 0.26f);
        material.SetFloat("_BumpStrength", 1.25f);
        material.SetFloat("_DetailStrength", 0.52f);
        material.SetFloat("_MacroVariation", 0.075f);
        material.SetFloat("_RoughnessStrength", 0.88f);
        material.SetFloat("_OcclusionStrength", 0.78f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureTexture(string path, bool normalMap, bool srgb)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = srgb;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 16;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();
    }

    private static void ApplyRacingRoad(Material asphalt)
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/racing.unity", OpenSceneMode.Single);
        GameObject road = GameObject.Find("Racing Asphalt Road Preview");
        if (road == null) return;
        MeshFilter filter = road.GetComponent<MeshFilter>();
        MeshRenderer renderer = road.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = asphalt;
        if (filter != null && filter.sharedMesh != null)
        {
            MeshCollider collider = road.GetComponent<MeshCollider>();
            if (collider == null) collider = road.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void BuildDomeRoad(Material asphalt)
    {
        const string scenePath = "Assets/Scenes/domeInTheMoon.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject dome = GameObject.Find("industrial_dome");
        GameObject guide = GameObject.Find("Dome Exit Road Guide (150m)");

        float roadZ = dome != null ? dome.transform.position.z : PlacedDomeZ;
        float startX = -930f;
        if (guide != null) startX = guide.transform.position.x - guide.transform.lossyScale.x * 0.5f;
        float domeX = dome != null ? dome.transform.position.x : PlacedDomeX;
        startX = Mathf.Max(startX, domeX + 190f);

        CreateOrUpdateRoad(scene, "Dome Exit Asphalt Road", "Assets/Terrain/DomeExit_AsphaltRoad.asset",
            new Vector3(startX, -19.65f, roadZ), new Vector3(3000f, -19.65f, roadZ), 150f, asphalt);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void BuildStraightRoad(string scenePath, string objectName, string meshPath,
        float startX, float endX, float z, float y, float width, Material asphalt)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        CreateOrUpdateRoad(scene, objectName, meshPath,
            new Vector3(startX, y, z), new Vector3(endX, y, z), width, asphalt);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void CreateOrUpdateRoad(Scene scene, string objectName, string meshPath,
        Vector3 start, Vector3 end, float width, Material asphalt)
    {
        Vector3 direction = (end - start).normalized;
        Vector3 lateral = Vector3.Cross(Vector3.up, direction) * (width * 0.5f);
        float length = Vector3.Distance(start, end);

        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (mesh == null)
        {
            mesh = new Mesh { name = objectName + " Mesh" };
            AssetDatabase.CreateAsset(mesh, meshPath);
        }
        mesh.Clear();
        mesh.vertices = new[] { start - lateral, start + lateral, end - lateral, end + lateral };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f), new Vector2(width / TileWorldSize, 0f),
            new Vector2(0f, length / TileWorldSize), new Vector2(width / TileWorldSize, length / TileWorldSize)
        };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);

        GameObject road = GameObject.Find(objectName);
        if (road == null)
        {
            road = new GameObject(objectName);
            SceneManager.MoveGameObjectToScene(road, scene);
        }
        MeshFilter filter = road.GetComponent<MeshFilter>();
        if (filter == null) filter = road.AddComponent<MeshFilter>();
        MeshRenderer renderer = road.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = road.AddComponent<MeshRenderer>();
        MeshCollider collider = road.GetComponent<MeshCollider>();
        if (collider == null) collider = road.AddComponent<MeshCollider>();
        filter.sharedMesh = mesh;
        renderer.sharedMaterial = asphalt;
        collider.sharedMesh = null;
        collider.sharedMesh = mesh;
        road.isStatic = true;
    }

}
