using UnityEngine;
namespace Damin.VFX.AirDistortion
{
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("DAMIN/Demo/Disposable Cube Flight")]
    public sealed class DemoCubeFlight : MonoBehaviour
    {
        public Transform Cube;
        public BulletAirDistortionVFX AirVFX;
        public Vector3 StartPosition=new Vector3(-5,1,0);
        public Vector3 Direction=Vector3.right;
        [Min(.1f)] public float FlightDistance=10;
        [Min(0)] public float BulletSpeed=60;
        [Range(.01f,1),Tooltip("테스트 전용 슬로 재생. 프로젝트 Time.timeScale은 변경하지 않습니다.")]
        public float PreviewPlaybackSpeed=.12f;
        [Min(0)] public float ReplayDelay=.8f;
        public bool Loop=true,PlayOnStart=true;
        public bool ManualSimulation {get;set;}
        public bool Flying {get;private set;}
        float travelled,wait;bool waiting;
        void Start(){if(PlayOnStart)Fire();}
        void Update(){if(!ManualSimulation)Simulate(Time.deltaTime);}
        [ContextMenu("테스트 큐브 다시 발사")]
        public void Fire(){
            if(!Application.isPlaying||!Cube)return;Cube.position=StartPosition;Cube.rotation=Quaternion.LookRotation(Direction.sqrMagnitude>.001f?Direction.normalized:Vector3.right);
            Cube.gameObject.SetActive(true);travelled=0;wait=0;waiting=false;Flying=true;
            if(AirVFX){AirVFX.SetTarget(Cube);AirVFX.MeasureTargetSpeed=false;AirVFX.BulletSpeed=BulletSpeed;AirVFX.SimulationTimeScale=1;}
        }
        public void Simulate(float dt){
            if(!Cube||dt<=0)return;
            if(AirVFX){AirVFX.BulletSpeed=BulletSpeed;AirVFX.SimulationTimeScale=1;}
            if(Flying){
                travelled=Mathf.Min(FlightDistance,travelled+BulletSpeed*PreviewPlaybackSpeed*dt);
                Cube.position=StartPosition+(Direction.sqrMagnitude>.001f?Direction.normalized:Vector3.right)*travelled;
                if(travelled>=FlightDistance){Flying=false;waiting=true;wait=0;if(AirVFX)AirVFX.Detach();Cube.gameObject.SetActive(false);}
            }else if(waiting&&Loop){wait+=dt;if(wait>=Mathf.Max(ReplayDelay,AirVFX?AirVFX.TrailFade:0))Fire();}
        }
    }
}
