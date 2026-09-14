using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps the shared moon TerrainData assets below the 35 m road meshes without
/// rebuilding or replacing the artists' existing terrain, craters, or hills.
/// This is idempotent so a Unity reimport cannot keep lowering the ground.
/// </summary>
public static class MoonRoadWidth35TerrainClearance
{
    private const float SafeHalfWidth = 20f;
    private const float FadeHalfWidth = 45f;
    private const float RoadClearance = 0.55f;

    private readonly struct Road
    {
        public readonly string TerrainPath;
        public readonly string MeshPath;
        public readonly Vector3 TerrainOrigin;

        public Road(string terrainPath, string meshPath, Vector3 terrainOrigin)
        {
            TerrainPath = terrainPath;
            MeshPath = meshPath;
            TerrainOrigin = terrainOrigin;
        }
    }

    private static readonly Road[] Roads =
    {
        new Road("Assets/Terrain/AvoidMissile_MoonTerrain.asset", "Assets/Terrain/AvoidMissile_AsphaltRoad.asset", new Vector3(-2000f, -60f, -1000f)),
        new Road("Assets/Terrain/BoostOn_MoonOpenPlain_v5.asset", "Assets/Terrain/BoostOn_AsphaltRoad.asset", new Vector3(-3000f, -80f, -1250f)),
        new Road("Assets/Terrain/DomeInTheMoon_Basin.asset", "Assets/Terrain/DomeExit_AsphaltRoad.asset", new Vector3(-3000f, -100f, -3250f)),
        new Road("Assets/Terrain/Racing_JCurveMoonTerrain.asset", "Assets/Terrain/Racing_DriftRoad.asset", new Vector3(-2500f, -80f, -2000f)),
    };

    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        EditorApplication.delayCall += Apply;
    }

    [MenuItem("NITRO ZERO/Moon Terrain/Ensure 35m Road Clearance")]
    public static void Apply()
    {
        bool anyChanged = false;
        foreach (Road road in Roads)
        {
            TerrainData terrain = AssetDatabase.LoadAssetAtPath<TerrainData>(road.TerrainPath);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(road.MeshPath);
            if (terrain == null || mesh == null || mesh.vertexCount < 4) continue;

            int resolution = terrain.heightmapResolution;
            float[,] heights = terrain.GetHeights(0, 0, resolution, resolution);
            Vector3[] vertices = mesh.vertices;
            bool changed = false;
            for (int i = 0; i + 3 < vertices.Length; i += 2)
            {
                Vector3 start = (vertices[i] + vertices[i + 1]) * 0.5f;
                Vector3 end = (vertices[i + 2] + vertices[i + 3]) * 0.5f;
                changed |= LowerSegment(heights, terrain.size, road.TerrainOrigin, start, end);
            }
            if (!changed) continue;

            terrain.SetHeightsDelayLOD(0, 0, heights);
            terrain.SyncHeightmap();
            EditorUtility.SetDirty(terrain);
            anyChanged = true;
        }
        if (anyChanged) AssetDatabase.SaveAssets();
    }

    private static bool LowerSegment(float[,] heights, Vector3 size, Vector3 origin, Vector3 start, Vector3 end)
    {
        int resolution = heights.GetLength(0);
        int xMin = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(start.x, end.x) - FadeHalfWidth - origin.x) / size.x * (resolution - 1)), 0, resolution - 1);
        int xMax = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(start.x, end.x) + FadeHalfWidth - origin.x) / size.x * (resolution - 1)), 0, resolution - 1);
        int zMin = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(start.z, end.z) - FadeHalfWidth - origin.z) / size.z * (resolution - 1)), 0, resolution - 1);
        int zMax = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(start.z, end.z) + FadeHalfWidth - origin.z) / size.z * (resolution - 1)), 0, resolution - 1);

        Vector2 a = new Vector2(start.x, start.z);
        Vector2 b = new Vector2(end.x, end.z);
        Vector2 ab = b - a;
        float lengthSquared = Mathf.Max(ab.sqrMagnitude, 0.0001f);
        float roadY = (start.y + end.y) * 0.5f;
        float target = roadY - RoadClearance;
        bool changed = false;
        for (int z = zMin; z <= zMax; z++)
        {
            float worldZ = origin.z + z * size.z / (resolution - 1);
            for (int x = xMin; x <= xMax; x++)
            {
                float worldX = origin.x + x * size.x / (resolution - 1);
                Vector2 p = new Vector2(worldX, worldZ);
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSquared);
                float distance = Vector2.Distance(p, a + ab * t);
                if (distance >= FadeHalfWidth) continue;

                float current = origin.y + heights[z, x] * size.y;
                float blend = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(SafeHalfWidth, FadeHalfWidth, distance));
                float lowered = Mathf.Min(current, Mathf.Lerp(current, target, blend));
                if (current - lowered < 0.001f) continue;
                heights[z, x] = Mathf.Clamp01((lowered - origin.y) / size.y);
                changed = true;
            }
        }
        return changed;
    }
}
