using UnityEditor;
using UnityEngine;

public static class RepairRacingCakeCliffs
{
    private const string TerrainPath = "Assets/Terrain/Racing_JCurveMoonTerrain.asset";
    private const float OriginY = -80f;
    private const float RoadY = -20.5f;

    [MenuItem("NITRO ZERO/Moon Terrain/Repair Racing Cake Cliffs And Color Bands")]
    public static void Apply()
    {
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
        if (data == null) throw new MissingReferenceException(TerrainPath);
        RepairHeight(data);
        RepairColor(data);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log("[NITRO ZERO] Racing cake-like cuts, spikes and road-side color bands repaired.");
    }

    private static void RepairHeight(TerrainData data)
    {
        int n = data.heightmapResolution;
        float[,] h = data.GetHeights(0, 0, n, n);
        float dx = data.size.x / (n - 1f), dz = data.size.z / (n - 1f);

        // Replace the previous hard 720 m cap with a continuous, noisy 1.2 km transition.
        for (int z = 0; z < n; z++)
        {
            float wz = -data.size.z * .5f + z * dz;
            for (int x = 0; x < n; x++)
            {
                float wx = -data.size.x * .5f + x * dx;
                float d = ClosestRoad(new Vector2(wx, wz)).distance;
                if (d > 1220f) continue;
                float current = OriginY + h[z, x] * data.size.y;
                float rise = Smooth(16f, 1080f, d);
                float irregular = (Fbm(wx, wz, .0018f, 4, 193f) - .5f) * 7f * rise;
                float cap = RoadY + Mathf.Lerp(8f, 73f, rise) + irregular;
                float influence = 1f - Smooth(900f, 1220f, d);
                float target = Mathf.Min(current, cap);
                current = Mathf.Lerp(current, target, influence);
                if (d <= 16f) current = RoadY;
                h[z, x] = Mathf.Clamp01((current - OriginY) / data.size.y);
            }
        }

        // Wide smoothing dissolves the former circular/vertical cut instead of tracing it.
        for (int pass = 0; pass < 12; pass++)
        {
            float[,] copy = (float[,])h.Clone();
            for (int z = 2; z < n - 2; z++)
            {
                float wz = -data.size.z * .5f + z * dz;
                for (int x = 2; x < n - 2; x++)
                {
                    float wx = -data.size.x * .5f + x * dx;
                    float d = ClosestRoad(new Vector2(wx, wz)).distance;
                    if (d <= 16f) { h[z, x] = (RoadY - OriginY) / data.size.y; continue; }
                    if (d > 1300f) continue;
                    float average = (copy[z,x] * 8f +
                        (copy[z-1,x] + copy[z+1,x] + copy[z,x-1] + copy[z,x+1]) * 2f +
                        copy[z-1,x-1] + copy[z-1,x+1] + copy[z+1,x-1] + copy[z+1,x+1]) / 20f;
                    float mask = Smooth(17f, 55f, d) * (1f - Smooth(1120f, 1300f, d));
                    h[z, x] = Mathf.Lerp(copy[z, x], average, .88f * mask);
                }
            }
        }

        // Downward-only spike limiter removes isolated needles while preserving broad hills.
        float maxNeedle = 2.2f / data.size.y;
        for (int pass = 0; pass < 5; pass++)
        {
            float[,] copy = (float[,])h.Clone();
            for (int z = 1; z < n - 1; z++)
                for (int x = 1; x < n - 1; x++)
                {
                    float neighbor = (copy[z-1,x] + copy[z+1,x] + copy[z,x-1] + copy[z,x+1]) * .25f;
                    if (copy[z,x] > neighbor + maxNeedle) h[z,x] = neighbor + maxNeedle;
                }
        }
        data.SetHeights(0, 0, h);
    }

    private static void RepairColor(TerrainData data)
    {
        int n = data.alphamapResolution, layers = data.alphamapLayers;
        if (layers < 1) return;
        float[,,] source = data.GetAlphamaps(0, 0, n, n);
        float[,,] result = (float[,,])source.Clone();
        for (int z = 0; z < n; z++)
        {
            float wz = (z / (float)(n - 1) - .5f) * data.size.z;
            for (int x = 0; x < n; x++)
            {
                float wx = (x / (float)(n - 1) - .5f) * data.size.x;
                Vector2 p = new Vector2(wx, wz);
                RoadHit hit = ClosestRoad(p);
                if (hit.distance > 680f) continue;
                Vector2 a = hit.point + hit.normal * 710f;
                Vector2 b = hit.point - hit.normal * 710f;
                int ax = Pixel(a.x, data.size.x, n), az = Pixel(a.y, data.size.z, n);
                int bx = Pixel(b.x, data.size.x, n), bz = Pixel(b.y, data.size.z, n);
                float blend = (1f - Smooth(390f, 680f, hit.distance)) * .9f;
                for (int layer = 0; layer < layers; layer++)
                {
                    float outside = (source[az, ax, layer] + source[bz, bx, layer]) * .5f;
                    result[z, x, layer] = Mathf.Lerp(source[z, x, layer], outside, blend);
                }
            }
        }
        data.SetAlphamaps(0, 0, result);
    }

    private struct RoadHit { public Vector2 point, normal; public float distance; }

    private static RoadHit ClosestRoad(Vector2 p)
    {
        RoadHit best = Segment(p, new Vector2(-2500f, 600f), new Vector2(-200f, 600f));
        Vector2 center = new Vector2(-200f, 0f), local = p - center;
        float angle = Mathf.Clamp(Mathf.Atan2(local.y, local.x), -20f * Mathf.Deg2Rad, 90f * Mathf.Deg2Rad);
        Vector2 arc = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 600f;
        RoadHit arcHit = new RoadHit { point = arc, normal = (arc-center).normalized, distance = Vector2.Distance(p, arc) };
        if (arcHit.distance < best.distance) best = arcHit;
        float endAngle = -20f * Mathf.Deg2Rad;
        Vector2 end = center + new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * 600f;
        Vector2 direction = new Vector2(Mathf.Sin(endAngle), -Mathf.Cos(endAngle));
        RoadHit exit = Segment(p, end, end + direction * 1900f);
        if (exit.distance < best.distance) best = exit;
        return best;
    }

    private static RoadHit Segment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b-a, tangent = ab.normalized;
        Vector2 q = a + ab * Mathf.Clamp01(Vector2.Dot(p-a, ab) / ab.sqrMagnitude);
        return new RoadHit { point=q, normal=new Vector2(-tangent.y,tangent.x), distance=Vector2.Distance(p,q) };
    }

    private static int Pixel(float world, float size, int n) => Mathf.Clamp(Mathf.RoundToInt((world/size+.5f)*(n-1)),0,n-1);
    private static float Smooth(float a,float b,float v) => Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(a,b,v));
    private static float Fbm(float x,float z,float scale,int octaves,float seed)
    {
        float sum=0f, amp=1f, total=0f;
        for(int i=0;i<octaves;i++){sum+=Mathf.PerlinNoise(x*scale+seed+i*13.1f,z*scale+seed*.37f+i*7.9f)*amp;total+=amp;amp*=.5f;scale*=2.07f;}
        return sum/total;
    }
}
