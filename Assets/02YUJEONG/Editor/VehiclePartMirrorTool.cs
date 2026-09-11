using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace YUJEONG
{
    /// <summary>
    /// 차량에 장착된 파츠(샤크핀, 용 비늘, 에어로 파츠 등)를 반대쪽 문/측면에 완벽한 거울 대칭으로 복사해주는 에디터 툴
    /// </summary>
    public class VehiclePartMirrorTool : EditorWindow
    {
        [MenuItem("Tools/YUJEONG/🪞 선택한 파츠 반대쪽 거울 복사 (Mirror) %#m")]
        [MenuItem("GameObject/🚗 차량 반대쪽에 거울 복사 (Mirror Duplicate)", false, 0)]
        public static void MirrorSelectedPartsQuick()
        {
            Transform[] selected = Selection.transforms;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("알림", "반대쪽에 복사할 파츠(오브젝트)를 씬이나 계층구조(Hierarchy)에서 먼저 선택해 주세요!", "확인");
                return;
            }

            int count = MirrorTransforms(selected, null);
            if (count > 0)
            {
                Debug.Log($"[VehiclePartMirrorTool] ✅ {count}개의 파츠가 반대쪽 문에 완벽하게 거울 대칭 복사되었습니다! (Ctrl+Z 로 취소 가능)");
            }
        }

        [MenuItem("Tools/YUJEONG/🪞 파츠 거울 복사 전용 윈도우 열기")]
        public static void OpenWindow()
        {
            VehiclePartMirrorTool window = GetWindow<VehiclePartMirrorTool>("파츠 거울 복사");
            window.minSize = new Vector2(340, 240);
            window.Show();
        }

        public Transform customCarRoot;

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("🚗 차량 파츠 좌우 거울 대칭 복사 툴", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("씬에서 반대쪽 문에 복사하고 싶은 파츠(용 비늘, 지느러미 등)를 선택하고 아래 버튼을 누르면, 차량 중심을 기준으로 반대편에 완벽하게 대칭 복사됩니다.", MessageType.Info);

            EditorGUILayout.Space(10);
            customCarRoot = (Transform)EditorGUILayout.ObjectField("차량 루트 (선택사항)", customCarRoot, typeof(Transform), true);
            if (customCarRoot == null)
            {
                EditorGUILayout.HelpBox("차량 루트를 비워두면 선택한 파츠의 부모 차량(SportCar_1 등)을 자동으로 감지합니다.", MessageType.None);
            }

            EditorGUILayout.Space(15);
            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
            if (GUILayout.Button("🪞 선택한 파츠 반대쪽에 복사하기 (단축키: Ctrl+Shift+M)", GUILayout.Height(42)))
            {
                MirrorSelectedPartsQuick();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);
            int selCount = Selection.transforms != null ? Selection.transforms.Length : 0;
            GUILayout.Label($"현재 선택된 파츠 수: {selCount}개", EditorStyles.miniLabel);
        }

        /// <summary>
        /// 주어진 트랜스폼들을 차량 기준 좌우 거울 대칭 복사합니다.
        /// </summary>
        public static int MirrorTransforms(Transform[] targets, Transform explicitCarRoot)
        {
            if (targets == null || targets.Length == 0) return 0;

            List<GameObject> newClones = new List<GameObject>();
            Undo.SetCurrentGroupName("Mirror Duplicate Vehicle Parts");
            int group = Undo.GetCurrentGroup();

            foreach (Transform src in targets)
            {
                if (src == null) continue;

                // 1. 기준이 될 차량 루트 찾기
                Transform carRoot = explicitCarRoot;
                if (carRoot == null)
                {
                    carRoot = FindBestCarRoot(src);
                }

                if (carRoot == null)
                {
                    Debug.LogWarning($"[VehiclePartMirrorTool] '{src.name}'의 기준 차량 루트를 찾지 못해 월드 좌표(0,0,0) 기준으로 반사합니다.");
                }

                // 2. 차량 루트 기준 상대 위치 계산 및 X축 반사
                Vector3 localPos = (carRoot != null) ? carRoot.InverseTransformPoint(src.position) : src.position;
                Vector3 mirroredLocalPos = new Vector3(-localPos.x, localPos.y, localPos.z);
                Vector3 worldPos = (carRoot != null) ? carRoot.TransformPoint(mirroredLocalPos) : mirroredLocalPos;

                // 3. 차량 루트 기준 상대 회전 계산 및 거울 반사 (M * R * M 변환)
                Quaternion localRot = (carRoot != null) ? Quaternion.Inverse(carRoot.rotation) * src.rotation : src.rotation;
                Quaternion mirroredLocalRot = new Quaternion(localRot.x, -localRot.y, -localRot.z, localRot.w);
                Quaternion worldRot = (carRoot != null) ? carRoot.rotation * mirroredLocalRot : mirroredLocalRot;

                // 4. 오브젝트 복제
                GameObject clone = GameObject.Instantiate(src.gameObject, src.parent);
                clone.name = GenerateMirroredName(src.name);
                clone.transform.position = worldPos;
                clone.transform.rotation = worldRot;
                clone.transform.localScale = src.localScale;

                Undo.RegisterCreatedObjectUndo(clone, "Mirror Duplicate Part");
                newClones.Add(clone);
            }

            Undo.CollapseUndoOperations(group);

            // 새로 복사된 파츠들을 선택 상태로 지정하여 씬 뷰에서 즉시 확인 가능하게 함
            if (newClones.Count > 0)
            {
                Selection.objects = newClones.ToArray();
            }

            return newClones.Count;
        }

        private static Transform FindBestCarRoot(Transform part)
        {
            // 부모 계층을 거슬러 올라가며 차량 이름 검색
            Transform curr = part.parent;
            Transform bestRoot = null;

            while (curr != null)
            {
                string lower = curr.name.ToLower();
                if (lower.Contains("sportcar") || lower.Contains("blue_car") || lower.Contains("car") || lower.Contains("vehicle"))
                {
                    bestRoot = curr;
                    break;
                }
                if (curr.parent == null)
                {
                    // 최상위 루트
                    bestRoot = curr;
                }
                curr = curr.parent;
            }

            if (bestRoot == null)
            {
                // 씬 전체에서 SportCar_1 또는 Blue_Car 탐색
                GameObject car = GameObject.Find("SportCar_1");
                if (car == null) car = GameObject.Find("SportCar_4");
                if (car == null) car = GameObject.Find("Blue_Car");
                if (car != null) bestRoot = car.transform;
            }

            return bestRoot;
        }

        private static string GenerateMirroredName(string origName)
        {
            if (origName.Contains("_L")) return origName.Replace("_L", "_R");
            if (origName.Contains("_l")) return origName.Replace("_l", "_r");
            if (origName.Contains("Left")) return origName.Replace("Left", "Right");
            if (origName.Contains("left")) return origName.Replace("left", "right");
            if (origName.Contains("좌")) return origName.Replace("좌", "우");

            return origName + "_Mirror_R";
        }
    }
}
