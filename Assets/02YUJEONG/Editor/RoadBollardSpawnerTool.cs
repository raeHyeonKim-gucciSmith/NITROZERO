using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace YUJEONG
{
    /// <summary>
    /// 도로를 따라 선택한 유도봉(2개 쌍)을 지정된 X 간격(-58 등)으로 도로 끝(-2000 등)까지 자동 복사/배치해주는 에디터 툴
    /// </summary>
    public class RoadBollardSpawnerTool : EditorWindow
    {
        [MenuItem("Tools/YUJEONG/🛣️ avoidMissile 도로 유도봉 깔기 (1940 -> -2000, -58간격) %#b")]
        public static void QuickSpawnRoadBollardsAvoidMissile()
        {
            RoadBollardSpawnerTool window = OpenWindow();
            window.stepX = -58f;
            window.manualEndX = -2000.05f;
            window.SpawnBollardsToTargetX(-2000.05f, -58f);
        }

        [MenuItem("Tools/YUJEONG/🛣️ 유도봉 자동 배치 전용 윈도우 열기")]
        public static RoadBollardSpawnerTool OpenWindow()
        {
            RoadBollardSpawnerTool window = GetWindow<RoadBollardSpawnerTool>("도로 유도봉 자동배치");
            window.minSize = new Vector2(400, 520);
            window.Show();
            window.RefreshSelectionInfo();
            return window;
        }

        public float stepX = -58f;
        public float manualEndX = -2000.05f;
        public int spawnCount = 68;

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
                if (selected[0].localPosition.z < selected[1].localPosition.z)
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
                float currentX = sourceLeft.localPosition.x;
                
                // avoidMissile 씬 (현재 X: 1940, 목표 끝: -2000, 간격: -58)
                if (Mathf.Abs(currentX - 1940f) < 20f)
                {
                    stepX = -58f;
                    manualEndX = -2000.05f;
                    float dist = currentX - manualEndX;
                    spawnCount = Mathf.CeilToInt(dist / Mathf.Abs(stepX)); // 68
                }
                else
                {
                    float dist = Mathf.Abs(currentX - manualEndX);
                    spawnCount = Mathf.Max(1, Mathf.RoundToInt(dist / Mathf.Abs(stepX)));
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("🛣️ 도로 유도봉 자동 복사 & 배치 툴", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("선택된 유도봉 쌍(2개)을 X값 간격만큼 빼면서 도로 끝까지 완벽하게 깔아줍니다.\n(Ctrl+Z 로 언제든 한 번에 취소 가능)", MessageType.Info);

            EditorGUILayout.Space(10);
            GUILayout.Label("📌 현재 선택된 유도봉 정보", EditorStyles.boldLabel);

            Transform[] selected = Selection.transforms;
            int selCount = (selected != null) ? selected.Length : 0;

            if (selCount < 2)
            {
                EditorGUILayout.HelpBox($"현재 {selCount}개 선택됨. 씬에서 좌우 유도봉 2개를 동시에 선택해 주세요!\n(Inspector 상단에 [2] 가 표시된 상태)", MessageType.Warning);
            }
            else
            {
                float x0 = selected[0].localPosition.x;
                float x1 = selected[1].localPosition.x;
                EditorGUILayout.HelpBox($"✅ 유도봉 2개 선택 확인 완료!\n• {selected[0].name} (X: {x0:F1})\n• {selected[1].name} (X: {x1:F1})\n\n👉 1번째 복사본 X: {x0 + stepX:F1} ({x0:F0} {stepX:F0})\n👉 2번째 복사본 X: {x0 + stepX * 2:F1} ({x0 + stepX:F0} {stepX:F0})", MessageType.None);
            }

            EditorGUILayout.Space(10);
            GUILayout.Label("⚙️ 배치 설정", EditorStyles.boldLabel);

            stepX = EditorGUILayout.FloatField("X 이동 간격 (현재: -58)", stepX);
            manualEndX = EditorGUILayout.FloatField("도로 끝 X 좌표 (현재: -2000)", manualEndX);

            float startX = (sourceLeft != null) ? sourceLeft.localPosition.x : 1940f;
            float totalDist = startX - manualEndX;
            int calculatedPairs = Mathf.Max(1, Mathf.CeilToInt(totalDist / Mathf.Abs(stepX)));

            EditorGUILayout.LabelField("이동 거리:", $"{totalDist:F1} m");
            EditorGUILayout.LabelField("도로 끝까지 필요한 쌍 수:", $"{calculatedPairs} 쌍 ({calculatedPairs * 2}개)");

            EditorGUILayout.Space(15);
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.35f);
            if (GUILayout.Button($"🚀 X {startX:F0}부터 {manualEndX:F0}까지 깔기 ({calculatedPairs}쌍, 단축키: Ctrl+Shift+B)", GUILayout.Height(50)))
            {
                SpawnBollardsToTargetX(manualEndX, stepX);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("➕ 10쌍 깔기 (-580m)", GUILayout.Height(32)))
            {
                SpawnBollardsCount(10);
            }
            if (GUILayout.Button("➕ 20쌍 깔기 (-1160m)", GUILayout.Height(32)))
            {
                SpawnBollardsCount(20);
            }
            if (GUILayout.Button("➕ 35쌍 깔기 (-2030m)", GUILayout.Height(32)))
            {
                SpawnBollardsCount(35);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12);
            if (lastCreatedObjects.Count > 0)
            {
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button($"↩️ 방금 생성한 {lastCreatedObjects.Count}개 즉시 삭제/취소 (Undo)", GUILayout.Height(32)))
                {
                    UndoCreatedObjects();
                }
                GUI.backgroundColor = Color.white;
            }
        }

        public void SpawnBollardsToTargetX(float targetX, float step)
        {
            RefreshSelectionInfo();
            if (sourceLeft == null && sourceRight == null)
            {
                EditorUtility.DisplayDialog("알림", "복사할 유도봉 2개를 먼저 선택해 주세요!", "확인");
                return;
            }

            float startX = sourceLeft != null ? sourceLeft.localPosition.x : sourceRight.localPosition.x;
            float diff = startX - targetX;
            int pairs = Mathf.Max(1, Mathf.CeilToInt(diff / Mathf.Abs(step)));

            stepX = step;
            manualEndX = targetX;

            SpawnBollardsCount(pairs);
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

                    // 인스펙터에 보이는 Transform.localPosition.x 에 정확히 -58 * i 적용
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

            Debug.Log($"[RoadBollardSpawnerTool] ✅ 유도봉 {pairs}쌍 (총 {newObjects.Count}개)이 Local X {stepX} 간격으로 배치되었습니다! (시작: {targets[0].localPosition.x:F1} -> 끝: {targets[0].localPosition.x + stepX * pairs:F1})");
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
    }
}
