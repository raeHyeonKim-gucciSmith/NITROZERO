using UnityEngine;
using UnityEngine.VFX;
using Damin.Trailer.MissileCar;

namespace Damin.Trailer.FinalVFX
{
    [System.Serializable]
    public sealed class InteriorSmokeSettings
    {
        [Min(0)] public float InteriorSmokeStartTime=.3f;
        [Min(.01f)] public float InteriorBuildUpDuration=2.2f;
        [Min(0)] public float InteriorSpawnRateMin=60, InteriorSpawnRateMax=700;
        [Min(.1f)] public float InteriorLifetimeMin=3.6f, InteriorLifetimeMax=4.8f;
        [Min(.001f)] public float InteriorStartSize=.04f, InteriorEndSize=.23f;
        [Range(0,1)] public float InteriorOpacity=.48f;
        public Color InteriorColor=new Color(.80f,.82f,.85f,1);
        [Range(0,.05f)] public float InteriorTurbulence=.012f;
        [Range(0,.08f)] public float InteriorUpwardForce=.015f;
        public Vector3 InteriorVolumeSize=new Vector3(.76f,.28f,.58f);
        public string Error(){if(InteriorSpawnRateMax<InteriorSpawnRateMin||InteriorLifetimeMax<InteriorLifetimeMin||InteriorEndSize<InteriorStartSize)return "내부 연기 Max/End 값은 Min/Start 값 이상이어야 합니다.";if(InteriorVolumeSize.x<=0||InteriorVolumeSize.y<=0||InteriorVolumeSize.z<=0)return "내부 연기 VolumeSize는 모든 축이 0보다 커야 합니다.";return null;}
    }
    [DefaultExecutionOrder(100), DisallowMultipleComponent]
    public sealed class HoodMissileVFXController_Final : MonoBehaviour
    {
        [Header("연결 — 기존 발사 스크립트가 신호를 보냅니다")]
        public TrailerMissileLauncher launcher;
        public Transform interiorVolume, leftVent, rightVent, roofProxy, floorProxy, bodyBlocker;
        [Header("내부 축적 전용 — 다른 연기의 공유 위치는 유지")]
        public Transform interiorFillVolume, interiorFloorProxy;
        public VisualEffect interior, sideLeft, sideRight, residual;
        public VisualEffect[] launchBursts;
        public InteriorSmokeSettings interiorAccumulation=new InteriorSmokeSettings();
        [System.NonSerialized] public bool interiorAccumulationEnabled=true;
        public float InteriorBuildProgress {get;private set;}
        public float InteriorActualSpawnRate {get;private set;}
        [Header("보넷 열림 시작 기준 / 초")]
        [Min(0)] public float interiorStart = 0f;
        [Min(.01f)] public float interiorFillDuration = .5f;
        [Min(0)] public float sideStart = .35f;
        [Min(.01f)] public float sideRiseDuration = .55f;
        [Range(0, .99f)] public float overflowThreshold = .75f;
        [Min(.01f)] public float maxLateralDistance = .2f;
        [Min(0)] public float interiorGrowth = .04f;
        [Header("실제 발사 후 기준 / 초")]
        [Min(0)] public float sideFadeDelay = .2f;
        [Min(.01f)] public float sideFadeDuration = 1.0f;
        [Min(0)] public float interiorFadeDelay = .45f;
        [Min(.01f)] public float interiorFadeDuration = 1.2f;
        [Min(0)] public float residualFadeDelay = .1f;
        [Min(.01f)] public float residualFadeDuration = 1.8f;
        [Header("내부에 고이는 연기")]
        [Min(0)] public float interiorSpawnRate = 150f;
        [Range(0,3)] public float interiorDensity = 1.1f;
        [Min(0)] public float interiorTurbulence = .025f;
        [Min(.01f)] public float interiorParticleSize = .17f;
        [Header("좌우 틈 — 밖으로 나온 뒤 상승")]
        [Min(0)] public float sideSpawnRate = 100f;
        [Range(0,4)] public float sideDensity = 1f;
        [Min(.01f)] public float sideParticleSize = .12f;
        [Min(0)] public float sideSpeed = .7f;
        [Min(0)] public float sideSpread = .17f;
        [Min(0)] public float sideTurbulence = .055f;
        [Min(0)] public float riseAfterDistance = .3f;
        [Min(0)] public float outsideRiseSpeed = .25f;
        [Min(0)] public float backflowSpeed = .1f;
        [Range(0,.4f)] public float sideAsymmetry = .1f;
        [Min(.1f)] public float sideLifetime=2.8f;
        [Min(0)] public float sideExpansion=.65f, sideDrag=4.2f, sideBuoyancy=.16f;
        public Color sideColor=new Color(.80f,.82f,.85f,1);
        [Range(0,8)] public float sideSwirlSpeed=2.4f;
        public float EffectiveSideStart => Mathf.Max(sideStart,interiorAccumulation.InteriorSmokeStartTime+interiorAccumulation.InteriorBuildUpDuration);
        [Header("발사 임팩트 / 잔연기")]
        [Min(0)] public float launchBurstIntensity = 1f;
        [Min(0)] public float residualSpawnRate = 50f;
        [Range(0,3)] public float residualDensity = .65f;
        [Header("공통 연기 모양")]
        public Color smokeColor = new Color(.76f,.78f,.8f);
        [Min(.05f)] public float smokeLifetime = 1.25f;
        [Min(0)] public float smokeExpansion = .24f;
        [Range(.4f,1.5f)] public float smokeRoundness = 1f;
        [Min(0)] public float collisionMargin = .025f;
        public bool showCollisionGuides = true;
        [SerializeField,HideInInspector] Vector3 roofAnchorPosition;
        [SerializeField,HideInInspector] Quaternion roofAnchorRotation=Quaternion.identity;
        [SerializeField,HideInInspector] bool roofAnchorCaptured;

        [ContextMenu("현재 지붕 프록시 위치를 보넷에 맞춰 기억")]
        public void CaptureRoofAnchor()
        {
            if(!roofProxy||!launcher||!launcher.deployment||!launcher.deployment.hatch)return;
            var hatch=launcher.deployment.hatch;roofAnchorPosition=hatch.InverseTransformPoint(roofProxy.position);
            roofAnchorRotation=Quaternion.Inverse(hatch.rotation)*roofProxy.rotation;roofAnchorCaptured=true;
        }

        public bool ActiveSequence { get; private set; }
        public bool ExternallyControlled { get; set; }
        public int BurstCount { get; private set; }
        public float InteriorLevel { get; private set; }
        public float SideLevel { get; private set; }
        float sequenceClock, afterLaunch = -1f;
        TrailerMissileFlight[] slotMissiles;
        void OnEnable()
        {
            if (launcher) { launcher.onSequenceStarted.AddListener(Begin); launcher.onMissileLaunched.AddListener(Fired); launcher.onReset.AddListener(ResetVFX); }
            ResetVFX();
        }
        void OnDisable()
        {
            if (launcher) { launcher.onSequenceStarted.RemoveListener(Begin); launcher.onMissileLaunched.RemoveListener(Fired); launcher.onReset.RemoveListener(ResetVFX); }
            foreach (var effect in GetComponentsInChildren<VisualEffect>(true)) VFXValues.Float(effect,"Emission",0);
        }
        void RememberSlots()
        {
            if (!launcher || launcher.slots == null) return;
            slotMissiles = new TrailerMissileFlight[launcher.slots.Length];
            for (int i=0;i<slotMissiles.Length;i++) slotMissiles[i]=launcher.slots[i]?.loadedMissile;
        }
        public void Begin()
        {
            if(ExternallyControlled)return;
            ActiveSequence=true; sequenceClock=launcher ? -Mathf.Max(0,launcher.sequenceStartDelay) : 0;
            afterLaunch=-1; RememberSlots(); Apply();
        }
        void LateUpdate() { if(!ExternallyControlled)Advance(Time.deltaTime); }
        public void SetTimelineTime(float time,float sinceLaunch,bool active)
        {sequenceClock=time;afterLaunch=sinceLaunch;ActiveSequence=active;Apply();}
        public void Advance(float dt)
        {
            if(ActiveSequence) { sequenceClock+=Mathf.Max(0,dt); if(afterLaunch>=0) afterLaunch+=Mathf.Max(0,dt); }
            Apply();
            if(afterLaunch>Mathf.Max(sideFadeDelay+sideFadeDuration,Mathf.Max(interiorFadeDelay+interiorFadeDuration,residualFadeDelay+residualFadeDuration))+smokeLifetime) ActiveSequence=false;
        }
        static float Rise(float time,float start,float duration) => Mathf.SmoothStep(0,1,Mathf.Clamp01((time-start)/Mathf.Max(.01f,duration)));
        static float Fade(float time,float start,float duration) => time<0 ? 1 : 1-Rise(time,start,duration);
        void Fired()
        {
            if(!launcher || !launcher.LastLaunched) return;
            var missile=launcher.LastLaunched;
            int slot=-1; if(slotMissiles!=null) for(int i=0;i<slotMissiles.Length;i++) if(slotMissiles[i]==missile)slot=i;
            if(slot<0 && launcher.slots!=null) { float closest=float.MaxValue; for(int i=0;i<launcher.slots.Length;i++) if(launcher.slots[i]?.mount){float d=(launcher.slots[i].mount.position-missile.transform.position).sqrMagnitude;if(d<closest){closest=d;slot=i;}} }
            if(!ActiveSequence) { ActiveSequence=true; sequenceClock=sideStart+sideRiseDuration; }
            afterLaunch=0; Apply();
            if(launchBursts!=null && slot>=0 && slot<launchBursts.Length && launchBursts[slot])
            {
                var effect=launchBursts[slot];
                VFXValues.Vector(effect,"EmitterPosition",effect.transform.InverseTransformPoint(missile.exhaustPoint ? missile.exhaustPoint.position : missile.transform.position));
                VFXValues.Float(effect,"Intensity",launchBurstIntensity); VFXValues.Color(effect,"SmokeColor",smokeColor);
                effect.SendEvent("Launch"); BurstCount++;
            }
        }
        void Apply()
        {
            if(roofAnchorCaptured&&roofProxy&&launcher&&launcher.deployment&&launcher.deployment.hatch){var hatch=launcher.deployment.hatch;roofProxy.SetPositionAndRotation(hatch.TransformPoint(roofAnchorPosition),hatch.rotation*roofAnchorRotation);}
            float fill=Rise(sequenceClock,interiorStart,interiorFillDuration);
            InteriorLevel=ActiveSequence ? fill*Fade(afterLaunch,interiorFadeDelay,interiorFadeDuration) : 0;
            // New interior build-up is authoritative; never leak before it has completed.
            SideLevel=ActiveSequence&&interiorAccumulationEnabled ? Rise(sequenceClock,EffectiveSideStart,sideRiseDuration)*Fade(afterLaunch,sideFadeDelay,sideFadeDuration) : 0;
            ConfigureInterior();
            Configure(sideLeft,leftVent,SideLevel*(1-sideAsymmetry),sideSpawnRate,sideDensity);
            Configure(sideRight,rightVent,SideLevel*(1+sideAsymmetry),sideSpawnRate,sideDensity);
            Configure(residual,interiorVolume,ActiveSequence&&afterLaunch>=0 ? Fade(afterLaunch,residualFadeDelay,residualFadeDuration) : 0,residualSpawnRate,residualDensity);
        }
        void Configure(VisualEffect effect,Transform point,float level,float rate,float density)
        {
            if(!effect)return;var frame=effect.transform;
            VFXValues.Float(effect,"Emission",level); VFXValues.Float(effect,"SpawnRate",rate); VFXValues.Float(effect,"SmokeDensity",density);
            VFXValues.Float(effect,"Lifetime",smokeLifetime); VFXValues.Float(effect,"Expansion",smokeExpansion); VFXValues.Color(effect,"SmokeColor",smokeColor);
            if(point){VFXValues.Vector(effect,"EmitterPosition",frame.InverseTransformPoint(point.position));VFXValues.Vector(effect,"EmitterDirection",frame.InverseTransformDirection(point.forward));}
            if(interiorVolume)VFXValues.Vector(effect,"VolumeSize",Abs(frame.InverseTransformVector(interiorVolume.TransformVector(Vector3.one))));
            VFXValues.Float(effect,"ParticleSize",effect==interior||effect==residual ? interiorParticleSize : sideParticleSize);
            VFXValues.Float(effect,"VerticalRoundness",smokeRoundness);
            VFXValues.Float(effect,"InteriorGrowth",interiorGrowth);
            VFXValues.Float(effect,"MaxLateralDistance",maxLateralDistance);
            VFXValues.Float(effect,"Turbulence",effect==interior||effect==residual ? interiorTurbulence : sideTurbulence);
            VFXValues.Float(effect,"Speed",sideSpeed); VFXValues.Float(effect,"Spread",sideSpread); VFXValues.Float(effect,"RiseAfterDistance",riseAfterDistance);
            VFXValues.Float(effect,"RiseSpeed",outsideRiseSpeed); VFXValues.Float(effect,"BackflowSpeed",backflowSpeed);
            VFXValues.Vector(effect,"BackDirection",frame.InverseTransformDirection(launcher&&launcher.vehicleFrame ? -launcher.vehicleFrame.forward : -transform.forward));
            if(roofProxy){VFXValues.Vector(effect,"RoofPosition",frame.InverseTransformPoint(roofProxy.position));VFXValues.Vector(effect,"RoofNormal",frame.InverseTransformDirection(-roofProxy.up));}
            if(floorProxy){VFXValues.Vector(effect,"FloorPosition",frame.InverseTransformPoint(floorProxy.position));VFXValues.Vector(effect,"FloorNormal",frame.InverseTransformDirection(floorProxy.up));}
            if(bodyBlocker){VFXValues.Vector(effect,"BodyCenter",frame.InverseTransformPoint(bodyBlocker.position));VFXValues.Vector(effect,"BodySize",Abs(frame.InverseTransformVector(bodyBlocker.TransformVector(Vector3.one))));}
            VFXValues.Float(effect,"CollisionMargin",collisionMargin);
            if(effect==sideLeft||effect==sideRight){
                VFXValues.Vector(effect,"UpDirection",frame.InverseTransformDirection(Vector3.up));
                VFXValues.Float(effect,"Lifetime",sideLifetime);VFXValues.Float(effect,"Expansion",sideExpansion);
                VFXValues.Float(effect,"Drag",sideDrag);VFXValues.Float(effect,"Buoyancy",sideBuoyancy);
                VFXValues.Color(effect,"SmokeColor",sideColor);
                VFXValues.Float(effect,"FlowTime",sequenceClock+(effect==sideRight?1.7f:0));
                VFXValues.Float(effect,"SwirlSpeed",sideSwirlSpeed);
            }
        }
        void ConfigureInterior()
        {
            if(!interior)return;
            var s=interiorAccumulation;float elapsed=sequenceClock-s.InteriorSmokeStartTime;
            InteriorBuildProgress=Rise(sequenceClock,s.InteriorSmokeStartTime,s.InteriorBuildUpDuration);
            float startFade=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.15f));
            float endFade=Fade(afterLaunch,interiorFadeDelay,interiorFadeDuration);
            float densityRise=1-(1-InteriorBuildProgress)*(1-InteriorBuildProgress);
            InteriorActualSpawnRate=ActiveSequence&&interiorAccumulationEnabled ? Mathf.Lerp(s.InteriorSpawnRateMin,s.InteriorSpawnRateMax,densityRise)*startFade*endFade:0;
            // Continuous rate only. Birth properties are read in Initialize, never used to flash existing particles on.
            VFXValues.Float(interior,"Emission",1);VFXValues.Float(interior,"SpawnRate",InteriorActualSpawnRate);
            VFXValues.Float(interior,"LifetimeMin",s.InteriorLifetimeMin);VFXValues.Float(interior,"LifetimeMax",Mathf.Lerp(s.InteriorLifetimeMin,s.InteriorLifetimeMax,InteriorBuildProgress));
            VFXValues.Float(interior,"StartSize",s.InteriorStartSize);VFXValues.Float(interior,"EndSize",s.InteriorEndSize);
            VFXValues.Float(interior,"BirthOpacity",s.InteriorOpacity*Mathf.Lerp(.6f,1,densityRise));
            VFXValues.Float(interior,"BirthFill",InteriorBuildProgress);
            VFXValues.Float(interior,"FlowTime",sequenceClock);
            VFXValues.Float(interior,"UpwardForce",s.InteriorUpwardForce);
            VFXValues.Float(interior,"Turbulence",s.InteriorTurbulence);VFXValues.Vector(interior,"VolumeSize",s.InteriorVolumeSize);
            VFXValues.Color(interior,"SmokeColor",s.InteriorColor);
            var frame=interior.transform;var volume=interiorFillVolume?interiorFillVolume:interiorVolume;if(volume)VFXValues.Vector(interior,"EmitterPosition",frame.InverseTransformPoint(volume.position));
            if(roofProxy){VFXValues.Vector(interior,"RoofPosition",frame.InverseTransformPoint(roofProxy.position));VFXValues.Vector(interior,"RoofNormal",frame.InverseTransformDirection(-roofProxy.up));}
            var floor=interiorFloorProxy?interiorFloorProxy:floorProxy;
            if(floor){VFXValues.Vector(interior,"FloorPosition",frame.InverseTransformPoint(floor.position));VFXValues.Vector(interior,"FloorNormal",frame.InverseTransformDirection(floor.up));}
            VFXValues.Float(interior,"CollisionMargin",collisionMargin);
        }
        static Vector3 Abs(Vector3 v)=>new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
        [ContextMenu("연기 초기화")]
        public void ResetVFX()
        {
            ActiveSequence=false;sequenceClock=0;afterLaunch=-1;BurstCount=0;RememberSlots();
            foreach(var effect in GetComponentsInChildren<VisualEffect>(true)){VFXValues.Float(effect,"Emission",0);effect.Reinit();}
            Apply();
        }
        void OnDrawGizmosSelected()
        {
            if(!showCollisionGuides)return;
            foreach(var p in new[]{interiorVolume,bodyBlocker})if(p){Gizmos.matrix=p.localToWorldMatrix;Gizmos.color=p==interiorVolume?Color.cyan:new Color(1,.5f,0);Gizmos.DrawWireCube(Vector3.zero,Vector3.one);}
            Gizmos.matrix=Matrix4x4.identity;if(roofProxy){Gizmos.color=Color.yellow;Gizmos.DrawRay(roofProxy.position,-roofProxy.up*.2f);}
        }
    }
    internal static class VFXValues
    {
        public static void Float(VisualEffect e,string n,float v){if(e&&e.HasFloat(n))e.SetFloat(n,v);}
        public static void Vector(VisualEffect e,string n,Vector3 v){if(e&&e.HasVector3(n))e.SetVector3(n,v);}
        public static void Color(VisualEffect e,string n,Color v)=>Vector(e,n,new Vector3(v.r,v.g,v.b));
    }
}
