using UnityEditor;
using UnityEngine;

public static class CurrentMoonTerrainRoughnessTuner
{
    private const string BoostPath = "Assets/Terrain/BoostOn_MoonOpenPlain_v5.asset";
    private const string DomePath = "Assets/Terrain/DomeInTheMoon_Basin.asset";

    [MenuItem("NITRO ZERO/Moon Terrain/Tune Current boostOn and dome Roughness")]
    public static void Apply()
    {
        TuneBoost();
        ReduceDome();
        AssetDatabase.SaveAssets();
        Debug.Log("[NITRO ZERO] Current boostOn roughness added and dome roughness reduced.");
    }

    private static void TuneBoost()
    {
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(BoostPath);
        if (data == null) return;
        int resolution = data.heightmapResolution;
        float[,] heights = data.GetHeights(0, 0, resolution, resolution);
        for (int z = 0; z < resolution; z++)
        {
            float worldZ = z / (float)(resolution - 1) * 2500f - 1250f;
            float roadMask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(70f, 260f, Mathf.Abs(worldZ)));
            for (int x = 0; x < resolution; x++)
            {
                float worldX = x / (float)(resolution - 1) * 6000f - 3000f;
                float rough = (Fbm(worldX, worldZ, 0.0072f, 3, 1103f) - 0.5f) * 11f;
                rough += (Fbm(worldX, worldZ, 0.026f, 2, 1151f) - 0.5f) * 4.5f;
                rough += (Fbm(worldX, worldZ, 0.070f, 2, 1193f) - 0.5f) * 1.8f;
                heights[z, x] = Mathf.Clamp01(heights[z, x] + rough * roadMask / data.size.y);
            }
        }
        data.SetHeights(0, 0, heights);
        EditorUtility.SetDirty(data);
    }

    private static void ReduceDome()
    {
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(DomePath);
        if (data == null) return;
        int resolution = data.heightmapResolution;
        float[,] heights = data.GetHeights(0, 0, resolution, resolution);
        for (int z = 0; z < resolution; z++)
        {
            float worldZ = z / (float)(resolution - 1) * 5000f - 2500f;
            for (int x = 0; x < resolution; x++)
            {
                float worldX = x / (float)(resolution - 1) * 5000f - 2500f;
                float roadMask = worldX >= -430f
                    ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(75f, 285f, Mathf.Abs(worldZ))) : 1f;
                float domeDistance = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(-1000f, 0f));
                float domeMask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(590f, 890f, domeDistance));
                float mask = roadMask * domeMask;
                float excess = (Fbm(worldX, worldZ, 0.0075f, 3, 907f) - 0.5f) * 7f;
                excess += (Fbm(worldX, worldZ, 0.027f, 2, 953f) - 0.5f) * 3f;
                excess += (Fbm(worldX, worldZ, 0.072f, 2, 991f) - 0.5f) * 1.2f;
                heights[z, x] = Mathf.Clamp01(heights[z, x] - excess * mask / data.size.y);
            }
        }
        data.SetHeights(0, 0, heights);
        EditorUtility.SetDirty(data);
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
}
