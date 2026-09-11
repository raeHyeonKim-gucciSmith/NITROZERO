using UnityEngine;
using UnityEngine.VFX;
using Damin.Trailer.MissileCar;

namespace Damin.Trailer.FinalVFX
{
    [DefaultExecutionOrder(110), DisallowMultipleComponent]
    public sealed class MissileExhaustVFXController : MonoBehaviour
    {
        [Header("미사일 ExhaustPoint 아래 — 비행 이벤트 자동 연결")]
        public VisualEffect core, flame, smokeTrail;
        [Header("실제 발사 기준 / 초")]
        [Min(0)] public float exhaustStartDelay = .02f;
        [Header("지속 추진 화염")]
        [Min(0)] public float intensity = 1f;
        [Min(.001f)] public float coreWidth = .035f;
        [Min(.001f)] public float flameWidth = .055f;
        [Min(0)] public float flameLength = .3f;
        [ColorUsage(false,true)] public Color coreColor = new Color(8,6,2);
        [ColorUsage(false,true)] public Color flameColor = new Color(5,1.3f,.12f);
        [Header("월드 공간에 남는 연기 Trail")]
        [Min(0)] public float trailSpawnRate = 250f;
        [Range(0,4)] public float trailDensity = 1f;
        [Min(0)] public float trailSpeed = 1.5f;
        [Min(.1f)] public float trailLength = 6f;
        [Min(.001f)] public float trailWidth = .075f;
        [Min(0)] public float trailExpansion = .35f;
        [Min(0)] public float trailTurbulence = .25f;
        [Min(.1f)] public float maxSmokeLifetime = 3f;
        public Color trailColor = new Color(.75f,.77f,.8f);
        [Tooltip("고속 이동 시 입자 사이 간격 보정. 발사량 상한은 아래에서 제한합니다.")]
        [Min(0)] public float extraParticlesPerMeter = 12f;
        [Min(1)] public float maxTrailSpawnRate = 1200f;
        [Min(.01f)] public float tailCleanupPadding = .25f;
        [Range(.1f,.95f)] public float tailFadeStart = .6f;
        [Min(0)] public float brightCoreIntensity = 1f;
        public bool Ignited { get; private set; }
        public bool Draining { get; private set; }
        public float EmissionLevel { get; private set; }
        [HideInInspector] public bool emissionEnabled=true;
        bool externallyPaused;
        public void SetPaused(bool value){externallyPaused=value;foreach(var e in new[]{core,flame,smokeTrail})if(e)e.pause=value;}
        TrailerMissileFlight flight;
        float elapsed,cleanupRemaining,measuredSpeed;
        Vector3 previousPosition, segmentStart;
        void OnEnable()
        {
            flight=GetComponentInParent<TrailerMissileFlight>();
            if(flight){flight.onLaunched.AddListener(Ignite);flight.onFlightEnded.AddListener(EndFlight);}
            Ignited=false;Draining=false;elapsed=0;EmissionLevel=0;previousPosition=segmentStart=transform.position;
            foreach(var effect in new[]{core,flame,smokeTrail})if(effect){VFXValues.Float(effect,"Emission",0);effect.Reinit();}
            Apply(0);
            if(flight&&flight.Flying)Ignite();
        }
        void Unsubscribe(){if(flight){flight.onLaunched.RemoveListener(Ignite);flight.onFlightEnded.RemoveListener(EndFlight);}}
        void OnDisable(){Unsubscribe();}
        public void Ignite(){if(Ignited)return;Ignited=true;elapsed=0;previousPosition=segmentStart=transform.position;measuredSpeed=0;}
        void LateUpdate()
        {
            if(externallyPaused)return;
            float dt=Time.deltaTime;
            if(Draining){Apply(0);cleanupRemaining-=dt;if(cleanupRemaining<=0)Destroy(gameObject);return;}
            segmentStart=previousPosition;
            if(dt>0)measuredSpeed=Vector3.Distance(previousPosition,transform.position)/dt;
            previousPosition=transform.position;
            if(Ignited)elapsed+=dt;
            Apply(Ignited&&elapsed>=exhaustStartDelay?1:0);
        }
        void Apply(float level)
        {
            if(!emissionEnabled)level=0;
            EmissionLevel=level;
            foreach(var effect in new[]{core,flame}){VFXValues.Float(effect,"Emission",level);VFXValues.Float(effect,"Intensity",intensity);VFXValues.Float(effect,"ParticleSize",effect==core?coreWidth:flameWidth);VFXValues.Float(effect,"Speed",effect==core?1.4f:flameLength/.16f);VFXValues.Color(effect,"FlameColor",effect==core?coreColor:flameColor);}
            VFXValues.Float(smokeTrail,"Emission",level);VFXValues.Float(smokeTrail,"SpawnRate",Mathf.Min(maxTrailSpawnRate,trailSpawnRate+measuredSpeed*extraParticlesPerMeter));
            VFXValues.Float(smokeTrail,"SmokeDensity",trailDensity);
            VFXValues.Float(smokeTrail,"TailFadeStart",tailFadeStart);
            VFXValues.Float(smokeTrail,"CoreIntensity",brightCoreIntensity);
            VFXValues.Float(smokeTrail,"Speed",trailSpeed);VFXValues.Float(smokeTrail,"MissileSpeed",measuredSpeed);VFXValues.Float(smokeTrail,"TrailLength",trailLength);
            VFXValues.Float(smokeTrail,"MaxSmokeLifetime",maxSmokeLifetime);VFXValues.Float(smokeTrail,"ParticleSize",trailWidth);VFXValues.Float(smokeTrail,"Expansion",trailExpansion);
            VFXValues.Float(smokeTrail,"Turbulence",trailTurbulence);VFXValues.Color(smokeTrail,"SmokeColor",trailColor);
            VFXValues.Vector(smokeTrail,"EmitterPosition",transform.position);VFXValues.Vector(smokeTrail,"PreviousEmitterPosition",segmentStart);VFXValues.Vector(smokeTrail,"EmitterDirection",transform.forward);
        }
        public void EndFlight()
        {
            if(Draining)return;Draining=true;Ignited=false;Apply(0);Unsubscribe();flight=null;
            // Keep existing WORLD particles alive after the missile model is destroyed.
            transform.SetParent(null,true);cleanupRemaining=Mathf.Max(.1f,maxSmokeLifetime)+tailCleanupPadding;
        }
    }
}
