using UnityEditor;
using UnityEngine;

public static class FinalizeDomeLunarSurface
{
    private const string DataPath = "Assets/Terrain/DomeInTheMoon_Basin.asset";
    private const float OriginY = -100f;
    private static readonly Vector2 DomeCenter = new Vector2(-722.1f, 28f);

    [MenuItem("NITRO ZERO/Moon Terrain/Finalize Matte Rough Dome Surface")]
    public static void Apply()
    {
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(DataPath);
        if (data == null) throw new MissingReferenceException(DataPath);
        ConfigureLayers(data);
        AddRoundedRoughness(data);
        RepaintWithoutGrid(data);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log("[NITRO ZERO] Dome surface finalized: matte, neutral, grid-free blend and rounded roughness.");
    }

    private static void ConfigureLayers(TerrainData data)
    {
        TerrainLayer[] layers = data.terrainLayers;
        float[] normal = { .24f, .18f, .28f, .26f, .16f };
        Vector2[] size = { new Vector2(67f, 83f), new Vector2(109f, 97f), new Vector2(47f, 59f), new Vector2(71f, 53f), new Vector2(149f, 127f) };
        Color[] tint = { Gray(.72f), Gray(.70f), Gray(.56f), Gray(.54f), Gray(.50f) };
        for (int i = 0; i < layers.Length; i++)
        {
            TerrainLayer layer = layers[i];
            if (layer == null) continue;
            int slot = i % normal.Length;
            layer.metallic = 0f;
            layer.smoothness = 0f;
            layer.specular = Color.clear;
            layer.normalScale = normal[slot];
            layer.tileSize = size[slot];
            layer.tileOffset = new Vector2(13.7f + i * 31.3f, 27.1f + i * 19.7f);
            layer.diffuseRemapMax = tint[slot];
            EditorUtility.SetDirty(layer);
        }
    }

    private static void AddRoundedRoughness(TerrainData data)
    {
        int n = data.heightmapResolution;
        float[,] heights = data.GetHeights(0, 0, n, n);
        for (int z = 0; z < n; z++)
        {
            float wz = (z / (float)(n - 1) - .5f) * data.size.z;
            for (int x = 0; x < n; x++)
            {
                float wx = (x / (float)(n - 1) - .5f) * data.size.x;
                Vector2 p = new Vector2(wx, wz);
                float domeDistance = Vector2.Distance(p, DomeCenter);
                float roadDistance = wx >= -925f ? Mathf.Abs(wz - 28f) : 9999f;
                float mask = Smooth(240f, 430f, domeDistance) * Smooth(19f, 85f, roadDistance);
                mask *= 1f - Smooth(2920f, 3230f, domeDistance);
                if (mask <= 0f) continue;

                Vector2 warped = Warp(wx, wz);
                float rounded = (Fbm(warped.x, warped.y, .0037f, 4, 401f) - .5f) * 9f;
                float medium = (Fbm(warped.x + 83f, warped.y - 47f, .0125f, 3, 463f) - .5f) * 3.2f;
                heights[z, x] = Mathf.Clamp01(heights[z, x] + (rounded + medium) * mask / data.size.y);
            }
        }
        data.SetHeights(0, 0, heights);
    }

    private static void RepaintWithoutGrid(TerrainData data)
    {
        int n = data.alphamapResolution, count = data.alphamapLayers;
        if (count < 1) return;
        float[,,] alpha = new float[n, n, count];
        for (int z = 0; z < n; z++)
        {
            float nz = z / (float)(n - 1);
            float wz = (nz - .5f) * data.size.z;
            for (int x = 0; x < n; x++)
            {
                float nx = x / (float)(n - 1);
                float wx = (nx - .5f) * data.size.x;
                Vector2 q = Warp(wx, wz);
                float broad = Fbm(q.x, q.y, .0017f, 4, 557f);
                float mid = Fbm(q.x - q.y * .31f, q.y + q.x * .23f, .0053f, 3, 613f);
                float fine = Fbm(q.x + 137f, q.y - 91f, .014f, 2, 677f);
                float slope = data.GetSteepness(nx, nz);

                float[] w = new float[count];
                w[0] = .42f + broad * .22f;
                if (count > 1) w[1] = .16f + (1f - mid) * .30f;
                if (count > 2) w[2] = .03f + Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(9f, 31f, slope)) * .62f;
                if (count > 3) w[3] = .06f + fine * mid * .25f;
                if (count > 4) w[4] = .05f + (1f - broad) * fine * .22f;
                for (int i = 5; i < count; i++) w[i] = .01f;
                float sum = 0f;
                for (int i = 0; i < count; i++) sum += w[i];
                for (int i = 0; i < count; i++) alpha[z, x, i] = w[i] / sum;
            }
        }
        data.SetAlphamaps(0, 0, alpha);
    }

    private static Vector2 Warp(float x, float z)
    {
        float wx = (Fbm(x, z, .0008f, 3, 719f) - .5f) * 310f;
        float wz = (Fbm(x, z, .0007f, 3, 773f) - .5f) * 270f;
        float u = (x + wx) * .83867f - (z + wz) * .54464f;
        float v = (x + wx) * .54464f + (z + wz) * .83867f;
        return new Vector2(u, v);
    }

    private static float Fbm(float x, float z, float scale, int octaves, float seed)
    {
        float value = 0f, amplitude = 1f, total = 0f;
        for (int i = 0; i < octaves; i++)
        {
            value += Mathf.PerlinNoise(x * scale + seed + i * 11.7f, z * scale + seed * .37f + i * 7.3f) * amplitude;
            total += amplitude;
            amplitude *= .5f;
            scale *= 2.07f;
        }
        return value / total;
    }

    private static float Smooth(float a, float b, float v) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, v));
    private static Color Gray(float v) => new Color(v, v, v, 1f);
}
