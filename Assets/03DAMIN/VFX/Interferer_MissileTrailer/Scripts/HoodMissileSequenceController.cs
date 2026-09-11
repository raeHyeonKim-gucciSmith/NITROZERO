using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.VFX;
using Damin.Trailer.FinalVFX;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Damin.Trailer.MissileCar
{
    [DefaultExecutionOrder(-300), DisallowMultipleComponent]
    [AddComponentMenu("DAMIN/보넷 미사일 통합 연출")]
    public sealed class HoodMissileSequenceController : MonoBehaviour
    {
        [Serializable] public sealed class PlaybackSettings
        { public bool useShift=true, playOnStart; [Min(0)] public float startDelay; }
        [Serializable] public sealed class MotionSettings
        {
            [Min(0)] public float hoodStart;
            [Min(.01f)] public float hoodDuration=.8f;
            public float hoodAngle=30;
            public bool holdHoodGap;
            [Min(0)] public float hoodGapAngle=6, hoodFullOpenStart=3.8f;
            [Min(0)] public float bayStart=.9f;
            [Min(.01f)] public float bayDuration=.7f;
            public float bayHeightCm=2.5f;
            [Range(1,4)] public int missileNumber=4;
            public bool liftMissile=true;
            [Min(0)] public float missileLiftStart=5.75f, missileLiftDuration=.35f, missileLiftHeightCm=3;
            [Min(0)] public float fireTime=6.5f;
            [Min(0)] public float speed=6, acceleration=2, clearanceHeight=.1f, clearanceForward=.65f;
            [Min(.01f)] public float clearanceDuration=.3f;
            [Min(.1f)] public float flightLifetime=6;
        }
        [Serializable] public sealed class BodySmokeSettings
        {
            public bool enabled=true;
            [Min(0)] public float interiorStart;
            [Min(.01f)] public float interiorFillDuration=4;
            [Min(0)] public float interiorSpawnRate=600;
            [Range(0,3)] public float interiorDensity=2;
            [Min(.01f)] public float interiorParticleSize=.3f;
            [Min(0)] public float interiorTurbulence=.04f;
            [Min(0)] public float sideStart=2.5f;
            [Min(.01f)] public float sideRiseDuration=2;
            [Range(0,.99f)] public float overflowThreshold=.75f;
            [Min(.01f)] public float maxLateralDistance=.2f;
            [Min(0)] public float interiorGrowth=.04f;
            [Min(0)] public float sideSpawnRate=450;
            [Range(0,4)] public float sideDensity=1.8f;
            [Min(.01f)] public float sideParticleSize=.28f;
            [Min(0)] public float sideSpeed=.5f,sideSpread=.22f,sideTurbulence=.07f,riseAfterDistance=.3f,outsideRiseSpeed=.25f,backflowSpeed=.1f;
            [Range(0,.4f)] public float sideAsymmetry=.1f;
            [Min(.1f)] public float sideLifetime=2.8f;
            [Min(0)] public float sideExpansion=.65f, sideDrag=4.2f, sideBuoyancy=.16f;
            public Color sideColor=new Color(.80f,.82f,.85f,1);
            [Range(0,8)] public float sideSwirlSpeed=2.4f;
            [Min(0)] public float interiorFadeDelay=3,sideFadeDelay=3,residualFadeDelay=.1f;
            [Min(.01f)] public float interiorFadeDuration=1.2f,sideFadeDuration=5,residualFadeDuration=1.8f;
            [Min(0)] public float launchBurstIntensity=1,residualSpawnRate=140;
            [Range(0,3)] public float residualDensity=1.2f;
            public Color smokeColor=new Color(.55f,.57f,.59f,1);
            [Min(.05f)] public float smokeLifetime=2.6f;
            [Min(0)] public float smokeExpansion=.65f,collisionMargin=.025f;
            [Range(.4f,1.5f)] public float smokeRoundness=1;
            public void Apply(HoodMissileVFXController_Final h)
            {
                if(!h)return;
                h.interiorStart=interiorStart;h.interiorFillDuration=interiorFillDuration;h.sideStart=sideStart;h.sideRiseDuration=sideRiseDuration;
                h.overflowThreshold=overflowThreshold;h.maxLateralDistance=maxLateralDistance;h.interiorGrowth=interiorGrowth;
                h.interiorSpawnRate=enabled?interiorSpawnRate:0;h.interiorDensity=interiorDensity;h.interiorParticleSize=interiorParticleSize;h.interiorTurbulence=interiorTurbulence;
                h.sideSpawnRate=enabled?sideSpawnRate:0;h.sideDensity=sideDensity;h.sideParticleSize=sideParticleSize;h.sideSpeed=sideSpeed;h.sideSpread=sideSpread;h.sideTurbulence=sideTurbulence;
                h.riseAfterDistance=riseAfterDistance;h.outsideRiseSpeed=outsideRiseSpeed;h.backflowSpeed=backflowSpeed;h.sideAsymmetry=sideAsymmetry;
                h.sideLifetime=sideLifetime;h.sideExpansion=sideExpansion;h.sideDrag=sideDrag;h.sideBuoyancy=sideBuoyancy;h.sideColor=sideColor;
                h.sideSwirlSpeed=sideSwirlSpeed;
                h.interiorFadeDelay=interiorFadeDelay;h.interiorFadeDuration=interiorFadeDuration;h.sideFadeDelay=sideFadeDelay;h.sideFadeDuration=sideFadeDuration;
                h.residualFadeDelay=residualFadeDelay;h.residualFadeDuration=residualFadeDuration;h.residualSpawnRate=enabled?residualSpawnRate:0;h.residualDensity=residualDensity;
                h.launchBurstIntensity=enabled?launchBurstIntensity:0;h.smokeColor=smokeColor;h.smokeLifetime=smokeLifetime;h.smokeExpansion=smokeExpansion;h.smokeRoundness=smokeRoundness;h.collisionMargin=collisionMargin;
            }
            public float TailDuration=>Mathf.Max(interiorFadeDelay+interiorFadeDuration,Mathf.Max(sideFadeDelay+sideFadeDuration,residualFadeDelay+residualFadeDuration))+smokeLifetime*1.2f+.2f;
        }
        [Serializable] public sealed class TrailSettings
        {
            public bool enabled=true;
            [Min(0)] public float exhaustStartDelay=.02f,trailSpawnRate=900;
            [Range(0,4)] public float trailDensity=2.2f;
            [Min(.001f)] public float trailWidth=.16f;
            [Min(0)] public float trailExpansion=1.8f,trailTurbulence=.06f,trailSpeed=1.3f;
            [Min(.1f)] public float trailLength=12,maxSmokeLifetime=3;
            public Color trailColor=new Color(.98f,.98f,.98f,1);
            [Min(0)] public float extraParticlesPerMeter=40;
            [Min(1)] public float maxTrailSpawnRate=2400;
            [Min(.01f)] public float tailCleanupPadding=.25f;
            [Range(.1f,.95f)] public float tailFadeStart=.6f;
            [Min(0)] public float brightCoreIntensity=1;
            public void Apply(MissileExhaustVFXController t)
            {
                if(!t)return;t.emissionEnabled=enabled;t.exhaustStartDelay=exhaustStartDelay;t.trailSpawnRate=trailSpawnRate;t.trailDensity=trailDensity;
                t.trailWidth=trailWidth;t.trailExpansion=trailExpansion;t.trailTurbulence=trailTurbulence;t.trailSpeed=trailSpeed;t.trailLength=trailLength;
                t.maxSmokeLifetime=maxSmokeLifetime;t.trailColor=trailColor;t.extraParticlesPerMeter=extraParticlesPerMeter;t.maxTrailSpawnRate=maxTrailSpawnRate;t.tailCleanupPadding=tailCleanupPadding;
                t.tailFadeStart=tailFadeStart;t.brightCoreIntensity=brightCoreIntensity;
            }
        }
        public PlaybackSettings playback=new PlaybackSettings();
        public MotionSettings motion=new MotionSettings();
        public BodySmokeSettings bodySmoke=new BodySmokeSettings();
        public InteriorSmokeSettings interiorSmoke=new InteriorSmokeSettings();
        public TrailSettings missileSmoke=new TrailSettings();
        [HideInInspector] public TrailerMissileLauncher launcher;
        [HideInInspector] public TrailerHoodDeployment deployment;
        [HideInInspector] public HoodMissileVFXController_Final hoodVFX;
        [SerializeField,HideInInspector] Vector3 bayLateralOffset;
        public enum SequenceState { Idle, Playing, Paused, Completed }
        public SequenceState State {get;private set;}
        public float TimeOnSequence=>Mathf.Max(0,clock);
        public bool HasFired {get;private set;}
        public TrailerMissileFlight ActiveMissile=>missile;
        public bool ManualSimulation {get;set;}
        MotionSettings run;
        readonly List<MissileExhaustVFXController> trails=new List<MissileExhaustVFXController>();
        TrailerMissileFlight missile;
        float clock;bool quitting;

        void OnEnable(){if(Application.isPlaying)Acquire();}
        void Start(){if(playback.playOnStart)Play();}
        void OnApplicationQuit(){quitting=true;}
        void OnDisable()
        {
            if(!Application.isPlaying||quitting)return;
            ResetAll();if(launcher){launcher.ExternallyControlled=false;launcher.ManualSimulation=false;}if(hoodVFX)hoodVFX.ExternallyControlled=false;
        }
        void Acquire(){if(launcher){launcher.ExternallyControlled=true;launcher.ManualSimulation=true;}if(hoodVFX)hoodVFX.ExternallyControlled=true;}
        void Update()
        {
            if(ManualSimulation)return;
            if(playback.useShift&&ShiftPressed()){if(State==SequenceState.Paused)Resume();else if(State!=SequenceState.Playing)Play();}
            if(State==SequenceState.Playing)Advance(Time.deltaTime);
        }
        public string ValidationError()
        {
            if(!launcher||!deployment||!deployment.Ready||!hoodVFX||!launcher.missilePrefab)return "고급 연결에 발사 컨트롤러·보넷·보넷 VFX·재장전용 미사일이 필요합니다.";
            foreach(var f in typeof(MotionSettings).GetFields())if(f.FieldType==typeof(float)){float v=(float)f.GetValue(motion);if(float.IsNaN(v)||float.IsInfinity(v))return "시간과 거리에는 유효한 숫자를 입력하세요.";}
            if(motion.hoodStart<0||motion.bayStart<0||motion.fireTime<0||motion.hoodDuration<=0||motion.bayDuration<=0)return "시작 시간은 0 이상, 열림·상승 소요 시간은 0보다 커야 합니다.";
            float hoodReady=(motion.holdHoodGap?motion.hoodFullOpenStart:motion.hoodStart)+motion.hoodDuration;
            if(motion.holdHoodGap&&(motion.hoodFullOpenStart<motion.hoodStart+motion.hoodDuration||Mathf.Abs(motion.hoodGapAngle)>Mathf.Abs(motion.hoodAngle)))return "보넷 틈 유지 후 완전 열림 시각·각도를 확인하세요.";
            if(motion.bayStart<hoodReady-.0001f)return "미사일 통 상승 시작이 보넷 열림 완료보다 빠릅니다. 시간을 직접 조정하세요.";
            float ready=Mathf.Max(motion.hoodStart+motion.hoodDuration,motion.bayStart+motion.bayDuration);
            if(motion.liftMissile){if(motion.missileLiftStart<ready-.0001f)return "개별 미사일 상승이 미사일 통 상승 완료보다 빠릅니다.";ready=Mathf.Max(ready,motion.missileLiftStart+motion.missileLiftDuration);}
            if(motion.fireTime<ready-.0001f)return "발사 시간이 보넷·발사대·미사일 상승 완료보다 빠릅니다. 시간을 직접 조정하세요.";
            if(motion.missileNumber<1||launcher.slots==null||motion.missileNumber>launcher.slots.Length||launcher.slots[motion.missileNumber-1]?.mount==null)return "발사할 번호의 장착 위치가 없습니다.";
            if(motion.flightLifetime<=motion.clearanceDuration)return "비행 유지 시간은 발사 후 이탈 시간보다 길게 설정하세요.";
            return null;
        }
        public bool Play()
        {
            if(!Application.isPlaying)return false;
            if(State==SequenceState.Playing||State==SequenceState.Paused)return false;
            string error=ValidationError();if(error!=null){Debug.LogWarning("[보넷 미사일 연출] "+error,this);return false;}
            Acquire();ResetAll();run=JsonUtility.FromJson<MotionSettings>(JsonUtility.ToJson(motion));
            if(!launcher.slots[run.missileNumber-1].loadedMissile)return false;
            clock=-Mathf.Max(0,playback.startDelay);HasFired=false;State=SequenceState.Playing;
            ApplyMotionSettings();ApplySmokeSettings();
            if(!launcher.BeginExternalSequence()){State=SequenceState.Idle;return false;}
            Evaluate(0);return true;
        }
        public void Advance(float dt)
        {
            if(State!=SequenceState.Playing||run==null||dt<0)return;
            float before=clock;clock+=dt;Evaluate(Mathf.Max(0,clock-Mathf.Max(before,run.fireTime)));
            float end=run.fireTime+Mathf.Max(bodySmoke.TailDuration,run.flightLifetime+missileSmoke.maxSmokeLifetime+missileSmoke.tailCleanupPadding+.1f);
            if(clock>end){State=SequenceState.Completed;hoodVFX.SetTimelineTime(clock,clock-run.fireTime,false);}
        }
        void ApplyMotionSettings()
        {
            deployment.openAngle=run.hoodAngle;deployment.openDuration=run.hoodDuration;deployment.liftDuration=run.bayDuration;
            deployment.liftOffset=new Vector3(bayLateralOffset.x,run.bayHeightCm*.01f,bayLateralOffset.z);
            launcher.clearanceHeight=run.clearanceHeight;launcher.clearanceForwardDistance=run.clearanceForward;launcher.clearanceTime=run.clearanceDuration;
            launcher.missileSpeed=run.speed;launcher.missileAcceleration=run.acceleration;launcher.missileLifetime=run.flightLifetime;
        }
        void Evaluate(float flightDt)
        {
            ApplySmokeSettings();
            if(run.holdHoodGap){float a=Mathf.SmoothStep(0,1,Mathf.Clamp01((clock-run.hoodStart)/run.hoodDuration));float b=Mathf.SmoothStep(0,1,Mathf.Clamp01((clock-run.hoodFullOpenStart)/run.hoodDuration));deployment.ApplyTimelineAngle(clock,run.bayStart,run.hoodGapAngle*a+(run.hoodAngle-run.hoodGapAngle)*b);}
            else deployment.ApplyTimeline(clock,run.hoodStart,run.bayStart);
            var slot=launcher.slots[run.missileNumber-1];
            if(!HasFired&&slot.loadedMissile){float amount=run.liftMissile&&clock>=run.missileLiftStart?(run.missileLiftDuration<=0?1:Mathf.SmoothStep(0,1,Mathf.Clamp01((clock-run.missileLiftStart)/run.missileLiftDuration))):0;
                var t=slot.loadedMissile.transform;Vector3 world=launcher.vehicleFrame.up*(run.missileLiftHeightCm*.01f*amount);t.localPosition=slot.localPosition+(t.parent?t.parent.InverseTransformVector(world):world);}
            hoodVFX.SetTimelineTime(clock,HasFired?clock-run.fireTime:-1,true);
            if(clock>=run.bayStart+run.bayDuration)launcher.ExternalBayReady(clock);
            if(!HasFired&&clock>=run.fireTime){
                if(launcher.ExternalLaunch(run.missileNumber-1,clock)){HasFired=true;missile=launcher.LastLaunched;}
                else {Pause();Debug.LogWarning("미사일 발사 연결을 확인하세요. 시간표는 변경하지 않았습니다.",this);return;}
            }
            if(missile&&missile.Flying&&flightDt>0)missile.Advance(flightDt);
            hoodVFX.SetTimelineTime(clock,HasFired?clock-run.fireTime:-1,true);
        }
        void ApplySmokeSettings(){bodySmoke.Apply(hoodVFX);if(hoodVFX){hoodVFX.interiorAccumulation=interiorSmoke;hoodVFX.interiorAccumulationEnabled=bodySmoke.enabled;}foreach(var t in trails)missileSmoke.Apply(t);}
        public void Pause(){if(State!=SequenceState.Playing)return;State=SequenceState.Paused;SetPaused(true);}
        public void Resume(){if(State!=SequenceState.Paused)return;SetPaused(false);State=SequenceState.Playing;}
        void SetPaused(bool value){if(hoodVFX)foreach(var e in hoodVFX.GetComponentsInChildren<VisualEffect>(true))e.pause=value;foreach(var t in trails)if(t)t.SetPaused(value);}
        public void ResetAll()
        {
            if(!Application.isPlaying)return;Acquire();SetPaused(false);
            foreach(var t in trails)if(t&&!t.transform.IsChildOf(transform))Destroy(t.gameObject);
            if(launcher){launcher.StopExternalSequence();if(launcher.slots!=null)foreach(var s in launcher.slots)if(s?.loadedMissile){var t=s.loadedMissile.transform;t.localPosition=s.localPosition;t.localRotation=s.localRotation;t.localScale=s.localScale;}launcher.ResetAndReload();}
            trails.Clear();if(launcher&&launcher.slots!=null)foreach(var s in launcher.slots)if(s?.loadedMissile){var t=s.loadedMissile.GetComponentInChildren<MissileExhaustVFXController>(true);if(t)trails.Add(t);}
            State=SequenceState.Idle;HasFired=false;clock=0;missile=null;run=null;ApplySmokeSettings();if(hoodVFX)hoodVFX.ResetVFX();
        }
        // Used only for explicit editor migration, never automatically in OnValidate.
        public void CaptureCurrentConfiguration()
        {
            if(!launcher)launcher=GetComponentInChildren<TrailerMissileLauncher>(true);
            if(!deployment&&launcher)deployment=launcher.deployment;
            if(!hoodVFX)hoodVFX=GetComponentInChildren<HoodMissileVFXController_Final>(true);
            if(!launcher||!deployment)return;
            playback.useShift=launcher.useShiftInput;playback.playOnStart=launcher.playOnStart;playback.startDelay=launcher.sequenceStartDelay;
            motion.hoodStart=0;motion.hoodDuration=deployment.openDuration;motion.hoodAngle=deployment.openAngle;
            motion.bayStart=deployment.openDuration+deployment.delayAfterHatch;motion.bayDuration=deployment.liftDuration;motion.bayHeightCm=deployment.liftOffset.y*100;bayLateralOffset=deployment.liftOffset;
            motion.missileNumber=launcher.launchOrder!=null&&launcher.launchOrder.Length>0?launcher.launchOrder[0]:4;
            motion.liftMissile=launcher.enablePreLaunchLift&&launcher.preLaunchMissileNumber==motion.missileNumber;
            motion.missileLiftHeightCm=launcher.preLaunchLiftHeightCm;motion.missileLiftDuration=launcher.preLaunchLiftDuration;
            motion.fireTime=launcher.FirstLaunchTime-launcher.sequenceStartDelay;
            motion.missileLiftStart=motion.fireTime-(motion.liftMissile?launcher.preLaunchLiftDuration+launcher.preLaunchHoldDuration:0);
            motion.speed=launcher.missileSpeed;motion.acceleration=launcher.missileAcceleration;motion.clearanceHeight=launcher.clearanceHeight;motion.clearanceForward=launcher.clearanceForwardDistance;motion.clearanceDuration=launcher.clearanceTime;motion.flightLifetime=launcher.missileLifetime;
            CopyFields(hoodVFX,bodySmoke);
            if(launcher.slots!=null)foreach(var s in launcher.slots)if(s?.loadedMissile){var t=s.loadedMissile.GetComponentInChildren<MissileExhaustVFXController>(true);if(t){CopyFields(t,missileSmoke);break;}}
        }
        static void CopyFields(object from,object to){if(from==null)return;foreach(var field in to.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance)){var source=from.GetType().GetField(field.Name);if(source!=null&&source.FieldType==field.FieldType)field.SetValue(to,source.GetValue(from));}}
        static bool ShiftPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if(Keyboard.current!=null)return Keyboard.current.leftShiftKey.wasPressedThisFrame||Keyboard.current.rightShiftKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.RightShift);
#else
            return false;
#endif
        }
    }
}
