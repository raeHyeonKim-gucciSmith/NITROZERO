using UnityEngine;
using UnityEditor;

namespace YUJEONG
{
    public class RivalDrivingClipHelper
    {
        [MenuItem("Tools/YUJEONG/🎬 라이벌 운전 애니메이션 복제 및 정상 세팅")]
        public static void SetupRivalDrivingAnimation()
        {
            // 1. Driving (1).fbx 에서 원본 믹사모 운전 클립 찾기
            string fbxPath = "Assets/02YUJEONG/Character/Rival/Driving (1).fbx";
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            AnimationClip originalClip = null;

            foreach (var a in subAssets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    originalClip = clip;
                    break;
                }
            }

            if (originalClip == null)
            {
                EditorUtility.DisplayDialog("오류", $"[{fbxPath}] 에서 애니메이션 클립을 찾을 수 없습니다.", "확인");
                return;
            }

            // 2. 02YUJEONG/Animation 폴더에 완전히 수정 가능한 .anim 클립으로 복제 생성
            string outDir = "Assets/02YUJEONG/Animation";
            if (!AssetDatabase.IsValidFolder(outDir))
            {
                AssetDatabase.CreateFolder("Assets/02YUJEONG", "Animation");
            }

            string outPath = $"{outDir}/Rival_Driving_LookMirror.anim";
            AnimationClip newClip = Object.Instantiate(originalClip);
            newClip.name = "Rival_Driving_LookMirror";
            
            AssetDatabase.CreateAsset(newClip, outPath);
            AssetDatabase.SaveAssets();

            // 3. 씬에서 라이벌 오브젝트 찾기
            GameObject rival = GameObject.Find("라이벌_사이드미러쳐다봄");
            if (rival == null) rival = GameObject.Find("라이벌_기본자세");

            if (rival != null)
            {
                Animator anim = rival.GetComponent<Animator>();
                if (anim == null) anim = Undo.AddComponent<Animator>(rival);

                // 애니메이터 컨트롤러 세팅
                string controllerPath = $"{outDir}/Rival_Controller.controller";
                UnityEditor.Animations.AnimatorController controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
                if (controller == null)
                {
                    controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                    controller.AddMotion(newClip);
                }
                else
                {
                    // 상태 모션 갱신
                    if (controller.layers.Length > 0 && controller.layers[0].stateMachine.states.Length > 0)
                    {
                        controller.layers[0].stateMachine.states[0].state.motion = newClip;
                    }
                    else
                    {
                        controller.AddMotion(newClip);
                    }
                }

                anim.runtimeAnimatorController = controller;

                // 0프레임 샘플링해서 앉은 자세 즉시 복원
                newClip.SampleAnimation(rival, 0f);

                Selection.activeGameObject = rival;
                EditorGUIUtility.PingObject(rival);
            }

            EditorUtility.DisplayDialog("완료", "✅ 라이벌 운전 자세 애니메이션이 성공적으로 준비되었습니다!\n\n이제 캐릭터가 땅에 파묻히지 않고 완벽한 운전 자세로 앉아있습니다.\n\nAnimation 창에서 [Rival_Driving_LookMirror] 클립을 확인해 보세요!", "확인");
        }
    }
}
