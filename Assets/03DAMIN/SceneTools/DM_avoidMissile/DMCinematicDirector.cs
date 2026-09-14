using System;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using Damin.Trailer.MissileCar;
using Damin.Trailer.FinalVFX;
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
        public enum CameraPlaybackMode { [InspectorName("전체 자동 전환")] FullSequence, [InspectorName("개별 카메라 촬영")] SingleCamera }
        [Serializable] public sealed class ShotTake
        {
            public string label;
            [Min(0),InspectorName("촬영 시작 시간")] public float startTime;
            [Min(.1f),InspectorName("촬영 종료 시간")] public float endTime;
            [InspectorName("촬영 시작 시 빨간차 X (m)"),Tooltip("DM 직선 도로의 월드 X 좌표. 높이와 차선은 유지하며, 여섯 차량과 촬영 경로를 같은 거리만큼 옮깁니다.")]
            public float redStartWorldX=1200;
        }
        [Header("카메라별 촬영 — 변경 후 다시 재생")]
        [InspectorName("촬영 모드")] public CameraPlaybackMode cameraPlaybackMode;
        [Range(1,10), InspectorName("촬영 카메라 번호")] public int takeCameraNumber=2;
        [Min(0), InspectorName("촬영 시작 시간"), Tooltip("전체 연출 시간표 기준 초. 이 시각까지는 연기·차량 상태를 맞추는 준비 재생을 합니다. 영상 저장은 별도의 녹화 도구를 사용하세요.")] public float takeStartTime;
        [Min(.1f), InspectorName("촬영 종료 시간"), Tooltip("전체 연출 길이 이내에서 지정합니다. 현재 20초라면 최대 20초. 끝에서는 연출을 멈춥니다. 컷 시간표 자체를 늘리지 않습니다.")] public float takeEndTime=20;
        [InspectorName("구간 반복 재생"), Tooltip("끝난 뒤 대기하고 처음부터 다시 준비 재생합니다. 영상 녹화 시에는 끄는 것을 권장합니다.")] public bool loopTake;
        [Min(.1f), InspectorName("반복 전 대기 시간")] public float takeLoopDelay=1;
        [InspectorName("SHOT별 촬영 구간 사용")] public bool useShotTakeTimes;
        [InspectorName("SHOT별 출발 위치 적용")] public bool repositionForTake;
        public ShotTake[] shotTakes=Array.Empty<ShotTake>();
        public ShotTake SelectedShotTake=>shotTakes!=null&&takeCameraNumber>=1&&takeCameraNumber<=shotTakes.Length?shotTakes[takeCameraNumber-1]:null;
        public float ConfiguredTakeStart=>useShotTakeTimes&&SelectedShotTake!=null?SelectedShotTake.startTime:takeStartTime;
        public float ConfiguredTakeEnd=>useShotTakeTimes&&SelectedShotTake!=null?SelectedShotTake.endTime:takeEndTime;
        [Header("구간 길이 (초) — 변경 후 다시 재생")]
        public FilmTiming timing=new FilmTiming();
        public bool playOnStart=true;
        [Tooltip("첫 구간 길이에 맞춰 01/02를 재생 시작 시 배치합니다. 재생 중에는 고정입니다.")]
        public bool fitOpeningToTiming=true;
        public Vector3 groundCameraOffset;
        public Vector3 rearCameraOffset;
        [Min(.1f),Tooltip("최신 차량 크기에 맞춘 첫 두 고정 카메라의 거리 비율. 시간표는 바꾸지 않습니다.")]
        public float vehicleFramingScale=1;
        [Header("촬영용 움직임")]
        [Min(1)] public float raceSpeedKph=90;
        [Range(.1f,8f), InspectorName("주행 속도 배율"), Tooltip("1 = 기존 속도, 4 = 전진 주행과 부스트 추가 속도 4배. 컷 시간표·맵·연기 준비 시간은 유지합니다. 정지 상태에서 저장 후 Play하거나, Play 중 변경값으로 처음부터 다시 재생 버튼을 누르세요. 큰 배율은 도로 끝을 벗어날 수 있습니다.")]
        public float drivingSpeedMultiplier=1;
        public float ConfiguredRaceSpeedKph=>Safe(raceSpeedKph,90,1,250)*Safe(drivingSpeedMultiplier,1,.1f,8);
        [Min(0)] public float dropBackMetres=14;
        [Min(0)] public float blueLeadMetres=8;
        [Min(0)] public float redDriftLagMetres=5;
        [Range(0,3)] public float driftSideMetres=.3f;
        [Header("시네마틱 궤적 — 이 씬 전용")]
        public bool cinematicMotion;
        [Range(0,1),Tooltip("부스트 때 카메라의 추가 가속이 늦는 시간. 차량 크기는 바꾸지 않습니다.")]
        public float boostCameraDelay=.35f;
        [Header("이 씬의 미사일 발사 — 공용 프리팹 변경 없음")]
        [InspectorName("발사 초반 연기 부드럽게 시작")] public bool shapeLaunchSmoke;
        [Min(.02f), InspectorName("발사 연기 차오름 시간 (초)")] public float launchSmokeRampSeconds=.12f;
        [Min(0), InspectorName("발사 순간 차량 대비 추가 속도 (m/s)")]
        public float launchRelativeSpeed=8;
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
        public DMRacePerformance racePerformance;
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
        public bool SingleCameraTakeActive=>runSingleTake;
        public bool TakePreparing=>Playing&&runSingleTake&&FilmTime<runTakeStart;
        public bool TakeReady=>Playing&&runSingleTake&&FilmTime>=runTakeStart;
        public float ActiveTakeStart=>runTakeStart;
        public float ActiveTakeEnd=>runTakeEnd;
        public int ActiveTakeCamera=>runTakeCamera;
        public float TakeElapsed=>Mathf.Clamp(FilmTime-runTakeStart,0,Mathf.Max(0,runTakeEnd-runTakeStart));
        public Vector3 ActiveTakeOffset=>runTakeOffset;
        public string TakePlacementWarning {get;private set;}
        public float[] ActiveDurations=>durations==null?timing.Values():(float[])durations.Clone();
        float[] durations,starts;
        Vector3[] initialPositions;
        Quaternion[] initialRotations;
        Vector3[] initialCameraPositions;
        Quaternion[] initialCameraRotations;
        Vector3 runTakeOffset;
        Vector3 forward,right,passLocal,exitLocal,launchRelative;
        float speed,retreat,lead,lag,side,slow,boostSpeed,baseFireTime;
        float previousSeqTime,bodySeconds,boosterRest;
        bool acquired,launched,sequenceStarted,oldOpeningManual,oldSequenceManual,oldPrepManual,oldAuto,oldPlayOnStart;
        bool useInterior;
        GameObject activeProjectile;
        TrailerMissileFlight launchHook;
        MissileExhaustVFXController launchTrail;
        float runLaunchRelativeSpeed,runLaunchSmokeRamp;
        bool runShapeLaunchSmoke;
        AnimationCurve driftRotation;
        Vector3 launchWorld,entryWorld,exitWorld,clearWorld;
        float runCameraDelay;
        bool runSingleTake,runLoopTake;
        int runTakeCamera;
        float runTakeStart,runTakeEnd,runLoopDelay,loopWait;
        void Awake(){Acquire();}
        void Start()
        {
            if(!Validate())return;
            initialPositions=opening.vehicles.Select(t=>t.position).ToArray();
            initialRotations=opening.vehicles.Select(t=>t.rotation).ToArray();
            initialCameraPositions=cameras.shots.Select(s=>s.camera.transform.position).ToArray();
            initialCameraRotations=cameras.shots.Select(s=>s.camera.transform.rotation).ToArray();
            if(racePerformance)racePerformance.CapturePositions();
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
            missileSequence.ResetAll();launchTrail=null;
            runTakeOffset=Vector3.zero;TakePlacementWarning=null;
            for(int i=0;i<initialPositions.Length;i++)opening.vehicles[i].SetPositionAndRotation(initialPositions[i],initialRotations[i]);
            for(int i=0;i<cameras.shots.Length&&i<initialCameraPositions.Length;i++)
                cameras.shots[i].camera.transform.SetPositionAndRotation(initialCameraPositions[i],initialCameraRotations[i]);
            durations=timing.Values();starts=new float[14];for(int i=0;i<13;i++)starts[i+1]=starts[i]+durations[i];
            FilmDuration=starts[13];FilmTime=0;Phase=0;Completed=false;Playing=true;sequenceStarted=launched=false;activeProjectile=null;launchHook=null;previousSeqTime=0;
            runSingleTake=cameraPlaybackMode==CameraPlaybackMode.SingleCamera;
            runTakeCamera=Mathf.Clamp(takeCameraNumber,1,cameras.shots.Length);
            runTakeEnd=runSingleTake?Safe(ConfiguredTakeEnd,FilmDuration,.1f,FilmDuration):FilmDuration;
            runTakeStart=runSingleTake?Safe(ConfiguredTakeStart,0,0,Mathf.Max(0,runTakeEnd-.1f)):0;
            runLoopTake=runSingleTake&&loopTake;runLoopDelay=Safe(takeLoopDelay,1,.1f,30);loopWait=0;
            runShapeLaunchSmoke=shapeLaunchSmoke;runLaunchSmokeRamp=Safe(launchSmokeRampSeconds,.12f,.02f,1);
            runLaunchRelativeSpeed=Safe(launchRelativeSpeed,8,0,100);
            forward=Vector3.ProjectOnPlane(opening.roadForward,Vector3.up).normalized;if(forward.sqrMagnitude<.5f)forward=Vector3.left;
            right=Vector3.Cross(Vector3.up,forward);
            float drivingMultiplier=Safe(drivingSpeedMultiplier,1,.1f,8);
            speed=ConfiguredRaceSpeedKph/3.6f;retreat=Safe(dropBackMetres,14,3,40);lead=Safe(blueLeadMetres,8,3,30);
            lag=Safe(redDriftLagMetres,5,0,10);side=Safe(driftSideMetres,.3f,0,3);slow=Safe(slowMotionRate,.08f,.02f,1);boostSpeed=Safe(boostExtraKph,70,0,150)*drivingMultiplier/3.6f;runCameraDelay=Safe(boostCameraDelay,.35f,0,1);
            // Tangents are degrees per film-second. Keep turning during the window pass.
            driftRotation=new AnimationCurve(new Keyframe(starts[7],0,0,0),new Keyframe(starts[8],76,10,10),new Keyframe(starts[9],86,6.6667f,6.6667f),new Keyframe(starts[10],94,6.6667f,6.6667f),new Keyframe(starts[11],112,60,60),new Keyframe(starts[12],360,0,0));
            foreach(var s in cameras.shots){s.boostPoseActive=false;s.worldPositionOffset=Vector3.zero;}
            passLocal=redCar.InverseTransformPoint(passengerWindow.position);exitLocal=redCar.InverseTransformPoint(driverWindow.position);
            useInterior=interiorCameraReady;
            if(racePerformance)racePerformance.Begin();
            baseFireTime=missileSequence.motion.fireTime;
            if(fitOpeningToTiming)
            {
                var frame=Quaternion.LookRotation(forward,Vector3.up);
                float convoy=initialPositions.Max(p=>Vector3.Dot(redCar.position-p,forward));
                float passDistance=Mathf.Max(6,speed*durations[0]-convoy);
                var a=cameras.shots[0].camera.transform;var b=cameras.shots[1].camera.transform;
                float framing=Safe(vehicleFramingScale,1,.1f,10);
                a.position=redCar.position+frame*(new Vector3(7.5f*framing,-.33f*framing,passDistance)+groundCameraOffset);
                a.LookAt(redCar.position+frame*new Vector3(0,.45f*framing,passDistance-10*framing));
                b.position=redCar.position+frame*(new Vector3(6.5f*framing,.5f*framing,Mathf.Max(0,speed*durations[0]-28))+rearCameraOffset);
                b.LookAt(redCar.position+frame*new Vector3(0,.1f*framing,speed*durations[0]+8));
            }
            PlaceTakeOnRoad();
            // Wheel distance and lane-shift baselines must move with the take, not with the map.
            if(racePerformance)racePerformance.CapturePositions();
            if(driftSmoke){driftSmoke.StopPreview();driftSmoke.SetSmokePower(0);}
            if(boostEffects!=null)foreach(var g in boostEffects)if(g)g.SetActive(false);
            if(redBoosterDeployment){redBoosterDeployment.SetDeployAmount(boosterRest);redBoosterDeployment.PreviewSequence();}
            foreach(var v in khakiCar.GetComponentsInChildren<VisualEffect>(true)){v.pause=false;v.Reinit();}
            ApplyFrame(0);
        }
        void PlaceTakeOnRoad()
        {
            if(!runSingleTake||!repositionForTake)return;
            var setup=SelectedShotTake;
            if(setup==null){TakePlacementWarning="이 카메라의 출발 위치 설정이 없습니다. 기존 위치를 사용합니다.";return;}
            if(Mathf.Abs(forward.x)<.99f){TakePlacementWarning="출발 위치 X 조절은 DM 씬의 X축 직선 도로용입니다. 기존 위치를 사용합니다.";return;}
            PredictRed(runTakeStart,out var before,out _);
            float anchor=Safe(setup.redStartWorldX,1200,-10000,10000);
            runTakeOffset=forward*((anchor-before.x)/forward.x);
            foreach(var car in opening.vehicles)car.position+=runTakeOffset;
            foreach(var shot in cameras.shots)if(shot.mode==DMShotCameraRig.ShotMode.FixedWorld)shot.camera.transform.position+=runTakeOffset;
            // This constant translation is captured once at Replay, before any smoke is emitted.
            // Never wrap or teleport an active take. Preroll may be outside the road.
            PredictRed(runTakeEnd,out var end,out _);
            int redIndex=Array.IndexOf(opening.vehicles,redCar);
            float low=initialPositions.Min(p=>p.x-initialPositions[redIndex].x)-40;
            float high=initialPositions.Max(p=>p.x-initialPositions[redIndex].x)+40;
            if(Mathf.Min(anchor,end.x)+low< -1990||Mathf.Max(anchor,end.x)+high>1990)
                TakePlacementWarning="이 촬영 구간은 현재 도로 길이를 벗어날 수 있습니다. 해당 SHOT의 시작 X 또는 촬영 시작·종료 시간을 조절하세요. 속도·맵·촬영 시간은 자동으로 바꾸지 않습니다.";
        }
        void Update(){if(!ManualSimulation&&(Playing||(Completed&&runLoopTake)))Advance(Time.deltaTime);}
        public void Advance(float dt)
        {
            if(!float.IsFinite(dt)||dt<=0)return;
            if(!Playing){if(Completed&&runLoopTake){loopWait+=dt;if(loopWait>=runLoopDelay)Replay();}return;}
            FilmTime=Mathf.Min(runTakeEnd,FilmTime+dt);ApplyFrame(FilmTime);
            if(FilmTime>=runTakeEnd){Playing=false;Completed=true;SetVfxRates(0);}
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
        float DriftProgress(float t)=>Mathf.Clamp01((t-starts[7])/(starts[12]-starts[7]));
        Vector3 DriftOffset(float t){float q=Ease(DriftProgress(t));return -forward*(lag*q)+right*(side*Mathf.Sin(Mathf.PI*q));}
        Vector3 RaceOffset(Transform car,float t)=>racePerformance?racePerformance.RoadOffset(car,t,starts[5],forward,right):Vector3.zero;
        void PredictRed(float t,out Vector3 p,out Quaternion r){int i=Array.IndexOf(opening.vehicles,redCar);p=initialPositions[i]+runTakeOffset+forward*(speed*WorldSeconds(t)+BoostDistance(t-starts[12]))+DriftOffset(t)+RaceOffset(redCar,t);r=Quaternion.AngleAxis(driftRotation.Evaluate(t),Vector3.up)*initialRotations[i];}
        Vector3 PredictKhakiAtFire(){int k=Array.IndexOf(opening.vehicles,khakiCar),r=Array.IndexOf(opening.vehicles,redCar);return initialPositions[k]+runTakeOffset+forward*(speed*WorldSeconds(starts[6])-retreat)-right*Vector3.Dot(initialPositions[k]-initialPositions[r],right)+RaceOffset(khakiCar,starts[6]);}
        float BoostDistance(float seconds)=>boostSpeed*durations[12]*EaseIntegral(Mathf.Clamp01(seconds/durations[12]));
        void ApplyFrame(float t)
        {
            Phase=12;for(int p=0;p<13;p++)if(t<starts[p+1]){Phase=p;break;}
            float u=U(Phase,t),world=WorldSeconds(t),rate=Rate(Phase,u);
            bodySeconds=world;
            for(int i=0;i<opening.vehicles.Length;i++)opening.vehicles[i].SetPositionAndRotation(initialPositions[i]+runTakeOffset+forward*(speed*world),initialRotations[i]);
            float drop=Ease(U(4,t));
            khakiCar.position-=forward*(retreat*drop);
            // Move into the lead car's lane only after opening a longitudinal gap.
            float align=Ease(Mathf.InverseLerp(.35f,1,U(4,t)));
            khakiCar.position-=right*(Vector3.Dot(initialPositions[Array.IndexOf(opening.vehicles,khakiCar)]-initialPositions[Array.IndexOf(opening.vehicles,redCar)],right)*align);
            blueCar.position+=forward*(lead*drop);
            float driftU=Mathf.Clamp01((t-starts[7])/(starts[12]-starts[7]));
            if(cinematicMotion)redCar.position+=DriftOffset(t);
            else{redCar.position-=forward*(lag*Ease(driftU));redCar.position+=right*(side*Mathf.Sin(Mathf.PI*driftU));}
            float spin=t<starts[8]?90*Ease(U(7,t)):t<starts[11]?90:90+270*Ease(U(11,t));
            if(t<starts[7])spin=0;
            if(cinematicMotion)spin=driftRotation.Evaluate(t);
            redCar.rotation=Quaternion.AngleAxis(spin,Vector3.up)*initialRotations[Array.IndexOf(opening.vehicles,redCar)];
            float boostU=U(12,t);
            redCar.position+=forward*(boostSpeed*durations[12]*EaseIntegral(boostU));
            if(racePerformance)racePerformance.Sample(t,starts[5],durations[4],world,speed,forward,right,redCar,khakiCar,Phase,driftU);
            else if(redCruise)redCruise.ApplyTimelinePose(this,world,speed*world,Phase>=7&&Phase<=11?-180*Mathf.Sin(Mathf.PI*driftU):0);
            DriveSequence(t);
            DriveMissile(t);
            ShapeLaunchSmoke(t);
            if(driftSmoke){
                float power=t<starts[7]?0:t<starts[11]?.8f:.8f*(1-Ease(U(11,t)));
                if(cinematicMotion&&Phase==7)power*=Ease(Mathf.Clamp01(u*3));
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
            cameras.activeShot=runSingleTake?runTakeCamera:shot;
            cameras.motionTime=world;
            cameras.shots[1].moveProgress=Phase==1?u:0;
            cameras.shots[5].moveProgress=Phase==5?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,u)):0;
            cameras.shots[6].moveProgress=Phase==6?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,1,u)):0;
            cameras.shots[7].moveProgress=Phase==12?Ease(u):Phase==11?Ease(u)*.4f:0;
            cameras.shots[7].boostPoseActive=cinematicMotion&&Phase==12;
            cameras.shots[7].worldPositionOffset=cinematicMotion&&Phase==12?forward*(BoostDistance(t-starts[12]-runCameraDelay)-BoostDistance(t-starts[12])):Vector3.zero;
            // A held camera must not snap back to its start pose when its usual cut ends.
            if(runSingleTake){
                if(runTakeCamera==2)cameras.shots[1].moveProgress=U(1,t);
                if(runTakeCamera==6)cameras.shots[5].moveProgress=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,U(5,t)));
                if(runTakeCamera==7)cameras.shots[6].moveProgress=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,1,U(6,t)));
                // Shot08 is one continuous base angle in a take; keep the boost lag but omit its automatic angle change.
                if(runTakeCamera==8){cameras.shots[7].moveProgress=0;cameras.shots[7].boostPoseActive=false;}
            }
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
            if(cinematicMotion&&launchHook==null){var slot=missileSequence.launcher.slots[missileSequence.motion.missileNumber-1];if(slot.loadedMissile){launchHook=slot.loadedMissile;var captured=launchHook;captured.onLaunched.AddListener(()=>CaptureLaunch(captured));}}
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
            if(cinematicMotion){var p=MissilePosition(t);var v=MissilePosition(t+.002f)-MissilePosition(Mathf.Max(starts[6],t-.002f));activeProjectile.transform.SetPositionAndRotation(p,Quaternion.LookRotation(v.sqrMagnitude>.000001f?v:forward,Vector3.up));return;}
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
        void CaptureLaunch(TrailerMissileFlight shot){
            activeProjectile=shot.gameObject;launched=true;launchWorld=PredictKhakiAtFire()+shot.transform.position-khakiCar.position;
            launchTrail=shot.GetComponentInChildren<MissileExhaustVFXController>(true);
            PredictRed(starts[9],out var a,out var qa);PredictRed(starts[10],out var b,out var qb);PredictRed(starts[11],out var c,out var qc);
            entryWorld=a+qa*Vector3.Scale(passLocal,redCar.lossyScale);exitWorld=b+qb*Vector3.Scale(exitLocal,redCar.lossyScale);clearWorld=c+qc*Vector3.Scale(exitLocal,redCar.lossyScale)+forward*6;
        }
        static Vector3 Hermite(Vector3 a,Vector3 b,Vector3 va,Vector3 vb,float duration,float u){u=Mathf.Clamp01(u);float u2=u*u,u3=u2*u;return (2*u3-3*u2+1)*a+(u3-2*u2+u)*duration*va+(-2*u3+3*u2)*b+(u3-u2)*duration*vb;}
        Vector3 MissilePosition(float t){
            float w=WorldSeconds(t),w0=WorldSeconds(starts[6]),w1=WorldSeconds(starts[9]),w2=WorldSeconds(starts[10]),w3=WorldSeconds(starts[11]);
            Vector3 passVelocity=(exitWorld-entryWorld)/Mathf.Max(.001f,w2-w1);Vector3 exitVelocity=forward*(speed+65);
            if(t<starts[9])return Hermite(launchWorld,entryWorld,forward*(speed+runLaunchRelativeSpeed),passVelocity,w1-w0,(w-w0)/Mathf.Max(.001f,w1-w0));
            if(t<starts[10])return Vector3.Lerp(entryWorld,exitWorld,(w-w1)/Mathf.Max(.001f,w2-w1));
            if(t<starts[11])return Hermite(exitWorld,clearWorld,passVelocity,exitVelocity,w3-w2,(w-w2)/Mathf.Max(.001f,w3-w2));
            return clearWorld+exitVelocity*(w-w3);
        }
        void ShapeLaunchSmoke(float t)
        {
            if(!runShapeLaunchSmoke||!launchTrail||!activeProjectile)return;
            // Sequence reapplies its authored settings every frame. Adjust only this live missile,
            // after sequence evaluation and before its exhaust controller's LateUpdate.
            float age=Mathf.Max(0,WorldSeconds(t)-WorldSeconds(starts[6]));
            float ramp=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/runLaunchSmokeRamp));
            var settings=missileSequence.missileSmoke;
            launchTrail.trailDensity=settings.trailDensity*ramp;
            launchTrail.trailWidth=settings.trailWidth*Mathf.Lerp(.45f,1,ramp);
            launchTrail.trailExpansion=settings.trailExpansion*Mathf.Lerp(.4f,1,ramp);
            launchTrail.brightCoreIntensity=settings.brightCoreIntensity*ramp;
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
            if(racePerformance)racePerformance.Release();
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
