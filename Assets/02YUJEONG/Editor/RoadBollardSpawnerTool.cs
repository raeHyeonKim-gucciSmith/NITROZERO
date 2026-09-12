using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace YUJEONG
{
    /// <summary>
    /// 도로를 따라 선택한 유도봉(2개 쌍)을 X값 -17 간격으로 도로 끝까지 자동 복사/배치해주는 에디터 툴
    /// </summary>
    public class RoadBollardSpawnerTool : EditorWindow
    {
        [MenuItem("Tools/YUJEONG/🛣️ 선택한 유도봉 도로 끝까지 깔기 (x -17) %#b")]
        public static void QuickSpawnRoadBollards()
        {
            RoadBollardSpawnerTool window = OpenWindow();
            window.SpawnBollardsToRoadEnd();
        }

        [MenuItem("Tools/YUJEONG/🛣️ 유도봉 자동 배치 전용 윈도우 열기")]
        public static RoadBollardSpawnerTool OpenWindow()
        {
            RoadBollardSpawnerTool window = GetWindow<RoadBollardSpawnerTool>("도로 유도봉 자동배치");
            window.minSize = new Vector2(380, 480);
            window.Show();
            window.RefreshSelectionInfo();
            return window;
        }

        public float stepX = -17f;
        public float detectedEndX = 0f;
        public float manualEndX = 0f;
        public int spawnCount = 30;
        public bool useDetectedEnd = true;

        private Vector3 startPosLeft = Vector3.zero;
        private Vector3 startPosRight = Vector3.zero;
        private Transform sourceLeft = null;
        private Transform sourceRight = null;
        private List<GameObject> lastCreatedObjects = new List<GameObject>();

        private void OnEnable()
        {
            RefreshSelectionInfo();
        }

        private void OnSelectionChange()
        {
            RefreshSelectionInfo();
            Repaint();
        }

        public void RefreshSelectionInfo()
        {
            Transform[] selected = Selection.transforms;
            if (selected == null || selected.Length == 0)
            {
                sourceLeft = null;
                sourceRight = null;
                return;
            }

            if (selected.Length >= 2)
            {
                // Z 좌표 기준으로 좌/우 구분
                if (selected[0].position.z < selected[1].position.z)
                {
                    sourceLeft = selected[0];
                    sourceRight = selected[1];
                }
                else
                {
                    sourceLeft = selected[1];
                    sourceRight = selected[0];
                }
            }
            else
            {
                sourceLeft = selected[0];
                sourceRight = null;
            }

            if (sourceLeft != null)
            {
                startPosLeft = sourceLeft.localPosition;
                if (sourceRight != null)
                {
                    startPosRight = sourceRight.localPosition;
                }

                // 도로 끝 X 자동 감지
                float roadEnd = DetectRoadEndX(sourceLeft.position);
                detectedEndX = roadEnd;
                manualEndX = roadEnd;

                float dist = Mathf.Abs(startPosLeft.x - detectedEndX);
                spawnCount = Mathf.Max(1, Mathf.RoundToInt(dist / Mathf.Abs(stepX)));
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("🛣️ 도로 유도봉 자동 복사 & 배치 툴", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("인스펙터의 Transform.X(로컬 좌표) 값을 기준으로 정확히 -17씩 빼면서 복제합니다.\n예: 2339 -> 2322 -> 2305 -> 2288 ...", MessageType.Info);

            EditorGUILayout.Space(10);
            GUILayout.Label("📌 현재 선택된 유도봉 정보", EditorStyles.boldLabel);

            Transform[] selected = Selection.transforms;
            int selCount = (selected != null) ? selected.Length : 0;

            if (selCount < 2)
            {
                EditorGUILayout.HelpBox($"현재 {selCount}개 선택됨. 씬에서 좌우 유도봉 2개를 동시에 선택해 주세요!\n(Inspector에 [2] 가 표시된 상태)", MessageType.Warning);
            }
            else
            {
                float x0 = selected[0].localPosition.x;
                float x1 = selected[1].localPosition.x;
                EditorGUILayout.HelpBox($"✅ 좌우 유도봉 2개 선택 확인 완료!\n• 오브젝트 1: {selected[0].name} (Local X: {x0:F1})\n• 오브젝트 2: {selected[1].name} (Local X: {x1:F1})\n\n👉 첫 번째 복사본 X: {x0 + stepX:F1} ({x0:F0} - 17)\n👉 두 번째 복사본 X: {x0 + stepX * 2:F1} ({x0 + stepX:F0} - 17)", MessageType.None);
            }

            EditorGUILayout.Space(10);
            GUILayout.Label("⚙️ 배치 설정", EditorStyles.boldLabel);

            stepX = EditorGUILayout.FloatField("X 이동 간격 (기본: -17)", stepX);

            EditorGUILayout.Space(5);
            useDetectedEnd = EditorGUILayout.Toggle("도로 끝 좌표 자동 계산", useDetectedEnd);

            if (useDetectedEnd)
            {
                float startX = (sourceLeft != null) ? sourceLeft.localPosition.x : 2339f;
                float dist = Mathf.Abs(startX - detectedEndX);
                int autoPairs = Mathf.Max(1, Mathf.RoundToInt(dist / Mathf.Abs(stepX)));
                EditorGUILayout.LabelField("감지된 도로 끝 X 좌표:", $"{detectedEndX:F1}");
                EditorGUILayout.LabelField("도로 끝까지 필요한 쌍 수:", $"{autoPairs} 쌍 ({autoPairs * 2}개)");
            }
            else
            {
                manualEndX = EditorGUILayout.FloatField("목표 끝 X 좌표", manualEndX);
                spawnCount = EditorGUILayout.IntField("생성할 쌍 수 (N쌍)", spawnCount);
            }

            EditorGUILayout.Space(15);
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
            if (GUILayout.Button("🚀 도로 끝까지 한 번에 쫙 깔기 (추천)", GUILayout.Height(45)))
            {
                SpawnBollardsToRoadEnd();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("➕ 10쌍 깔기 (-170m)", GUILayout.Height(30)))
            {
                SpawnBollardsCount(10);
            }
            if (GUILayout.Button("➕ 30쌍 깔기 (-510m)", GUILayout.Height(30)))
            {
                SpawnBollardsCount(30);
            }
            if (GUILayout.Button("➕ 50쌍 깔기 (-850m)", GUILayout.Height(30)))
            {
                SpawnBollardsCount(50);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            if (lastCreatedObjects.Count > 0)
            {
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button($"↩️ 방금 생성한 {lastCreatedObjects.Count}개 실행 취소 (Undo)", GUILayout.Height(32)))
                {
                    UndoCreatedObjects();
                }
                GUI.backgroundColor = Color.white;
            }
        }

        public void SpawnBollardsToRoadEnd()
        {
            RefreshSelectionInfo();
            if (sourceLeft == null && sourceRight == null)
            {
                EditorUtility.DisplayDialog("알림", "유니티 씬이나 Hierarchy에서 유도봉 2개를 먼저 선택해 주세요!", "확인");
                return;
            }

            float startX = sourceLeft != null ? sourceLeft.localPosition.x : sourceRight.localPosition.x;
            float targetEnd = useDetectedEnd ? detectedEndX : manualEndX;
            
            // 만약 도로 끝이 감지되지 않았거나 시작점보다 크다면 기본 50쌍(약 850m) 깔기
            float diff = startX - targetEnd;
            int pairsToSpawn = 50;
            if (diff > 1f)
            {
                pairsToSpawn = Mathf.CeilToInt(diff / Mathf.Abs(stepX));
            }

            SpawnBollardsCount(pairsToSpawn);
        }

        public void SpawnBollardsCount(int pairs)
        {
            RefreshSelectionInfo();
            Transform[] targets = Selection.transforms;
            if (targets == null || targets.Length == 0)
            {
                EditorUtility.DisplayDialog("알림", "복사할 유도봉을 선택해 주세요!", "확인");
                return;
            }

            List<GameObject> newObjects = new List<GameObject>();
            Undo.SetCurrentGroupName("Spawn Road Bollards");
            int undoGroup = Undo.GetCurrentGroup();

            for (int i = 1; i <= pairs; i++)
            {
                float offsetX = stepX * i;

                foreach (Transform src in targets)
                {
                    if (src == null) continue;

                    // 유니티 Ctrl+D 와 100% 동일하게 부모 계층 및 세팅 유지 복제
                    GameObject clone = GameObject.Instantiate(src.gameObject, src.parent);

                    // 인스펙터에 보이는 Transform.localPosition.x 에 정확히 -17 * i 적용
                    Vector3 newLocalPos = src.localPosition;
                    newLocalPos.x += offsetX;
                    clone.transform.localPosition = newLocalPos;
                    clone.transform.localRotation = src.localRotation;
                    clone.transform.localScale = src.localScale;

                    // 이름 정리
                    string baseName = src.name;
                    int parenIdx = baseName.IndexOf('(');
                    if (parenIdx > 0) baseName = baseName.Substring(0, parenIdx).Trim();
                    clone.name = $"{baseName}";

                    Undo.RegisterCreatedObjectUndo(clone, "Spawn Road Bollard");
                    newObjects.Add(clone);
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            lastCreatedObjects = newObjects;

            Debug.Log($"[RoadBollardSpawnerTool] ✅ 유도봉 {pairs}쌍 (총 {newObjects.Count}개)이 localPosition.x {stepX} 간격으로 정확히 배치되었습니다! (예: 2339 -> 2322 -> 2305 ...)");
        }

        private void UndoCreatedObjects()
        {
            if (lastCreatedObjects == null || lastCreatedObjects.Count == 0) return;

            Undo.SetCurrentGroupName("Undo Spawned Bollards");
            int group = Undo.GetCurrentGroup();

            foreach (var obj in lastCreatedObjects)
            {
                if (obj != null)
                {
                    Undo.DestroyObjectImmediate(obj);
                }
            }

            Undo.CollapseUndoOperations(group);
            lastCreatedObjects.Clear();
            Debug.Log("[RoadBollardSpawnerTool] ↩️ 생성된 유도봉이 모두 취소(삭제)되었습니다.");
        }

        private static float DetectRoadEndX(Vector3 startPos)
        {
            float minX = startPos.x;
            MeshRenderer[] allRenderers = GameObject.FindObjectsOfType<MeshRenderer>();

            foreach (var r in allRenderers)
            {
                if (r == null) continue;
                string lower = r.gameObject.name.ToLower();

                // 도로 관련 오브젝트이거나 바닥 평면인 경우
                bool isRoad = lower.Contains("road") || lower.Contains("track") || lower.Contains("street") ||
                              lower.Contains("highway") || lower.Contains("bridge") || lower.Contains("asphalt") ||
                              lower.Contains("ground") || lower.Contains("floor") || lower.Contains("lane");

                Bounds b = r.bounds;
                // 유도봉과 비슷한 높이(Y) 및 폭(Z)을 가진 메시
                if ((isRoad || Mathf.Abs(b.center.y - startPos.y) < 10f) && Mathf.Abs(b.center.z - startPos.z) < 60f)
                {
                    if (b.min.x < minX)
                    {
                        minX = b.min.x;
                    }
                }
            }

            // 만약 감지된 minX가 시작점과 같다면 (도로가 긴 하나의 평면이거나 끝이 감지 안될 때)
            // 기본 안전값으로 -500f 또는 시작점에서 1000m 앞 반환
            if (minX >= startPos.x - 10f)
            {
                minX = startPos.x - 850f; // 약 50쌍 분량
            }

            return minX;
        }
    }
}
