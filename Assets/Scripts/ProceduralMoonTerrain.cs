using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Terrain), typeof(TerrainCollider))]
public sealed class ProceduralMoonTerrain : MonoBehaviour
{
    [Header("Map")]
    [SerializeField] private Vector3 terrainSize = new Vector3(4000f, 320f, 2000f);
    [SerializeField] private int heightmapResolution = 1025;
    [SerializeField] private float terrainOriginY = -60f;

    [Header("Straight asphalt foundation")]
    [SerializeField] private float roadSurfaceY = -30f;
    [SerializeField] private float flatRoadWidth = 100f;
    [SerializeField] private float roadBlendWidth = 250f;

    private TerrainData runtimeData;
    private bool generated;

    private readonly Vector4[] craters =
    {
        new Vector4(-1810f, -735f, 285f, 61f), new Vector4(-1265f, 428f, 118f, 24f),
        new Vector4(-735f, -812f, 205f, 43f), new Vector4(-365f, 685f, 79f, 14f),
        new Vector4(185f, -458f, 162f, 33f), new Vector4(745f, 773f, 318f, 72f),
        new Vector4(1038f, -682f, 96f, 18f), new Vector4(1515f, 356f, 221f, 48f),
        new Vector4(1842f, -845f, 61f, 9f), new Vector4(-1540f, 163f, 54f, 8f),
        new Vector4(468f, 342f, 72f, 11f), new Vector4(1328f, -291f, 47f, 7f)
    };

    private readonly Vector4[] peaks =
    {
        new Vector4(-1725f, 835f, 455f, 188f), new Vector4(-1048f, -748f, 268f, 142f),
        new Vector4(-612f, 914f, 176f, 119f), new Vector4(96f, -865f, 402f, 176f),
        new Vector4(885f, 628f, 305f, 154f), new Vector4(1372f, -905f, 512f, 205f),
        new Vector4(1915f, 744f, 214f, 131f), new Vector4(1668f, 668f, 126f, 98f)
    };

    private void OnEnable()
    {
        if (!generated)
            Generate();
    }

    [ContextMenu("Regenerate Moon Surface")]
    public void Generate()
    {
        Terrain terrain = GetComponent<Terrain>();
        TerrainCollider terrainCollider = GetComponent<TerrainCollider>();
        TerrainData source = terrain.terrainData;
        if (source == null)
            return;

        if (runtimeData != null && runtimeData != source)
        {
            if (Application.isPlaying)
                Destroy(runtimeData);
            else
                DestroyImmediate(runtimeData);
        }

        runtimeData = Application.isPlaying ? Instantiate(source) : source;
        if (Application.isPlaying)
            runtimeData.name = "AvoidMissile Moon Terrain (Runtime)";
        runtimeData.heightmapResolution = heightmapResolution;
        runtimeData.size = terrainSize;
        runtimeData.SetHeights(0, 0, BuildHeights());

        terrain.terrainData = runtimeData;
        terrainCollider.terrainData = runtimeData;
        terrain.transform.position = new Vector3(-terrainSize.x * 0.5f, terrainOriginY, -terrainSize.z * 0.5f);
        terrain.heightmapPixelError = 4f;
        terrain.basemapDistance = 3500f;
        terrain.drawInstanced = true;
        generated = true;
    }

    private float[,] BuildHeights()
    {
        float[,] result = new float[heightmapResolution, heightmapResolution];
        float flatHalf = flatRoadWidth * 0.5f;
        float blendHalf = roadBlendWidth * 0.5f;
        float roadLocalY = roadSurfaceY - terrainOriginY;

        for (int z = 0; z < heightmapResolution; z++)
        {
            float worldZ = z / (float)(heightmapResolution - 1) * terrainSize.z - terrainSize.z * 0.5f;
            for (int x = 0; x < heightmapResolution; x++)
            {
                float worldX = x / (float)(heightmapResolution - 1) * terrainSize.x - terrainSize.x * 0.5f;
                Vector2 warp = DomainWarp(worldX, worldZ);
                float height = 7f + Fbm(warp.x, warp.y, 0.00115f, 4, 17.2f) * 18f;
                height += (Fbm(warp.x, warp.y, 0.0045f, 3, 81.7f) - 0.5f) * 10f;
                height += (Fbm(worldX, worldZ, 0.015f, 2, 143.1f) - 0.5f) * 3.2f;
                height += RidgedNoise(warp.x, warp.y, 0.0018f, 3, 33.4f) * 4f;
                height += RoughHighland(worldX, worldZ);
                height += Mathf.Pow(Mathf.Abs(worldZ) / (terrainSize.z * 0.5f), 2.2f) * 24f;

                for (int i = 0; i < peaks.Length; i++)
                    height += PeakHeight(worldX, worldZ, peaks[i]);
                for (int i = 0; i < craters.Length; i++)
                {
                    Vector4 crater = craters[i];
                    if (Mathf.Abs(crater.y) - crater.z * 1.35f > blendHalf + 35f)
                        height += CraterHeight(worldX, worldZ, crater);
                }

                // Several independent scales create overlapping young, old and micro craters.
                height += CraterField(worldX, worldZ, 620f, 0.34f, 0.19f, 0.090f, 11);
                height += CraterField(worldX, worldZ, 270f, 0.38f, 0.16f, 0.074f, 29);
                height += CraterField(worldX, worldZ, 115f, 0.25f, 0.12f, 0.052f, 53);
                height += CraterField(worldX, worldZ, 52f, 0.14f, 0.080f, 0.032f, 97);

                float transitionVariation = (Mathf.PerlinNoise(worldX * 0.0014f + 9.3f, 17.6f) - 0.5f) * 26f;
                float roadBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(flatHalf, blendHalf + transitionVariation, Mathf.Abs(worldZ)));
                height = Mathf.Lerp(roadLocalY, height, roadBlend);
                if (Mathf.Abs(worldZ) <= flatHalf)
                    height = roadLocalY;

                result[z, x] = Mathf.Clamp01(height / terrainSize.y);
            }
        }
        SmoothHeightmap(result, 2, roadLocalY / terrainSize.y, flatHalf);
        return result;
    }

    private static float CraterHeight(float x, float z, Vector4 crater)
    {
        float angle = Mathf.Atan2(z - crater.y, x - crater.x);
        float d = Vector2.Distance(new Vector2(x, z), new Vector2(crater.x, crater.y)) / crater.z;
        if (d >= 1.35f) return 0f;
        float size01 = Mathf.InverseLerp(45f, 320f, crater.z);
        float rimHeight = Mathf.Lerp(crater.z * 0.055f, crater.z * 0.108f, size01);
        float bowl = -crater.w * Mathf.Pow(Mathf.Clamp01(1f - d), 2.25f);
        bool intactRim = Mathf.Repeat(Mathf.Abs(crater.x) * 0.017f, 1f) > 0.72f;
        float gapCenter = Mathf.Repeat(Mathf.Abs(crater.y) * 0.019f, Mathf.PI * 2f) - Mathf.PI;
        float gapDistance = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, gapCenter * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
        float breakMask = intactRim ? 1f : Mathf.Lerp(0.025f, 1f, Mathf.SmoothStep(0.24f, 0.72f, gapDistance));
        float rimRoughness = Mathf.Lerp(0.55f, 1.38f, Fbm(x, z, 0.018f, 3, crater.x * 0.013f + 401f));
        float rimWidth = Mathf.Lerp(0.09f, 0.15f, Fbm(x, z, 0.01f, 2, crater.y * 0.017f + 433f));
        float rim = rimHeight * breakMask * rimRoughness * Mathf.Exp(-Mathf.Pow((d - 1f) / rimWidth, 2f));
        float ejecta = rimHeight * 0.14f * Mathf.Exp(-Mathf.Pow((d - 1.2f) / 0.17f, 2f));
        return bowl + rim + ejecta;
    }

    private float CraterField(float x, float z, float cellSize, float probability, float depthRatio, float rimRatio, int seed)
    {
        int cellX = Mathf.FloorToInt(x / cellSize);
        int cellZ = Mathf.FloorToInt(z / cellSize);
        float result = 0f;
        for (int dz = -1; dz <= 1; dz++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int cx = cellX + dx;
                int cz = cellZ + dz;
                float spawn = Hash01(cx, cz, seed);
                if (spawn > probability) continue;

                float centerX = (cx + 0.12f + Hash01(cx, cz, seed + 7) * 0.76f) * cellSize;
                float centerZ = (cz + 0.12f + Hash01(cx, cz, seed + 19) * 0.76f) * cellSize;
                float radius = cellSize * Mathf.Lerp(0.12f, 0.38f, Hash01(cx, cz, seed + 31));
                if (Mathf.Abs(centerZ) - radius * 1.32f <= roadBlendWidth * 0.5f + 35f) continue;
                float angle = Hash01(cx, cz, seed + 43) * Mathf.PI * 2f;
                float localX = x - centerX;
                float localZ = z - centerZ;
                float rx = (localX * Mathf.Cos(angle) - localZ * Mathf.Sin(angle)) / radius;
                float rz = (localX * Mathf.Sin(angle) + localZ * Mathf.Cos(angle)) / radius;
                float craterAngle = Mathf.Atan2(rz, rx);
                float distance = Mathf.Sqrt(rx * rx + rz * rz);
                if (distance >= 1.32f) continue;

                float age = Hash01(cx, cz, seed + 71);
                float gapCenter = Hash01(cx, cz, seed + 83) * Mathf.PI * 2f - Mathf.PI;
                float gapDistance = Mathf.Abs(Mathf.DeltaAngle(craterAngle * Mathf.Rad2Deg, gapCenter * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                bool intactRim = Hash01(cx, cz, seed + 91) > 0.72f;
                float gapMask = intactRim ? 1f : Mathf.Lerp(0.025f, 1f, Mathf.SmoothStep(0.2f, 0.68f, gapDistance));
                float depth = radius * depthRatio * Mathf.Lerp(0.55f, 1f, age);
                float rimRoughness = Mathf.Lerp(0.58f, 1.34f, Fbm(x, z, 0.022f, 3, seed + spawn * 37f));
                float rim = radius * rimRatio * Mathf.Lerp(0.55f, 1f, age) * rimRoughness * gapMask;
                float softenedBowl = -depth * Mathf.Pow(Mathf.Clamp01(1f - distance), Mathf.Lerp(1.7f, 2.5f, age));
                float rimWidth = Mathf.Lerp(0.09f, 0.15f, Fbm(x, z, 0.013f, 2, seed + 19f));
                float rimShape = rim * Mathf.Exp(-Mathf.Pow((distance - 1f) / rimWidth, 2f));
                result += softenedBowl + rimShape;
            }
        }
        return result;
    }

    private static float RoughHighland(float x, float z)
    {
        float edge = Mathf.SmoothStep(0.38f, 0.92f, Mathf.Abs(z) / 1000f);
        float brokenMask = Mathf.SmoothStep(0.4f, 0.72f, Fbm(x, z, 0.00085f, 3, 205.4f));
        float broadRidge = RidgedNoise(x, z, 0.00115f, 3, 64.2f);
        return edge * brokenMask * (22f + broadRidge * 54f);
    }

    private static float Hash01(int x, int z, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + z * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h & 0x00ffffffu) / 16777215f;
        }
    }

    private static float PeakHeight(float x, float z, Vector4 peak)
    {
        float aspect = 0.38f + Mathf.Repeat(Mathf.Abs(peak.x) * 0.0137f, 0.43f);
        float angle = Mathf.Repeat(Mathf.Abs(peak.y) * 0.021f, 2f * Mathf.PI);
        float localX = x - peak.x;
        float localZ = z - peak.y;
        float rotatedX = localX * Mathf.Cos(angle) - localZ * Mathf.Sin(angle);
        float rotatedZ = localX * Mathf.Sin(angle) + localZ * Mathf.Cos(angle);
        float dx = rotatedX / peak.z;
        float dz = rotatedZ / (peak.z * aspect);
        float d = Mathf.Sqrt(dx * dx + dz * dz);
        if (d >= 1.35f) return 0f;
        float body = Mathf.Pow(Mathf.Clamp01(1f - d / 1.35f), 2.15f);
        float asymmetricSlope = Mathf.Clamp(0.78f + dx * 0.11f - dz * 0.08f, 0.62f, 0.94f);
        float brokenSilhouette = 0.8f + Fbm(x, z, 0.009f, 3, peak.y * 0.031f) * 0.42f;
        float shoulderX = dx - Mathf.Sin(angle * 1.7f) * 0.34f;
        float shoulderZ = dz + Mathf.Cos(angle * 1.3f) * 0.27f;
        float shoulder = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(shoulderX * shoulderX + shoulderZ * shoulderZ) / 0.82f), 2.7f);
        float gentleVariation = 0.88f + Fbm(x, z, 0.0038f, 3, peak.x * 0.019f) * 0.2f;
        return peak.w * (body * asymmetricSlope * brokenSilhouette + shoulder * 0.2f) * gentleVariation;
    }

    private static Vector2 DomainWarp(float x, float z)
    {
        float wx = (Mathf.PerlinNoise(x * 0.0017f + 12.3f, z * 0.0017f + 41.8f) - 0.5f) * 170f;
        float wz = (Mathf.PerlinNoise(x * 0.0019f + 87.1f, z * 0.0019f + 9.6f) - 0.5f) * 150f;
        return new Vector2(x + wx, z + wz);
    }

    private static float RidgedNoise(float x, float z, float frequency, int octaves, float seed)
    {
        float sum = 0f, amplitude = 0.5f, normalization = 0f;
        for (int i = 0; i < octaves; i++)
        {
            float noise = Mathf.PerlinNoise(x * frequency + seed, z * frequency + seed * 0.61f);
            float ridge = 1f - Mathf.Abs(noise * 2f - 1f);
            sum += ridge * ridge * amplitude;
            normalization += amplitude;
            amplitude *= 0.48f;
            frequency *= 2.13f;
        }
        return sum / normalization;
    }

    private void SmoothHeightmap(float[,] heights, int passes, float roadHeight, float flatHalfWidth)
    {
        int size = heights.GetLength(0);
        float[,] buffer = new float[size, size];
        for (int pass = 0; pass < passes; pass++)
        {
            for (int z = 0; z < size; z++)
            {
                float worldZ = z / (float)(size - 1) * terrainSize.z - terrainSize.z * 0.5f;
                for (int x = 0; x < size; x++)
                {
                    if (Mathf.Abs(worldZ) <= flatHalfWidth)
                    {
                        buffer[z, x] = roadHeight;
                        continue;
                    }

                    float sum = heights[z, x] * 4f;
                    float weight = 4f;
                    for (int oz = -1; oz <= 1; oz++)
                    {
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            if (ox == 0 && oz == 0) continue;
                            int sampleX = Mathf.Clamp(x + ox, 0, size - 1);
                            int sampleZ = Mathf.Clamp(z + oz, 0, size - 1);
                            float sampleWeight = ox == 0 || oz == 0 ? 2f : 1f;
                            sum += heights[sampleZ, sampleX] * sampleWeight;
                            weight += sampleWeight;
                        }
                    }
                    buffer[z, x] = sum / weight;
                }
            }

            float[,] swap = heights;
            heights = buffer;
            buffer = swap;
        }

        // An even pass count leaves the final data in the original array.
    }

    private static float Fbm(float x, float z, float frequency, int octaves, float seed)
    {
        float sum = 0f, amplitude = 0.5f, normalization = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += Mathf.PerlinNoise(x * frequency + seed, z * frequency + seed * 0.71f) * amplitude;
            normalization += amplitude;
            amplitude *= 0.5f;
            frequency *= 2.05f;
        }
        return sum / normalization;
    }
}
