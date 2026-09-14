using UnityEngine;
namespace Damin.SceneOnly
{
    [DisallowMultipleComponent]
    public sealed class DMFilmAudio : MonoBehaviour
    {
        public DMCinematicDirector director;
        [Tooltip("이 씬의 임시 사운드 믹스. 원본 오디오 파일은 수정하지 않습니다.")]
        public bool enablePreviewAudio=true;
        [Range(0,1)] public float masterVolume=.55f;
        public AudioClip redEngine,blueEngine,khakiEngine,wind,drift,launch,boost;
        AudioSource[] engines;
        AudioSource air,events;
        float previousTime=-1;
        int previousPhase=-1;
        bool started;
        AudioSource Source(Transform parent,string name,AudioClip clip,bool spatial,bool loop){
            var g=new GameObject(name);g.transform.SetParent(parent,false);var s=g.AddComponent<AudioSource>();
            s.playOnAwake=false;s.clip=clip;s.loop=loop;s.spatialBlend=spatial?1:0;s.minDistance=5;s.maxDistance=120;s.dopplerLevel=spatial?.35f:0;s.volume=0;return s;
        }
        void Start(){if(!director)return;engines=new[]{Source(director.redCar,"DM_Audio_Red",redEngine,true,true),Source(director.blueCar,"DM_Audio_Blue",blueEngine,true,true),Source(director.khakiCar,"DM_Audio_Khaki",khakiEngine,true,true)};air=Source(transform,"DM_Audio_Wind",wind,false,true);events=Source(transform,"DM_Audio_Cues",null,false,false);}
        void Update(){
            if(!director||engines==null)return;
            if(!enablePreviewAudio||!director.Playing||director.ManualSimulation){StopSources();return;}
            if(!started||director.FilmTime<previousTime){StopSources();foreach(var s in engines)if(s.clip)s.Play();if(air.clip)air.Play();started=true;previousPhase=-1;}
            int p=director.Phase;float cinematicQuiet=p>=8&&p<=10?.18f:1;
            float pitch=p>=8&&p<=10?.6f:p==7?.85f:p==12?1.35f:1.05f;
            for(int i=0;i<engines.Length;i++){engines[i].volume=masterVolume*cinematicQuiet*(i==0?.4f:.24f);engines[i].pitch=pitch+(i-1)*.03f;}
            air.volume=masterVolume*.12f*cinematicQuiet;air.pitch=pitch;events.volume=masterVolume*.45f;
            if(p!=previousPhase){if(p==6&&launch)events.PlayOneShot(launch,.8f);if(p==7&&drift)events.PlayOneShot(drift,.65f);if(p==12&&boost)events.PlayOneShot(boost,.8f);if(p==8)events.Stop();previousPhase=p;}
            previousTime=director.FilmTime;
        }
        void StopSources(){if(!started)return;foreach(var s in engines)if(s)s.Stop();if(air)air.Stop();if(events)events.Stop();started=false;previousPhase=-1;}
        void OnDisable(){StopSources();}
        void OnDestroy(){if(engines!=null)foreach(var s in engines)if(s)Destroy(s.gameObject);if(air)Destroy(air.gameObject);if(events)Destroy(events.gameObject);}
    }
}
