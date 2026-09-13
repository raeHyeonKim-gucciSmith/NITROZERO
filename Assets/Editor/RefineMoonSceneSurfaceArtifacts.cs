using UnityEditor;
using UnityEngine;

public static class RefineMoonSceneSurfaceArtifacts
{
    private const string BoostPath = "Assets/Terrain/BoostOn_MoonOpenPlain_v5.asset";
    private const string DomePath = "Assets/Terrain/DomeInTheMoon_Basin.asset";
    private const string DomeLayerFolder = "Assets/Terrain/MoonSurfaceLayers";

    [MenuItem("NITRO ZERO/Moon Terrain/Refine Boost Craters And Dome Surface")]
    public static void Apply()
    {
        ReduceBoostCratersAndBlendColor();
        RemoveDomeGridPattern();
        AssetDatabase.SaveAssets();
        Debug.Log("[NITRO ZERO] Boost craters, road-side tint and dome checker pattern refined.");
    }

    private static void ReduceBoostCratersAndBlendColor()
    {
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(BoostPath);
        if (data == null) throw new MissingReferenceException(BoostPath);
        int n = data.heightmapResolution;
        float[,] heights = data.GetHeights(0, 0, n, n);
        for (int z = 0; z < n; z++)
        {
            float worldZ = (z / (float)(n - 1) - 0.5f) * data.size.z;
            for (int x = 0; x < n; x++)
            {
                float worldX = (x / (float)(n - 1) - 0.5f) * data.size.x;
                float removed = BoostOnMoonTerrainGenerator.RemovedScatteredCraterHeight(worldX, worldZ);
                if (Mathf.Abs(removed) > 0.0001f)
                    heights[z, x] = Mathf.Clamp01(heights[z, x] - removed / data.size.y);
            }
        }
        data.SetHeights(0, 0, heights);

        Debug.Log("[NITRO ZERO] Boost terrain layer count: " + data.terrainLayers.Length);
        if (data.terrainLayers.Length > 1)
        {
            int a = data.alphamapResolution;
            float[,,] alpha = data.GetAlphamaps(0, 0, a, a);
            float[,,] result = (float[,,])alpha.Clone();
            int layers = alpha.GetLength(2);
            for (int z = 0; z < a; z++)
            {
                float worldZ = (z / (float)(a - 1) - 0.5f) * data.size.z;
                float distance = Mathf.Abs(worldZ);
                if (distance >= 225f) continue;
                float weight = 1f - Smooth(18f, 225f, distance);
                int referenceZ = Mathf.Clamp(Mathf.RoundToInt((Mathf.Sign(worldZ) * 235f / data.size.z + 0.5f) * (a - 1)), 0, a - 1);
                for (int x = 0; x < a; x++)
                    for (int layer = 0; layer < layers; layer++)
                        result[z, x, layer] = Mathf.Lerp(alpha[z, x, layer], alpha[referenceZ, x, layer], weight);
            }
            data.SetAlphamaps(0, 0, result);
        }
        EditorUtility.SetDirty(data);
    }

    private static void RemoveDomeGridPattern()
    {
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(DomePath);
        if (data == null) throw new MissingReferenceException(DomePath);
        int n = data.heightmapResolution;
        float[,] heights = data.GetHeights(0, 0, n, n);
        Vector2 dome = new Vector2(-722.1f, 28f);
        for (int z = 0; z < n; z++)
        {
            float worldZ = (z / (float)(n - 1) - 0.5f) * data.size.z;
            for (int x = 0; x < n; x++)
            {
                float worldX = (x / (float)(n - 1) - 0.5f) * data.size.x;
                Vector2 point = new Vector2(worldX, worldZ);
                float fromDome = Vector2.Distance(point, dome);
                if (fromDome <= 190f || fromDome >= 1740f) continue;
                float road = Vector2.Distance(point, new Vector2(Mathf.Clamp(worldX, -925f, 3000f), 28f));
                float mask = Smooth(190f, 390f, fromDome) * (1f - Smooth(1380f, 1740f, fromDome)) *
                    Smooth(15f, 105f, road);
                if (mask <= 0f) continue;

                // Remove the prior axis-aligned Perlin stamp before replacing it.
                float oldLarge = Fbm(worldX, worldZ, 0.0075f, 250f) - 0.5f;
                float oldSmall = Fbm(worldX, worldZ, 0.028f, 330f) - 0.5f;
                float warpX = (Fbm(worldX, worldZ, 0.0024f, 601f) - 0.5f) * 105f;
                float warpZ = (Fbm(worldX, worldZ, 0.0021f, 647f) - 0.5f) * 93f;
                float u = (worldX + warpX) * 0.819f - (worldZ + warpZ) * 0.574f;
                float v = (worldX + warpX) * 0.574f + (worldZ + warpZ) * 0.819f;
                float newLarge = Fbm(u, v, 0.0068f, 809f) - 0.5f;
                float newSmall = Fbm(u + 41f, v - 29f, 0.018f, 853f) - 0.5f;
                float delta = (newLarge * 6.5f + newSmall * 2f - oldLarge * 7f - oldSmall * 2.5f) * mask;
                heights[z, x] = Mathf.Clamp01(heights[z, x] + delta / data.size.y);
            }
        }
        data.SetHeights(0, 0, heights);

        TerrainLayer[] sourceLayers = data.terrainLayers;
        if (sourceLayers.Length > 0)
        {
            int alphaResolution = data.alphamapResolution;
            float[,,] preservedAlpha = data.GetAlphamaps(0, 0, alphaResolution, alphaResolution);
            TerrainLayer[] domeLayers = new TerrainLayer[sourceLayers.Length];
            int[] tileX = { 61, 101, 43, 53, 139 };
            int[] tileZ = { 73, 89, 51, 47, 121 };
            for (int i = 0; i < sourceLayers.Length; i++)
            {
                string path = DomeLayerFolder + "/DomeIrregular_" + i + ".terrainlayer";
                TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                if (layer == null)
                {
                    layer = Object.Instantiate(sourceLayers[i]);
                    layer.name = "DomeIrregular_" + i;
                    AssetDatabase.CreateAsset(layer, path);
                }
                int slot = i % tileX.Length;
                layer.tileSize = new Vector2(tileX[slot], tileZ[slot]);
                layer.tileOffset = new Vector2(11f + i * 23.7f, 29f + i * 17.9f);
                domeLayers[i] = layer;
                EditorUtility.SetDirty(layer);
            }
            data.terrainLayers = domeLayers;
            data.SetAlphamaps(0, 0, preservedAlpha);
        }
        EditorUtility.SetDirty(data);
    }

    private static float Smooth(float from, float to, float value)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, value));
    }

    private static float Fbm(float x, float z, float scale, float seed)
    {
        float total = 0f, strength = 1f, weight = 0f;
        for (int i = 0; i < 3; i++)
        {
            total += Mathf.PerlinNoise(x * scale + seed, z * scale + seed * 0.37f) * strength;
            weight += strength;
            strength *= 0.5f;
            scale *= 2f;
        }
        return total / weight;
    }
}
