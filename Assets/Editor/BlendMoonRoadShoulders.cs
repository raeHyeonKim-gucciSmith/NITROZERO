using System;
using UnityEditor;
using UnityEngine;

public static class BlendMoonRoadShoulders
{
    private enum Route { Straight, Dome, Racing }

    [MenuItem("NITRO ZERO/Moon Terrain/Blend All Road Shoulders")]
    public static void ApplyAll()
    {
        Apply("Assets/Terrain/AvoidMissile_MoonTerrain.asset", -60f, -30.5f, 330f, 17f, Route.Straight);
        Apply("Assets/Terrain/BoostOn_MoonOpenPlain_v5.asset", -80f, -24.5f, 410f, 43f, Route.Straight);
        Apply("Assets/Terrain/DomeInTheMoon_Basin.asset", -100f, -20.45f, 470f, 79f, Route.Dome);
        Apply("Assets/Terrain/Racing_JCurveMoonTerrain.asset", -80f, -20.5f, 410f, 113f, Route.Racing);
        AssetDatabase.SaveAssets();
        Debug.Log("[NITRO ZERO] All four 20m road shoulders blended into uneven lunar ground.");
    }

    private static void Apply(string path, float originY, float roadGroundY, float nominalReach,
        float seed, Route route)
    {
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
        if (data == null) throw new MissingReferenceException(path);
        int n = data.heightmapResolution;
        float[,] source = data.GetHeights(0, 0, n, n);
        float[,] result = (float[,])source.Clone();
        float spacingX = data.size.x / (n - 1f);
        float spacingZ = data.size.z / (n - 1f);
        Vector2 dome = new Vector2(-722.1f, 28f);

        for (int z = 0; z < n; z++)
        {
            float worldZ = -data.size.z * 0.5f + z * spacingZ;
            for (int x = 0; x < n; x++)
            {
                float worldX = -data.size.x * 0.5f + x * spacingX;
                Vector2 point = new Vector2(worldX, worldZ);
                Vector2 closest = ClosestRoadPoint(point, route);
                Vector2 offset = point - closest;
                float distance = offset.magnitude;
                float originalY = originY + source[z, x] * data.size.y;
                float h = originalY;

                // Only the asphalt's actual 20m footprint remains flat. Its outer ground
                // follows existing elevations, with a locally varied blend distance.
                float routeStart = route == Route.Straight ? -data.size.x * 0.5f : -2500f;
                if (distance < nominalReach + 115f && closest.x >= routeStart && closest.x <= data.size.x * 0.5f)
                {
                    Vector2 normal = distance > 0.001f ? offset / distance : new Vector2(0f, 1f);
                    float broad = Fbm(worldX, worldZ, 0.0021f, seed);
                    float reach = nominalReach + (broad - 0.5f) * 145f;
                    Vector2 reference = closest + normal * (reach + 55f);
                    float referenceY = Mathf.Max(roadGroundY - 2f, SampleHeight(source, data, originY, reference));
                    float rise = Smooth(10.5f, reach, distance);
                    float connectingY = Mathf.Lerp(roadGroundY, referenceY, rise);

                    float fine = Fbm(worldX, worldZ, 0.026f, seed + 73f) - 0.5f;
                    float mid = Fbm(worldX, worldZ, 0.009f, seed + 31f) - 0.5f;
                    float roughness = (mid * 5.2f + fine * 2.3f) *
                        Smooth(11f, 75f, distance) * (1f - Smooth(reach - 105f, reach + 30f, distance));
                    connectingY += roughness;

                    float influence = 1f - Smooth(reach - 115f, reach + 65f, distance);
                    if (route == Route.Dome)
                        influence *= Smooth(185f, 350f, Vector2.Distance(point, dome));
                    h = Mathf.Lerp(originalY, connectingY, influence);
                    if (distance <= 10.5f) h = roadGroundY;
                }

                if (route == Route.Dome)
                {
                    float fromDome = Vector2.Distance(point, dome);
                    float craterFloor = Smooth(190f, 390f, fromDome) *
                        (1f - Smooth(1380f, 1740f, fromDome));
                    float offRoad = Smooth(15f, 105f, distance);
                    float large = Fbm(worldX, worldZ, 0.0075f, seed + 171f) - 0.5f;
                    float small = Fbm(worldX, worldZ, 0.028f, seed + 251f) - 0.5f;
                    h += (large * 7.0f + small * 2.5f) * craterFloor * offRoad;
                }

                result[z, x] = Mathf.Clamp01((h - originY) / data.size.y);
            }
        }

        data.SetHeights(0, 0, result);
        EditorUtility.SetDirty(data);
    }

    private static Vector2 ClosestRoadPoint(Vector2 p, Route route)
    {
        if (route == Route.Straight) return new Vector2(p.x, 0f);
        if (route == Route.Dome) return new Vector2(Mathf.Clamp(p.x, -925f, 3000f), 28f);

        Vector2 straight = SegmentClosest(p, new Vector2(-2050f, 600f), new Vector2(-200f, 600f));
        Vector2 center = new Vector2(-200f, 0f);
        Vector2 relative = p - center;
        float angle = Mathf.Clamp(Mathf.Atan2(relative.y, relative.x), -20f * Mathf.Deg2Rad, 90f * Mathf.Deg2Rad);
        Vector2 arc = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 600f;
        float endAngle = -20f * Mathf.Deg2Rad;
        Vector2 arcEnd = center + new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * 600f;
        Vector2 exitDirection = new Vector2(Mathf.Sin(endAngle), -Mathf.Cos(endAngle));
        Vector2 exit = SegmentClosest(p, arcEnd, arcEnd + exitDirection * 1450f);
        float best = (p - straight).sqrMagnitude;
        Vector2 selected = straight;
        if ((p - arc).sqrMagnitude < best) { best = (p - arc).sqrMagnitude; selected = arc; }
        if ((p - exit).sqrMagnitude < best) selected = exit;
        return selected;
    }

    private static Vector2 SegmentClosest(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        return a + ab * Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
    }

    private static float SampleHeight(float[,] source, TerrainData data, float originY, Vector2 world)
    {
        int n = data.heightmapResolution;
        float fx = Mathf.Clamp((world.x / data.size.x + 0.5f) * (n - 1f), 0f, n - 1f);
        float fz = Mathf.Clamp((world.y / data.size.z + 0.5f) * (n - 1f), 0f, n - 1f);
        int x0 = Mathf.FloorToInt(fx), z0 = Mathf.FloorToInt(fz);
        int x1 = Mathf.Min(x0 + 1, n - 1), z1 = Mathf.Min(z0 + 1, n - 1);
        float a = Mathf.Lerp(source[z0, x0], source[z0, x1], fx - x0);
        float b = Mathf.Lerp(source[z1, x0], source[z1, x1], fx - x0);
        return originY + Mathf.Lerp(a, b, fz - z0) * data.size.y;
    }

    private static float Smooth(float from, float to, float value)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, value));
    }

    private static float Fbm(float x, float z, float scale, float seed)
    {
        float total = 0f, strength = 1f, weight = 0f;
        for (int octave = 0; octave < 3; octave++)
        {
            total += Mathf.PerlinNoise(x * scale + seed, z * scale + seed * 0.37f) * strength;
            weight += strength;
            strength *= 0.5f;
            scale *= 2f;
        }
        return total / weight;
    }
}
