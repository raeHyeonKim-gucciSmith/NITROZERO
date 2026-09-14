using UnityEditor;
using UnityEngine;

namespace YUJEONG
{
    [CustomEditor(typeof(BoosterTransformationDirector))]
    public class BoosterTransformationDirectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            BoosterTransformationDirector director = (BoosterTransformationDirector)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🎬 부스터 변형 시네마틱 제어", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                GUI.backgroundColor = new Color(0.25f, 0.9f, 0.4f);
                if (GUILayout.Button("🎬 [주행 시작 ➔ 1초 후 부스터 변형 실행 (Space)]", GUILayout.Height(38)))
                {
                    director.PlaySequence();
                }

                EditorGUILayout.Space(4);
                GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
                if (GUILayout.Button("🚀 [두 차량 부스터 변형 즉시 시작]", GUILayout.Height(30)))
                {
                    director.TriggerBothTransformations();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("▶️ Play 모드를 실행하면 스플라인 주행 시작 후 설정된 대기 시간(1.0초) 뒤에 두 차량이 자동으로 멋지게 부스터 변형합니다.", MessageType.Info);
            }

            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(0.9f, 0.7f, 0.2f);
            if (GUILayout.Button("🔄 [두 차량 기본 미변형 모습 복귀 (0%)]", GUILayout.Height(28)))
            {
                Undo.RecordObject(director, "Reset Vehicles To Normal");
                director.ResetVehiclesToNormal();
            }

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("⚡ [두 차량 즉시 100% 풀 부스터 완료 (Snap)]", GUILayout.Height(28)))
            {
                Undo.RecordObject(director, "Snap Both Vehicles Full Boosted");
                director.SnapBothVehiclesFullBoosted();
            }

            GUI.backgroundColor = Color.white;
        }
    }
}
