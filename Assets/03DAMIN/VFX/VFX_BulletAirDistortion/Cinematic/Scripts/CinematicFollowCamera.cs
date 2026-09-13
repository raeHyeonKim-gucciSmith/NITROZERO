using UnityEngine;

namespace Damin.VFX.AirDistortion.Cinematic
{
    // Demo movement runs at -100, camera at 0, refraction mesh generation at 100.
    [DefaultExecutionOrder(0),DisallowMultipleComponent,RequireComponent(typeof(Camera))]
    public sealed class CinematicFollowCamera : MonoBehaviour
    {
        [Header("옆모습 유지 / 카메라만 이동")]
        [Tooltip("VFX의 따라갈 총알을 그대로 따라갑니다. 실제 총알로 교체해도 자동 연결됩니다.")]
        public CinematicAirDistortion AirVFX;
        [Tooltip("총알 기준 카메라의 고정 월드 위치 차이. 시야각이나 총알 이동 속도는 변경하지 않습니다.")]
        public Vector3 FollowOffset=new Vector3(0,0,-1.6f);
        public bool ManualSimulation {get;set;}
        Quaternion viewRotation;
        void OnEnable(){viewRotation=transform.rotation;}
        void Start(){if(!ManualSimulation)FollowNow();}
        void LateUpdate(){if(!ManualSimulation)FollowNow();}
        public void FollowNow(){
            if(!isActiveAndEnabled||!AirVFX)return;
            var target=AirVFX.Target;
            if(!target||!target.gameObject.activeInHierarchy)return;
            transform.SetPositionAndRotation(target.position+FollowOffset,viewRotation);
        }
    }
}
