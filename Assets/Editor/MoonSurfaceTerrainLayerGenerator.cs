using UnityEditor;
using UnityEngine;

public static class MoonSurfaceTerrainLayerGenerator
{
    private const string OutputFolder = "Assets/Terrain/MoonSurfaceLayers";
    private const string BaseColor = "Assets/02YUJEONG/Moon/moon_04_2k.blend/textures/moon_04_diff_2k.jpg";
    private const string BaseNormal = "Assets/02YUJEONG/Moon/moon_04_2k.blend/textures/moon_04_nor_gl_2k.exr";
    private const string DustColor = "Assets/02YUJEONG/Moon/moon_dusted_01_2k.blend/textures/moon_dusted_01_diff_2k.jpg";
    private const string DustNormal = "Assets/02YUJEONG/Moon/moon_dusted_01_2k.blend/textures/moon_dusted_01_nor_gl_2k.exr";
    private const string RockColor = "Assets/01RAEHYEON/Material/Rock030_4K-JPG/Rock030_4K-JPG_Color.jpg";
    private const string RockNormal = "Assets/01RAEHYEON/Material/Rock030_4K-JPG/Rock030_4K-JPG_NormalGL.jpg";

    [MenuItem("NITRO ZERO/Moon Terrain/Apply Three-Layer Moon Surface")]
    public static void ApplyAll()
    {
        EnsureFolder();
        ConfigureTexture(BaseColor, false, 2048);
        ConfigureTexture(BaseNormal, true, 2048);
        ConfigureTexture(DustColor, false, 2048);
        ConfigureTexture(DustNormal, true, 2048);
        ConfigureTexture(RockColor, false, 4096);
        ConfigureTexture(RockNormal, true, 4096);

        TerrainLayer baseLayer = CreateLayer("HighQualityMoon_Base", BaseColor, BaseNormal, 57f, 0.82f, 0.008f,
            new Color(0.88f, 0.89f, 0.91f, 1f));
        TerrainLayer dustLayer = CreateLayer("HighQualityMoon_Dust", DustColor, DustNormal, 93f, 0.52f, 0.004f,
            new Color(0.82f, 0.84f, 0.88f, 1f));
        TerrainLayer rockLayer = CreateLayer("HighQualityMoon_Rock", RockColor, RockNormal, 37f, 1.05f, 0.012f,
            new Color(0.58f, 0.61f, 0.66f, 1f));
        TerrainLayer[] layers = { baseLayer, dustLayer, rockLayer };

        ApplyToTerrain("Assets/Terrain/AvoidMissile_MoonTerrain.asset", layers, 17f, SceneKind.AvoidMissile);
        ApplyToTerrain("Assets/Terrain/BoostOn_MoonOpenPlain_v5.asset", layers, 43f, SceneKind.BoostOn);
        ApplyToTerrain("Assets/Terrain/DomeInTheMoon_Basin.asset", layers, 79f, SceneKind.Dome);
        ApplyToTerrain("Assets/Terrain/Racing_JCurveMoonTerrain.asset", layers, 113f, SceneKind.Racing);
        AssetDatabase.SaveAssets();
        Debug.Log("[NITRO ZERO] Three-layer moon surface applied without changing terrain heights.");
    }

    private enum SceneKind { AvoidMissile, BoostOn, Dome, Racing }

    private static void ApplyToTerrain(string path, TerrainLayer[] layers, float seed, SceneKind kind)
    {
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
        if (data == null) throw new MissingReferenceException("TerrainData not found: " + path);
        data.terrainLayers = layers;
        int resolution = data.alphamapResolution;
        float[,,] alpha = new float[resolution, resolution, 3];
        for (int z = 0; z < resolution; z++)
        {
            float nz = z / (float)(resolution - 1);
            float worldZ = nz * data.size.z - data.size.z * 0.5f;
            for (int x = 0; x < resolution; x++)
            {
                float nx = x / (float)(resolution - 1);
                float worldX = nx * data.size.x - data.size.x * 0.5f;
                float slope = data.GetSteepness(nx, nz);
                float normalizedHeight = data.GetInterpolatedHeight(nx, nz) / data.size.y;

                float broad = Fbm(worldX, worldZ, 0.0027f, seed);
                float regional = Fbm(worldX, worldZ, 0.00083f, seed + 31f);
                float crossNoise = Fbm(worldX + worldZ * 0.37f, worldZ - worldX * 0.29f, 0.0051f, seed + 53f);
                float warped = Fbm(worldX + (broad - 0.5f) * 260f, worldZ + (regional - 0.5f) * 230f, 0.0044f, seed + 67f);

                float rockBySlope = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(11f, 36f, slope));
                float rockByHeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.22f, 0.55f, normalizedHeight));
                float rock = Mathf.Clamp01(rockBySlope * Mathf.Lerp(0.62f, 1.08f, broad)
                    + rockByHeight * Mathf.Lerp(0.08f, 0.34f, regional));
                rock *= Mathf.Lerp(0.58f, 1.08f, warped * 0.65f + crossNoise * 0.35f);

                float flatness = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5f, 24f, slope));
                float dustPattern = Mathf.Clamp01(regional * 0.58f + crossNoise * 0.42f);
                float dust = flatness * Mathf.Lerp(0.16f, 0.62f, dustPattern) * (1f - rock * 0.72f);
                if (kind == SceneKind.Dome)
                {
                    float domeDistance = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(-1500f, 0f));
                    dust += (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(430f, 900f, domeDistance))) * 0.24f;
                }
                if (IsRoad(kind, worldX, worldZ)) dust = Mathf.Max(dust, 0.62f);

                dust = Mathf.Clamp01(dust);
                float baseWeight = Mathf.Max(0.12f, 1f - rock - dust);
                float sum = baseWeight + dust + rock;
                alpha[z, x, 0] = baseWeight / sum;
                alpha[z, x, 1] = dust / sum;
                alpha[z, x, 2] = rock / sum;
            }
        }
        data.SetAlphamaps(0, 0, alpha);
        EditorUtility.SetDirty(data);
    }

    private static bool IsRoad(SceneKind kind, float x, float z)
    {
        if (kind == SceneKind.AvoidMissile) return Mathf.Abs(z) <= 62f;
        if (kind == SceneKind.BoostOn) return Mathf.Abs(z) <= 82f;
        if (kind == SceneKind.Dome) return x >= -950f && Mathf.Abs(z - 32f) <= 88f;
        return DistanceToRacingRoad(x, z) <= 90f;
    }

    private static float DistanceToRacingRoad(float x, float z)
    {
        Vector2 p = new Vector2(x, z);
        float straight = DistanceToSegment(p, new Vector2(-2050f, 600f), new Vector2(300f, 600f));
        Vector2 center = new Vector2(300f, 0f), local = p - center;
        float angle = Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
        float endAngle = -20f * Mathf.Deg2Rad;
        Vector2 arcStart = new Vector2(300f, 600f);
        Vector2 arcEnd = center + new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * 600f;
        float arc = angle >= -20f && angle <= 90f ? Mathf.Abs(local.magnitude - 600f)
            : Mathf.Min(Vector2.Distance(p, arcStart), Vector2.Distance(p, arcEnd));
        Vector2 exitDirection = new Vector2(Mathf.Sin(endAngle), -Mathf.Cos(endAngle));
        return Mathf.Min(straight, Mathf.Min(arc, DistanceToSegment(p, arcEnd, arcEnd + exitDirection * 1450f)));
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    private static float Fbm(float x, float z, float scale, float seed)
    {
        float sum = 0f, amplitude = 1f, total = 0f;
        for (int i = 0; i < 4; i++)
        {
            sum += Mathf.PerlinNoise(x * scale + seed, z * scale + seed * 0.37f) * amplitude;
            total += amplitude;
            amplitude *= 0.5f;
            scale *= 2.07f;
        }
        return sum / total;
    }

    private static TerrainLayer CreateLayer(string name, string colorPath, string normalPath,
        float tileSize, float normalScale, float smoothness, Color tint)
    {
        string path = OutputFolder + "/" + name + ".terrainlayer";
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if (layer == null)
        {
            layer = new TerrainLayer { name = name };
            AssetDatabase.CreateAsset(layer, path);
        }
        layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath);
        layer.normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        layer.tileSize = new Vector2(tileSize, tileSize);
        layer.tileOffset = name.Contains("Dust") ? new Vector2(17.3f, 41.7f)
            : name.Contains("Rock") ? new Vector2(63.1f, 11.9f) : new Vector2(0f, 0f);
        layer.normalScale = normalScale;
        layer.metallic = 0f;
        layer.smoothness = smoothness;
        layer.specular = new Color(0.015f, 0.015f, 0.015f, 1f);
        layer.diffuseRemapMin = Color.black;
        layer.diffuseRemapMax = tint;
        EditorUtility.SetDirty(layer);
        return layer;
    }

    private static void ConfigureTexture(string path, bool normalMap, int maxSize)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normalMap;
        importer.maxTextureSize = maxSize;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 16;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder)) AssetDatabase.CreateFolder("Assets/Terrain", "MoonSurfaceLayers");
    }
}
