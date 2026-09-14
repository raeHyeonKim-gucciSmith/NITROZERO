using System;
using UnityEngine;
using Unity.Cinemachine;
using Damin.Trailer.MissileCar;

namespace Damin.SceneOnly
{
    /// <summary>Scene-only shot selection. Fixed cameras remain fixed; no automatic story choreography.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(2000)]
    public sealed class DMShotCameraRig : MonoBehaviour
    {
        public enum ShotMode { FollowRoadFrame, FixedWorld, FollowVehicleFrame }
        [Serializable]
        public sealed class Shot
        {
            public string label;
            public CinemachineCamera camera;
            public Transform target;
            public ShotMode mode;
            [Tooltip("X 좌우, Y 높이, Z 앞뒤. 단위 m. 고정 카메라는 CM 오브젝트 Transform을 직접 조절합니다.")]
            public Vector3 positionOffset;
            public Vector3 aimOffset;
            public bool followFiredMissile;
            public bool overhead;
            [Tooltip("36mm 폭·16:9 게이트 상당의 초점거리로 수직 FOV를 계산합니다. 물리 카메라 센서와 별개인 화각 프리셋입니다.")]
            public bool useFocalLength;
            [Range(14,135), Tooltip("렌즈 초점거리(mm). 36mm 폭 / 16:9 기준 화각으로 환산합니다. FOV 각도가 아닙니다.")]
            public float focalLengthMm=35;
            public Vector3 overheadUp;
            [Range(0,.3f),Tooltip("추적 시 아주 작은 카메라 흔들림(m). 고정·탑뷰 샷은 0 권장.")]
            public float vibrationMetres;
            public bool hasBoostPose;
            public Vector3 boostPositionOffset;
            public Vector3 boostAimOffset;
            [NonSerialized] public bool boostPoseActive;
            [NonSerialized] public Vector3 worldPositionOffset;
            [Tooltip("두 차량을 함께 보여줄 때 보조 조준 대상. 카메라 위치는 기본 대상을 따릅니다.")]
            public Transform compositionTarget;
            [Range(0,1)] public float compositionWeight;
            [Tooltip("근접→측면 또는 미사일 추적→상승 구도를 조절합니다. 자동 시간표가 아닙니다.")]
            public bool hasEndPose;
            [Range(0,1)] public float moveProgress;
            public Vector3 endPositionOffset;
            public Vector3 endAimOffset;
            [Tooltip("미사일 상승 구도에서 함께 보여줄 빨간 차량. 두 대상의 중간 지점을 봅니다.")]
            public Transform wideTarget;
            [TextArea] public string productionNote;
        }
        public Camera outputCamera;
        public CinemachineBrain brain;
        public DMRaceOpening opening;
        public HoodMissileSequenceController missileSequence;
        [Range(1,10)] public int activeShot=1;
        public Vector3 roadForward=Vector3.left;
        public Shot[] shots=new Shot[10];
        [Header("첫 두 장면 자동 재생")]
        [Tooltip("Play 시작 시 01 → 02만 자동 전환합니다. 다른 샷 버튼을 누르면 수동 확인으로 전환합니다.")]
        public bool autoPlayOpening;
        [Min(.1f), Tooltip("차량 주행 시작부터 2번 후방 화면으로 자르는 시간(초). 속도나 카메라 위치를 바꾸면 함께 조정하세요.")]
        public float firstShotDuration = 1.75f;
        public bool OpeningPlaybackActive { get; private set; }
        public bool ManualSimulation { get; set; }
        [NonSerialized] public float motionTime;

        void Start()
        {
            OpeningPlaybackActive = autoPlayOpening;
            if (OpeningPlaybackActive) activeShot = 1;
        }
        public void EvaluateOpeningShot()
        {
            if (autoPlayOpening && OpeningPlaybackActive && opening && opening.isActiveAndEnabled)
                activeShot = opening.ElapsedSeconds < Mathf.Max(.1f, firstShotDuration) ? 1 : 2;
        }
        public void RestartOpeningSequence()
        {
            if (!Application.isPlaying || !opening) return;
            opening.RestartOpening();
            OpeningPlaybackActive = autoPlayOpening;
            activeShot = 1;
            ApplyShotPoses();
        }

        void LateUpdate()
        {
            if(!Application.isPlaying || ManualSimulation) return;
            EvaluateOpeningShot();
            ApplyShotPoses();
            if(brain && brain.enabled) brain.ManualUpdate();
        }
        public void ApplyShotPoses()
        {
            if(shots==null) return;
            var forward=Vector3.ProjectOnPlane(roadForward,Vector3.up).normalized;
            if(forward.sqrMagnitude<.9f) forward=Vector3.forward;
            var roadFrame=Quaternion.LookRotation(forward,Vector3.up);
            activeShot=Mathf.Clamp(activeShot,1,Mathf.Max(1,shots.Length));
            for(int i=0;i<shots.Length;i++)
            {
                var shot=shots[i];if(shot==null||!shot.camera) continue;
                if(shot.useFocalLength){var lens=shot.camera.Lens;lens.FieldOfView=2*Mathf.Atan(20.25f/(2*Mathf.Clamp(shot.focalLengthMm,14,135)))*Mathf.Rad2Deg;shot.camera.Lens=lens;}
                int priority=i==activeShot-1?100:10;
                if(shot.camera.Priority.Value!=priority)
                {
                    shot.camera.Priority=priority;
                    // Refresh the priority queue before this frame's manual Brain update.
                    if(Application.isPlaying && shot.camera.isActiveAndEnabled) shot.camera.Prioritize();
                }
                if(shot.mode==ShotMode.FixedWorld)
                {
                    // Deliberately do not aim at or follow the moving vehicle.
                    shot.camera.Follow=null;shot.camera.LookAt=null;continue;
                }
                var target=shot.followFiredMissile&&missileSequence&&missileSequence.ActiveMissile
                    ?missileSequence.ActiveMissile.transform:shot.target;
                if(!target) continue;
                Quaternion frame=shot.mode==ShotMode.FollowVehicleFrame?target.rotation:roadFrame;
                Vector3 aim=target.position+frame*shot.aimOffset;
                Vector3 position=target.position+frame*shot.positionOffset;
                float progress=shot.hasEndPose?Mathf.SmoothStep(0,1,shot.moveProgress):0;
                if(progress>0)
                {
                    var endAim=target.position+frame*shot.endAimOffset;
                    var endPosition=target.position+frame*shot.endPositionOffset;
                    if(shot.wideTarget)
                    {
                        Vector3 middle=(target.position+shot.wideTarget.position)*.5f;
                        var offset=shot.endPositionOffset;
                        float halfFov=Mathf.Clamp(shot.camera.Lens.FieldOfView,10,120)*Mathf.Deg2Rad*.5f;
                        float distance=Vector3.Distance(target.position,shot.wideTarget.position);
                        offset.y=Mathf.Max(offset.y,distance/(2*Mathf.Tan(halfFov))*1.15f);
                        endAim=middle+frame*shot.endAimOffset;
                        endPosition=middle+frame*offset;
                    }
                    position=Vector3.Lerp(position,endPosition,progress);aim=Vector3.Lerp(aim,endAim,progress);
                }
                if(shot.compositionTarget)aim+=(shot.compositionTarget.position-target.position)*shot.compositionWeight;
                if(shot.hasBoostPose&&shot.boostPoseActive){position=target.position+frame*shot.boostPositionOffset;aim=target.position+frame*shot.boostAimOffset;}
                position+=shot.worldPositionOffset;
                if(Application.isPlaying&&shot.vibrationMetres>0)position+=frame*new Vector3(Mathf.Sin(motionTime*19.3f+i)*shot.vibrationMetres,Mathf.Sin(motionTime*27.1f+i*2)*shot.vibrationMetres*.6f,0);
                var direction=aim-position;if(direction.sqrMagnitude<.000001f) continue;
                var up=shot.overhead?(shot.overheadUp.sqrMagnitude>.1f?shot.overheadUp.normalized:forward):Vector3.up;
                shot.camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(direction,up));
                shot.camera.Follow=target;shot.camera.LookAt=target;
            }
        }
        public void SelectShot(int number)
        {
            if (Application.isPlaying) OpeningPlaybackActive = false;
            activeShot=Mathf.Clamp(number,1,shots==null?10:Mathf.Max(1,shots.Length));ApplyShotPoses();
            if(!Application.isPlaying&&outputCamera&&shots!=null&&activeShot<=shots.Length)
            {
                var shot=shots[activeShot-1];if(shot==null||!shot.camera)return;
                outputCamera.transform.SetPositionAndRotation(shot.camera.transform.position,shot.camera.transform.rotation);
                outputCamera.fieldOfView=shot.camera.Lens.FieldOfView;
            }
        }
    }
}
