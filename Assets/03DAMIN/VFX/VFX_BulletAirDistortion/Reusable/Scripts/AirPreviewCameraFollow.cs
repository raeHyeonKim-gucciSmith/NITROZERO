using UnityEngine;

namespace Damin.VFX.AirDistortion.Reusable
{
    [DefaultExecutionOrder(9000),DisallowMultipleComponent]
    [AddComponentMenu("DAMIN/VFX/Air Preview Camera Follow (Test Only)")]
    public sealed class AirPreviewCameraFollow : MonoBehaviour
    {
        public ReusableBulletAirDistortion AirVFX;
        [InspectorName("현재 구도 그대로 유지")] public bool KeepCurrentOffset=true;
        [InspectorName("수동 카메라 거리 (월드)")] public Vector3 FollowOffset=new Vector3(-.22f,0,-.8f);
        [InspectorName("OFF 시 원래 카메라 위치 복원")] public bool RestoreOnDisable=true;
        public bool ManualSimulation {get;set;}
        public string Status {get;private set;}="";
        Camera bound;
        Transform tracked;
        Vector3 initialPosition,offset;
        Quaternion initialRotation;
        void LateUpdate(){if(!ManualSimulation)FollowNow();}
        public void FollowNow()
        {
            if(!Application.isPlaying||!isActiveAndEnabled)return;
            if(!AirVFX)AirVFX=GetComponent<ReusableBulletAirDistortion>();
            Camera camera=AirVFX?AirVFX.ViewCamera:null;Transform target=AirVFX?AirVFX.Target:null;
            if(camera!=bound){Release();}
            if(!camera||!target||!target.gameObject.scene.IsValid()){Status="왜곡에 씬 총알과 실제 Camera를 연결하세요.";return;}
            if(!target.gameObject.activeInHierarchy)return;
            if(!bound)
            {
                bound=camera;initialPosition=camera.transform.position;initialRotation=camera.transform.rotation;
                offset=KeepCurrentOffset?initialPosition-target.position:FollowOffset;
            }
            // A new pooled/preview target keeps the previous framing, not its previous screen displacement.
            if(tracked!=target)tracked=target;
            bound.transform.SetPositionAndRotation(target.position+offset,initialRotation);
            Status="현재 회전·FOV 유지하며 위치만 추적 · 다른 카메라 컨트롤러와 동시 사용 금지";
        }
        [ContextMenu("현재 구도를 기준으로 다시 잡기")]
        public void RecaptureFraming(){bound=null;tracked=null;}
        void Release(){if(bound&&RestoreOnDisable)bound.transform.SetPositionAndRotation(initialPosition,initialRotation);bound=null;tracked=null;Status="";}
        void OnDisable(){Release();}
    }
}
