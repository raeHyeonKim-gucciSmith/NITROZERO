using UnityEditor;
using UnityEngine;

public static class FinalizeRacingTerrainHeight
{
    private const string TerrainPath = "Assets/Terrain/Racing_JCurveMoonTerrain.asset";
    private const float OriginY = -80f;
    private const float RoadY = -20.5f;

    [MenuItem("NITRO ZERO/Moon Terrain/Finalize Racing Lower Smooth Terrain")]
    public static void Apply()
    {
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
        if (data == null) throw new MissingReferenceException(TerrainPath);
        int n = data.heightmapResolution;
        float[,] original = data.GetHeights(0, 0, n, n);
        float[,] result = (float[,])original.Clone();
        float stepX = data.size.x / (n - 1f), stepZ = data.size.z / (n - 1f);

        for (int z = 0; z < n; z++)
        {
            float wz = -data.size.z * .5f + z * stepZ;
            for (int x = 0; x < n; x++)
            {
                float wx = -data.size.x * .5f + x * stepX;
                float current = OriginY + original[z, x] * data.size.y;
                float distance = RoadDistance(new Vector2(wx, wz));
                float edgeDistance = Mathf.Min(data.size.x * .5f - Mathf.Abs(wx), data.size.z * .5f - Mathf.Abs(wz));

                // Lower positive relief while retaining enough outer silhouette to hide the terrain boundary.
                float edgeKeep = 1f - Smooth(170f, 620f, edgeDistance);
                float scale = Mathf.Lerp(.72f, .88f, edgeKeep);
                float nearRoad = 1f - Smooth(260f, 850f, distance);
                scale = Mathf.Lerp(scale, .60f, nearRoad);
                float target = current > RoadY ? RoadY + (current - RoadY) * scale : current;

                // Prevent the inner ridge from towering over the now 30 m road.
                if (distance < 720f)
                {
                    float cap = RoadY + Mathf.Lerp(20f, 76f, Smooth(18f, 720f, distance));
                    target = Mathf.Min(target, cap);
                }
                if (distance <= 16f) target = RoadY;
                result[z, x] = Mathf.Clamp01((target - OriginY) / data.size.y);
            }
        }

        // Repeated low-radius smoothing removes terraces without erasing broad lunar landforms.
        for (int pass = 0; pass < 7; pass++)
        {
            float[,] copy = (float[,])result.Clone();
            for (int z = 2; z < n - 2; z++)
            {
                float wz = -data.size.z * .5f + z * stepZ;
                for (int x = 2; x < n - 2; x++)
                {
                    float wx = -data.size.x * .5f + x * stepX;
                    float distance = RoadDistance(new Vector2(wx, wz));
                    if (distance <= 16f)
                    {
                        result[z, x] = (RoadY - OriginY) / data.size.y;
                        continue;
                    }
                    float average = (copy[z, x] * 4f + copy[z-1, x] + copy[z+1, x] +
                        copy[z, x-1] + copy[z, x+1]) / 8f;
                    float roadBlend = Smooth(17f, 48f, distance);
                    result[z, x] = Mathf.Lerp(copy[z, x], average, .78f * roadBlend);
                }
            }
        }

        data.SetHeights(0, 0, result);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log("[NITRO ZERO] Racing terrain relief lowered and stair-step transitions smoothed.");
    }

    private static float RoadDistance(Vector2 p)
    {
        float best = SegmentDistance(p, new Vector2(-2050f, 600f), new Vector2(-200f, 600f));
        Vector2 center = new Vector2(-200f, 0f), local = p - center;
        float angle = Mathf.Clamp(Mathf.Atan2(local.y, local.x), -20f * Mathf.Deg2Rad, 90f * Mathf.Deg2Rad);
        Vector2 arc = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 600f;
        best = Mathf.Min(best, Vector2.Distance(p, arc));
        float endAngle = -20f * Mathf.Deg2Rad;
        Vector2 end = center + new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * 600f;
        Vector2 direction = new Vector2(Mathf.Sin(endAngle), -Mathf.Cos(endAngle));
        return Mathf.Min(best, SegmentDistance(p, end, end + direction * 1450f));
    }

    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        return Vector2.Distance(p, a + ab * Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude));
    }

    private static float Smooth(float a, float b, float v) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, v));
}
