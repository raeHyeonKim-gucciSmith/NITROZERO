using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.IO;

namespace YUJEONG
{
    [InitializeOnLoad]
    public static class HindererDrivingSetupTool
    {
        static HindererDrivingSetupTool()
        {
            EditorApplication.delayCall += DelayedAutoSetup;
        }

        private static void DelayedAutoSetup()
        {
            // Auto run once if target exists in scene
            GameObject hindererRoot = GameObject.Find("방해자_운전기본");
            if (hindererRoot != null)
            {
                PlayableDirector pd = hindererRoot.GetComponent<PlayableDirector>();
                if (pd == null || pd.playableAsset == null)
                {
                    SetupHindererDriving();
                }
            }
        }

        [MenuItem("Tools/YUJEONG/🚗 방해자 운전 기본 자세 및 타임라인 자동 세팅")]
        public static void SetupHindererDriving()
        {
            Debug.Log("=== [YUJEONG] Starting 방해자_운전기본 Setup ===");

            // 1. Find 방해자_운전기본 in active scene
            GameObject hindererRoot = GameObject.Find("방해자_운전기본");
            if (hindererRoot == null)
            {
                if (Selection.activeGameObject != null && Selection.activeGameObject.name.Contains("방해자"))
                {
                    hindererRoot = Selection.activeGameObject;
                }
            }

            if (hindererRoot == null)
            {
                EditorUtility.DisplayDialog("알림", "씬에서 [방해자_운전기본] 오브젝트를 찾을 수 없습니다.\n하이어라키 창에서 해당 오브젝트가 있는지 확인해주세요.", "확인");
                return;
            }

            // 2. Find Character child under 방해자_운전기본
            Transform charTransform = hindererRoot.transform.Find("방해자_기본자세 (1)");
            if (charTransform == null)
            {
                for (int i = 0; i < hindererRoot.transform.childCount; i++)
                {
                    Transform t = hindererRoot.transform.GetChild(i);
                    if (t.Find("mixamorig:Hips") != null)
                    {
                        charTransform = t;
                        break;
                    }
                }
            }

            if (charTransform == null)
            {
                EditorUtility.DisplayDialog("오류", "[방해자_운전기본] 하위에 캐릭터(방해자_기본자세)를 찾을 수 없습니다.", "확인");
                return;
            }

            GameObject charObj = charTransform.gameObject;

            // 3. Ensure Animator on Character child
            Animator anim = charObj.GetComponent<Animator>();
            if (anim == null)
            {
                anim = Undo.AddComponent<Animator>(charObj);
                Debug.Log($"[YUJEONG] Animator added to {charObj.name}");
            }
            anim.applyRootMotion = false;

            // 4. Load Timeline Asset
            string timelinePath = "Assets/02YUJEONG/Character/Hinderer/Hinderer_Driving_Timeline.playable";
            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
            if (timeline == null)
            {
                AssetDatabase.Refresh();
                timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
            }

            if (timeline == null)
            {
                EditorUtility.DisplayDialog("오류", $"타임라인 파일 [{timelinePath}]을 로드할 수 없습니다. 프로젝트 창을 새로고침해주세요.", "확인");
                return;
            }

            // Ensure AvatarMask is linked in track
            string maskPath = "Assets/02YUJEONG/Character/Hinderer/Hinderer_Driving_Mask.mask";
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
            foreach (var outTrack in timeline.GetOutputTracks())
            {
                if (outTrack is AnimationTrack at)
                {
                    at.applyAvatarMask = true;
                    if (mask != null) at.avatarMask = mask;
                    at.trackOffset = TrackOffset.ApplySceneOffsets;
                }
            }
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();

            // 5. Add / Configure PlayableDirector on 방해자_운전기본
            PlayableDirector director = hindererRoot.GetComponent<PlayableDirector>();
            if (director == null)
            {
                director = Undo.AddComponent<PlayableDirector>(hindererRoot);
            }

            Undo.RecordObject(director, "Setup Hinderer Driving Timeline");
            director.playableAsset = timeline;
            director.extrapolationMode = DirectorWrapMode.Loop;
            director.playOnAwake = true;

            // Bind animation track to character Animator
            foreach (var outTrack in timeline.GetOutputTracks())
            {
                if (outTrack is AnimationTrack)
                {
                    director.SetGenericBinding(outTrack, anim);
                    Debug.Log($"[YUJEONG] Bound track '{outTrack.name}' to {charObj.name} (Animator)");
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hindererRoot.scene);

            Selection.activeGameObject = hindererRoot;
            EditorGUIUtility.PingObject(hindererRoot);

            Debug.Log("=== [YUJEONG] ✅ 방해자_운전기본 타임라인 및 마스크 세팅 완료! ===");
            EditorUtility.DisplayDialog("완료", "✅ [방해자_운전기본] 운전 자세 타임라인 세팅이 완료되었습니다!\n\n- 플레이어/라이벌과 동일하게 아바타 마스크(손/발 고정, 상체 미세 진동)가 적용되었습니다.\n- 게임을 재생(Play)하면 설정해두신 운전대 잡은 자세를 유지하며 자연스러운 애니메이션이 진행됩니다.", "확인");
        }
    }
}
