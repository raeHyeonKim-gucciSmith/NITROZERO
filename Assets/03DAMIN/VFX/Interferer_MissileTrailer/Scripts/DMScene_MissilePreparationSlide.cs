using UnityEngine;
using Damin.Trailer.MissileCar;

namespace Damin.SceneOnly
{
    // Optional scene-instance behaviour. Never edits a prefab, graph, or shared controller.
    [DisallowMultipleComponent]
    [AddComponentMenu("DAMIN/DM 씬 전용/4번 미사일 앞쪽 준비 이동")]
    public sealed class DMScene_MissilePreparationSlide : MonoBehaviour
    {
        [InspectorName("이 씬의 통합 연출")] public HoodMissileSequenceController sequence;
        [InspectorName("4번 미사일 장착점")] public Transform preparationMount;
        [Min(0),InspectorName("앞쪽 돌출 거리 (cm)")] public float forwardDistanceCm=4;
        [Tooltip("시작 시각과 소요 시간은 통합 연출의 개별 미사일 준비 시간 설정을 사용합니다. 위쪽 이동 높이는 0으로 유지하세요.")]
        [SerializeField,HideInInspector] Vector3 restLocalPosition;
        public bool ManualSimulation {get;set;}
        public void CaptureRestPose(){if(preparationMount)restLocalPosition=preparationMount.localPosition;}
        void OnEnable(){if(sequence&&sequence.launcher)sequence.launcher.onReset.AddListener(ResetPose);}
        void OnDisable(){if(sequence&&sequence.launcher)sequence.launcher.onReset.RemoveListener(ResetPose);if(Application.isPlaying)ResetPose();}
        void LateUpdate()
        {
            if(!Application.isPlaying||ManualSimulation||!sequence)return;
            if(!sequence.isActiveAndEnabled||sequence.State==HoodMissileSequenceController.SequenceState.Idle){ResetPose();return;}
            Sample(sequence.TimeOnSequence);
        }
        public void Sample(float sequenceTime)
        {
            if(!sequence||!sequence.launcher||!preparationMount)return;
            var launcher=sequence.launcher;
            // Only slot 4 is affected; all other slot poses and original launch logic are retained.
            if(launcher.slots==null||launcher.slots.Length<4||launcher.slots[3].mount!=preparationMount)return;
            var m=sequence.motion;
            float amount=m.liftMissile&&m.missileNumber==4?Mathf.SmoothStep(0,1,Mathf.Clamp01((sequenceTime-m.missileLiftStart)/Mathf.Max(.001f,m.missileLiftDuration))):0;
            var direction=launcher.flightDirection?launcher.flightDirection.forward:launcher.vehicleFrame?launcher.vehicleFrame.forward:transform.forward;
            Vector3 worldOffset=direction.normalized*(Mathf.Max(0,forwardDistanceCm)*.01f*amount);
            preparationMount.localPosition=restLocalPosition+(preparationMount.parent?preparationMount.parent.InverseTransformVector(worldOffset):worldOffset);
        }
        public void ResetPose(){if(preparationMount)preparationMount.localPosition=restLocalPosition;}
    }
}
