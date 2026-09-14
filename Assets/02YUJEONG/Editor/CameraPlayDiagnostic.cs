using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace YUJEONG
{
    public static class CameraPlayDiagnostic
    {
        [MenuItem("Tools/YUJEONG/📷 카메라 설정 진단 (Cinemachine & Volume)")]
        public static void CheckCameraSetup()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var camGo = GameObject.Find("Main Camera");
                if (camGo != null) mainCam = camGo.GetComponent<Camera>();
            }

            if (mainCam == null)
            {
                EditorUtility.DisplayDialog("오류", "Main Camera를 찾을 수 없습니다.", "확인");
                return;
            }

            string report = "=== [카메라 진단 결과] ===\n\n";

            // 1. CinemachineBrain check
            var brain = mainCam.GetComponent("CinemachineBrain");
            if (brain != null)
            {
                report += "1. [CinemachineBrain 감지됨]\n" +
                          "Main Camera에 'CinemachineBrain'이 붙어 있습니다.\n" +
                          "→ CinemachineBrain이 켜져 있으면, 게임 실행(Play) 시 수동으로 맞춰둔 Main Camera의 위치/각도/FOV를 무시하고 가상 카메라(CinemachineCamera)에 맞춰 강제로 덮어씁니다!\n\n";
            }
            else
            {
                report += "1. [CinemachineBrain 없음]\n" +
                          "Main Camera가 Cinemachine의 제어를 받지 않고 있습니다.\n\n";
            }

            // 2. Virtual Cameras in scene
            var vcamTypes = TypeCache.GetTypesDerivedFrom<Component>();
            int vcamCount = 0;
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var c in go.GetComponentsInChildren<Component>(true))
                {
                    if (c == null) continue;
                    string typeName = c.GetType().Name;
                    if (typeName.Contains("CinemachineCamera") || typeName.Contains("CinemachineVirtualCamera"))
                    {
                        vcamCount++;
                        report += $"  • 가상 카메라 발견: [{c.gameObject.name}] (Active: {c.gameObject.activeInHierarchy})\n";
                    }
                }
            }

            // 3. Post Processing Volume
            var volumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
            report += $"\n2. [볼륨/포스트 프로세싱: {volumes.Length}개]\n";
            foreach (var v in volumes)
            {
                report += $"  • Volume: [{v.gameObject.name}], IsGlobal: {v.isGlobal}, Weight: {v.weight}, Profile: {(v.sharedProfile ? v.sharedProfile.name : "None")}\n";
            }

            report += $"\n3. [현재 Main Camera 값]\n" +
                      $"Position: {mainCam.transform.position}\n" +
                      $"Rotation: {mainCam.transform.eulerAngles}\n" +
                      $"FOV: {mainCam.fieldOfView}\n";

            Debug.Log(report);
            EditorUtility.DisplayDialog("카메라 진단 보고서", report, "확인");
        }
    }
}
