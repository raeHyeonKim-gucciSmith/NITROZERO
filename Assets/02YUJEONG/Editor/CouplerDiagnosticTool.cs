using System.IO;
using UnityEditor;
using UnityEngine;

namespace YUJEONG
{
    [InitializeOnLoad]
    public static class CouplerDiagnosticTool
    {
        static CouplerDiagnosticTool()
        {
            EditorApplication.delayCall += AutoDiagnoseAndRestore;
        }

        private static void AutoDiagnoseAndRestore()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying) return;
            DiagnoseAndRestore(false);
        }

        [MenuItem("Tools/YUJEONG/연결부2 완벽 복구 (원래 위치 & 회전)")]
        public static void ForceRestoreCouplerRight()
        {
            DiagnoseAndRestore(true);
        }

        public static void DiagnoseAndRestore(bool forceDirty)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            {
                return;
            }

            GameObject c1 = GameObject.Find("부품_연결부1");
            GameObject c2 = GameObject.Find("부품_연결부2");

            if (c2 == null)
            {
                return;
            }

            Undo.RecordObject(c2.transform, "Restore Coupler2");

            // 연결부1의 대칭 좌표 계산 (기준: X 대칭)
            Vector3 targetPos = (c1 != null)
                ? new Vector3(-c1.transform.localPosition.x, c1.transform.localPosition.y, c1.transform.localPosition.z)
                : new Vector3(0.441f, -0.048f, -1.481f);

            c2.transform.localPosition = targetPos;
            c2.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            c2.transform.localScale = new Vector3(-0.3367273f, 0.3367273f, 0.3367273f);

            // VehicleTransformationController 프리뷰 상태 초기화
            VehicleTransformationController vtc = Object.FindFirstObjectByType<VehicleTransformationController>();
            if (vtc != null)
            {
                Undo.RecordObject(vtc, "Reset VTC Preview");
                vtc.ResetTransformationPreview();
                vtc.AutoBindBoosterFx();
                EditorUtility.SetDirty(vtc);
            }

            EditorUtility.SetDirty(c2);
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying && c2.scene.IsValid() && c2.scene.isLoaded)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(c2.scene);
            }
            SceneView.RepaintAll();

            Debug.Log($"[CouplerDiagnostic] ✅ 연결부2 복구 완료! Pos: {c2.transform.localPosition:F5}, Rot: {c2.transform.localEulerAngles:F3}, Scale: {c2.transform.localScale:F5}");
        }
    }
}
