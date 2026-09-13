using UnityEngine;
namespace Damin.VFX.AirDistortion
{
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("DAMIN/Demo/Disposable Cube Flight")]
    public sealed class DemoCubeFlight : MonoBehaviour
    {
        [Header("테스트 큐브 전용 / 실제 총알을 연결하면 자동 정지")]
        public Transform Cube;
        public BulletAirDistortionVFX AirVFX;
        [Tooltip("프리팹을 이동·회전하면 발사 경로도 같이 이동합니다.")]
        public bool UseLocalSpace=true;
        public Vector3 StartPosition=new Vector3(-2,0,0);
        public Vector3 Direction=Vector3.right;
        [Min(.1f)] public float FlightDistance=4;
        [Min(0),Tooltip("테스트 큐브의 기준 이동 속도. VFX 왜곡 강도와는 별개입니다.")]
        public float BulletSpeed=60;
        [Range(.001f,1),Tooltip("큐브만 느리게 이동. 왜곡 강도와 프로젝트 Time.timeScale은 변경하지 않습니다.")]
        public float PreviewPlaybackSpeed=.012f;
        [Min(0)] public float ReplayDelay=.8f;
        public bool Loop=true,PlayOnStart=true;
        public bool ManualSimulation {get;set;}
        public bool Flying {get;private set;}
        float travelled,wait;bool waiting;
        bool OwnsTarget=>Cube&&(!AirVFX||AirVFX.Target==Cube);
        Vector3 WorldStart=>UseLocalSpace?transform.TransformPoint(StartPosition):StartPosition;
        Vector3 WorldDirection{
            get{var d=Direction.sqrMagnitude>.001f?Direction.normalized:Vector3.right;
                return UseLocalSpace?transform.TransformDirection(d).normalized:d;}
        }
        void Start(){if(PlayOnStart)Fire();}
        void Update(){if(!ManualSimulation)Simulate(Time.deltaTime);}
        void HideDemo(){Flying=waiting=false;if(Cube)Cube.gameObject.SetActive(false);}
        [ContextMenu("테스트 큐브 다시 발사")]
        public void Fire(){
            if(!Application.isPlaying||!Cube)return;
            // Never steal the real projectile reference or change its movement/settings.
            if(!OwnsTarget){HideDemo();return;}
            Cube.position=WorldStart;Cube.rotation=Quaternion.LookRotation(WorldDirection);
            Cube.gameObject.SetActive(true);travelled=wait=0;waiting=false;Flying=true;
            if(AirVFX)AirVFX.ClearTrail();
        }
        public void Simulate(float dt){
            if(!Cube||dt<=0)return;
            if(!OwnsTarget){HideDemo();return;}
            if(Flying){
                travelled=Mathf.Min(FlightDistance,travelled+Mathf.Max(0,BulletSpeed)*Mathf.Max(0,PreviewPlaybackSpeed)*dt);
                Cube.position=WorldStart+WorldDirection*travelled;
                if(travelled>=FlightDistance){Flying=false;waiting=true;wait=0;Cube.gameObject.SetActive(false);}
            }else if(waiting&&Loop){wait+=dt;if(wait>=Mathf.Max(ReplayDelay,AirVFX?AirVFX.TrailFade:0))Fire();}
        }
        void OnDisable(){HideDemo();}
    }
}
