using UnityEngine;
using UnityEngine.VFX;
using Damin.Trailer.FinalVFX;

namespace Damin.CinematicCopy
{
    // One runtime-only outer smoke layer. No material, graph or prefab asset is edited.
    [DefaultExecutionOrder(1500)]
    public sealed class DMCinematicWake : MonoBehaviour
    {
        public MissileExhaustVFXController source;
        public DMCinematicDirector director;
        [Range(0,1)] public float density=.22f;
        VisualEffect wisps;
        float age;
        void Start(){
            if(!source||!source.smokeTrail)return;
            wisps=Instantiate(source.smokeTrail,source.smokeTrail.transform.parent);
            wisps.name="DM_Cinematic_OuterWisps_Runtime";
            wisps.resetSeedOnPlay=false;wisps.startSeed=17391;
            Set("Emission",0);wisps.Reinit();
        }
        void Set(string key,float value){if(wisps&&wisps.HasFloat(key))wisps.SetFloat(key,value);}
        void LateUpdate(){
            if(!wisps||!source||!director)return;
            if(director.Playing)age+=Time.deltaTime;
            var main=source.smokeTrail;
            foreach(string key in new[]{"EmitterPosition","PreviousEmitterPosition","EmitterDirection"})if(wisps.HasVector3(key)&&main.HasVector3(key))wisps.SetVector3(key,main.GetVector3(key));
            float pulse=.8f+.12f*Mathf.Sin(age*7.3f)+.08f*Mathf.Sin(age*13.1f+1.8f);
            Set("Emission",director.Playing?source.EmissionLevel*Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.16f)):0);
            Set("SpawnRate",Mathf.Min(4400,main.GetFloat("SpawnRate")*.65f)*pulse);
            Set("SmokeDensity",density);Set("ParticleSize",.075f);Set("Expansion",1.15f);
            Set("Turbulence",.48f);Set("Speed",2.3f);Set("MissileSpeed",main.GetFloat("MissileSpeed"));
            Set("TrailLength",38);Set("MaxSmokeLifetime",1.5f);Set("TailFadeStart",.32f);Set("CoreIntensity",0);
            if(wisps.HasVector4("SmokeColor"))wisps.SetVector4("SmokeColor",new Vector4(.74f,.74f,.74f,1));
            wisps.playRate=main.playRate;
        }
    }
}
