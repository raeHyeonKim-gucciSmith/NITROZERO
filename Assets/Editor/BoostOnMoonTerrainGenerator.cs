using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BoostOnMoonTerrainGenerator
{
    private const string ScenePath = "Assets/Scenes/boostOn.unity";
    private const string DataPath = "Assets/Terrain/BoostOn_MoonOpenPlain_v5.asset";
    private const string LayerPath = "Assets/02YUJEONG/Moon/moon_04_2k.blend/textures/NewLayer.terrainlayer";
    private const int Resolution = 2049;
    private const float Length = 6000f;
    private const float Width = 2500f;
    private const float Height = 360f;
    private const float OriginY = -80f;
    private const float RoadY = -24f;
    private const float FlatHalfWidth = 70f;
    private const float BlendHalfWidth = 165f;
    private const int Version = 8;

    private readonly struct Basin
    {
        public readonly float X, Z, Radius, Depth, Rim;
        public Basin(float x, float z, float radius, float depth, float rim)
        { X = x; Z = z; Radius = radius; Depth = depth; Rim = rim; }
    }

    private readonly struct Hill
    {
        public readonly float X, Z, RadiusX, RadiusZ, Height;
        public Hill(float x, float z, float radiusX, float radiusZ, float height)
        { X = x; Z = z; RadiusX = radiusX; RadiusZ = radiusZ; Height = height; }
    }

    private static readonly Basin[] Basins =
    {
        new Basin(-2470f, -720f, 360f, 48f, 20f),
        new Basin(-1510f, 830f, 245f, 34f, 14f),
        new Basin(-420f, -910f, 430f, 52f, 23f),
        new Basin(760f, 680f, 190f, 28f, 11f),
        new Basin(1740f, -790f, 310f, 43f, 17f),
        new Basin(2590f, 780f, 390f, 49f, 21f),
        new Basin(2180f, 430f, 88f, 12f, 4f),
        new Basin(-2050f, 480f, 72f, 9f, 3f),
        new Basin(110f, 520f, 105f, 14f, 5f)
    };

    private static readonly Hill[] Hills =
    {
        new Hill(-2670f, 880f, 510f, 260f, 86f),
        new Hill(-2130f, -940f, 370f, 220f, 58f),
        new Hill(-1210f, 1030f, 610f, 285f, 102f),
        new Hill(-520f, -870f, 430f, 250f, 69f),
        new Hill(380f, 920f, 520f, 310f, 81f),
        new Hill(1140f, -1010f, 650f, 290f, 96f),
        new Hill(1960f, 860f, 440f, 270f, 72f),
        new Hill(2740f, -820f, 560f, 300f, 88f),
        new Hill(2860f, 960f, 310f, 190f, 54f)
    };

    [InitializeOnLoadMethod]
    private static void GenerateOnce()
    {
        string key = $"NITROZERO.BoostOnMoonCanyon.{Application.dataPath.GetHashCode()}";
        if (AssetDatabase.LoadAssetAtPath<TerrainData>(DataPath) != null && EditorPrefs.GetInt(key, 0) >= Version)
            return;

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            try
            {
                Generate();
                EditorPrefs.SetInt(key, Version);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        };
    }

    [MenuItem("NITRO ZERO/Moon Terrain/Generate boostOn Moon Canyon")]
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
            terrainObject.name = "BoostOn Moon Canyon 6000x2500";
            SceneManager.MoveGameObjectToScene(terrainObject, scene);
            terrain = terrainObject.GetComponent<Terrain>();
        }
        terrain.terrainData = data;
        terrain.gameObject.name = "BoostOn Moon Open Plain 6000x2500";
        terrain.transform.position = new Vector3(-Length * 0.5f, OriginY, -Width * 0.5f);
        terrain.heightmapPixelError = 4f;
        terrain.basemapDistance = 4500f;
        terrain.drawInstanced = true;
        terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        terrain.gameObject.isStatic = true;
        TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
        if (collider == null) collider = terrain.gameObject.AddComponent<TerrainCollider>();
        collider.terrainData = data;

        CreateGuide(scene);
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                camera.farClipPlane = Mathf.Max(camera.farClipPlane, 4500f);

        EditorUtility.SetDirty(data);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (close) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[NITRO ZERO] boostOn 6000x2500 moon canyon generated.");
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

    private static void CreateGuide(Scene scene)
    {
        GameObject guide = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "Boost Road Guide (140m Flat Corridor)") guide = root;
        if (guide == null)
        {
            guide = new GameObject("Boost Road Guide (140m Flat Corridor)");
            SceneManager.MoveGameObjectToScene(guide, scene);
            guide.AddComponent<MoonRoadPlacementGuide>();
        }
        guide.transform.position = new Vector3(0f, RoadY, 0f);
        guide.transform.localScale = new Vector3(Length, 1f, FlatHalfWidth * 2f);
        MoonRoadPlacementGuide info = guide.GetComponent<MoonRoadPlacementGuide>();
        info.mapLength = Length;
        info.flatWidth = FlatHalfWidth * 2f;
        info.blendedWidth = BlendHalfWidth * 2f;
        info.roadSurfaceY = RoadY;
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
                // Keep the broad plain near the road elevation so the map does not read as a giant bowl.
                float h = 53f + (Fbm(warp.x, warp.y, 0.0007f, 4, 13f) - 0.5f) * 22f;
                h += (Fbm(warp.x, warp.y, 0.0032f, 3, 71f) - 0.5f) * 8f;
                for (int i = 0; i < Hills.Length; i++) h += HillHeight(worldX, worldZ, Hills[i]);
                for (int i = 0; i < Basins.Length; i++) h += BasinHeight(worldX, worldZ, Basins[i]);
                h += ScatteredCrater(worldX, worldZ, 420f, 0.38f, 37);
                h += ScatteredCrater(worldX, worldZ, 180f, 0.28f, 73);
                h += IrregularSideElevation(worldX, worldZ);

                float varyingBlend = BlendHalfWidth + (Mathf.PerlinNoise(worldX * 0.0011f + 5f, 8f) - 0.5f) * 38f;
                float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(FlatHalfWidth, varyingBlend, Mathf.Abs(worldZ)));
                h = Mathf.Lerp(roadLocal, h, blend);
                if (Mathf.Abs(worldZ) <= FlatHalfWidth) h = roadLocal;
                result[z, x] = Mathf.Clamp01(h / Height);
            }
        }
        Smooth(result, 2, roadLocal / Height);
        return result;
    }

    private static float IrregularSideElevation(float x, float z)
    {
        // Each side uses a different low-frequency profile so neither the rise nor its peak mirrors the other.
        bool rightSide = z >= 0f;
        float sideSeed = rightSide ? 613f : 947f;
        float alongRoad = x * 0.00042f + sideSeed;
        float broad = Mathf.PerlinNoise(alongRoad, sideSeed * 0.013f);
        float secondary = Mathf.PerlinNoise(x * 0.00093f + sideSeed * 0.17f, sideSeed * 0.021f);

        float riseStart = Mathf.Lerp(255f, 545f, broad);
        float peakHeight = Mathf.Lerp(47f, 92f, broad * 0.62f + secondary * 0.38f);
        float sideDistance = Mathf.Abs(z);
        float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(riseStart, Width * 0.5f, sideDistance));

        // A very broad undulation prevents the outer elevation from ending at one uniform contour line.
        float outerVariation = Mathf.Lerp(0.82f, 1.16f,
            Mathf.PerlinNoise(x * 0.00058f + sideSeed * 0.31f, z * 0.00037f + sideSeed));
        return rise * peakHeight * outerVariation;
    }

    private static float HillHeight(float x, float z, Hill hill)
    {
        float angle = Mathf.Repeat(Mathf.Abs(hill.X + hill.Z) * 0.009f, Mathf.PI);
        float lx = x - hill.X;
        float lz = z - hill.Z;
        float rx = lx * Mathf.Cos(angle) - lz * Mathf.Sin(angle);
        float rz = lx * Mathf.Sin(angle) + lz * Mathf.Cos(angle);
        float d = Mathf.Sqrt(rx * rx / (hill.RadiusX * hill.RadiusX) + rz * rz / (hill.RadiusZ * hill.RadiusZ));
        if (d >= 1.35f) return 0f;
        float body = Mathf.Pow(Mathf.Clamp01(1f - d / 1.35f), 2.6f);
        float asymmetry = 0.86f + (Fbm(x, z, 0.0024f, 3, hill.X * 0.01f) - 0.5f) * 0.3f;
        return hill.Height * body * asymmetry;
    }

    private static float ScatteredCrater(float x, float z, float cellSize, float probability, int seed)
    {
        int cellX = Mathf.FloorToInt(x / cellSize);
        int cellZ = Mathf.FloorToInt(z / cellSize);
        float total = 0f;
        for (int oz = -1; oz <= 1; oz++)
            for (int ox = -1; ox <= 1; ox++)
            {
                int cx = cellX + ox;
                int cz = cellZ + oz;
                float spawn = Hash(cx, cz, seed);
                if (spawn > probability) continue;
                float centerX = (cx + 0.18f + Hash(cx, cz, seed + 11) * 0.64f) * cellSize;
                float centerZ = (cz + 0.18f + Hash(cx, cz, seed + 23) * 0.64f) * cellSize;
                float radius = cellSize * Mathf.Lerp(0.13f, 0.30f, Hash(cx, cz, seed + 41));
                // Reject the entire crater, including its rim, when it could touch the road transition.
                if (Mathf.Abs(centerZ) - radius * 1.32f <= BlendHalfWidth + 35f) continue;
                float angle = Hash(cx, cz, seed + 59) * Mathf.PI * 2f;
                bool intactRim = Hash(cx, cz, seed + 79) > 0.72f;
                float lx = x - centerX;
                float lz = z - centerZ;
                float rx = (lx * Mathf.Cos(angle) - lz * Mathf.Sin(angle)) / radius;
                float rz = (lx * Mathf.Sin(angle) + lz * Mathf.Cos(angle)) / radius;
                float craterAngle = Mathf.Atan2(rz, rx);
                float d = Mathf.Sqrt(rx * rx + rz * rz);
                if (d > 1.3f) continue;
                float gapCenter = Hash(cx, cz, seed + 91) * Mathf.PI * 2f - Mathf.PI;
                float gapDistance = Mathf.Abs(Mathf.DeltaAngle(craterAngle * Mathf.Rad2Deg, gapCenter * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                float gap = intactRim ? 1f : Mathf.Lerp(0.025f, 1f, Mathf.SmoothStep(0.2f, 0.68f, gapDistance));
                float rimRoughness = Mathf.Lerp(0.58f, 1.34f, Fbm(x, z, 0.022f, 3, seed + spawn * 37f));
                float rimWidth = Mathf.Lerp(0.085f, 0.135f, Fbm(x, z, 0.013f, 2, seed + 19f));
                float bowl = -radius * 0.16f * Mathf.Pow(Mathf.Clamp01(1f - d), 2.15f);
                float rim = radius * 0.09f * gap * rimRoughness * Mathf.Exp(-Mathf.Pow((d - 1f) / rimWidth, 2f));
                total += bowl + rim;
            }
        return total;
    }

    private static float Hash(int x, int z, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + z * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h & 0x00ffffffu) / 16777215f;
        }
    }

    private static float BasinHeight(float x, float z, Basin basin)
    {
        float localX = x - basin.X;
        float localZ = z - basin.Z;
        float angle = Mathf.Atan2(localZ, localX);
        float d = Mathf.Sqrt(localX * localX + localZ * localZ) / basin.Radius;
        if (d > 1.35f) return 0f;
        float gapCenter = Mathf.Repeat(Mathf.Abs(basin.Z) * 0.019f, Mathf.PI * 2f) - Mathf.PI;
        float gapDistance = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, gapCenter * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
        bool intactRim = basin.Radius < 120f || Mathf.Abs(basin.X - 760f) < 1f;
        float gap = intactRim ? 1f : Mathf.Lerp(0.025f, 1f, Mathf.SmoothStep(0.28f, 0.78f, gapDistance));
        bool emphasized = Mathf.Abs(basin.X + 2470f) < 1f || Mathf.Abs(basin.X + 420f) < 1f || Mathf.Abs(basin.X - 2590f) < 1f;
        float bowl = -basin.Depth * (emphasized ? 1.32f : 1f) * Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
        float rimRoughness = Mathf.Lerp(0.55f, 1.38f, Fbm(x, z, 0.018f, 3, basin.X * 0.013f + 401f));
        float rimWidth = Mathf.Lerp(0.095f, 0.15f, Fbm(x, z, 0.01f, 2, basin.Z * 0.017f + 433f));
        float rim = basin.Rim * (emphasized ? 1.65f : 1.15f) * gap * rimRoughness * Mathf.Exp(-Mathf.Pow((d - 1f) / rimWidth, 2f));
        return bowl + rim;
    }

    private static Vector2 Warp(float x, float z)
    {
        float wx = (Mathf.PerlinNoise(x * 0.0009f + 17f, z * 0.0009f + 31f) - 0.5f) * 230f;
        float wz = (Mathf.PerlinNoise(x * 0.0011f + 61f, z * 0.0011f + 7f) - 0.5f) * 170f;
        return new Vector2(x + wx, z + wz);
    }

    private static float Fbm(float x, float z, float frequency, int octaves, float seed)
    {
        float sum = 0f, amplitude = 0.5f, norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += Mathf.PerlinNoise(x * frequency + seed, z * frequency + seed * 0.67f) * amplitude;
            norm += amplitude;
            amplitude *= 0.5f;
            frequency *= 2.07f;
        }
        return sum / norm;
    }

    private static void Smooth(float[,] heights, int passes, float roadHeight)
    {
        int size = heights.GetLength(0);
        float[,] buffer = new float[size, size];
        for (int pass = 0; pass < passes; pass++)
        {
            for (int z = 0; z < size; z++)
            {
                float worldZ = z / (float)(size - 1) * Width - Width * 0.5f;
                for (int x = 0; x < size; x++)
                {
                    if (Mathf.Abs(worldZ) <= FlatHalfWidth) { buffer[z, x] = roadHeight; continue; }
                    float sum = heights[z, x] * 4f;
                    float weight = 4f;
                    for (int oz = -1; oz <= 1; oz++)
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            if (ox == 0 && oz == 0) continue;
                            int sx = Mathf.Clamp(x + ox, 0, size - 1);
                            int sz = Mathf.Clamp(z + oz, 0, size - 1);
                            float w = ox == 0 || oz == 0 ? 2f : 1f;
                            sum += heights[sz, sx] * w;
                            weight += w;
                        }
                    buffer[z, x] = sum / weight;
                }
            }
            float[,] swap = heights; heights = buffer; buffer = swap;
        }
    }
}
