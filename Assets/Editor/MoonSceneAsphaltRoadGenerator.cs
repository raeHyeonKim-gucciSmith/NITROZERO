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
    private const string Asphalt004Folder = "Assets/01RAEHYEON/Material/Asphalt004_4K-JPG/";
    private const string Asphalt006Folder = "Assets/01RAEHYEON/Material/Asphalt006_4K-JPG/";
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

    [MenuItem("NITRO ZERO/Moon Terrain/Restore Avoid Missile Asphalt Road")]
    public static void ApplyAvoidMissileOnly()
    {
        Material asphalt = CreateHighQualityAsphalt();
        BuildStraightRoad("Assets/Scenes/avoidMissile.unity", "Avoid Missile Asphalt Road",
            "Assets/Terrain/AvoidMissile_AsphaltRoad.asset", -2000f, 2000f, 0f, -29.65f, 100f, asphalt);
        AssetDatabase.SaveAssets();
        Debug.Log("[NITRO ZERO] avoidMissile asphalt road restored without changing other scenes.");
    }

    private static Material CreateHighQualityAsphalt()
    {
        ConfigureTexture(ColorPath, false, true);
        ConfigureTexture(NormalPath, true, false);
        ConfigureTexture(RoughnessPath, false, false);
        ConfigureTexture(OcclusionPath, false, false);
        ConfigureTexture(HeightPath, false, false);
        ConfigureTexture(Asphalt004Folder + "Asphalt004_4K-JPG_Color.jpg", false, true);
        ConfigureTexture(Asphalt004Folder + "Asphalt004_4K-JPG_NormalGL.jpg", true, false);
        ConfigureTexture(Asphalt004Folder + "Asphalt004_4K-JPG_Roughness.jpg", false, false);
        ConfigureTexture(Asphalt004Folder + "Asphalt004_4K-JPG_AmbientOcclusion.jpg", false, false);
        ConfigureTexture(Asphalt006Folder + "Asphalt006_4K-JPG_Color.jpg", false, true);
        ConfigureTexture(Asphalt006Folder + "Asphalt006_4K-JPG_NormalGL.jpg", true, false);
        ConfigureTexture(Asphalt006Folder + "Asphalt006_4K-JPG_Roughness.jpg", false, false);
        ConfigureTexture(Asphalt006Folder + "Asphalt006_4K-JPG_AmbientOcclusion.jpg", false, false);

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
        material.SetTexture("_BaseMapB", AssetDatabase.LoadAssetAtPath<Texture2D>(Asphalt004Folder + "Asphalt004_4K-JPG_Color.jpg"));
        material.SetTexture("_BumpMapB", AssetDatabase.LoadAssetAtPath<Texture2D>(Asphalt004Folder + "Asphalt004_4K-JPG_NormalGL.jpg"));
        material.SetTexture("_RoughnessMapB", AssetDatabase.LoadAssetAtPath<Texture2D>(Asphalt004Folder + "Asphalt004_4K-JPG_Roughness.jpg"));
        material.SetTexture("_OcclusionMapB", AssetDatabase.LoadAssetAtPath<Texture2D>(Asphalt004Folder + "Asphalt004_4K-JPG_AmbientOcclusion.jpg"));
        material.SetTexture("_BaseMapC", AssetDatabase.LoadAssetAtPath<Texture2D>(Asphalt006Folder + "Asphalt006_4K-JPG_Color.jpg"));
        material.SetTexture("_BumpMapC", AssetDatabase.LoadAssetAtPath<Texture2D>(Asphalt006Folder + "Asphalt006_4K-JPG_NormalGL.jpg"));
        material.SetTexture("_RoughnessMapC", AssetDatabase.LoadAssetAtPath<Texture2D>(Asphalt006Folder + "Asphalt006_4K-JPG_Roughness.jpg"));
        material.SetTexture("_OcclusionMapC", AssetDatabase.LoadAssetAtPath<Texture2D>(Asphalt006Folder + "Asphalt006_4K-JPG_AmbientOcclusion.jpg"));
        material.SetColor("_BlendBTint", new Color(0.43f, 0.44f, 0.45f, 1f));
        material.SetColor("_BlendCTint", new Color(0.48f, 0.49f, 0.50f, 1f));
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
        float length = Vector3.Distance(start, end);
        Vector3 direction = (end - start).normalized;
        Vector3 lateral = Vector3.Cross(Vector3.up, direction) * (width * 0.5f);
        int segments = Mathf.Max(1, Mathf.CeilToInt(length / 100f));

        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (mesh == null)
        {
            mesh = new Mesh { name = objectName + " Mesh" };
            AssetDatabase.CreateAsset(mesh, meshPath);
        }
        mesh.Clear();
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[segments * 6];
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 center = Vector3.Lerp(start, end, t);
            vertices[i * 2] = center - lateral;
            vertices[i * 2 + 1] = center + lateral;
            uvs[i * 2] = new Vector2(0f, length * t / TileWorldSize);
            uvs[i * 2 + 1] = new Vector2(width / TileWorldSize, length * t / TileWorldSize);
            if (i == segments) continue;
            int v = i * 2, q = i * 6;
            triangles[q] = v; triangles[q + 1] = v + 2; triangles[q + 2] = v + 1;
            triangles[q + 3] = v + 1; triangles[q + 4] = v + 2; triangles[q + 5] = v + 3;
        }
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
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
