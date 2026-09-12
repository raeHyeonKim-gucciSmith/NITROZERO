using UnityEditor;
using UnityEngine;

public static class MoonRoadShoulderRoughnessGenerator
{
    [MenuItem("NITRO ZERO/Moon Terrain/Roughen Flattened Areas Outside Roads")]
    public static void ApplyAll()
    {
        ApplyStraight("Assets/Terrain/AvoidMissile_MoonTerrain.asset", 50f, 150f, 17.3f);
        ApplyStraight("Assets/Terrain/BoostOn_MoonOpenPlain_v5.asset", 70f, 205f, 43.7f);
        ApplyDome("Assets/Terrain/DomeInTheMoon_Basin.asset", 75f, 850f, 79.1f);
        ApplyRacing("Assets/Terrain/Racing_JCurveMoonTerrain.asset", 75f, 300f, 113.9f);
        AssetDatabase.SaveAssets();
        Debug.Log("[NITRO ZERO] Lunar roughness added outside all asphalt roads without adding craters.");
    }

    public static void ApplyRacingOnly()
    {
        ApplyRacing("Assets/Terrain/Racing_JCurveMoonTerrain.asset", 75f, 300f, 113.9f);
        AssetDatabase.SaveAssets();
    }

    public static void ApplyDomeOnly()
    {
        ApplyDome("Assets/Terrain/DomeInTheMoon_Basin.asset", 75f, 1550f, 79.1f);
        AssetDatabase.SaveAssets();
    }

    private static void ApplyStraight(string path, float roadHalfWidth, float affectedWidth, float seed)
    {
        Apply(path, roadHalfWidth, affectedWidth, seed, (x, z) => Mathf.Abs(z));
    }

    private static void ApplyDome(string path, float roadHalfWidth, float affectedWidth, float seed)
    {
        Apply(path, roadHalfWidth, affectedWidth, seed, (x, z) => x >= -950f ? Mathf.Abs(z - 32f) : 99999f);
    }

    private static void ApplyRacing(string path, float roadHalfWidth, float affectedWidth, float seed)
    {
        Apply(path, roadHalfWidth, affectedWidth, seed, DistanceToRacingRoad);
    }

    private static void Apply(string path, float roadHalfWidth, float affectedWidth, float seed,
        System.Func<float, float, float> roadDistance)
    {
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
        if (data == null) throw new MissingReferenceException("TerrainData not found: " + path);
        int resolution = data.heightmapResolution;
        float[,] heights = data.GetHeights(0, 0, resolution, resolution);
        for (int z = 0; z < resolution; z++)
        {
            float worldZ = z / (float)(resolution - 1) * data.size.z - data.size.z * 0.5f;
            for (int x = 0; x < resolution; x++)
            {
                float worldX = x / (float)(resolution - 1) * data.size.x - data.size.x * 0.5f;
                float distance = roadDistance(worldX, worldZ);
                if (distance <= roadHalfWidth + 2f || distance >= affectedWidth) continue;
                float enter = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(roadHalfWidth + 2f, roadHalfWidth + 28f, distance));
                float exit = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(affectedWidth * 0.72f, affectedWidth, distance));
                float mask = enter * exit;
                float warpedX = worldX + (Noise(worldX, worldZ, 0.0031f, seed + 9f) - 0.5f) * 95f;
                float warpedZ = worldZ + (Noise(worldX, worldZ, 0.0027f, seed + 21f) - 0.5f) * 95f;
                float broad = (Fbm(warpedX, warpedZ, 0.0062f, seed) - 0.5f) * 5.2f;
                float medium = (Fbm(warpedX, warpedZ, 0.017f, seed + 37f) - 0.5f) * 2.1f;
                float fine = (Noise(warpedX, warpedZ, 0.043f, seed + 71f) - 0.5f) * 0.7f;
                heights[z, x] = Mathf.Clamp01(heights[z, x] + (broad + medium + fine) * mask / data.size.y);
            }
        }
        data.SetHeights(0, 0, heights);
        EditorUtility.SetDirty(data);
    }

    private static float DistanceToRacingRoad(float x, float z)
    {
        Vector2 p = new Vector2(x, z);
        float straight = SegmentDistance(p, new Vector2(-2050f, 600f), new Vector2(-200f, 600f));
        Vector2 center = new Vector2(-200f, 0f), local = p - center;
        float angle = Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
        float endAngle = -20f * Mathf.Deg2Rad;
        Vector2 arcStart = new Vector2(-200f, 600f);
        Vector2 arcEnd = center + new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * 600f;
        float arc = angle >= -20f && angle <= 90f ? Mathf.Abs(local.magnitude - 600f)
            : Mathf.Min(Vector2.Distance(p, arcStart), Vector2.Distance(p, arcEnd));
        Vector2 exitDirection = new Vector2(Mathf.Sin(endAngle), -Mathf.Cos(endAngle));
        return Mathf.Min(straight, Mathf.Min(arc, SegmentDistance(p, arcEnd, arcEnd + exitDirection * 1450f)));
    }

    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    private static float Noise(float x, float z, float scale, float seed)
    {
        return Mathf.PerlinNoise(x * scale + seed, z * scale + seed * 0.371f);
    }

    private static float Fbm(float x, float z, float scale, float seed)
    {
        float sum = 0f, amplitude = 1f, total = 0f;
        for (int i = 0; i < 4; i++)
        {
            sum += Noise(x, z, scale, seed + i * 17.3f) * amplitude;
            total += amplitude;
            amplitude *= 0.5f;
            scale *= 2.07f;
        }
        return sum / total;
    }
}
