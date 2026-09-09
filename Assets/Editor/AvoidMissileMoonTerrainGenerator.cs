using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AvoidMissileMoonTerrainGenerator
{
    private const string ScenePath = "Assets/Scenes/avoidMissile.unity";
    private const string TerrainDataPath = "Assets/Terrain/AvoidMissile_MoonTerrain.asset";
    private const string TerrainLayerPath = "Assets/02YUJEONG/Moon/moon_04_2k.blend/textures/NewLayer.terrainlayer";

    private const int HeightmapResolution = 1025;
    private const float MapLength = 4000f;
    private const float MapWidth = 2000f;
    private const float TerrainHeight = 320f;
    private const float TerrainOriginY = -60f;
    private const float RoadWorldHeight = -30f;
    private const float RoadFlatHalfWidth = 50f;
    private const float RoadBlendHalfWidth = 125f;
    private const int GeneratorVersion = 7;

    private readonly struct Crater
    {
        public readonly float X;
        public readonly float Z;
        public readonly float Radius;
        public readonly float Depth;
        public readonly float RimHeight;

        public Crater(float x, float z, float radius, float depth, float rimHeight)
        {
            X = x;
            Z = z;
            Radius = radius;
            Depth = depth;
            RimHeight = rimHeight;
        }
    }

    private readonly struct Peak
    {
        public readonly float X;
        public readonly float Z;
        public readonly float RadiusX;
        public readonly float RadiusZ;
        public readonly float Height;

        public Peak(float x, float z, float radiusX, float radiusZ, float height)
        {
            X = x;
            Z = z;
            RadiusX = radiusX;
            RadiusZ = radiusZ;
            Height = height;
        }
    }

    private static readonly Crater[] Craters =
    {
        new Crater(-1810f, -735f, 285f, 61f, 24f),
        new Crater(-1265f, 428f, 118f, 24f, 7f),
        new Crater(-735f, -812f, 205f, 43f, 15f),
        new Crater(-365f, 685f, 79f, 14f, 4f),
        new Crater(185f, -458f, 162f, 33f, 11f),
        new Crater(745f, 773f, 318f, 72f, 29f),
        new Crater(1038f, -682f, 96f, 18f, 5f),
        new Crater(1515f, 356f, 221f, 48f, 17f),
        new Crater(1842f, -845f, 61f, 9f, 3f),
        new Crater(-1540f, 163f, 54f, 8f, 2.5f),
        new Crater(468f, 342f, 72f, 11f, 3.5f),
        new Crater(1328f, -291f, 47f, 7f, 2f)
    };

    private static readonly Peak[] Peaks =
    {
        new Peak(-1725f, 835f, 455f, 235f, 188f),
        new Peak(-1048f, -748f, 268f, 112f, 142f),
        new Peak(-612f, 914f, 176f, 105f, 119f),
        new Peak(96f, -865f, 402f, 168f, 176f),
        new Peak(885f, 628f, 305f, 210f, 154f),
        new Peak(1372f, -905f, 512f, 198f, 205f),
        new Peak(1915f, 744f, 214f, 142f, 131f),
        new Peak(1668f, 668f, 126f, 77f, 98f)
    };

    [InitializeOnLoadMethod]
    private static void GenerateOnceAfterImport()
    {
        string versionKey = $"NITROZERO.AvoidMissileMoonTerrain.{Application.dataPath.GetHashCode()}";
        if (AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath) != null &&
            EditorPrefs.GetInt(versionKey, 0) >= GeneratorVersion)
            return;

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            try
            {
                Generate();
                EditorPrefs.SetInt(versionKey, GeneratorVersion);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        };
    }

    [MenuItem("NITRO ZERO/Moon Terrain/Generate avoidMissile Moon Surface")]
    public static void GenerateFromMenu()
    {
        Generate();
    }

    // Entry point for command-line verification or regeneration.
    public static void GenerateFromCommandLine()
    {
        Generate();
        EditorApplication.Exit(0);
    }

    private static void Generate()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool closeWhenFinished = !scene.isLoaded;
        if (closeWhenFinished)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        Terrain terrain = FindTerrain(scene);
        if (terrain == null)
            throw new InvalidOperationException("avoidMissile scene does not contain a Terrain.");

        TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
        if (terrainData == null)
        {
            terrainData = new TerrainData();
            AssetDatabase.CreateAsset(terrainData, TerrainDataPath);
        }

        terrainData.heightmapResolution = HeightmapResolution;
        terrainData.size = new Vector3(MapLength, TerrainHeight, MapWidth);
        terrainData.alphamapResolution = 512;
        terrainData.baseMapResolution = 1024;
        terrainData.SetHeights(0, 0, BuildHeightmap());

        TerrainLayer moonLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(TerrainLayerPath);
        if (moonLayer != null)
        {
            moonLayer.tileSize = new Vector2(28f, 28f);
            moonLayer.normalScale = 1.15f;
            moonLayer.smoothness = 0.04f;
            terrainData.terrainLayers = new[] { moonLayer };
            float[,,] splat = new float[terrainData.alphamapResolution, terrainData.alphamapResolution, 1];
            for (int z = 0; z < terrainData.alphamapResolution; z++)
                for (int x = 0; x < terrainData.alphamapResolution; x++)
                    splat[z, x, 0] = 1f;
            terrainData.SetAlphamaps(0, 0, splat);
            EditorUtility.SetDirty(moonLayer);
        }

        terrain.terrainData = terrainData;
        terrain.transform.position = new Vector3(-MapLength * 0.5f, TerrainOriginY, -MapWidth * 0.5f);
        terrain.heightmapPixelError = 4f;
        terrain.basemapDistance = 3500f;
        terrain.drawInstanced = true;
        terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
        if (collider == null)
            collider = terrain.gameObject.AddComponent<TerrainCollider>();
        collider.terrainData = terrainData;

        terrain.gameObject.name = "Moon Terrain 4000x2000";
        terrain.gameObject.isStatic = true;
        CreateRoadGuide(scene);
        ConfigureCameras(scene);

        EditorUtility.SetDirty(terrainData);
        EditorUtility.SetDirty(terrain);
        EditorUtility.SetDirty(collider);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (closeWhenFinished)
            EditorSceneManager.CloseScene(scene, true);

        Debug.Log("[NITRO ZERO] avoidMissile moon terrain generated: 4000x2000m, 100m flat road corridor, craters and mountain ridges.");
    }

    private static Terrain FindTerrain(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Terrain found = root.GetComponentInChildren<Terrain>(true);
            if (found != null)
                return found;
        }
        return null;
    }

    private static void CreateRoadGuide(Scene scene)
    {
        GameObject existing = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Road Placement Guide (100m Flat Corridor)")
            {
                existing = root;
                break;
            }
        }
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);

        GameObject guide = new GameObject("Road Placement Guide (100m Flat Corridor)");
        guide.transform.position = new Vector3(0f, RoadWorldHeight, 0f);
        guide.transform.rotation = Quaternion.identity;
        guide.transform.localScale = new Vector3(MapLength, 1f, RoadFlatHalfWidth * 2f);
        guide.isStatic = true;

        MoonRoadPlacementGuide component = guide.AddComponent<MoonRoadPlacementGuide>();
        component.mapLength = MapLength;
        component.flatWidth = RoadFlatHalfWidth * 2f;
        component.blendedWidth = RoadBlendHalfWidth * 2f;
        component.roadSurfaceY = RoadWorldHeight;

        SceneManager.MoveGameObjectToScene(guide, scene);
    }

    private static void ConfigureCameras(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                camera.farClipPlane = Mathf.Max(camera.farClipPlane, 3500f);
        }
    }

    private static float[,] BuildHeightmap()
    {
        float[,] heights = new float[HeightmapResolution, HeightmapResolution];
        float roadNormalizedHeight = (RoadWorldHeight - TerrainOriginY) / TerrainHeight;

        for (int z = 0; z < HeightmapResolution; z++)
        {
            float nz = z / (float)(HeightmapResolution - 1);
            float worldZ = nz * MapWidth - MapWidth * 0.5f;

            for (int x = 0; x < HeightmapResolution; x++)
            {
                float nx = x / (float)(HeightmapResolution - 1);
                float worldX = nx * MapLength - MapLength * 0.5f;

                Vector2 warp = DomainWarp(worldX, worldZ);
                float broad = Fbm(warp.x, warp.y, 0.00115f, 4, 2.03f, 0.51f, 17.2f) * 18f;
                float medium = (Fbm(warp.x, warp.y, 0.0045f, 3, 2.1f, 0.48f, 81.7f) - 0.5f) * 10f;
                float fine = (Fbm(worldX, worldZ, 0.015f, 2, 2f, 0.45f, 143.1f) - 0.5f) * 3.2f;
                float height = 7f + broad + medium + fine;
                height += RidgedNoise(warp.x, warp.y, 0.0018f, 3, 33.4f) * 4f;
                height += RoughHighland(worldX, worldZ);

                float edgeDistance = Mathf.Abs(worldZ) / (MapWidth * 0.5f);
                height += Mathf.Pow(Mathf.SmoothStep(0f, 1f, edgeDistance), 2.2f) * 24f;

                for (int i = 0; i < Peaks.Length; i++)
                    height += MountainHeight(worldX, worldZ, Peaks[i]);

                for (int i = 0; i < Craters.Length; i++)
                {
                    Crater crater = Craters[i];
                    if (Mathf.Abs(crater.Z) - crater.Radius * 1.35f > RoadBlendHalfWidth + 35f)
                        height += CraterHeight(worldX, worldZ, crater);
                }

                height += CraterField(worldX, worldZ, 620f, 0.34f, 0.19f, 0.090f, 11);
                height += CraterField(worldX, worldZ, 270f, 0.38f, 0.16f, 0.074f, 29);
                height += CraterField(worldX, worldZ, 115f, 0.25f, 0.12f, 0.052f, 53);
                height += CraterField(worldX, worldZ, 52f, 0.14f, 0.080f, 0.032f, 97);

                float distanceFromRoad = Mathf.Abs(worldZ);
                float transitionVariation = (Mathf.PerlinNoise(worldX * 0.0014f + 9.3f, 17.6f) - 0.5f) * 26f;
                float roadBlend = Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(RoadFlatHalfWidth, RoadBlendHalfWidth + transitionVariation, distanceFromRoad));
                float roadHeight = RoadWorldHeight - TerrainOriginY;
                height = Mathf.Lerp(roadHeight, height, roadBlend);

                // Keep the full straight corridor mathematically flat for asphalt placement.
                if (distanceFromRoad <= RoadFlatHalfWidth)
                    heights[z, x] = roadNormalizedHeight;
                else
                    heights[z, x] = Mathf.Clamp01(height / TerrainHeight);
            }
        }

        SmoothHeightmap(heights, 2, roadNormalizedHeight);
        return heights;
    }

    private static float MountainHeight(float x, float z, Peak peak)
    {
        float angle = Mathf.Repeat(Mathf.Abs(peak.Z) * 0.021f, 2f * Mathf.PI);
        float localX = x - peak.X;
        float localZ = z - peak.Z;
        float dx = (localX * Mathf.Cos(angle) - localZ * Mathf.Sin(angle)) / peak.RadiusX;
        float dz = (localX * Mathf.Sin(angle) + localZ * Mathf.Cos(angle)) / peak.RadiusZ;
        float distance = Mathf.Sqrt(dx * dx + dz * dz);
        if (distance >= 1.35f)
            return 0f;

        float body = Mathf.Pow(Mathf.Clamp01(1f - distance / 1.35f), 2.15f);
        float ridgeNoise = 0.7f + 0.3f * Fbm(x, z, 0.012f, 3, 2.2f, 0.5f, peak.X * 0.01f);
        float asymmetricSlope = Mathf.Clamp(0.78f + dx * 0.11f - dz * 0.08f, 0.62f, 0.94f);
        float shoulderX = dx - Mathf.Sin(angle * 1.7f) * 0.34f;
        float shoulderZ = dz + Mathf.Cos(angle * 1.3f) * 0.27f;
        float shoulder = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(shoulderX * shoulderX + shoulderZ * shoulderZ) / 0.82f), 2.7f);
        float gentleVariation = 0.88f + Fbm(x, z, 0.0038f, 3, 2.05f, 0.5f, peak.X * 0.019f) * 0.2f;
        return peak.Height * (body * ridgeNoise * asymmetricSlope + shoulder * 0.2f) * gentleVariation;
    }

    private static float CraterHeight(float x, float z, Crater crater)
    {
        float dx = x - crater.X;
        float dz = z - crater.Z;
        float angle = Mathf.Atan2(dz, dx);
        float distance = Mathf.Sqrt(dx * dx + dz * dz) / crater.Radius;
        if (distance >= 1.35f)
            return 0f;

        float bowl = -crater.Depth * Mathf.Pow(Mathf.Clamp01(1f - distance), 2.25f);
        bool intactRim = Mathf.Repeat(Mathf.Abs(crater.X) * 0.017f, 1f) > 0.72f;
        float gapCenter = Mathf.Repeat(Mathf.Abs(crater.Z) * 0.019f, Mathf.PI * 2f) - Mathf.PI;
        float gapDistance = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, gapCenter * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
        float breakMask = intactRim ? 1f : Mathf.Lerp(0.025f, 1f, Mathf.SmoothStep(0.24f, 0.72f, gapDistance));
        float rimRoughness = Mathf.Lerp(0.55f, 1.38f, Fbm(x, z, 0.018f, 3, 2.05f, 0.5f, crater.X * 0.013f + 401f));
        float rimWidth = Mathf.Lerp(0.09f, 0.15f, Fbm(x, z, 0.01f, 2, 2.05f, 0.5f, crater.Z * 0.017f + 433f));
        float rim = crater.RimHeight * 1.2f * breakMask * rimRoughness * Mathf.Exp(-Mathf.Pow((distance - 1f) / rimWidth, 2f));
        float outerEjecta = crater.RimHeight * 0.18f * Mathf.Exp(-Mathf.Pow((distance - 1.17f) / 0.16f, 2f));
        return bowl + rim + outerEjecta;
    }

    private static float CraterField(float x, float z, float cellSize, float probability, float depthRatio, float rimRatio, int seed)
    {
        int cellX = Mathf.FloorToInt(x / cellSize);
        int cellZ = Mathf.FloorToInt(z / cellSize);
        float result = 0f;
        for (int dz = -1; dz <= 1; dz++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int cx = cellX + dx;
                int cz = cellZ + dz;
                float spawn = Hash01(cx, cz, seed);
                if (spawn > probability) continue;

                float centerX = (cx + 0.12f + Hash01(cx, cz, seed + 7) * 0.76f) * cellSize;
                float centerZ = (cz + 0.12f + Hash01(cx, cz, seed + 19) * 0.76f) * cellSize;
                float radius = cellSize * Mathf.Lerp(0.12f, 0.38f, Hash01(cx, cz, seed + 31));
                if (Mathf.Abs(centerZ) - radius * 1.32f <= RoadBlendHalfWidth + 35f) continue;
                float angle = Hash01(cx, cz, seed + 43) * Mathf.PI * 2f;
                float localX = x - centerX;
                float localZ = z - centerZ;
                float rx = (localX * Mathf.Cos(angle) - localZ * Mathf.Sin(angle)) / radius;
                float rz = (localX * Mathf.Sin(angle) + localZ * Mathf.Cos(angle)) / radius;
                float craterAngle = Mathf.Atan2(rz, rx);
                float distance = Mathf.Sqrt(rx * rx + rz * rz);
                if (distance >= 1.32f) continue;

                float age = Hash01(cx, cz, seed + 71);
                float gapCenter = Hash01(cx, cz, seed + 83) * Mathf.PI * 2f - Mathf.PI;
                float gapDistance = Mathf.Abs(Mathf.DeltaAngle(craterAngle * Mathf.Rad2Deg, gapCenter * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                bool intactRim = Hash01(cx, cz, seed + 91) > 0.72f;
                float gapMask = intactRim ? 1f : Mathf.Lerp(0.025f, 1f, Mathf.SmoothStep(0.2f, 0.68f, gapDistance));
                float depth = radius * depthRatio * Mathf.Lerp(0.55f, 1f, age);
                float rimRoughness = Mathf.Lerp(0.58f, 1.34f, Fbm(x, z, 0.022f, 3, 2.05f, 0.5f, seed + spawn * 37f));
                float rim = radius * rimRatio * Mathf.Lerp(0.55f, 1f, age) * rimRoughness * gapMask;
                float bowl = -depth * Mathf.Pow(Mathf.Clamp01(1f - distance), Mathf.Lerp(1.7f, 2.5f, age));
                float rimWidth = Mathf.Lerp(0.09f, 0.15f, Fbm(x, z, 0.013f, 2, 2.05f, 0.5f, seed + 19f));
                float rimShape = rim * Mathf.Exp(-Mathf.Pow((distance - 1f) / rimWidth, 2f));
                result += bowl + rimShape;
            }
        }
        return result;
    }

    private static float RoughHighland(float x, float z)
    {
        float edge = Mathf.SmoothStep(0.38f, 0.92f, Mathf.Abs(z) / (MapWidth * 0.5f));
        float brokenMask = Mathf.SmoothStep(0.4f, 0.72f, Fbm(x, z, 0.00085f, 3, 2.05f, 0.5f, 205.4f));
        float broadRidge = RidgedNoise(x, z, 0.00115f, 3, 64.2f);
        return edge * brokenMask * (22f + broadRidge * 54f);
    }

    private static float Hash01(int x, int z, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + z * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h & 0x00ffffffu) / 16777215f;
        }
    }

    private static float Fbm(float x, float z, float frequency, int octaves, float lacunarity, float gain, float seed)
    {
        float sum = 0f;
        float amplitude = 0.5f;
        float normalization = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += Mathf.PerlinNoise(x * frequency + seed, z * frequency + seed * 0.71f) * amplitude;
            normalization += amplitude;
            amplitude *= gain;
            frequency *= lacunarity;
        }
        return sum / normalization;
    }

    private static Vector2 DomainWarp(float x, float z)
    {
        float wx = (Mathf.PerlinNoise(x * 0.0017f + 12.3f, z * 0.0017f + 41.8f) - 0.5f) * 170f;
        float wz = (Mathf.PerlinNoise(x * 0.0019f + 87.1f, z * 0.0019f + 9.6f) - 0.5f) * 150f;
        return new Vector2(x + wx, z + wz);
    }

    private static float RidgedNoise(float x, float z, float frequency, int octaves, float seed)
    {
        float sum = 0f;
        float amplitude = 0.5f;
        float normalization = 0f;
        for (int i = 0; i < octaves; i++)
        {
            float noise = Mathf.PerlinNoise(x * frequency + seed, z * frequency + seed * 0.61f);
            float ridge = 1f - Mathf.Abs(noise * 2f - 1f);
            sum += ridge * ridge * amplitude;
            normalization += amplitude;
            amplitude *= 0.48f;
            frequency *= 2.13f;
        }
        return sum / normalization;
    }

    private static void SmoothHeightmap(float[,] heights, int passes, float roadHeight)
    {
        int size = heights.GetLength(0);
        float[,] buffer = new float[size, size];
        for (int pass = 0; pass < passes; pass++)
        {
            for (int z = 0; z < size; z++)
            {
                float worldZ = z / (float)(size - 1) * MapWidth - MapWidth * 0.5f;
                for (int x = 0; x < size; x++)
                {
                    if (Mathf.Abs(worldZ) <= RoadFlatHalfWidth)
                    {
                        buffer[z, x] = roadHeight;
                        continue;
                    }

                    float sum = heights[z, x] * 4f;
                    float weight = 4f;
                    for (int oz = -1; oz <= 1; oz++)
                    {
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            if (ox == 0 && oz == 0) continue;
                            int sampleX = Mathf.Clamp(x + ox, 0, size - 1);
                            int sampleZ = Mathf.Clamp(z + oz, 0, size - 1);
                            float sampleWeight = ox == 0 || oz == 0 ? 2f : 1f;
                            sum += heights[sampleZ, sampleX] * sampleWeight;
                            weight += sampleWeight;
                        }
                    }
                    buffer[z, x] = sum / weight;
                }
            }

            float[,] swap = heights;
            heights = buffer;
            buffer = swap;
        }
    }
}
