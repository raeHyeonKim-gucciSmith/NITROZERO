using UnityEditor;
using UnityEngine;

namespace YUJEONG
{
    [CustomEditor(typeof(VehicleTransformationController))]
    public class VehicleTransformationControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            VehicleTransformationController controller = (VehicleTransformationController)target;

            // 상단 스타일리시한 미리보기 전용 컨트롤 박스
            EditorGUILayout.Space(6);
            GUIStyle boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 10, 10)
            };

            EditorGUILayout.BeginVertical(boxStyle);

            // 타이틀 헤더
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.4f, 0.85f, 1f) : new Color(0.1f, 0.4f, 0.8f);

            EditorGUILayout.LabelField("🚗 [ 에디터 변신 실시간 미리보기 ]", titleStyle);
            EditorGUILayout.LabelField("게임을 실행하지 않아도 씬 뷰에서 최종 변신 모습을 즉시 확인할 수 있습니다.", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(6);

            // 메인 대형 토글 버튼
            bool isTransformed = controller.PreviewProgress >= 0.99f;
            Color prevBg = GUI.backgroundColor;

            if (isTransformed)
            {
                GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
                if (GUILayout.Button("🔄  기본 원래 모습으로 복귀 (0%)", GUILayout.Height(36)))
                {
                    Undo.RecordObject(controller, "Reset Vehicle Transformation");
                    controller.ResetTransformationPreview();
                    EditorUtility.SetDirty(controller);
                    SceneView.RepaintAll();
                }
            }
            else
            {
                GUI.backgroundColor = new Color(0.4f, 0.95f, 0.55f);
                if (GUILayout.Button("🚀  최종 변신 모습 보기 (100%)", GUILayout.Height(36)))
                {
                    Undo.RecordObject(controller, "Apply Vehicle Transformation");
                    controller.ApplyFinalTransformationPreview();
                    EditorUtility.SetDirty(controller);
                    SceneView.RepaintAll();
                }
            }
            GUI.backgroundColor = prevBg;

            EditorGUILayout.Space(6);

            // 세밀한 진행도 조절 슬라이더 (0% ~ 100%)
            EditorGUI.BeginChangeCheck();
            float currentProgress = controller.PreviewProgress;
            float newProgress = EditorGUILayout.Slider(new GUIContent("변신 진행도", "0%(기본)부터 100%(최종 변신)까지 실시간으로 파츠 움직임을 조절합니다."), currentProgress, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(controller, "Change Transformation Progress");
                controller.PreviewProgress = newProgress;
                EditorUtility.SetDirty(controller);
                SceneView.RepaintAll();
            }

            // 빠른 단계 버튼들 (0%, 25%, 50%, 75%, 100%)
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("0% 기본", EditorStyles.miniButtonLeft))
            {
                Undo.RecordObject(controller, "Set Progress 0%");
                controller.PreviewProgress = 0f;
                EditorUtility.SetDirty(controller);
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("25%", EditorStyles.miniButtonMid))
            {
                Undo.RecordObject(controller, "Set Progress 25%");
                controller.PreviewProgress = 0.25f;
                EditorUtility.SetDirty(controller);
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("50%", EditorStyles.miniButtonMid))
            {
                Undo.RecordObject(controller, "Set Progress 50%");
                controller.PreviewProgress = 0.5f;
                EditorUtility.SetDirty(controller);
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("75%", EditorStyles.miniButtonMid))
            {
                Undo.RecordObject(controller, "Set Progress 75%");
                controller.PreviewProgress = 0.75f;
                EditorUtility.SetDirty(controller);
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("100% 최종", EditorStyles.miniButtonRight))
            {
                Undo.RecordObject(controller, "Set Progress 100%");
                controller.PreviewProgress = 1f;
                EditorUtility.SetDirty(controller);
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("🔧 부품_연결부2 원래 자리 즉시 복구 (좌측 완벽 대칭)", GUILayout.Height(26)))
            {
                CouplerDiagnosticTool.ForceRestoreCouplerRight();
            }

            // 부스터 VFX 이펙트 빠른 수동 테스트 버튼
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔥 보조 4종(pf_2) ON", EditorStyles.miniButtonLeft))
            {
                controller.SetSubBoosterFxActive(true);
            }
            if (GUILayout.Button("💥 메인(pf_1) ON", EditorStyles.miniButtonMid))
            {
                controller.SetMainBoosterFxActive(true);
            }
            if (GUILayout.Button("🌑 이펙트 전체 OFF", EditorStyles.miniButtonRight))
            {
                controller.SetSubBoosterFxActive(false);
                controller.SetMainBoosterFxActive(false);
            }
            EditorGUILayout.EndHorizontal();

            // 파츠 바인딩 상태 확인 및 자동 연결 버튼
            bool hasMissingParts = controller.ventLeft == null || controller.ventRight == null ||
                                   controller.scanner3D == null || controller.couplerLeft == null ||
                                   controller.couplerRight == null || controller.armoredCowl == null ||
                                   controller.boosterNozzle == null || controller.mainCover == null;

            if (hasMissingParts)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox("일부 파츠 레퍼런스가 비어 있습니다. 아래 버튼을 누르면 차량 하위 파츠가 자동으로 연결됩니다.", MessageType.Warning);
                if (GUILayout.Button("🔍 차량 파츠 자동 연결 (Auto-Bind Parts)", GUILayout.Height(26)))
                {
                    Undo.RecordObject(controller, "Auto Bind Vehicle Parts");
                    controller.AutoBindParts();
                    EditorUtility.SetDirty(controller);
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(8);

            // 기본 인스펙터 속성들 표시
            DrawDefaultInspector();
        }
    }
}
