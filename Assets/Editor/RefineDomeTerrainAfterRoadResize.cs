using UnityEditor;
using UnityEngine;

public static class RefineDomeTerrainAfterRoadResize
{
    private const string TerrainPath = "Assets/Terrain/DomeInTheMoon_Basin.asset";
    private const float OriginY = -100f;
    private const float RoadZ = 28f;
    private const float RoadStartX = -925f;
    private const float RoadGroundY = -20.45f;
    private static readonly Vector2 DomeCenter = new Vector2(-1114.9f, 28f);

    [MenuItem("NITRO ZERO/Moon Terrain/Refine Dome Road And Ridges")]
    public static void Apply()
    {
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
        if (data == null) throw new MissingReferenceException(TerrainPath);
        int n = data.heightmapResolution;
        float[,] source = data.GetHeights(0, 0, n, n);
        float[,] broad = Blur(Blur(Blur(source, 32), 32), 32);
        float[,] result = (float[,])source.Clone();
        float dx = data.size.x / (n - 1f);
        float dz = data.size.z / (n - 1f);

        for (int z = 0; z < n; z++)
        {
            float wz = -data.size.z * 0.5f + z * dz;
            for (int x = 0; x < n; x++)
            {
                float wx = -data.size.x * 0.5f + x * dx;
                float dome = Vector2.Distance(new Vector2(wx, wz), DomeCenter);
                float road = Mathf.Abs(wz - RoadZ);
                float roadAlong = Smooth( RoadStartX - 170f, RoadStartX + 110f, wx);
                float nearRoad = roadAlong * (1f - Smooth(230f, 520f, road));
                float nearDome = 1f - Smooth(900f, 1650f, dome);
                float smoothing = Mathf.Max(0.24f, Mathf.Max(nearRoad * 0.86f, nearDome * 0.9f));
                float h = Mathf.Lerp(source[z, x], broad[z, x], smoothing) * data.size.y + OriginY;

                // Lower the basin's high skyline without flattening its broad lunar shapes.
                float aboveRoad = Mathf.Max(0f, h - RoadGroundY);
                h -= aboveRoad * 0.42f * Smooth(55f, 115f, aboveRoad);

                // The small dome needs a wide continuous apron, not the former 650m terrace.
                float domePad = 1f - Smooth(195f, 1010f, dome);
                h = Mathf.Lerp(h, RoadGroundY, domePad);

                // The 20m asphalt is at y=-19.65. Keep the whole carriageway visible.
                if (wx >= RoadStartX - 180f && road < 145f)
                {
                    float along = Smooth(RoadStartX - 180f, RoadStartX + 110f, wx);
                    float cross = 1f - Smooth(10.5f, 145f, road);
                    h = Mathf.Lerp(h, RoadGroundY, along * cross);
                    if (wx >= RoadStartX && road <= 10.5f) h = RoadGroundY;
                }

                result[z, x] = Mathf.Clamp01((h - OriginY) / data.size.y);
            }
        }

        data.SetHeights(0, 0, result);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log("[NITRO ZERO] Dome road clearance, terrace transitions and high ridges refined.");
    }

    private static float Smooth(float from, float to, float value)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, value));
    }

    private static float[,] Blur(float[,] source, int radius)
    {
        int n = source.GetLength(0);
        float[,] horizontal = new float[n, n];
        float[,] result = new float[n, n];
        int count = radius * 2 + 1;
        for (int z = 0; z < n; z++)
        {
            float sum = 0f;
            for (int k = -radius; k <= radius; k++) sum += source[z, Mathf.Clamp(k, 0, n - 1)];
            for (int x = 0; x < n; x++)
            {
                horizontal[z, x] = sum / count;
                sum += source[z, Mathf.Min(x + radius + 1, n - 1)] - source[z, Mathf.Max(x - radius, 0)];
            }
        }
        for (int x = 0; x < n; x++)
        {
            float sum = 0f;
            for (int k = -radius; k <= radius; k++) sum += horizontal[Mathf.Clamp(k, 0, n - 1), x];
            for (int z = 0; z < n; z++)
            {
                result[z, x] = sum / count;
                sum += horizontal[Mathf.Min(z + radius + 1, n - 1), x] - horizontal[Mathf.Max(z - radius, 0), x];
            }
        }
        return result;
    }
}
