using System;
using UnityEditor;
using UnityEngine;

public static class RefineRacingTerrainContinuity
{
    private const string TerrainPath = "Assets/Terrain/Racing_JCurveMoonTerrain.asset";
    private const float OriginY = -80f;
    private const float RoadY = -20.5f;

    [MenuItem("NITRO ZERO/Moon Terrain/Refine Racing Terrain Continuity")]
    public static void Apply()
    {
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
        if (data == null) throw new MissingReferenceException(TerrainPath);
        RefineHeights(data);
        UnifySurface(data);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log("[NITRO ZERO] Racing terrain shoulders, inner ridge and road-end continuity refined.");
    }

    private static void RefineHeights(TerrainData data)
    {
        int n = data.heightmapResolution;
        float[,] source = data.GetHeights(0, 0, n, n);
        float[,] result = (float[,])source.Clone();
        float sx = data.size.x / (n - 1f), sz = data.size.z / (n - 1f);

        for (int z = 0; z < n; z++)
        {
            float wz = -data.size.z * .5f + z * sz;
            for (int x = 0; x < n; x++)
            {
                float wx = -data.size.x * .5f + x * sx;
                RoadHit hit = ClosestRoad(new Vector2(wx, wz), true);
                float d = hit.distance;
                if (d > 390f) continue;

                float current = OriginY + source[z, x] * data.size.y;
                float rise = Smooth(15f, 340f, d);
                float broadNoise = (Mathf.PerlinNoise(wx * .0041f + 21f, wz * .0041f + 47f) - .5f) * 7f * rise;
                float ceiling = RoadY + 49f * rise + broadNoise;
                float lowered = Mathf.Min(current, ceiling);

                // The inside of the bend previously contained the stepped, over-tall ridge.
                float inside = 1f - Smooth(250f, 760f, Vector2.Distance(new Vector2(wx, wz), new Vector2(-210f, 20f)));
                float influence = (1f - Smooth(265f, 390f, d)) * Mathf.Lerp(.62f, .88f, inside);
                float target = Mathf.Lerp(current, lowered, influence);
                if (d <= 16f) target = RoadY;
                result[z, x] = Mathf.Clamp01((target - OriginY) / data.size.y);
            }
        }

        // Local low-pass removes terrace lines without flattening the broad lunar form.
        for (int pass = 0; pass < 4; pass++)
        {
            float[,] copy = (float[,])result.Clone();
            for (int z = 2; z < n - 2; z++)
            {
                float wz = -data.size.z * .5f + z * sz;
                for (int x = 2; x < n - 2; x++)
                {
                    float wx = -data.size.x * .5f + x * sx;
                    float d = ClosestRoad(new Vector2(wx, wz), true).distance;
                    if (d <= 16f) { result[z, x] = (RoadY - OriginY) / data.size.y; continue; }
                    if (d > 390f) continue;
                    float average = (copy[z, x] * 4f + copy[z-1, x] + copy[z+1, x] + copy[z, x-1] + copy[z, x+1]) / 8f;
                    float mask = (1f - Smooth(300f, 390f, d)) * Smooth(16f, 42f, d);
                    result[z, x] = Mathf.Lerp(copy[z, x], average, .72f * mask);
                }
            }
        }
        data.SetHeights(0, 0, result);
    }

    private static void UnifySurface(TerrainData data)
    {
        int n = data.alphamapResolution;
        int layers = data.alphamapLayers;
        if (layers < 1) return;
        float[,,] source = data.GetAlphamaps(0, 0, n, n);
        float[,,] result = (float[,,])source.Clone();

        for (int z = 0; z < n; z++)
        {
            float wz = z / (float)(n - 1) * data.size.z - data.size.z * .5f;
            for (int x = 0; x < n; x++)
            {
                float wx = x / (float)(n - 1) * data.size.x - data.size.x * .5f;
                Vector2 p = new Vector2(wx, wz);
                RoadHit hit = ClosestRoad(p, true);
                if (hit.distance > 330f) continue;
                Vector2 normal = hit.normal * (Vector2.Dot(p - hit.point, hit.normal) < 0f ? -1f : 1f);
                Vector2 reference = hit.point + normal * 345f;
                int rx = Mathf.Clamp(Mathf.RoundToInt((reference.x / data.size.x + .5f) * (n - 1)), 0, n - 1);
                int rz = Mathf.Clamp(Mathf.RoundToInt((reference.y / data.size.z + .5f) * (n - 1)), 0, n - 1);
                float blend = (1f - Smooth(245f, 330f, hit.distance)) * .82f;
                blend *= Smooth(12f, 55f, hit.distance);
                for (int l = 0; l < layers; l++) result[z, x, l] = Mathf.Lerp(source[z, x, l], source[rz, rx, l], blend);
            }
        }
        data.SetAlphamaps(0, 0, result);
    }

    private struct RoadHit { public Vector2 point, normal; public float distance; }

    private static RoadHit ClosestRoad(Vector2 p, bool extendEnds)
    {
        RoadHit best = Segment(p, new Vector2(extendEnds ? -2500f : -2050f, 600f), new Vector2(-200f, 600f));
        Vector2 center = new Vector2(-200f, 0f), local = p - center;
        float angle = Mathf.Clamp(Mathf.Atan2(local.y, local.x), -20f * Mathf.Deg2Rad, 90f * Mathf.Deg2Rad);
        Vector2 arc = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 600f;
        RoadHit arcHit = new RoadHit { point = arc, normal = (arc - center).normalized, distance = Vector2.Distance(p, arc) };
        if (arcHit.distance < best.distance) best = arcHit;
        float endAngle = -20f * Mathf.Deg2Rad;
        Vector2 arcEnd = center + new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * 600f;
        Vector2 direction = new Vector2(Mathf.Sin(endAngle), -Mathf.Cos(endAngle));
        RoadHit exit = Segment(p, arcEnd, arcEnd + direction * (extendEnds ? 1900f : 1450f));
        if (exit.distance < best.distance) best = exit;
        return best;
    }

    private static RoadHit Segment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        Vector2 q = a + ab * t;
        Vector2 tangent = ab.normalized;
        return new RoadHit { point = q, normal = new Vector2(-tangent.y, tangent.x), distance = Vector2.Distance(p, q) };
    }

    private static float Smooth(float a, float b, float v)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, v));
    }
}
