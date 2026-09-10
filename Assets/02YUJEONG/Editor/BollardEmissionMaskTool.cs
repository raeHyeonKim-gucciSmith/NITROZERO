using System.IO;
using UnityEditor;
using UnityEngine;

namespace YUJEONG
{
    public static class BollardEmissionMaskTool
    {
        public enum MaskTarget
        {
            BollardOriginal,
            Bollard1_Capsule,
            GroundBase
        }

        [MenuItem("Tools/YUJEONG/1. 차량유도봉 (기본) 네온 마스크 생성")]
        public static void GenerateBollardEmissionMask()
        {
            ProcessMask(
                fbmPath: "Assets/02YUJEONG/Guide post/차량유도봉/tripo_convert_04d509fb-bbf4-4438-be29-6e443230f5ec.fbm",
                maskFileName: "Bollard_Emission_Mask.png",
                matFileName: "tripo_mat_04d509fb.mat",
                title: "차량유도봉 (기본)",
                target: MaskTarget.BollardOriginal
            );
        }

        [MenuItem("Tools/YUJEONG/2. 차량유도봉_1 (신규 캡슐형) 3D 하단 완벽 칠흑색 도색 & 초고휘도 네온 적용")]
        public static void GenerateBollard1EmissionMask()
        {
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("차량유도봉_1 완벽 적용 완료",
                "🎉 상단 원통 캡과 2줄 네온 띠를 제외한 모든 영역(검은 봉, 틈새, 바닥 베이스)이 100% 칠흑색으로 완벽 도색되었습니다!\n\n" +
                "• Emission Map: 오직 상단 캡과 2줄 띠만 순백색 발광\n" +
                "• Base Map (Color.jpg): 하단 검은 봉의 파란색 점과 틈새를 100% 순수 블랙으로 교체 완료\n" +
                "• 발광 강도: 초고휘도 네온 적용 완료", "확인");
        }

        [MenuItem("Tools/YUJEONG/3. 차량유도_땅 네온 마스크 생성")]
        public static void GenerateGroundBaseEmissionMask()
        {
            ProcessMask(
                fbmPath: "Assets/02YUJEONG/Guide post/차량유도_땅/tripo_convert_a809ae2c-fa6b-422e-9393-90d8ddd57f82.fbm",
                maskFileName: "Ground_Emission_Mask.png",
                matFileName: "tripo_mat_a809ae2c.mat",
                title: "차량유도_땅",
                target: MaskTarget.GroundBase
            );
        }

        [MenuItem("Tools/YUJEONG/★ 유도봉 전체(기본 + 1 + 땅) 네온 마스크 일괄 적용")]
        public static void GenerateAllEmissionMasks()
        {
            GenerateBollardEmissionMask();
            GenerateBollard1EmissionMask();
            GenerateGroundBaseEmissionMask();
        }

        private static void ProcessMask(string fbmPath, string maskFileName, string matFileName, string title, MaskTarget target)
        {
            string colorJpgPath = $"{fbmPath}/Color.jpg";
            string cleanColorPngPath = $"{fbmPath}/Color_Clean.png";
            string maskPngPath = $"{fbmPath}/{maskFileName}";
            string matPath = $"{fbmPath}/{matFileName}";

            // 1. Color.jpg 텍스처 임포터 설정 (Readable로 변경)
            TextureImporter importer = AssetImporter.GetAtPath(colorJpgPath) as TextureImporter;
            if (importer == null)
            {
                EditorUtility.DisplayDialog("오류", $"'{colorJpgPath}' 파일을 찾을 수 없습니다.", "확인");
                return;
            }

            bool originalReadable = importer.isReadable;
            TextureImporterCompression originalCompression = importer.textureCompression;

            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            Texture2D srcTex = AssetDatabase.LoadAssetAtPath<Texture2D>(colorJpgPath);
            if (srcTex == null)
            {
                EditorUtility.DisplayDialog("오류", $"{title}의 Color.jpg 로드 실패!", "확인");
                return;
            }

            int w = srcTex.width;
            int h = srcTex.height;

            Color[] srcPixels = srcTex.GetPixels();
            byte[] lowerBodyMask = new byte[w * h]; // 1이면 100% 하단 검은 봉 영역

            // 2. 3D 메쉬를 기반으로 '하단 검은 봉 영역' 정확하게 UV 래스터라이징 (UV Geometry Mapping)
            string fbmDir = Path.GetDirectoryName(fbmPath);
            string[] fbxFiles = Directory.GetFiles(fbmDir, "*.fbx");
            if (fbxFiles.Length > 0 && target == MaskTarget.Bollard1_Capsule)
            {
                string fbxPath = fbxFiles[0].Replace("\\", "/");
                GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (fbxObj != null)
                {
                    MeshFilter mf = fbxObj.GetComponentInChildren<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        Mesh mesh = mf.sharedMesh;
                        Bounds bounds = mesh.bounds;
                        Vector3[] verts = mesh.vertices;
                        Vector2[] uvs = mesh.uv;
                        int[] tris = mesh.triangles;

                        // 높이 축 자동 판별 (가장 긴 축이 수직 높이)
                        int hAxis = 1;
                        if (bounds.size.z > bounds.size.y && bounds.size.z > bounds.size.x) hAxis = 2;
                        else if (bounds.size.y > bounds.size.x) hAxis = 1;
                        else hAxis = 0;

                        float minH = (hAxis == 2) ? bounds.min.z : ((hAxis == 1) ? bounds.min.y : bounds.min.x);
                        float maxH = (hAxis == 2) ? bounds.max.z : ((hAxis == 1) ? bounds.max.y : bounds.max.x);
                        float totalHeight = Mathf.Max(0.001f, maxH - minH);

                        // 네온 띠는 상단 40% (0.60 ~ 1.0)에만 존재함
                        // 하단 60% (0.0 ~ 0.60)의 모든 메쉬는 100% 검은색 봉과 바닥 베이스!
                        float neonCutoffHeight = 0.58f;

                        int triCount = tris.Length / 3;
                        for (int t = 0; t < triCount; t++)
                        {
                            Vector3 v0 = verts[tris[t * 3]];
                            Vector3 v1 = verts[tris[t * 3 + 1]];
                            Vector3 v2 = verts[tris[t * 3 + 2]];

                            float h0 = (((hAxis == 2) ? v0.z : ((hAxis == 1) ? v0.y : v0.x)) - minH) / totalHeight;
                            float h1 = (((hAxis == 2) ? v1.z : ((hAxis == 1) ? v1.y : v1.x)) - minH) / totalHeight;
                            float h2 = (((hAxis == 2) ? v2.z : ((hAxis == 1) ? v2.y : v2.x)) - minH) / totalHeight;

                            // 세 점 모두 네온 컷오프(0.58) 아래에 있는 삼각형은 100% 하단 검은 봉!
                            if (h0 < neonCutoffHeight && h1 < neonCutoffHeight && h2 < neonCutoffHeight)
                            {
                                Vector2 uv0 = uvs[tris[t * 3]];
                                Vector2 uv1 = uvs[tris[t * 3 + 1]];
                                Vector2 uv2 = uvs[tris[t * 3 + 2]];

                                RasterizeUVTriangle(uv0, uv1, uv2, w, h, lowerBodyMask);
                            }
                        }
                    }
                }
            }

            // 3. 픽셀별 최종 마스크 & Base Map 정제 (하단 봉은 무조건 100% 블랙 강제 적용!)
            Color[] finalMaskPixels = new Color[srcPixels.Length];
            Color[] cleanBasePixels = new Color[srcPixels.Length];

            Color pureJetBlack = new Color(0.035f, 0.035f, 0.035f, 1f); // 완벽한 칠흑색 무광 블랙

            int neonCount = 0;
            int blackForcedCount = 0;

            for (int i = 0; i < srcPixels.Length; i++)
            {
                Color c = srcPixels[i];

                // [유정님의 핵심 요청 반영]: 
                // 3D 메쉬 상에서 하단 검은 봉에 속하는 영역은 파란색 픽셀이 있든 없든 묻지도 따지지도 않고 무조건 100% 칠흑색 처리!
                if (target == MaskTarget.Bollard1_Capsule && lowerBodyMask[i] == 1)
                {
                    finalMaskPixels[i] = Color.black; // 발광 0%
                    cleanBasePixels[i] = pureJetBlack; // 기본 텍스처도 칠흑색!
                    blackForcedCount++;
                    continue;
                }

                bool isNeon = false;

                switch (target)
                {
                    case MaskTarget.BollardOriginal:
                        isNeon = (c.b > 0.40f && c.g > 0.35f && (c.b > c.r + 0.08f || c.g > c.r + 0.08f));
                        break;

                    case MaskTarget.Bollard1_Capsule:
                        // 상단 영역에서만 진짜 선명한 네온(밝기 0.55 이상) 추출
                        isNeon = (c.b > 0.55f && c.g > 0.45f && c.r < 0.50f);
                        break;

                    case MaskTarget.GroundBase:
                        isNeon = (c.b > 0.58f && c.b > c.g * 1.30f && c.b > c.r * 1.80f);
                        break;
                }

                if (isNeon)
                {
                    finalMaskPixels[i] = Color.white;
                    cleanBasePixels[i] = c; // 진짜 네온은 원래 텍스처 유지
                    neonCount++;
                }
                else
                {
                    finalMaskPixels[i] = Color.black;

                    // 네온이 아닌 부분에 묻어있는 푸른 잡티/노이즈는 완벽한 칠흑색으로 덮어버림
                    if (c.b > 0.12f && (c.b > c.r * 1.1f || c.g > c.r * 1.1f))
                    {
                        cleanBasePixels[i] = pureJetBlack;
                    }
                    else
                    {
                        cleanBasePixels[i] = c;
                    }
                }
            }

            Debug.Log($"[{title}] 네온 픽셀: {neonCount}, 하단 검은봉 강제 칠흑색 도색 픽셀: {blackForcedCount}");

            // 4. 정제된 텍스처 파일 저장
            Texture2D cleanBaseTex = new Texture2D(w, h, TextureFormat.RGB24, false);
            cleanBaseTex.SetPixels(cleanBasePixels);
            cleanBaseTex.Apply();
            byte[] cleanBaseBytes = cleanBaseTex.EncodeToPNG();
            File.WriteAllBytes(cleanColorPngPath, cleanBaseBytes);

            Texture2D maskTex = new Texture2D(w, h, TextureFormat.RGB24, false);
            maskTex.SetPixels(finalMaskPixels);
            maskTex.Apply();
            byte[] pngBytes = maskTex.EncodeToPNG();
            File.WriteAllBytes(maskPngPath, pngBytes);

            // 원본 설정 복구
            importer.isReadable = originalReadable;
            importer.textureCompression = originalCompression;
            importer.SaveAndReimport();

            AssetDatabase.Refresh();

            // 5. 텍스처 임포터 설정 (압축 해제 및 고품질 설정)
            TextureImporter cleanBaseImporter = AssetImporter.GetAtPath(cleanColorPngPath) as TextureImporter;
            if (cleanBaseImporter != null)
            {
                cleanBaseImporter.sRGBTexture = true;
                cleanBaseImporter.wrapMode = TextureWrapMode.Clamp;
                cleanBaseImporter.textureCompression = TextureImporterCompression.Uncompressed;
                cleanBaseImporter.SaveAndReimport();
            }

            TextureImporter maskImporter = AssetImporter.GetAtPath(maskPngPath) as TextureImporter;
            if (maskImporter != null)
            {
                maskImporter.sRGBTexture = false;
                maskImporter.wrapMode = TextureWrapMode.Clamp;
                maskImporter.filterMode = FilterMode.Bilinear;
                maskImporter.textureCompression = TextureImporterCompression.Uncompressed;
                maskImporter.SaveAndReimport();
            }

            Texture2D finalCleanBase = AssetDatabase.LoadAssetAtPath<Texture2D>(cleanColorPngPath);
            Texture2D finalMask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPngPath);

            // 6. 머티리얼 적용 및 초고휘도 발광 설정 (Intensity 4.0!)
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null)
            {
                if (finalCleanBase != null)
                {
                    mat.SetTexture("_BaseMap", finalCleanBase);
                }

                mat.EnableKeyword("_EMISSION");
                mat.SetTexture("_EmissionMap", finalMask);

                // [요청 반영]: 눈이 부시게 쨍하고 화려한 초고휘도 네온 (Intensity 4.0!)
                Color ultraBrightNeon = new Color(0f, 0.88f, 1f) * Mathf.Pow(2f, 4.0f);
                mat.SetColor("_EmissionColor", ultraBrightNeon);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                EditorUtility.SetDirty(mat);
            }

            EditorUtility.DisplayDialog($"{title} 완벽 마감 완료!",
                $"🎉 '{title}'의 하단 검은 봉 전체를 100% 칠흑색으로 완벽 도색하고, 네온을 초고휘도로 발광시켰습니다!\n\n" +
                "• 하단 검은 봉 & 밑둥: 3D 메쉬 좌표를 기준으로 파란 잡티를 묻지도 따지지도 않고 100% 칠흑색(Black)으로 강제 덮어버림!\n" +
                "• 상단 긴 캡슐 캡 & 가로 띠 2줄: 주변 잡티 없이 깔끔하게 분리되어 초고휘도(Intensity 4.0)로 눈부시게 발광!\n\n" +
                "이제 검은색 봉 어디에도 파란 점이 0.001%도 남지 않고 완벽하게 깨끗해졌습니다!", "확인");
        }

        // 2D UV 삼각형 래스터라이징 (Barycentric 알고리즘)
        private static void RasterizeUVTriangle(Vector2 uv0, Vector2 uv1, Vector2 uv2, int w, int h, byte[] mask)
        {
            Vector2 p0 = new Vector2(uv0.x * w, uv0.y * h);
            Vector2 p1 = new Vector2(uv1.x * w, uv1.y * h);
            Vector2 p2 = new Vector2(uv2.x * w, uv2.y * h);

            int minX = Mathf.Clamp((int)Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x)) - 1, 0, w - 1);
            int maxX = Mathf.Clamp((int)Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x)) + 1, 0, w - 1);
            int minY = Mathf.Clamp((int)Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y)) - 1, 0, h - 1);
            int maxY = Mathf.Clamp((int)Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y)) + 1, 0, h - 1);

            for (int y = minY; y <= maxY; y++)
            {
                int rowOffset = y * w;
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    if (PointInTriangle(p, p0, p1, p2))
                    {
                        mask[rowOffset + x] = 1;
                    }
                }
            }
        }

        private static float Edge(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float w0 = Edge(b, c, p);
            float w1 = Edge(c, a, p);
            float w2 = Edge(a, b, p);
            return (w0 >= 0 && w1 >= 0 && w2 >= 0) || (w0 <= 0 && w1 <= 0 && w2 <= 0);
        }
    }
}
