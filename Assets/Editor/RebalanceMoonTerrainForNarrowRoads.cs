using UnityEditor;
using UnityEngine;

public static class RebalanceMoonTerrainForNarrowRoads
{
    public static void ApplyAll()
    {
        Apply("Assets/Terrain/AvoidMissile_MoonTerrain.asset", -30f, -60f, 50f, 175f, 18f, 17f, StraightDistance);
        Apply("Assets/Terrain/BoostOn_MoonOpenPlain_v5.asset", -24f, -80f, 70f, 230f, 20f, 43f, StraightDistance);
        Apply("Assets/Terrain/DomeInTheMoon_Basin.asset", -20f, -100f, 75f, 310f, 22f, 79f, DomeRoadDistance, true);
        Apply("Assets/Terrain/Racing_JCurveMoonTerrain.asset", -20f, -80f, 75f, 270f, 24f, 113f, RacingDistance);
        AssetDatabase.SaveAssets();
        Debug.Log("[NITRO ZERO] All four lunar terrains rebalanced around 20m roads and the smaller dome.");
    }

    private static void Apply(string path, float roadY, float originY, float formerFlatHalfWidth,
        float transitionEnd, float shoulderHeight,
        float seed, System.Func<float, float, float> distanceToRoad, bool dome = false)
    {
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
        if (data == null) throw new MissingReferenceException(path);
        int resolution = data.heightmapResolution;
        float[,] heights = data.GetHeights(0, 0, resolution, resolution);
        float minX = -data.size.x * 0.5f;
        float minZ = -data.size.z * 0.5f;

        for (int z = 0; z < resolution; z++)
        {
            float worldZ = minZ + z * data.size.z / (resolution - 1f);
            for (int x = 0; x < resolution; x++)
            {
                float worldX = minX + x * data.size.x / (resolution - 1f);
                float roadDistance = distanceToRoad(worldX, worldZ);
                float domeDistance = dome ? Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(-1114.9f, 28f)) : float.MaxValue;
                if (roadDistance > transitionEnd && domeDistance > 780f) continue;

                float original = originY + heights[z, x] * data.size.y;
                float roadMask = roadDistance < transitionEnd
                    ? 1f - SmoothStep(transitionEnd * 0.76f, transitionEnd, roadDistance) : 0f;
                float padMask = domeDistance < 780f
                    ? 1f - SmoothStep(620f, 780f, domeDistance) : 0f;
                if (roadMask <= 0f && padMask <= 0f) continue;

                // A narrow safety strip remains flush with the actual 20m road.
                float alongRoad = Mathf.Max(0f, roadDistance - 11.5f);
                float rise = SmoothStep(0f, transitionEnd - 11.5f, alongRoad);
                float irregular = Fbm(worldX, worldZ, 0.012f, seed) - 0.5f;
                float roadTarget = roadY + rise * shoulderHeight + irregular * 2.8f * rise;
                if (roadDistance <= 11.5f) roadTarget = roadY;
                float roadResult = roadDistance <= formerFlatHalfWidth
                    ? Mathf.Lerp(original, roadTarget, roadMask * 0.9f)
                    : Mathf.Lerp(original, Mathf.Min(original, roadTarget), roadMask);
                if (roadDistance > 11.5f)
                    roadResult += roadMask * SmoothStep(11.5f, 65f, roadDistance) * (1f - rise) *
                        (Fbm(worldX, worldZ, 0.023f, seed + 39f) - 0.45f) * 2.3f;

                // The reduced dome only needs a compact flat pad; the former pad
                // becomes softly rolling lunar ground instead of an abrupt bowl.
                if (dome && domeDistance > 210f && domeDistance < 780f)
                {
                    float padRise = SmoothStep(210f, 780f, domeDistance);
                    float padTarget = roadY + padRise * 18f +
                        (Fbm(worldX, worldZ, 0.008f, seed + 71f) - 0.5f) * 5f * padRise;
                    float padInfluence = padMask * SmoothStep(12f, 110f, roadDistance);
                    float domeTarget = domeDistance <= 650f ? padTarget : Mathf.Min(roadResult, padTarget);
                    roadResult = Mathf.Lerp(roadResult, domeTarget, padInfluence);
                    roadResult += padInfluence * padRise * (1f - padRise) *
                        (Fbm(worldX, worldZ, 0.025f, seed + 103f) - 0.45f) * 2f;
                }

                heights[z, x] = Mathf.Clamp01((roadResult - originY) / data.size.y);
            }
        }

        data.SetHeights(0, 0, heights);
        EditorUtility.SetDirty(data);
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

    private static float SmoothStep(float min, float max, float value)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(min, max, value));
    }

    private static float Fbm(float x, float z, float scale, float seed)
    {
        float sum = 0f, weight = 1f, total = 0f;
        for (int i = 0; i < 4; i++)
        {
            sum += Mathf.PerlinNoise(x * scale + seed + i * 13.1f,
                z * scale + seed * 0.37f + i * 7.9f) * weight;
            total += weight;
            weight *= 0.5f;
            scale *= 2.07f;
        }
        return sum / total;
    }
}
