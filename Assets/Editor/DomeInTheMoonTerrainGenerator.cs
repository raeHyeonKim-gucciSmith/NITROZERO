using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DomeInTheMoonTerrainGenerator
{
    private const string ScenePath = "Assets/Scenes/domeInTheMoon.unity";
    private const string DataPath = "Assets/Terrain/DomeInTheMoon_Basin.asset";
    private const string LayerPath = "Assets/02YUJEONG/Moon/moon_04_2k.blend/textures/NewLayer.terrainlayer";
    private const int Resolution = 2049;
    private const float Length = 6000f;
    private const float Width = 6500f;
    private const float Height = 560f;
    private const float OriginY = -100f;
    private const float RoadY = -20f;
    private const float DefaultDomeX = -722.1f;
    private const float DefaultDomeZ = 28f;
    private const float DomeFlatRadius = 195f;
    private const float RoadStartX = -925f;
    private const float RoadFlatHalfWidth = 21f;
    private const float RoadBlendHalfWidth = 240f;
    private const int Version = 11;

    [InitializeOnLoadMethod]
    private static void GenerateOnce()
    {
        string key = $"NITROZERO.DomeInTheMoonTerrain.{Application.dataPath.GetHashCode()}";
        if (AssetDatabase.LoadAssetAtPath<TerrainData>(DataPath) != null && EditorPrefs.GetInt(key, 0) >= Version)
            return;
        EditorApplication.delayCall += TryGenerateOnce;
    }

    private static void TryGenerateOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryGenerateOnce;
            return;
        }
        string key = $"NITROZERO.DomeInTheMoonTerrain.{Application.dataPath.GetHashCode()}";
        if (AssetDatabase.LoadAssetAtPath<TerrainData>(DataPath) != null && EditorPrefs.GetInt(key, 0) >= Version)
            return;
        try { Generate(); EditorPrefs.SetInt(key, Version); }
        catch (Exception exception) { Debug.LogException(exception); }
    }

    [MenuItem("NITRO ZERO/Moon Terrain/Generate domeInTheMoon Basin")]
    public static void Generate()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool close = !scene.isLoaded;
        if (close) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        GameObject dome = FindRoot(scene, "industrial_dome");
        float domeX = dome != null ? dome.transform.position.x : DefaultDomeX;
        float domeZ = dome != null ? dome.transform.position.z : DefaultDomeZ;

        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(DataPath);
        if (data == null)
        {
            data = new TerrainData();
            AssetDatabase.CreateAsset(data, DataPath);
        }
        data.heightmapResolution = Resolution;
        data.size = new Vector3(Length, Height, Width);
        data.alphamapResolution = 1024;
        data.baseMapResolution = 2048;
        data.SetHeights(0, 0, BuildHeights(domeX, domeZ));

        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerPath);
        if (layer != null && data.terrainLayers.Length == 0)
        {
            data.terrainLayers = new[] { layer };
            float[,,] alpha = new float[data.alphamapResolution, data.alphamapResolution, 1];
            for (int z = 0; z < data.alphamapResolution; z++)
                for (int x = 0; x < data.alphamapResolution; x++) alpha[z, x, 0] = 1f;
            data.SetAlphamaps(0, 0, alpha);
        }

        Terrain terrain = FindTerrain(scene);
        if (terrain == null)
        {
            GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
            SceneManager.MoveGameObjectToScene(terrainObject, scene);
            terrain = terrainObject.GetComponent<Terrain>();
        }
        terrain.terrainData = data;
        terrain.gameObject.name = "Dome Moon Basin 6000x6500";
        terrain.transform.position = new Vector3(-Length * 0.5f, OriginY, -Width * 0.5f);
        terrain.heightmapPixelError = 4f;
        terrain.basemapDistance = 5500f;
        terrain.drawInstanced = true;
        terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        terrain.gameObject.isStatic = true;
        TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
        if (collider == null) collider = terrain.gameObject.AddComponent<TerrainCollider>();
        collider.terrainData = data;

        CreateGuides(scene, domeX, domeZ);
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                camera.farClipPlane = Mathf.Max(camera.farClipPlane, 8000f);

        EditorUtility.SetDirty(data);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (close) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[NITRO ZERO] domeInTheMoon 6000x6500 basin terrain generated.");
    }

    private static float[,] BuildHeights(float domeX, float domeZ)
    {
        float[,] result = new float[Resolution, Resolution];
        const float groundY = -20.45f; // 0.8m below the asphalt mesh.
        float roadLocal = groundY - OriginY;
        float craterX = Mathf.Lerp(domeX, 0f, 0.48f);
        float craterZ = Mathf.Lerp(domeZ, 0f, 0.35f);
        for (int z = 0; z < Resolution; z++)
        {
            float worldZ = z / (float)(Resolution - 1) * Width - Width * 0.5f;
            for (int x = 0; x < Resolution; x++)
            {
                float worldX = x / (float)(Resolution - 1) * Length - Length * 0.5f;
                float dx = worldX - craterX;
                float dz = worldZ - craterZ;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);
                float signedAngle = Mathf.Atan2(dz, dx);
                float angle = Mathf.Abs(signedAngle);
                float broadNoise = Fbm(worldX, worldZ, 0.00085f, 4, 311f);
                float detailNoise = Fbm(worldX, worldZ, 0.0032f, 3, 109f);

                // A roughly 4.2km impact crater, shifted from the dome toward map center.
                float westFactor = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.2f, 3.14f, angle));
                float westClearance = 230f * westFactor;
                float rimRadius = 2100f - westClearance + (broadNoise - 0.5f) * 175f +
                    Mathf.Sin(signedAngle * 5f + 0.7f) * 42f;
                float crestNoise = Fbm(worldX, worldZ, 0.00125f, 4, 533f);
                float crestVariation = 0.5f +
                    Mathf.Sin(signedAngle * 3f + 0.6f) * 0.23f +
                    Mathf.Sin(signedAngle * 7f - 1.3f) * 0.16f +
                    Mathf.Sin(signedAngle * 11f + 2f) * 0.08f +
                    (crestNoise - 0.5f) * 0.62f;
                float rimHeight = Mathf.Lerp(135f, 365f, Mathf.Clamp01(crestVariation));

                // Higher sections have a slightly broader inner foot, never a needle-like crest.
                float innerWidth = Mathf.Lerp(235f, 315f, crestNoise) + (rimHeight - 250f) * 0.17f;
                float outerWidth = Mathf.Lerp(615f, 805f, crestNoise) * (1f - westFactor * 0.23f);
                float radial = (distance - rimRadius) / (distance < rimRadius ? innerWidth : outerWidth);
                float rimProfile = Mathf.Exp(-radial * radial);

                // The road-side ends reach nearer the road, then taper over a longer arc.
                float heightFactor = Mathf.InverseLerp(135f, 365f, rimHeight);
                float openingStart = Mathf.Lerp(0.075f, 0.045f, heightFactor);
                float openingEnd = Mathf.Lerp(0.47f, 0.59f, heightFactor);
                float gapVariation = (Fbm(worldX, worldZ, 0.00065f, 3, 719f) - 0.5f) * 0.045f;
                float opening = Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(openingStart, openingEnd, angle + gapVariation));
                float rim = rimProfile * rimHeight * opening;

                // The interior is a broad floor, not a steep, cup-shaped pit.
                float floorRise = 8f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(550f, 1550f, distance));
                float outerRise = 16f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2500f, 3300f, distance));
                float surface = (broadNoise - 0.5f) * 9f + (detailNoise - 0.5f) * 3.5f;
                surface *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(190f, 420f, distance));
                float h = groundY + floorRise + outerRise + surface + rim;

                // Level only the actual dome footprint, blending into the crater floor.
                float domeDistance = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(domeX, domeZ));
                float domeFlat = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(185f, 355f, domeDistance));
                h = Mathf.Lerp(h, groundY, domeFlat);

                // The east rim is broken for the straight 20m road; its shoulders fade gradually.
                if (worldX >= RoadStartX - 180f)
                {
                    float along = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(RoadStartX - 180f, RoadStartX + 100f, worldX));
                    float fromRoad = Mathf.Abs(worldZ - domeZ);
                    float cross = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(RoadFlatHalfWidth, RoadBlendHalfWidth, fromRoad));
                    h = Mathf.Lerp(h, groundY, along * cross);
                    if (worldX >= RoadStartX && fromRoad <= RoadFlatHalfWidth) h = groundY;
                }

                result[z, x] = Mathf.Clamp01((h - OriginY) / Height);
            }
        }
        Smooth(result, 2, roadLocal / Height, domeX, domeZ);
        return result;
    }

    private static float Fbm(float x, float z, float scale, int octaves, float seed)
    {
        float sum = 0f, amplitude = 1f, total = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += Mathf.PerlinNoise(x * scale + seed, z * scale + seed * 0.37f) * amplitude;
            total += amplitude;
            amplitude *= 0.5f;
            scale *= 2f;
        }
        return sum / total;
    }

    private static void Smooth(float[,] heights, int passes, float roadHeight, float domeX, float domeZ)
    {
        int size = heights.GetLength(0);
        float[,] copy = new float[size, size];
        for (int pass = 0; pass < passes; pass++)
        {
            Array.Copy(heights, copy, heights.Length);
            for (int z = 1; z < size - 1; z++)
                for (int x = 1; x < size - 1; x++)
                {
                    float worldX = x / (float)(size - 1) * Length - Length * 0.5f;
                    float worldZ = z / (float)(size - 1) * Width - Width * 0.5f;
                    float domeDistance = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(domeX, domeZ));
                    if (domeDistance <= DomeFlatRadius - 10f || (worldX >= RoadStartX && Mathf.Abs(worldZ - domeZ) <= RoadFlatHalfWidth))
                    { heights[z, x] = roadHeight; continue; }
                    heights[z, x] = (copy[z, x] * 4f + copy[z - 1, x] + copy[z + 1, x] + copy[z, x - 1] + copy[z, x + 1]) / 8f;
                }
        }
    }

    private static Terrain FindTerrain(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Terrain terrain = root.GetComponentInChildren<Terrain>(true);
            if (terrain != null) return terrain;
        }
        return null;
    }

    private static void CreateGuides(Scene scene, float domeX, float domeZ)
    {
        GameObject domeGuide = FindRoot(scene, "Dome Flat Area Guide (1300m)");
        if (domeGuide == null) domeGuide = FindRoot(scene, "Dome Crater Floor Guide (390m)");
        if (domeGuide == null)
        {
            domeGuide = new GameObject("Dome Crater Floor Guide (390m)");
            SceneManager.MoveGameObjectToScene(domeGuide, scene);
        }
        domeGuide.name = "Dome Crater Floor Guide (390m)";
        domeGuide.transform.position = new Vector3(domeX, RoadY, domeZ);
        domeGuide.transform.localScale = new Vector3(DomeFlatRadius * 2f, 1f, DomeFlatRadius * 2f);

        GameObject roadGuide = FindRoot(scene, "Dome Exit Road Guide (30m)") ??
            FindRoot(scene, "Dome Exit Road Guide (35m)") ??
            FindRoot(scene, "Dome Exit Road Guide (20m)");
        if (roadGuide == null)
        {
            roadGuide = new GameObject("Dome Exit Road Guide (30m)");
            SceneManager.MoveGameObjectToScene(roadGuide, scene);
            roadGuide.AddComponent<MoonRoadPlacementGuide>();
        }
        float roadLength = Length * 0.5f - RoadStartX;
        roadGuide.transform.position = new Vector3(RoadStartX + roadLength * 0.5f, RoadY, 28f);
        roadGuide.name = "Dome Exit Road Guide (30m)";
        roadGuide.transform.localScale = new Vector3(roadLength, 1f, 30f);
        MoonRoadPlacementGuide info = roadGuide.GetComponent<MoonRoadPlacementGuide>();
        info.mapLength = roadLength;
        info.flatWidth = 30f;
        info.blendedWidth = RoadBlendHalfWidth * 2f;
        info.roadSurfaceY = RoadY;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
        return null;
    }
}
