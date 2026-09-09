using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DomeInTheMoonTerrainGenerator
{
    private const string ScenePath = "Assets/Scenes/domeInTheMoon.unity";
    private const string DataPath = "Assets/Terrain/DomeInTheMoon_Basin.asset";
    private const string LayerPath = "Assets/02YUJEONG/Moon/moon_04_2k.blend/textures/NewLayer.terrainlayer";
    private const int Resolution = 2049;
    private const float Length = 6000f;
    private const float Width = 4000f;
    private const float Height = 560f;
    private const float OriginY = -100f;
    private const float RoadY = -20f;
    private const float DomeX = -1500f;
    private const float DomeZ = 0f;
    private const float DomeFlatRadius = 650f;
    private const float DomeBlendRadius = 1080f;
    private const float RoadStartX = -930f;
    private const float RoadFlatHalfWidth = 75f;
    private const float RoadBlendHalfWidth = 850f;
    private const int Version = 4;

    private readonly struct Crater
    {
        public readonly float X, Z, Radius, Depth, Rim, GapAngle, GapSize;
        public Crater(float x, float z, float radius, float depth, float rim, float gapAngle, float gapSize)
        { X = x; Z = z; Radius = radius; Depth = depth; Rim = rim; GapAngle = gapAngle; GapSize = gapSize; }
    }

    // Five outer craters. Together with the two pad-side craters this keeps about 28% intact rims.
    private static readonly Crater[] OuterCraters =
    {
        new Crater(-2550f, -1350f, 185f, 25f, 14f, 0.4f, 0f),
        new Crater( 2180f,  1420f, 230f, 31f, 17f, 2.1f, 0f),
        new Crater(-2500f,  1300f, 150f, 20f, 12f, 1.2f, 0.72f),
        new Crater( -300f, -1520f, 115f, 15f,  9f, 4.5f, 0.60f),
        new Crater( 1900f, -1320f, 170f, 23f, 13f, 5.4f, 0.68f)
    };

    // Small collapsed craters moved onto the broad dome-side flat, outside the protected installation core.
    private static readonly Crater[] PadSideCraters =
    {
        new Crater(-1970f,  520f, 65f, 8f, 5f, 2.5f, 0.72f),
        new Crater(-1180f, -650f, 72f, 9f, 6f, 5.7f, 0.76f)
    };

    [InitializeOnLoadMethod]
    private static void GenerateOnce()
    {
        string key = $"NITROZERO.DomeInTheMoonTerrain.{Application.dataPath.GetHashCode()}";
        if (AssetDatabase.LoadAssetAtPath<TerrainData>(DataPath) != null && EditorPrefs.GetInt(key, 0) >= Version)
            return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            try { Generate(); EditorPrefs.SetInt(key, Version); }
            catch (Exception exception) { Debug.LogException(exception); }
        };
    }

    [MenuItem("NITRO ZERO/Moon Terrain/Generate domeInTheMoon Basin")]
    public static void Generate()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool close = !scene.isLoaded;
        if (close) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(DataPath);
        if (data == null)
        {
            data = new TerrainData();
            AssetDatabase.CreateAsset(data, DataPath);
        }
        data.heightmapResolution = Resolution;
        data.size = new Vector3(Length, Height, Width);
        data.alphamapResolution = 1024;
        data.baseMapResolution = 2048;
        data.SetHeights(0, 0, BuildHeights());

        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerPath);
        if (layer != null)
        {
            data.terrainLayers = new[] { layer };
            float[,,] alpha = new float[data.alphamapResolution, data.alphamapResolution, 1];
            for (int z = 0; z < data.alphamapResolution; z++)
                for (int x = 0; x < data.alphamapResolution; x++) alpha[z, x, 0] = 1f;
            data.SetAlphamaps(0, 0, alpha);
        }

        Terrain terrain = FindTerrain(scene);
        if (terrain == null)
        {
            GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
            SceneManager.MoveGameObjectToScene(terrainObject, scene);
            terrain = terrainObject.GetComponent<Terrain>();
        }
        terrain.terrainData = data;
        terrain.gameObject.name = "Dome Moon Basin 6000x4000";
        terrain.transform.position = new Vector3(-Length * 0.5f, OriginY, -Width * 0.5f);
        terrain.heightmapPixelError = 4f;
        terrain.basemapDistance = 5500f;
        terrain.drawInstanced = true;
        terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        terrain.gameObject.isStatic = true;
        TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
        if (collider == null) collider = terrain.gameObject.AddComponent<TerrainCollider>();
        collider.terrainData = data;

        CreateGuides(scene);
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                camera.farClipPlane = Mathf.Max(camera.farClipPlane, 6000f);

        EditorUtility.SetDirty(data);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (close) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[NITRO ZERO] domeInTheMoon 6000x4000 basin terrain generated.");
    }

    private static float[,] BuildHeights()
    {
        float[,] result = new float[Resolution, Resolution];
        float roadLocal = RoadY - OriginY;
        for (int z = 0; z < Resolution; z++)
        {
            float worldZ = z / (float)(Resolution - 1) * Width - Width * 0.5f;
            for (int x = 0; x < Resolution; x++)
            {
                float worldX = x / (float)(Resolution - 1) * Length - Length * 0.5f;
                Vector2 warp = Warp(worldX, worldZ);
                float basinDistance = Mathf.Sqrt(
                    Mathf.Pow((worldX - DomeX * 0.25f) / (Length * 0.54f), 2f) +
                    Mathf.Pow(worldZ / (Width * 0.54f), 2f));
                float edgeRise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.34f, 0.84f, basinDistance));
                float edgeVariation = Mathf.Lerp(0.68f, 1.28f, Fbm(warp.x, warp.y, 0.00055f, 3, 313f));
                float h = roadLocal + edgeRise * Mathf.Lerp(178f, 292f, edgeVariation);
                h += (Fbm(warp.x, warp.y, 0.0011f, 4, 41f) - 0.5f) * (9f + edgeRise * 44f);
                h += BroadRidge(worldX, worldZ, -2350f, 1250f, 820f, 430f, 58f);
                h += BroadRidge(worldX, worldZ, 900f, -1550f, 1050f, 390f, 64f);
                h += BroadRidge(worldX, worldZ, 2380f, 1280f, 720f, 360f, 52f);
                for (int i = 0; i < OuterCraters.Length; i++) h += CraterHeight(worldX, worldZ, OuterCraters[i], i);

                float domeDistance = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(DomeX, DomeZ));
                float domeBoundaryNoise = Fbm(worldX, worldZ, 0.0017f, 3, 811f) - 0.5f;
                float domeFlatEdge = DomeFlatRadius + domeBoundaryNoise * 110f;
                float domeBlendEdge = DomeBlendRadius + domeBoundaryNoise * 190f;
                float domeBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(domeFlatEdge, domeBlendEdge, domeDistance));
                h = Mathf.Lerp(roadLocal, h, domeBlend);
                if (domeDistance <= DomeFlatRadius - 60f) h = roadLocal;
                for (int i = 0; i < PadSideCraters.Length; i++)
                    h += CraterHeight(worldX, worldZ, PadSideCraters[i], i + OuterCraters.Length);

                if (worldX >= RoadStartX)
                {
                    bool upperSide = worldZ >= 0f;
                    float sideSeed = upperSide ? 227f : 563f;
                    float broad = Mathf.PerlinNoise(worldX * 0.00046f + sideSeed, sideSeed * 0.019f);
                    float detail = Mathf.PerlinNoise(worldX * 0.00105f + sideSeed * 0.21f, sideSeed * 0.031f);
                    float riseStart = Mathf.Lerp(145f, 335f, broad);
                    float fullHeightAt = Mathf.Lerp(890f, 1420f, broad * 0.65f + detail * 0.35f);
                    float roadBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(riseStart, fullHeightAt, Mathf.Abs(worldZ)));
                    h = Mathf.Lerp(roadLocal, h, roadBlend);
                    if (Mathf.Abs(worldZ) <= RoadFlatHalfWidth) h = roadLocal;
                }
                result[z, x] = Mathf.Clamp01(h / Height);
            }
        }
        Smooth(result, 2, roadLocal / Height);
        return result;
    }

    private static float CraterHeight(float x, float z, Crater crater, int index)
    {
        float dx = x - crater.X;
        float dz = z - crater.Z;
        float d = Mathf.Sqrt(dx * dx + dz * dz) / crater.Radius;
        if (d > 1.32f) return 0f;
        float angle = Mathf.Atan2(dz, dx);
        float angleNoise = Fbm(x, z, 0.014f, 3, 71f + index * 29f);
        float rimWidth = Mathf.Lerp(0.075f, 0.15f, angleNoise);
        float rimHeight = Mathf.Lerp(0.58f, 1.38f, Fbm(x, z, 0.021f, 3, 109f + index * 17f));
        float gap = 1f;
        if (crater.GapSize > 0f)
        {
            float delta = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, crater.GapAngle * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
            gap = Mathf.SmoothStep(0.03f, 1f, Mathf.InverseLerp(crater.GapSize * 0.4f, crater.GapSize, delta));
        }
        float bowl = -crater.Depth * Mathf.Pow(Mathf.Clamp01(1f - d), 2.1f);
        float rim = crater.Rim * rimHeight * gap * Mathf.Exp(-Mathf.Pow((d - 1f) / rimWidth, 2f));
        return bowl + rim;
    }

    private static float BroadRidge(float x, float z, float cx, float cz, float rx, float rz, float height)
    {
        float dx = (x - cx) / rx;
        float dz = (z - cz) / rz;
        float d = Mathf.Sqrt(dx * dx + dz * dz);
        if (d >= 1.4f) return 0f;
        float body = Mathf.Pow(Mathf.Clamp01(1f - d / 1.4f), 2.5f);
        return height * body * Mathf.Lerp(0.78f, 1.16f, Fbm(x, z, 0.0017f, 3, cx * 0.01f));
    }

    private static Vector2 Warp(float x, float z)
    {
        float wx = (Mathf.PerlinNoise(x * 0.00065f + 31f, z * 0.00065f + 17f) - 0.5f) * 220f;
        float wz = (Mathf.PerlinNoise(x * 0.00057f + 73f, z * 0.00057f + 47f) - 0.5f) * 220f;
        return new Vector2(x + wx, z + wz);
    }

    private static float Fbm(float x, float z, float scale, int octaves, float seed)
    {
        float sum = 0f, amplitude = 1f, total = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += Mathf.PerlinNoise(x * scale + seed, z * scale + seed * 0.37f) * amplitude;
            total += amplitude;
            amplitude *= 0.5f;
            scale *= 2f;
        }
        return sum / total;
    }

    private static void Smooth(float[,] heights, int passes, float roadHeight)
    {
        int size = heights.GetLength(0);
        float[,] copy = new float[size, size];
        for (int pass = 0; pass < passes; pass++)
        {
            Array.Copy(heights, copy, heights.Length);
            for (int z = 1; z < size - 1; z++)
                for (int x = 1; x < size - 1; x++)
                {
                    float worldX = x / (float)(size - 1) * Length - Length * 0.5f;
                    float worldZ = z / (float)(size - 1) * Width - Width * 0.5f;
                    float domeDistance = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(DomeX, DomeZ));
                    if (domeDistance <= DomeFlatRadius - 60f || (worldX >= RoadStartX && Mathf.Abs(worldZ) <= RoadFlatHalfWidth))
                    { heights[z, x] = roadHeight; continue; }
                    heights[z, x] = (copy[z, x] * 4f + copy[z - 1, x] + copy[z + 1, x] + copy[z, x - 1] + copy[z, x + 1]) / 8f;
                }
        }
    }

    private static Terrain FindTerrain(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Terrain terrain = root.GetComponentInChildren<Terrain>(true);
            if (terrain != null) return terrain;
        }
        return null;
    }

    private static void CreateGuides(Scene scene)
    {
        GameObject domeGuide = FindRoot(scene, "Dome Flat Area Guide (1300m)");
        if (domeGuide == null)
        {
            domeGuide = new GameObject("Dome Flat Area Guide (1300m)");
            SceneManager.MoveGameObjectToScene(domeGuide, scene);
        }
        domeGuide.transform.position = new Vector3(DomeX, RoadY, DomeZ);
        domeGuide.transform.localScale = new Vector3(DomeFlatRadius * 2f, 1f, DomeFlatRadius * 2f);

        GameObject roadGuide = FindRoot(scene, "Dome Exit Road Guide (150m)");
        if (roadGuide == null)
        {
            roadGuide = new GameObject("Dome Exit Road Guide (150m)");
            SceneManager.MoveGameObjectToScene(roadGuide, scene);
            roadGuide.AddComponent<MoonRoadPlacementGuide>();
        }
        float roadLength = Length * 0.5f - RoadStartX;
        roadGuide.transform.position = new Vector3(RoadStartX + roadLength * 0.5f, RoadY, 0f);
        roadGuide.transform.localScale = new Vector3(roadLength, 1f, RoadFlatHalfWidth * 2f);
        MoonRoadPlacementGuide info = roadGuide.GetComponent<MoonRoadPlacementGuide>();
        info.mapLength = roadLength;
        info.flatWidth = RoadFlatHalfWidth * 2f;
        info.blendedWidth = RoadBlendHalfWidth * 2f;
        info.roadSurfaceY = RoadY;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
        return null;
    }
}
