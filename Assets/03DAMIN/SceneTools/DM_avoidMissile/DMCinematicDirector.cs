using System;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using Damin.Trailer.MissileCar;
using Damin.VFX.TireSmoke.Progressive;

namespace Damin.SceneOnly
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-500)]
    public sealed class DMCinematicDirector : MonoBehaviour
    {
        [Serializable] public sealed class FilmTiming
        {
            [Min(.1f), InspectorName("01 지면 통과")] public float groundPass=2;
            [Min(.1f), InspectorName("02 후방 질주")] public float rearDeparture=1.5f;
            [Min(.1f), InspectorName("03 파랑 신호 (임시)")] public float blueSignal=.8f;
            [Min(.1f), InspectorName("04 카키 응답 (임시)")] public float khakiResponse=.8f;
            [Min(.1f), InspectorName("05 카키 후퇴")] public float dropBack=1.5f;
            [Min(.1f), InspectorName("06 보넷 준비·발사")] public float preparation=3.6f;
            [Min(.1f), InspectorName("07 미사일 추적·상승")] public float missileChase=1.8f;
            [Min(.1f), InspectorName("08 드리프트 진입")] public float driftEntry=1.2f;
            [Min(.1f), InspectorName("09 탑뷰 창문 접근")] public float windowApproach=.8f;
            [Min(.1f), InspectorName("10 실내 통과 (대체 가능)")] public float cabinPass=1.2f;
            [Min(.1f), InspectorName("11 탑뷰 이탈")] public float windowExit=.6f;
            [Min(.1f), InspectorName("12 회전 완료")] public float recover=1.4f;
            [Min(.1f), InspectorName("13 부스트 재가속")] public float boost=2.8f;
            public float[] Values()=>new[]{groundPass,rearDeparture,blueSignal,khakiResponse,dropBack,preparation,missileChase,driftEntry,windowApproach,cabinPass,windowExit,recover,boost}.Select(SafeDuration).ToArray();
        }
        public static readonly string[] Labels={"지면 통과","후방 질주","파랑 신호 (임시)","카키 응답 (임시)","카키 후퇴","보넷 준비·발사","미사일 추적·상승","드리프트 진입","탑뷰 창문 접근","실내 통과","탑뷰 이탈","회전 완료","부스트 재가속"};
        [Header("구간 길이 (초) — 변경 후 다시 재생")]
        public FilmTiming timing=new FilmTiming();
        public bool playOnStart=true;
        [Tooltip("첫 구간 길이에 맞춰 01/02를 재생 시작 시 배치합니다. 재생 중에는 고정입니다.")]
        public bool fitOpeningToTiming=true;
        public Vector3 groundCameraOffset;
        public Vector3 rearCameraOffset;
        [Header("촬영용 움직임")]
        [Min(1)] public float raceSpeedKph=90;
        [Min(0)] public float dropBackMetres=14;
        [Min(0)] public float blueLeadMetres=8;
        [Min(0)] public float redDriftLagMetres=5;
        [Range(0,1)] public float driftSideMetres=.3f;
        [Range(.02f,1)] public float slowMotionRate=.08f;
        [Min(0)] public float boostExtraKph=70;
        [Header("창문 통과 — 빨간 차 로컬 좌표")]
        public Transform passengerWindow;
        public Transform driverWindow;
        [Tooltip("실내 모델/창문이 준비된 후 켜세요. 끄면 해당 시간 구간은 탑뷰로 보여줍니다.")]
        public bool interiorCameraReady;
        [Header("이 씬 연결")]
        public DMShotCameraRig cameras;
        public DMRaceOpening opening;
        public HoodMissileSequenceController missileSequence;
        public DMScene_MissilePreparationSlide preparationSlide;
        public Transform redCar,blueCar,khakiCar;
        public TrailerCruiseMotion redCruise;
        public BoosterDeploymentController redBoosterDeployment;
        public ProgressiveTireSmokeController driftSmoke;
        public GameObject[] boostEffects;
        public Renderer[] boostNozzles;
        [TextArea] public string productionNotes="눈빛 연기는 운전자 모델/애니메이션이 필요한 임시 구간입니다. 빨간 차 실내는 준비 전이므로 기본 탑뷰 대체입니다. 창문 조절점은 촬영용 경로이지 물리 충돌 검증이 아닙니다.";

        public float FilmTime {get;private set;}
        public float FilmDuration {get;private set;}
        public int Phase {get;private set;}
        public bool Playing {get;private set;}
        public bool Completed {get;private set;}
        public bool ManualSimulation {get;set;}
        public string Error {get;private set;}
        public float[] ActiveDurations=>durations==null?timing.Values():(float[])durations.Clone();
        float[] durations,starts;
        Vector3[] initialPositions;
        Quaternion[] initialRotations;
        Vector3 forward,right,passLocal,exitLocal,launchRelative;
        float speed,retreat,lead,lag,side,slow,boostSpeed,baseFireTime;
        float previousSeqTime,bodySeconds,boosterRest;
        bool acquired,launched,sequenceStarted,oldOpeningManual,oldSequenceManual,oldPrepManual,oldAuto,oldPlayOnStart;
        bool useInterior;
        GameObject activeProjectile;
        void Awake(){Acquire();}
        void Start()
        {
            if(!Validate())return;
            initialPositions=opening.vehicles.Select(t=>t.position).ToArray();
            initialRotations=opening.vehicles.Select(t=>t.rotation).ToArray();
            boosterRest=redBoosterDeployment?redBoosterDeployment.DeployAmount:0;
            if(playOnStart)Replay();
        }
        void Acquire()
        {
            if(acquired||!opening||!cameras||!missileSequence)return;
            oldOpeningManual=opening.ManualSimulation;oldSequenceManual=missileSequence.ManualSimulation;
            oldAuto=cameras.autoPlayOpening;oldPlayOnStart=missileSequence.playback.playOnStart;
            opening.ManualSimulation=true;cameras.autoPlayOpening=false;
            missileSequence.ManualSimulation=true;missileSequence.playback.playOnStart=false;
            if(preparationSlide){oldPrepManual=preparationSlide.ManualSimulation;preparationSlide.ManualSimulation=true;}
            acquired=true;
        }
        public static float SafeDuration(float v)=>float.IsNaN(v)||float.IsInfinity(v)?1:Mathf.Clamp(v,.1f,120);
        static float Safe(float v,float fallback,float min,float max)=>float.IsNaN(v)||float.IsInfinity(v)?fallback:Mathf.Clamp(v,min,max);
        bool Validate()
        {
            if(!opening||!cameras||!missileSequence||!redCar||!blueCar||!khakiCar||!passengerWindow||!driverWindow||
                opening.vehicles==null||opening.vehicles.Any(t=>!t)||!opening.vehicles.Contains(redCar)||!opening.vehicles.Contains(blueCar)||!opening.vehicles.Contains(khakiCar)||
                cameras.shots==null||cameras.shots.Length<10||cameras.shots.Any(s=>s==null||!s.camera))
                Error="촬영 연결이 없습니다. Director의 차량·카메라·창문 조절점을 확인하세요.";
            else Error=missileSequence.ValidationError();
            if(Error!=null){Playing=false;Debug.LogWarning("[DM 촬영] "+Error,this);return false;}return true;
        }
        public void Replay()
        {
            if(!Application.isPlaying||!Validate())return;
            Acquire();if(initialPositions==null)return;
            missileSequence.ResetAll();
            for(int i=0;i<initialPositions.Length;i++)opening.vehicles[i].SetPositionAndRotation(initialPositions[i],initialRotations[i]);
            durations=timing.Values();starts=new float[14];for(int i=0;i<13;i++)starts[i+1]=starts[i]+durations[i];
            FilmDuration=starts[13];FilmTime=0;Phase=0;Completed=false;Playing=true;sequenceStarted=launched=false;activeProjectile=null;previousSeqTime=0;
            forward=Vector3.ProjectOnPlane(opening.roadForward,Vector3.up).normalized;if(forward.sqrMagnitude<.5f)forward=Vector3.left;
            right=Vector3.Cross(Vector3.up,forward);
            speed=Safe(raceSpeedKph,90,1,250)/3.6f;retreat=Safe(dropBackMetres,14,3,40);lead=Safe(blueLeadMetres,8,3,30);
            lag=Safe(redDriftLagMetres,5,0,10);side=Safe(driftSideMetres,.3f,0,1);slow=Safe(slowMotionRate,.08f,.02f,1);boostSpeed=Safe(boostExtraKph,70,0,150)/3.6f;
            passLocal=redCar.InverseTransformPoint(passengerWindow.position);exitLocal=redCar.InverseTransformPoint(driverWindow.position);
            useInterior=interiorCameraReady;
            baseFireTime=missileSequence.motion.fireTime;
            if(fitOpeningToTiming)
            {
                var frame=Quaternion.LookRotation(forward,Vector3.up);
                float convoy=initialPositions.Max(p=>Vector3.Dot(redCar.position-p,forward));
                float passDistance=Mathf.Max(6,speed*durations[0]-convoy);
                var a=cameras.shots[0].camera.transform;var b=cameras.shots[1].camera.transform;
                a.position=redCar.position+frame*(new Vector3(7.5f,-.33f,passDistance)+groundCameraOffset);
                a.LookAt(redCar.position+frame*new Vector3(0,.45f,passDistance-10));
                b.position=redCar.position+frame*(new Vector3(6.5f,.5f,Mathf.Max(0,speed*durations[0]-28))+rearCameraOffset);
                b.LookAt(redCar.position+frame*new Vector3(0,.1f,speed*durations[0]+8));
            }
            if(driftSmoke){driftSmoke.StopPreview();driftSmoke.SetSmokePower(0);}
            if(boostEffects!=null)foreach(var g in boostEffects)if(g)g.SetActive(false);
            if(redBoosterDeployment){redBoosterDeployment.SetDeployAmount(boosterRest);redBoosterDeployment.PreviewSequence();}
            foreach(var v in khakiCar.GetComponentsInChildren<VisualEffect>(true)){v.pause=false;v.Reinit();}
            ApplyFrame(0);
        }
        void Update(){if(Playing&&!ManualSimulation)Advance(Time.deltaTime);}
        public void Advance(float dt)
        {
            if(!Playing||!float.IsFinite(dt)||dt<=0)return;
            FilmTime=Mathf.Min(FilmDuration,FilmTime+dt);ApplyFrame(FilmTime);
            if(FilmTime>=FilmDuration){Playing=false;Completed=true;SetVfxRates(0);}
        }
        float U(int p,float t)=>Mathf.Clamp01((t-starts[p])/durations[p]);
        static float Ease(float u)=>u*u*(3-2*u);
        static float EaseIntegral(float u)=>u*u*u-.5f*u*u*u*u;
        float WorldSeconds(float t)
        {
            float total=0;
            for(int p=0;p<13;p++){
                float u=U(p,t),a=1,b=1;
                if(p==7)b=slow;else if(p>=8&&p<=10)a=b=slow;else if(p==11){a=slow;b=1;}
                total+=durations[p]*(a*u+(b-a)*EaseIntegral(u));
            }return total;
        }
        float Rate(int p,float u)=>p==7?Mathf.Lerp(1,slow,Ease(u)):p>=8&&p<=10?slow:p==11?Mathf.Lerp(slow,1,Ease(u)):1;
        void ApplyFrame(float t)
        {
            Phase=12;for(int p=0;p<13;p++)if(t<starts[p+1]){Phase=p;break;}
            float u=U(Phase,t),world=WorldSeconds(t),rate=Rate(Phase,u);
            bodySeconds=world;
            for(int i=0;i<opening.vehicles.Length;i++)opening.vehicles[i].SetPositionAndRotation(initialPositions[i]+forward*(speed*world),initialRotations[i]);
            float drop=Ease(U(4,t));
            khakiCar.position-=forward*(retreat*drop);
            // Move into the lead car's lane only after opening a longitudinal gap.
            float align=Ease(Mathf.InverseLerp(.35f,1,U(4,t)));
            khakiCar.position-=right*(Vector3.Dot(initialPositions[Array.IndexOf(opening.vehicles,khakiCar)]-initialPositions[Array.IndexOf(opening.vehicles,redCar)],right)*align);
            blueCar.position+=forward*(lead*drop);
            float driftU=Mathf.Clamp01((t-starts[7])/(starts[12]-starts[7]));
            redCar.position-=forward*(lag*Ease(driftU));redCar.position+=right*(side*Mathf.Sin(Mathf.PI*driftU));
            float spin=t<starts[8]?90*Ease(U(7,t)):t<starts[11]?90:90+270*Ease(U(11,t));
            if(t<starts[7])spin=0;
            redCar.rotation=Quaternion.AngleAxis(spin,Vector3.up)*initialRotations[Array.IndexOf(opening.vehicles,redCar)];
            float boostU=U(12,t);
            redCar.position+=forward*(boostSpeed*durations[12]*EaseIntegral(boostU));
            if(redCruise)redCruise.ApplyTimelinePose(this,world,speed*world,Phase>=7&&Phase<=11?-180*Mathf.Sin(Mathf.PI*driftU):0);
            DriveSequence(t);
            DriveMissile(t);
            if(driftSmoke){
                float power=t<starts[7]?0:t<starts[11]?.8f:.8f*(1-Ease(U(11,t)));
                driftSmoke.SetSmokePower(power);driftSmoke.SetVehicleSpeed(speed*rate);
            }
            if(redBoosterDeployment){redBoosterDeployment.SetDeployAmount(Mathf.Lerp(boosterRest,1,Ease(Mathf.Clamp01(boostU*4))));redBoosterDeployment.PreviewSequence();}
            if(boostEffects!=null)for(int i=0;i<boostEffects.Length;i++)if(boostEffects[i]){
                bool active=Phase==12&&boostU>.08f;if(boostEffects[i].activeSelf!=active)boostEffects[i].SetActive(active);
                if(boostNozzles!=null&&i<boostNozzles.Length&&boostNozzles[i])
                    boostEffects[i].transform.SetPositionAndRotation(boostNozzles[i].bounds.center-forward*.08f,redCar.rotation);
            }
            SetVfxRates(rate);
            int[] shots={1,2,3,4,5,6,7,8,9,10,9,8,8};
            int shot=Phase==9&&!useInterior?9:shots[Phase];
            cameras.activeShot=shot;
            cameras.shots[5].moveProgress=Phase==5?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,u)):0;
            cameras.shots[6].moveProgress=Phase==6?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,1,u)):0;
            cameras.shots[7].moveProgress=Phase==12?Ease(u):Phase==11?Ease(u)*.4f:0;
        }
        void DriveSequence(float t)
        {
            if(t<starts[5])return;
            if(!sequenceStarted){
                float oldLife=missileSequence.motion.flightLifetime;
                // The run copy gets a longer lifetime, but the serialized/shared setup stays unchanged.
                missileSequence.motion.flightLifetime=Mathf.Max(oldLife,FilmDuration+30);
                sequenceStarted=missileSequence.Play();missileSequence.motion.flightLifetime=oldLife;
                if(!sequenceStarted){Error="발사 준비를 시작할 수 없습니다.";Playing=false;return;}
            }
            float clock=t<starts[6]?baseFireTime*U(5,t):baseFireTime+.0001f+WorldSeconds(t)-WorldSeconds(starts[6]);
            missileSequence.Advance(Mathf.Max(0,clock-missileSequence.TimeOnSequence));
            previousSeqTime=clock;
            if(preparationSlide)preparationSlide.Sample(missileSequence.TimeOnSequence);
            if(missileSequence.ActiveMissile&&!launched){
                launched=true;activeProjectile=missileSequence.ActiveMissile.gameObject;
                launchRelative=activeProjectile.transform.position-redCar.position;
            }
        }
        Vector3 WindowRelative(Vector3 local)
        {
            var initial=initialRotations[Array.IndexOf(opening.vehicles,redCar)];
            return Quaternion.AngleAxis(90,Vector3.up)*initial*Vector3.Scale(local,redCar.lossyScale);
        }
        void DriveMissile(float t)
        {
            if(!activeProjectile)return;
            var entry=WindowRelative(passLocal);var exit=WindowRelative(exitLocal);
            Vector3 rel;
            if(t<starts[7])rel=Vector3.Lerp(launchRelative,entry-forward*2.5f,Ease(U(6,t)));
            else if(t<starts[8])rel=Vector3.Lerp(entry-forward*2.5f,entry-forward*.8f,Ease(U(7,t)));
            else if(t<starts[9])rel=Vector3.Lerp(entry-forward*.8f,entry,U(8,t));
            else if(t<starts[10])rel=Vector3.Lerp(entry,exit,U(9,t));
            else if(t<starts[11])rel=Vector3.Lerp(exit,exit+forward*3,U(10,t));
            else rel=exit+forward*(3+40*(WorldSeconds(t)-WorldSeconds(starts[11])));
            activeProjectile.transform.SetPositionAndRotation(redCar.position+rel,Quaternion.LookRotation(forward,Vector3.up));
        }
        void SetVfxRates(float rate)
        {
            if(!Application.isPlaying)return;
            // Instance properties only; never change a shared VFX Graph.
            foreach(var root in new[]{redCar,khakiCar})if(root)foreach(var v in root.GetComponentsInChildren<VisualEffect>(true))v.playRate=rate;
            if(activeProjectile)foreach(var v in activeProjectile.GetComponentsInChildren<VisualEffect>(true))v.playRate=rate;
            if(driftSmoke)foreach(var v in gameObject.scene.GetRootGameObjects().Where(g=>g.name.StartsWith("ProgressiveSmoke_")).SelectMany(g=>g.GetComponentsInChildren<VisualEffect>(true)))v.playRate=rate;
        }
        void OnDisable()
        {
            if(!Application.isPlaying||!acquired)return;
            Playing=false;if(redCruise)redCruise.ReleaseTimeline(this);
            SetVfxRates(1);
            if(driftSmoke)driftSmoke.SetSmokePower(0);
            if(boostEffects!=null)foreach(var g in boostEffects)if(g)g.SetActive(false);
            if(missileSequence){missileSequence.ResetAll();missileSequence.ManualSimulation=oldSequenceManual;missileSequence.playback.playOnStart=oldPlayOnStart;}
            if(opening)opening.ManualSimulation=oldOpeningManual;
            if(cameras)cameras.autoPlayOpening=oldAuto;
            if(preparationSlide)preparationSlide.ManualSimulation=oldPrepManual;
            acquired=false;
        }
        void OnDrawGizmosSelected()
        {
            if(passengerWindow&&driverWindow){Gizmos.color=Color.cyan;Gizmos.DrawSphere(passengerWindow.position,.04f);Gizmos.DrawSphere(driverWindow.position,.04f);Gizmos.DrawLine(passengerWindow.position,driverWindow.position);}
        }
    }
}
