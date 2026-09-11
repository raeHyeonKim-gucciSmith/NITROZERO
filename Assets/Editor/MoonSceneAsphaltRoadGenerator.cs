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
    private const string RoadLineFolder = "Assets/01RAEHYEON/Material/RoadLines002_4K-JPG/";
    private const string RoadLineMaterialPath = RoadLineFolder + "OldMoonRoadLine.mat";
    private const string RoadDustMaterialPath = AsphaltFolder + "MoonRoadEdgeDust.mat";
    private const float TileWorldSize = 4f;
    private const float PlacedDomeX = -1123f;
    private const float PlacedDomeZ = 32f;

    [MenuItem("NITRO ZERO/Moon Terrain/Apply Asphalt Roads To All Moon Scenes")]
    public static void ApplyAll()
    {
        Material asphalt = CreateHighQualityAsphalt();
        Material roadLine = CreateOverlayMaterial(false);
        Material roadDust = CreateOverlayMaterial(true);
        BuildStraightRoad("Assets/Scenes/avoidMissile.unity", "Avoid Missile Asphalt Road",
            "Assets/Terrain/AvoidMissile_AsphaltRoad.asset", -2000f, 2000f, 0f, -29.65f, 100f, asphalt, roadLine, roadDust);
        BuildStraightRoad("Assets/Scenes/boostOn.unity", "Boost On Asphalt Road",
            "Assets/Terrain/BoostOn_AsphaltRoad.asset", -3000f, 3000f, 0f, -23.65f, 140f, asphalt, roadLine, roadDust);
        BuildDomeRoad(asphalt, roadLine, roadDust);
        ApplyRacingRoad(asphalt, roadLine, roadDust);

        AssetDatabase.SaveAssets();
        Debug.Log("[NITRO ZERO] highQualityAsphalt applied to all four moon-road scenes.");
    }

    [MenuItem("NITRO ZERO/Moon Terrain/Restore Avoid Missile Asphalt Road")]
    public static void ApplyAvoidMissileOnly()
    {
        Material asphalt = CreateHighQualityAsphalt();
        BuildStraightRoad("Assets/Scenes/avoidMissile.unity", "Avoid Missile Asphalt Road",
            "Assets/Terrain/AvoidMissile_AsphaltRoad.asset", -2000f, 2000f, 0f, -29.65f, 100f, asphalt,
            CreateOverlayMaterial(false), CreateOverlayMaterial(true));
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

    private static Material CreateOverlayMaterial(bool dust)
    {
        string colorPath = dust ? "Assets/02YUJEONG/Moon/moon_dusted_01_2k.blend/textures/moon_dusted_01_diff_2k.jpg"
            : RoadLineFolder + "RoadLines002_4K-JPG_Color.jpg";
        string opacityPath = dust ? colorPath : RoadLineFolder + "RoadLines002_4K-JPG_Opacity.jpg";
        ConfigureTexture(colorPath, false, true);
        ConfigureTexture(opacityPath, false, false);
        if (!dust) ConfigureTexture(RoadLineFolder + "RoadLines002_4K-JPG_NormalGL.jpg", true, false);
        string path = dust ? RoadDustMaterialPath : RoadLineMaterialPath;
        Shader shader = Shader.Find("NITRO ZERO/Moon Road Overlay");
        if (shader == null) throw new MissingReferenceException("Moon Road Overlay shader was not found.");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.shader = shader;
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath));
        material.SetTexture("_OpacityMap", AssetDatabase.LoadAssetAtPath<Texture2D>(opacityPath));
        material.SetColor("_Tint", dust ? new Color(0.46f,0.47f,0.48f,1f) : new Color(0.76f,0.75f,0.70f,1f));
        material.SetFloat("_RepeatLength", dust ? 19f : 3.95f);
        material.SetFloat("_Opacity", dust ? 0.54f : 0.92f);
        material.SetFloat("_NoiseScale", dust ? 23f : 9f);
        material.SetFloat("_Cutoff", dust ? 0.035f : 0.1f);
        material.SetFloat("_IsDust", dust ? 1f : 0f);
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

    private static void ApplyRacingRoad(Material asphalt, Material roadLine, Material roadDust)
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
            CreateRoadOverlays(scene, road, filter.sharedMesh, "Assets/Terrain/Racing", roadLine, roadDust);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void BuildDomeRoad(Material asphalt, Material roadLine, Material roadDust)
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
            new Vector3(startX, -19.65f, roadZ), new Vector3(3000f, -19.65f, roadZ), 150f, asphalt, roadLine, roadDust);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void BuildStraightRoad(string scenePath, string objectName, string meshPath,
        float startX, float endX, float z, float y, float width, Material asphalt, Material roadLine, Material roadDust)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        CreateOrUpdateRoad(scene, objectName, meshPath,
            new Vector3(startX, y, z), new Vector3(endX, y, z), width, asphalt, roadLine, roadDust);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void CreateOrUpdateRoad(Scene scene, string objectName, string meshPath,
        Vector3 start, Vector3 end, float width, Material asphalt, Material roadLine, Material roadDust)
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
        CreateRoadOverlays(scene, road, mesh, meshPath.Substring(0, meshPath.Length - 6), roadLine, roadDust);
    }

    private static void CreateRoadOverlays(Scene scene, GameObject road, Mesh source, string assetPrefix,
        Material roadLine, Material roadDust)
    {
        Vector3[] sourceVertices = source.vertices;
        if (sourceVertices.Length < 4 || sourceVertices.Length % 2 != 0) return;
        int sections = sourceVertices.Length / 2;
        BuildEdgeOverlay(scene, road, sourceVertices, sections, assetPrefix + "_EdgeLines.asset",
            road.name + " Edge Lines", 4.0f, 2.5f, 0.018f, roadLine);
        BuildEdgeOverlay(scene, road, sourceVertices, sections, assetPrefix + "_EdgeDust.asset",
            road.name + " Edge Dust", 0f, 7.5f, 0.009f, roadDust);
    }

    private static void BuildEdgeOverlay(Scene scene, GameObject road, Vector3[] edges, int sections,
        string meshPath, string objectName, float inset, float stripWidth, float height, Material material)
    {
        Vector3[] vertices = new Vector3[sections * 4];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[(sections - 1) * 12];
        float distance = 0f;
        for (int i = 0; i < sections; i++)
        {
            Vector3 left = edges[i * 2], right = edges[i * 2 + 1];
            Vector3 inward = (right - left).normalized;
            if (i > 0) distance += Vector3.Distance((edges[(i-1)*2]+edges[(i-1)*2+1])*0.5f, (left+right)*0.5f);
            vertices[i*4] = left + inward*inset + Vector3.up*height;
            vertices[i*4+1] = left + inward*(inset+stripWidth) + Vector3.up*height;
            vertices[i*4+2] = right - inward*(inset+stripWidth) + Vector3.up*height;
            vertices[i*4+3] = right - inward*inset + Vector3.up*height;
            uvs[i*4]=new Vector2(0,distance); uvs[i*4+1]=new Vector2(1,distance);
            uvs[i*4+2]=new Vector2(1,distance); uvs[i*4+3]=new Vector2(0,distance);
            if(i==sections-1) continue;
            int v=i*4,q=i*12;
            triangles[q]=v; triangles[q+1]=v+4; triangles[q+2]=v+1;
            triangles[q+3]=v+1; triangles[q+4]=v+4; triangles[q+5]=v+5;
            triangles[q+6]=v+2; triangles[q+7]=v+6; triangles[q+8]=v+3;
            triangles[q+9]=v+3; triangles[q+10]=v+6; triangles[q+11]=v+7;
        }
        Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(mesh==null){ mesh=new Mesh{name=objectName+" Mesh"}; AssetDatabase.CreateAsset(mesh,meshPath); }
        mesh.Clear(); mesh.vertices=vertices; mesh.uv=uvs; mesh.triangles=triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
        GameObject overlay=GameObject.Find(objectName);
        if(overlay==null){ overlay=new GameObject(objectName); SceneManager.MoveGameObjectToScene(overlay,scene); }
        overlay.transform.SetPositionAndRotation(road.transform.position,road.transform.rotation);
        overlay.transform.localScale=road.transform.localScale;
        MeshFilter filter=overlay.GetComponent<MeshFilter>(); if(filter==null) filter=overlay.AddComponent<MeshFilter>();
        MeshRenderer renderer=overlay.GetComponent<MeshRenderer>(); if(renderer==null) renderer=overlay.AddComponent<MeshRenderer>();
        filter.sharedMesh=mesh; renderer.sharedMaterial=material; overlay.isStatic=true;
    }

}
