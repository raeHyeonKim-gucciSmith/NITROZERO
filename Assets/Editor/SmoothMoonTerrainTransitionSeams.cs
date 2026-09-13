using UnityEditor;
using UnityEngine;

public static class SmoothMoonTerrainTransitionSeams
{
    [MenuItem("NITRO ZERO/Moon Terrain/Smooth Road And Dome Terrain Seams")]
    public static void ApplyAll()
    {
        Apply("Assets/Terrain/AvoidMissile_MoonTerrain.asset", 50f, 175f, false, StraightDistance);
        Apply("Assets/Terrain/BoostOn_MoonOpenPlain_v5.asset", 70f, 230f, false, StraightDistance);
        Apply("Assets/Terrain/DomeInTheMoon_Basin.asset", 75f, 310f, true, DomeRoadDistance);
        Apply("Assets/Terrain/Racing_JCurveMoonTerrain.asset", 75f, 270f, false, RacingDistance);
        AssetDatabase.SaveAssets();
        Debug.Log("[NITRO ZERO] Straight terrace seams around 20m roads and the smaller dome have been smoothed.");
    }

    private static void Apply(string path, float formerFlatHalfWidth, float transitionEnd, bool dome,
        System.Func<float, float, float> roadDistance)
    {
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
        if (data == null) throw new MissingReferenceException(path);
        int n = data.heightmapResolution;
        float[,] original = data.GetHeights(0, 0, n, n);
        float[,] smoothed = BoxBlur(BoxBlur(BoxBlur(original, 24), 24), 24);
        float[,] result = (float[,])original.Clone();
        float spacingX = data.size.x / (n - 1f);
        float spacingZ = data.size.z / (n - 1f);

        for (int z = 1; z < n - 1; z++)
        {
            float worldZ = -data.size.z * 0.5f + z * spacingZ;
            for (int x = 1; x < n - 1; x++)
            {
                float worldX = -data.size.x * 0.5f + x * spacingX;
                float road = roadDistance(worldX, worldZ);
                float pad = dome ? Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(-1114.9f, 28f)) : float.MaxValue;
                if (road > transitionEnd + 180f && pad > 850f) continue;

                float roadBand = 0f;
                if (road > 12.5f && road < transitionEnd + 180f)
                {
                    float oldFlatSeam = 1f - SmoothStep(45f, 125f, Mathf.Abs(road - formerFlatHalfWidth));
                    float outerSeam = 1f - SmoothStep(65f, 170f, Mathf.Abs(road - transitionEnd));
                    float shoulder = 0.37f * (1f - SmoothStep(transitionEnd, transitionEnd + 180f, road));
                    roadBand = Mathf.Max(shoulder, Mathf.Max(oldFlatSeam, outerSeam) * 0.94f);
                    roadBand *= SmoothStep(12.5f, 43f, road);
                }

                float padBand = 0f;
                if (dome && pad > 205f && pad < 850f)
                {
                    float innerPadSeam = 1f - SmoothStep(55f, 145f, Mathf.Abs(pad - 210f));
                    float formerPadSeam = 1f - SmoothStep(75f, 200f, Mathf.Abs(pad - 650f));
                    padBand = Mathf.Max(0.42f, Mathf.Max(innerPadSeam, formerPadSeam) * 0.96f);
                    padBand *= SmoothStep(205f, 255f, pad);
                    padBand *= 1f - SmoothStep(760f, 850f, pad);
                }

                float weight = Mathf.Max(roadBand, padBand);
                if (weight <= 0f) continue;
                float localSlope = Mathf.Max(
                    Mathf.Abs(original[z, x + 1] - original[z, x - 1]) * data.size.y / (2f * spacingX),
                    Mathf.Abs(original[z + 1, x] - original[z - 1, x]) * data.size.y / (2f * spacingZ));
                if (localSlope > 0.22f) weight = Mathf.Max(weight, Mathf.Min(0.98f, weight + 0.18f));
                result[z, x] = Mathf.Lerp(original[z, x], smoothed[z, x], weight);
            }
        }

        data.SetHeights(0, 0, result);
        EditorUtility.SetDirty(data);
    }

    private static float[,] BoxBlur(float[,] source, int radius)
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

    private static float SmoothStep(float min, float max, float value)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(min, max, value));
    }

    private static float StraightDistance(float x, float z) { return Mathf.Abs(z); }

    private static float DomeRoadDistance(float x, float z)
    {
        return x >= -925f ? Mathf.Abs(z - 28f) : float.MaxValue;
    }

    private static float RacingDistance(float x, float z)
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
        return Vector2.Distance(p, a + ab * Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude));
    }
}
