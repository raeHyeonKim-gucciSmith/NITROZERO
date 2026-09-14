using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace YUJEONG
{
    [CustomEditor(typeof(CameraWindBlastShock))]
    public class CameraWindBlastShockEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CameraWindBlastShock shock = (CameraWindBlastShock)target;

            if (shock.enableKnockdown)
            {
                EditorGUILayout.Space(6);
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                EditorGUILayout.HelpBox("⚠️ 현재 [하늘 보기 전복(Knockdown)]이 켜져 있습니다!\n창문 캠 촬영 시 카메라가 뒤로 넘어가지 않도록 아래 빨간 버튼을 눌러 꺼주세요.", MessageType.Warning);
                if (GUILayout.Button("🛑 [하늘 보기 전복 즉시 끄기 (OFF)]", GUILayout.Height(34)))
                {
                    Undo.RecordObject(shock, "Disable Knockdown");
                    shock.enableKnockdown = false;
                    EditorUtility.SetDirty(shock);
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("✅ 하늘 보기 전복이 [꺼짐(OFF)] 상태입니다. (창문 온보드 촬영 가능)", MessageType.Info);
            }

            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
            if (GUILayout.Button("🛑 [어안렌즈 왜곡 & 헬멧 비네트 1초 완전 제거]", GUILayout.Height(32)))
            {
                AutoCleanHelmetVignette();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(0.2f, 0.85f, 1f);
            if (GUILayout.Button("🚀 [🎬 booston_yujeong_1: 1초 변형 ➔ 카메라 두고 두 차 부와앙- 발진 연출 적용]", GUILayout.Height(38)))
            {
                Undo.RecordObject(shock, "Apply Booster Departure Preset");
                shock.ApplyBoosterTransformationShotPreset();
                EditorUtility.SetDirty(shock);
            }

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.2f, 1f, 0.5f);
            if (GUILayout.Button("⚡ [🎬 도로 조용함 ➔ 진동 고조 ➔ 멀어지며 조용해짐 연출 적용]", GUILayout.Height(34)))
            {
                Undo.RecordObject(shock, "Apply Road PassBy Fade Preset");
                shock.ApplyRoadPassByFadePreset();
                EditorUtility.SetDirty(shock);
            }

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(1f, 0.7f, 0.1f);
            if (GUILayout.Button("🏎️ [차량 창문 온보드 액션캠 프리셋 (속도감 & 떨림)]", GUILayout.Height(28)))
            {
                Undo.RecordObject(shock, "Apply Onboard Window Cam Preset");
                shock.ApplyOnboardWindowCamPreset();
                EditorUtility.SetDirty(shock);
            }

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.2f, 0.85f, 1f);
            if (GUILayout.Button("🎬 [카메라 전복 & 하늘 3초 응시 연출 적용]", GUILayout.Height(28)))
            {
                Undo.RecordObject(shock, "Apply Knockdown Sky 3s Preset");
                shock.ApplyKnockdownSky3sPreset();
                EditorUtility.SetDirty(shock);
            }

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f);
            if (GUILayout.Button("🎬 [8초 도로 질주 프리셋 (고정 촬영)]", GUILayout.Height(28)))
            {
                Undo.RecordObject(shock, "Apply 8s Drive Preset");
                shock.Apply8SecondDrivePreset();
                EditorUtility.SetDirty(shock);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("🎬 시네마틱 연출 테스트", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                GUI.backgroundColor = new Color(0.3f, 1f, 0.4f);
                if (GUILayout.Button("🎬 시네마틱 시퀀스 처음부터 재생 (Space)", GUILayout.Height(36)))
                {
                    shock.StartCinematicSequence();
                }
                EditorGUILayout.Space(4);

                GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
                if (GUILayout.Button("🚀 두 차량 풀 부스터 점등 즉시 적용", GUILayout.Height(28)))
                {
                    shock.ApplyVehiclesFullBoosted();
                }
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.4f, 0.85f, 1f);
            if (GUILayout.Button("🔄 카메라 원위치 리셋", GUILayout.Height(30)))
            {
                shock.ResetCameraTest();
            }

            if (shock.enableKnockdown)
            {
                GUI.backgroundColor = new Color(1f, 0.45f, 0.3f);
                if (GUILayout.Button("💥 풍압 전복 테스트 (하늘 보기)", GUILayout.Height(30)))
                {
                    shock.TriggerKnockdownTest();
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "🎬 [도로 조용함 ➔ 진동 고조 ➔ 멀어지며 조용해짐] 타임라인:\n" +
                "• 0.0s ~ 1.2s (1.2초): 도로 조용함 (카메라가 고요한 도로와 우주 밤하늘을 평온하게 응시)\n" +
                "• 1.2s ~ 3.0s (1.8초): 점점 진동 흔들림 (뒤에서 슈퍼카들의 엔진 굉음이 다가오며 지면 진동이 점점 거세짐)\n" +
                "• 3.0s 시점: 스플라인 출발! 두 차량이 카메라 옆을 굉음과 함께 질주 추월 (폭발적인 피크 진동 & 난기류)\n" +
                "• 3.0s ~ 8.0s (5.0초간): 차들이 지평선 너머로 멀어지면서 진동이 자연스럽게 잦아들어 완전한 고요함으로 복귀!\n" +
                "• 카메라 넘어짐 없음: 카메라가 안정적으로 서서 멀어져가는 차량들을 끝까지 촬영합니다.\n" +
                "• 재생 중 언제든지 스페이스바(Space)를 누르면 처음부터 다시 재생됩니다.",
                MessageType.Info);
        }

        [MenuItem("Tools/YUJEONG/🛑 헬멧 왜곡 및 FirstPersonVignette 완전 제거")]
        public static void AutoCleanHelmetVignette()
        {
            int vignetteCount = 0;
            int hiddenCount = 0;

            // 1. FirstPersonVignette 컴포넌트 모두 찾아서 삭제
            var vignettes = Resources.FindObjectsOfTypeAll<FirstPersonVignette>();
            foreach (var v in vignettes)
            {
                if (v != null && !EditorUtility.IsPersistent(v))
                {
                    Undo.DestroyObjectImmediate(v);
                    vignetteCount++;
                }
            }

            // 2. HideFlags로 숨겨진 "Helmet View Overrides" 등 임시 오브젝트 완전 제거
            var allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allGameObjects)
            {
                if (go != null && !EditorUtility.IsPersistent(go))
                {
                    if (go.name.Contains("Helmet") || go.name.Contains("View Overrides"))
                    {
                        Undo.DestroyObjectImmediate(go);
                        hiddenCount++;
                    }
                }
            }

            // 3. 클린 프로필 에셋 준비
            string cleanProfilePath = "Assets/02YUJEONG/Yujeong_CleanVolumeProfile.asset";
            VolumeProfile cleanProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(cleanProfilePath);

            if (cleanProfile != null)
            {
                // 클린 프로필에서 혹시라도 남아있는 LensDistortion과 Vignette 완전 삭제
                for (int i = cleanProfile.components.Count - 1; i >= 0; i--)
                {
                    var comp = cleanProfile.components[i];
                    if (comp is LensDistortion || comp is Vignette)
                    {
                        cleanProfile.components.RemoveAt(i);
                        Undo.DestroyObjectImmediate(comp);
                    }
                }
                EditorUtility.SetDirty(cleanProfile);
                AssetDatabase.SaveAssets();
            }

            // 4. 씬 내의 모든 Volume에 대해 런타임 인스턴스화된 profile 초기화 및 클린 프로필 강제 할당
            var allVolumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var vol in allVolumes)
            {
                if (vol != null)
                {
                    Undo.RecordObject(vol, "Reset Volume Profile");
                    vol.profile = null; // 런타임에 FirstPersonVignette가 복사해 넣었던 인스턴스 프로필 완전 파기!
                    if (cleanProfile != null)
                    {
                        vol.sharedProfile = cleanProfile;
                    }
                    EditorUtility.SetDirty(vol);
                }
            }

            // 5. 카메라의 물리적 렌즈 왜곡(Barrel Clipping / Curvature) 초기화
            var allCameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var cam in allCameras)
            {
                if (cam != null)
                {
                    Undo.RecordObject(cam, "Reset Physical Camera");
                    cam.usePhysicalProperties = false;
                    EditorUtility.SetDirty(cam);
                }
            }

            // 6. Cinemachine 카메라들의 BarrelClipping 초기화
            var cineCams = Object.FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var cc in cineCams)
            {
                if (cc != null)
                {
                    Undo.RecordObject(cc, "Reset Cinemachine Lens");
                    var lens = cc.Lens;
                    lens.PhysicalProperties.BarrelClipping = 0f;
                    lens.PhysicalProperties.Curvature = Vector2.zero;
                    lens.PhysicalProperties.Anamorphism = 0f;
                    cc.Lens = lens;
                    EditorUtility.SetDirty(cc);
                }
            }

            // 7. 뷰 강제 리프레시
            SceneView.RepaintAll();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

            Debug.Log($"[YUJEONG] 🛑 헬멧 왜곡 정리 완료! (FirstPersonVignette: {vignetteCount}개, 숨김오브젝트: {hiddenCount}개, Volume profile 클리어 완료)");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
}
