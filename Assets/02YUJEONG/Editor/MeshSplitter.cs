#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace YUJEONG.Tools
{
    public class MeshSplitter : Editor
    {
        [MenuItem("GameObject/NITROZERO/수직 핀 딱 2개로 쪼개기 (2-Way Split)", false, 20)]
        [MenuItem("Tools/NITROZERO/수직 핀 딱 2개로 쪼개기 (2-Way Split)")]
        public static void SplitIntoTwo()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("알림", "분리할 원래 수직 핀 오브젝트를 선택해 주세요.", "확인");
                return;
            }

            MeshFilter mf = selected.GetComponent<MeshFilter>() ?? selected.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                EditorUtility.DisplayDialog("오류", "선택한 오브젝트에 MeshFilter(메쉬)가 없습니다.", "확인");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(mf.sharedMesh);
            if (!string.IsNullOrEmpty(assetPath))
            {
                ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer != null && !importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }
            }

            Mesh src = mf.sharedMesh;
            Vector3[] verts = src.vertices;
            int[] tris = src.triangles;
            Vector3[] normals = src.normals;
            Vector2[] uvs = src.uv;
            Vector4[] tangents = src.tangents;

            if (verts.Length == 0 || tris.Length == 0) return;

            Bounds bounds = src.bounds;
            
            // Determine split axis: test X and Z by finding which axis has a larger gap near center
            int bestAxis = 0; // 0 = X, 2 = Z
            float bestSplitValue = 0f;
            float maxGap = -1f;

            int[] testAxes = new int[] { 0, 2 }; // X and Z
            foreach (int axis in testAxes)
            {
                float min = axis == 0 ? bounds.min.x : bounds.min.z;
                float max = axis == 0 ? bounds.max.x : bounds.max.z;
                float center = (min + max) * 0.5f;

                List<float> centroids = new List<float>(tris.Length / 3);
                for (int t = 0; t < tris.Length; t += 3)
                {
                    float c = (verts[tris[t]][axis] + verts[tris[t + 1]][axis] + verts[tris[t + 2]][axis]) / 3f;
                    centroids.Add(c);
                }
                centroids.Sort();

                float rangeMin = min + (max - min) * 0.2f;
                float rangeMax = min + (max - min) * 0.8f;
                float localMaxGap = 0f;
                float splitVal = center;

                for (int i = 0; i < centroids.Count - 1; i++)
                {
                    if (centroids[i] >= rangeMin && centroids[i + 1] <= rangeMax)
                    {
                        float gap = centroids[i + 1] - centroids[i];
                        if (gap > localMaxGap)
                        {
                            localMaxGap = gap;
                            splitVal = (centroids[i] + centroids[i + 1]) * 0.5f;
                        }
                    }
                }

                if (localMaxGap > maxGap)
                {
                    maxGap = localMaxGap;
                    bestAxis = axis;
                    bestSplitValue = splitVal;
                }
            }

            // Group triangles into exactly two lists based on the gap
            List<int> part1Tris = new List<int>();
            List<int> part2Tris = new List<int>();

            for (int t = 0; t < tris.Length; t += 3)
            {
                float c = (verts[tris[t]][bestAxis] + verts[tris[t + 1]][bestAxis] + verts[tris[t + 2]][bestAxis]) / 3f;
                if (c < bestSplitValue)
                {
                    part1Tris.Add(tris[t]);
                    part1Tris.Add(tris[t + 1]);
                    part1Tris.Add(tris[t + 2]);
                }
                else
                {
                    part2Tris.Add(tris[t]);
                    part2Tris.Add(tris[t + 1]);
                    part2Tris.Add(tris[t + 2]);
                }
            }

            if (part1Tris.Count == 0 || part2Tris.Count == 0)
            {
                EditorUtility.DisplayDialog("알림", "두 파츠를 구분할 수 있는 경계를 찾지 못했습니다.", "확인");
                return;
            }

            // Create container
            GameObject rootObj = new GameObject(selected.name + "_2Pieces");
            rootObj.transform.position = selected.transform.position;
            rootObj.transform.rotation = selected.transform.rotation;
            rootObj.transform.localScale = selected.transform.localScale;
            if (selected.transform.parent != null)
            {
                rootObj.transform.SetParent(selected.transform.parent, true);
            }

            MeshRenderer sourceRenderer = selected.GetComponent<MeshRenderer>() ?? selected.GetComponentInChildren<MeshRenderer>();
            Material[] mats = sourceRenderer != null ? sourceRenderer.sharedMaterials : new Material[0];

            string[] names = new string[] { "Fin_Left", "Fin_Right" };
            List<int>[] parts = new List<int>[] { part1Tris, part2Tris };

            for (int p = 0; p < 2; p++)
            {
                List<int> triList = parts[p];
                Dictionary<int, int> oldToNew = new Dictionary<int, int>();
                List<Vector3> newVerts = new List<Vector3>();
                List<Vector3> newNormals = new List<Vector3>();
                List<Vector2> newUVs = new List<Vector2>();
                List<Vector4> newTangents = new List<Vector4>();
                List<int> newTris = new List<int>();

                for (int i = 0; i < triList.Count; i++)
                {
                    int oldIdx = triList[i];
                    if (!oldToNew.TryGetValue(oldIdx, out int newIdx))
                    {
                        newIdx = newVerts.Count;
                        oldToNew[oldIdx] = newIdx;
                        newVerts.Add(verts[oldIdx]);
                        if (normals != null && normals.Length > oldIdx) newNormals.Add(normals[oldIdx]);
                        if (uvs != null && uvs.Length > oldIdx) newUVs.Add(uvs[oldIdx]);
                        if (tangents != null && tangents.Length > oldIdx) newTangents.Add(tangents[oldIdx]);
                    }
                    newTris.Add(newIdx);
                }

                // Compute center of piece for center pivot
                Vector3 center = Vector3.zero;
                for (int v = 0; v < newVerts.Count; v++) center += newVerts[v];
                center /= newVerts.Count;

                for (int v = 0; v < newVerts.Count; v++)
                {
                    newVerts[v] -= center;
                }

                Mesh pieceMesh = new Mesh();
                pieceMesh.name = $"{selected.name}_{names[p]}";
                pieceMesh.SetVertices(newVerts);
                pieceMesh.SetTriangles(newTris, 0);
                if (newNormals.Count > 0) pieceMesh.SetNormals(newNormals);
                else pieceMesh.RecalculateNormals();
                if (newUVs.Count > 0) pieceMesh.SetUVs(0, newUVs);
                if (newTangents.Count > 0) pieceMesh.SetTangents(newTangents);
                else pieceMesh.RecalculateTangents();
                pieceMesh.RecalculateBounds();

                string folder = "Assets/02YUJEONG/Parts/수직_핀";
                if (!AssetDatabase.IsValidFolder(folder)) folder = "Assets/02YUJEONG/Parts";
                string meshAssetPath = $"{folder}/{pieceMesh.name}.asset";
                meshAssetPath = AssetDatabase.GenerateUniqueAssetPath(meshAssetPath);
                AssetDatabase.CreateAsset(pieceMesh, meshAssetPath);

                GameObject pieceGo = new GameObject(names[p]);
                pieceGo.transform.SetParent(rootObj.transform, false);
                pieceGo.transform.localPosition = center;

                MeshFilter pieceMf = pieceGo.AddComponent<MeshFilter>();
                pieceMf.sharedMesh = pieceMesh;

                MeshRenderer pieceMr = pieceGo.AddComponent<MeshRenderer>();
                pieceMr.sharedMaterials = mats;
            }

            AssetDatabase.SaveAssets();
            Selection.activeGameObject = rootObj;
            Undo.RegisterCreatedObjectUndo(rootObj, "Split 2 Pieces");
            selected.SetActive(false);

            EditorUtility.DisplayDialog("완료", "딱 2개(Fin_Left, Fin_Right)로 깔끔하게 분리되었습니다!\n새 오브젝트를 확인해 보세요.", "확인");
        }
    }
}
#endif
