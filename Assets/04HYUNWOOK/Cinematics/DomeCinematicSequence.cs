using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Unity.Cinemachine;

namespace Nitrozero.Cinematics
{
[DisallowMultipleComponent]
public sealed class DomeCinematicSequence : MonoBehaviour
{
    public const float Duration = 20f;
    public bool useSplineDolly;
    public bool fastFourShot;
    public bool sixShot;
    public bool omitRollShot;
    public bool equalShotDurations;
    public bool omitObliqueShot;
    static readonly float[] fourEqualCuts={0,1.25f,2.5f,3.75f,5f};
    static readonly float[] equalCuts={0,1,2,3,4,5};
    static readonly float[] noRollCuts={0,.6f,1.4f,2.2f,3.7f,5f};
    static readonly float[] sixCuts={0,.6f,1.4f,2.2f,3f,3.7f,5f};
    public float fastSpeedMetresPerSecond=100f;
    [InspectorName("차량 출발 앞당김 (초)"), Min(0)]
    [Tooltip("카메라 시간은 그대로 두고 차량의 기본 주행 위치를 이 시간만큼 앞당깁니다.")]
    public float vehicleDepartureAdvanceSeconds;
    [InspectorName("1번 컷 종료 기준 차량 출발")]
    public bool departBeforeFirstShotEnds;
    [InspectorName("1번 컷 종료 전 출발 시간 (초)"), Min(0)]
    public float firstShotDepartureLeadSeconds=.5f;
    public bool continueVehiclesAfterCamera;
    [InspectorName("마지막 컷 차량 가속")]
    public bool accelerateLastShot;
    [InspectorName("마지막 컷 시작 속도 별도 설정")]
    public bool overrideLastShotStartSpeed;
    [InspectorName("마지막 컷 시작 속도 (km/h)"), Min(0)]
    public float lastShotStartSpeedKmh=400f;
    [InspectorName("마지막 컷 시작 속도 유지 시간 (초)"), Min(0)]
    public float lastShotStartHoldSeconds;
    [InspectorName("마지막 컷 차량 속도 (km/h)"), Min(0)]
    public float lastShotSpeedKmh=450f;
    [InspectorName("마지막 컷 가속 시간 (초)"), Min(0)]
    public float lastShotAccelerationSeconds=.15f;
    [InspectorName("연출 종료 시 플레이 자동 종료")]
    [Tooltip("Unity Editor에서는 Timeline 종료 시 플레이 모드를 종료합니다. 빌드에서는 시퀀스만 정지합니다.")]
    public bool stopPlayAtEnd;
    public bool freezeCameraAfterEnd = true;
    [Tooltip("끄면 차량 Transform을 직접 조정할 수 있습니다.")]
    public bool driveVehicles = true;
    public Vector3 redVehicleOffset;
    public Vector3 blueVehicleOffset;
    float continuationSeconds;
    bool continuingVehicles;
    double playbackStartedAt = -1;
    int completionFrame = -1;
    static readonly float[] fastCuts={0,.7f,1.9f,3.3f,4.8f};
    public CinemachineSplineDolly[] dollies = Array.Empty<CinemachineSplineDolly>();
    public DomeDollyAim[] dollyAim = Array.Empty<DomeDollyAim>();
    public float PlaybackDuration => director && director.playableAsset && director.playableAsset.duration>0
        && !double.IsInfinity(director.playableAsset.duration) ? (float)director.playableAsset.duration
        : (sixShot ? 5f : (useSplineDolly ? 4.8f : Duration));
    public Camera cam;
    public Camera[] shotCameras = Array.Empty<Camera>();
    public Transform red;
    public Transform car;
    public PlayableDirector director;
    public Vector3 portal = new Vector3(-497, -19.65f, 28);
    public Vector3 start;
    public Vector3 redStart;
    [Tooltip("Controllers that would overwrite the cinematic pose; restored when stopped.")]
    public Behaviour[] suspendDuringSequence = Array.Empty<Behaviour>();
    static readonly float[] newer = {0, 2.4f, 6.2f, 8, 10.4f, 12.5f, 16, 20};
    static readonly float[] previous = {0, 2, 5.2f, 6.3f, 8.3f, 10, 12, 15};
    readonly List<Behaviour> suspended = new List<Behaviour>();
    readonly List<PoseSnapshot> poses = new List<PoseSnapshot>();
    readonly List<BodySnapshot> bodies = new List<BodySnapshot>();
    bool captured;
    readonly List<CameraSnapshot> cameras = new List<CameraSnapshot>();
    struct CameraSnapshot { public Camera target; public bool enabled; public float fov; public AudioListener listener; public bool listenerEnabled; }
    public bool IsPreviewing => captured;
    struct PoseSnapshot
    {
        public Transform target;
        public Vector3 position, scale;
        public Quaternion rotation;
    }
    struct BodySnapshot
    {
        public Rigidbody target;
        public bool kinematic;
        public Vector3 velocity, angularVelocity;
    }
    static float E(float x) { x = Mathf.Clamp01(x); return x*x*(3-2*x); }
    public bool Begin()
    {
        if (captured) return true;
        if (!cam || !red || !car) return false;
        captured = true;
        foreach (var camera in shotCameras.Length > 0 ? shotCameras : new[] {cam})
        {
            if (!camera) continue;
            var listener=camera.GetComponent<AudioListener>();
            cameras.Add(new CameraSnapshot {target=camera, enabled=camera.enabled, fov=camera.fieldOfView,
                listener=listener, listenerEnabled=listener && listener.enabled});
            Capture(camera.transform);
        }
        foreach (var root in new[] {red, car})
        {
            foreach (var tr in root.GetComponentsInChildren<Transform>(true)) Capture(tr);
            foreach (var body in root.GetComponentsInChildren<Rigidbody>(true))
            {
                bodies.Add(new BodySnapshot {target=body, kinematic=body.isKinematic,
                    velocity=body.linearVelocity, angularVelocity=body.angularVelocity});
                body.isKinematic = true;
            }
        }
        foreach (var component in suspendDuringSequence)
            if (component && component.enabled && component != this)
            { suspended.Add(component); component.enabled = false; }
        ActivateCamera(0);
        playbackStartedAt = Application.isPlaying ? Time.realtimeSinceStartupAsDouble : -1;
        completionFrame = -1;
        return true;
    }
    void Capture(Transform tr) => poses.Add(new PoseSnapshot {
        target=tr, position=tr.localPosition, rotation=tr.localRotation, scale=tr.localScale});
    public void Evaluate(float seconds, int timelineShot = -1, float timelineProgress = 0)
    {
        if (!Begin()) return;
        if(fastFourShot || sixShot)
        {
            seconds=Mathf.Clamp(seconds,0,PlaybackDuration);
            ActivateCamera(0);
            MoveFastVehicles(continuingVehicles ? PlaybackDuration+continuationSeconds : seconds);
            var cuts=sixShot?(omitRollShot?(equalShotDurations?(omitObliqueShot?fourEqualCuts:equalCuts):noRollCuts):sixCuts):fastCuts;
            int index=0;while(index<cuts.Length-2&&seconds>=cuts[index+1])index++;
            float progress=Mathf.InverseLerp(cuts[index],cuts[index+1],seconds);
            if(timelineShot>=0){index=timelineShot;progress=timelineProgress;}
            if(index<dollyAim.Length&&dollyAim[index]&&dollyAim[index].enabled)
                dollyAim[index].ApplyTimelineProgress(progress,index<dollies.Length?dollies[index]:null);
            return;
        }
        seconds=Mathf.Clamp(seconds, 0, Duration);
        int shot=0;while(shot<6 && seconds>=newer[shot+1])shot++;
        if (useSplineDolly)
        {
            ActivateCamera(0);
            MoveVehicles(seconds);
            float u=Mathf.InverseLerp(newer[shot],newer[shot+1],seconds);
            if(shot<dollies.Length && dollies[shot] && dollies[shot].Spline)
                dollies[shot].CameraPosition=u*(dollies[shot].Spline.Spline.Count-1);
            if(shot<dollyAim.Length && dollyAim[shot])dollyAim[shot].position=u;
        }
        else { ActivateCamera(shot); Pose(seconds); }
    }
    void MoveFastVehicles(float seconds)
    {
        if(!driveVehicles)return;
        float distance=GetVehicleTravelDistance(seconds);
        if(car)car.position=new Vector3(portal.x+distance+17,start.y+.1f,start.z)+blueVehicleOffset;
        if(red)red.position=GetRedPositionAtTime(seconds);
    }
    public float LastShotStartTime
    {
        get
        {
            if(director && director.playableAsset is UnityEngine.Timeline.TimelineAsset timeline)
                foreach(var track in timeline.GetOutputTracks())
                    if(track is DomeCinematicTrack)
                    {
                        float last=0;
                        foreach(var clip in track.GetClips())last=Mathf.Max(last,(float)clip.start);
                        return last;
                    }
            return 3.75f;
        }
    }
    public Vector3 GetRedPositionAtTime(float seconds) =>
        new Vector3(portal.x+GetVehicleTravelDistance(seconds),redStart.y+.3f,redStart.z)+redVehicleOffset;
    public float VehicleDepartureTime
    {
        get
        {
            float firstEnd=1.5f;
            if(director && director.playableAsset is UnityEngine.Timeline.TimelineAsset timeline)
                foreach(var track in timeline.GetOutputTracks())
                    if(track is DomeCinematicTrack)
                    {
                        double earliest=double.PositiveInfinity;
                        foreach(var clip in track.GetClips())
                            if(clip.start<earliest){earliest=clip.start;firstEnd=(float)clip.end;}
                        break;
                    }
            return Mathf.Max(0,firstEnd-Mathf.Max(0,firstShotDepartureLeadSeconds));
        }
    }
    float GetVehicleTravelDistance(float seconds)
    {
        float distance=-41+fastSpeedMetresPerSecond*(seconds+Mathf.Max(0,vehicleDepartureAdvanceSeconds)-(sixShot?(equalShotDurations?(omitObliqueShot?2.5f:2f):1.4f):1.9f));
        if(departBeforeFirstShotEnds)
            distance=-41+fastSpeedMetresPerSecond*Mathf.Max(0,seconds-VehicleDepartureTime);
        if(accelerateLastShot)
        {
            float lastStart=LastShotStartTime;
            if(departBeforeFirstShotEnds)lastStart=Mathf.Max(lastStart,VehicleDepartureTime);
            float elapsed=Mathf.Max(0,seconds-lastStart);
            float initialSpeed=overrideLastShotStartSpeed ? Mathf.Max(0,lastShotStartSpeedKmh)/3.6f : fastSpeedMetresPerSecond;
            // Integrate from the cut boundary so changing speed never teleports a car.
            distance+=(initialSpeed-fastSpeedMetresPerSecond)*elapsed;
            float t=Mathf.Max(0,elapsed-Mathf.Max(0,lastShotStartHoldSeconds)),r=Mathf.Max(0,lastShotAccelerationSeconds);
            float integrated=t;
            if(r>.0001f)
            {
                float u=Mathf.Clamp01(t/r);
                integrated=r*(u*u*u-.5f*u*u*u*u)+Mathf.Max(0,t-r);
            }
            distance+=(Mathf.Max(0,lastShotSpeedKmh)/3.6f-initialSpeed)*integrated;
        }
        return distance;
    }
    void LateUpdate()
    {
        if(!Application.isPlaying || !captured || !sixShot || !director) return;
        if(playbackStartedAt < 0) playbackStartedAt = Time.realtimeSinceStartupAsDouble;
        if(stopPlayAtEnd && director.time>=PlaybackDuration-.0001)
        {
            // Timeline can reach its endpoint during initialization or before this
            // frame renders. Keep the final camera alive until both clocks finish,
            // then allow a full frame before restoring the scene and exiting Play.
            if(Time.realtimeSinceStartupAsDouble-playbackStartedAt < PlaybackDuration) return;
            if(completionFrame < 0) { completionFrame = Time.frameCount; return; }
            if(Time.frameCount <= completionFrame) return;
            StopAndRestore();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#endif
            return;
        }
        completionFrame = -1;
        if(!continueVehiclesAfterCamera)return;
        if(!continuingVehicles)
        {
            if(director.time<PlaybackDuration-.0001) return;
            continuingVehicles=true;
            continuationSeconds=0;
            // Hold the final shot's orientation while the cars drive away.
        }
        if(dollyAim.Length>0 && dollyAim[dollyAim.Length-1])
            dollyAim[dollyAim.Length-1].holdOrientation=freezeCameraAfterEnd;
        continuationSeconds+=Time.deltaTime;
        MoveFastVehicles(PlaybackDuration+continuationSeconds);
    }
    // Pure authoring function for the approved four-shot preview; creates no images or scene mutations.
    public void GetFourShotPose(float time,out Vector3 position,out Quaternion rotation,out float fov)
    {
        if(time<.7f)
        {
            float t=Mathf.Lerp(.6f,1.05f,time/.7f),s=E(t/2);
            position=portal+new Vector3(600-25*s,85,-360+20*s);
            var target=portal+Vector3.Lerp(new Vector3(-190,85,0),new Vector3(-270,115,200),s);
            rotation=Quaternion.LookRotation(target-position,Vector3.up);fov=58;return;
        }
        if(time<1.9f)
        {
            float t=Mathf.Lerp(2.5f,5.2f,(time-.7f)/1.2f),u=Mathf.Clamp01((t-2)/3.2f);
            float s=u*u*u*(u*(u*6-15)+10);
            position=portal+new Vector3(202-8*s,1.1f+2*s,0);
            var low=Quaternion.LookRotation(new Vector3(-5,-1,0),Vector3.up);
            var high=Quaternion.LookRotation(portal+new Vector3(-125,70,0)-position,Vector3.up);
            rotation=Quaternion.Slerp(low,high,s);fov=57;return;
        }
        if(time<3.3f)
        {
            position=portal+new Vector3(35,180,0);
            rotation=Quaternion.LookRotation(Vector3.down,Vector3.forward);fov=63;return;
        }
        float passDistance=-41+fastSpeedMetresPerSecond*(4.05f-1.9f);
        position=new Vector3(portal.x+passDistance,portal.y+.16f,redStart.z);
        float dx=fastSpeedMetresPerSecond*(time-4.05f);
        float pitch=-Mathf.Atan2(Mathf.Max(.1f,redStart.y+.55f-position.y),Mathf.Abs(dx))*Mathf.Rad2Deg;
        rotation=Quaternion.Euler(pitch,dx<0?-90:90,0);fov=76;
    }
    public void GetSixShotPose(float time,out Vector3 position,out Quaternion rotation,out float fov)
    {
        if(omitRollShot && equalShotDurations && omitObliqueShot)
        {
            if(time<1.25f)
            {
                float u=time/1.25f;
                GetFourShotPose(u*.7f,out position,out rotation,out fov);
                rotation=Quaternion.AngleAxis(12f*E(u),Vector3.up)*rotation;
                fov=63;return;
            }
            if(time<2.5f)
            {GetFourShotPose(.7f+(time-1.25f)/1.25f*1.1999f,out position,out rotation,out fov);return;}
            var target=new Vector3(portal.x-41+fastSpeedMetresPerSecond*(time-2.5f),redStart.y+.3f,redStart.z);
            if(time<3.75f)
            {
                position=target+new Vector3(0,180,0);
                rotation=Quaternion.LookRotation(Vector3.down,Vector3.forward);fov=63;return;
            }
            position=new Vector3(portal.x-41+fastSpeedMetresPerSecond*(4.375f-2.5f),portal.y+.16f,redStart.z);
            float delta=target.x-position.x;
            float tilt=-Mathf.Atan2(Mathf.Max(.1f,target.y+.25f-position.y),Mathf.Abs(delta))*Mathf.Rad2Deg;
            rotation=Quaternion.Euler(tilt,delta<0?-90:90,0);fov=76;return;
        }
        if(omitRollShot && equalShotDurations)
        {
            if(time<1f)
            {
                GetFourShotPose(time*.7f,out position,out rotation,out fov);
                rotation=Quaternion.AngleAxis(12f*E(time),Vector3.up)*rotation;
                fov=63;return;
            }
            if(time<2f) {GetFourShotPose(.7f+(time-1f)*1.1999f,out position,out rotation,out fov);return;}
            var target=new Vector3(portal.x-41+fastSpeedMetresPerSecond*(time-2f),redStart.y+.3f,redStart.z);
            if(time<3f)
            {
                position=target+new Vector3(0,180,0);
                rotation=Quaternion.LookRotation(Vector3.down,Vector3.forward);fov=63;return;
            }
            if(time<4f)
            {
                position=target+Vector3.Lerp(new Vector3(-30,60,-75),new Vector3(-10,34,-48),E(time-3f));
                rotation=Quaternion.LookRotation(target-position,Vector3.up);fov=53;return;
            }
            position=new Vector3(portal.x-41+fastSpeedMetresPerSecond*2.5f,portal.y+.16f,redStart.z);
            float delta=target.x-position.x;
            float tilt=-Mathf.Atan2(Mathf.Max(.1f,target.y+.25f-position.y),Mathf.Abs(delta))*Mathf.Rad2Deg;
            rotation=Quaternion.Euler(tilt,delta<0?-90:90,0);fov=76;return;
        }
        if(time<.6f) {GetFourShotPose(time/.6f*.7f,out position,out rotation,out fov);return;}
        if(time<1.4f) {GetFourShotPose(.7f+(time-.6f)/.8f*1.1999f,out position,out rotation,out fov);return;}
        var redPosition=new Vector3(portal.x-41+fastSpeedMetresPerSecond*(time-1.4f),redStart.y+.3f,redStart.z);
        if(time<2.2f)
        {
            position=redPosition+new Vector3(0,180,0);
            rotation=Quaternion.LookRotation(Vector3.down,Vector3.forward);fov=63;return;
        }
        if(time<(omitRollShot?3.7f:3f))
        {
            float u=E((time-2.2f)/(omitRollShot?1.5f:.8f));
            position=redPosition+Vector3.Lerp(new Vector3(-30,60,-75),new Vector3(-10,34,-48),u);
            rotation=Quaternion.LookRotation(redPosition-position,Vector3.up);fov=53;return;
        }
        if(time<3.7f)
        {
            float u=E((time-3f)/.7f);
            position=redPosition+Vector3.Lerp(new Vector3(-31,13,-3),new Vector3(-21,4,-3),u);
            rotation=Quaternion.LookRotation(redPosition+new Vector3(7,1,0)-position,Vector3.up)*Quaternion.AngleAxis(180*(1-u),Vector3.forward);
            fov=67;return;
        }
        position=new Vector3(portal.x-41+fastSpeedMetresPerSecond*(4.35f-1.4f),portal.y+.16f,redStart.z);
        float dx=redPosition.x-position.x;
        float pitch=-Mathf.Atan2(Mathf.Max(.1f,redPosition.y+.25f-position.y),Mathf.Abs(dx))*Mathf.Rad2Deg;
        rotation=Quaternion.Euler(pitch,dx<0?-90:90,0);fov=76;
    }
    void ActivateCamera(int index)
    {
        if (shotCameras.Length > index && shotCameras[index]) cam=shotCameras[index];
        foreach (var snapshot in cameras)
        {
            if (snapshot.target) snapshot.target.enabled=snapshot.target==cam;
            if (snapshot.listener) snapshot.listener.enabled=snapshot.target==cam;
        }
    }
    public void Restore()
    {
        playbackStartedAt = -1;
        completionFrame = -1;
        continuingVehicles=false;
        continuationSeconds=0;
        foreach(var aim in dollyAim)if(aim)aim.holdOrientation=false;
        if (!captured) return;
        captured = false;
        foreach (var component in suspended) if (component) component.enabled = true;
        foreach (var snapshot in poses)
            if (snapshot.target)
            {
                snapshot.target.SetLocalPositionAndRotation(snapshot.position, snapshot.rotation);
                snapshot.target.localScale = snapshot.scale;
            }
        foreach (var snapshot in bodies)
            if (snapshot.target)
            {
                snapshot.target.isKinematic = snapshot.kinematic;
                if (!snapshot.kinematic)
                { snapshot.target.linearVelocity=snapshot.velocity; snapshot.target.angularVelocity=snapshot.angularVelocity; }
            }
        foreach(var snapshot in cameras)
        {
            if(snapshot.target) { snapshot.target.enabled=snapshot.enabled; snapshot.target.fieldOfView=snapshot.fov; }
            if(snapshot.listener) snapshot.listener.enabled=snapshot.listenerEnabled;
        }
        cameras.Clear();
        if(shotCameras.Length>0)cam=shotCameras[0];
        suspended.Clear(); poses.Clear(); bodies.Clear();
    }
    public void OnTimelineGraphStopped()
    {
        // Hold mode can pause the graph at its natural end. Keep the captured
        // vehicle state until an explicit stop, graph destruction, or disable.
        if(Application.isPlaying && captured && (continueVehiclesAfterCamera || stopPlayAtEnd) && sixShot && director
            && director.extrapolationMode==DirectorWrapMode.Hold && director.time>=PlaybackDuration-.0001) return;
        Restore();
    }
    [ContextMenu("Replay 20-second cinematic")]
    public void Replay()
    {
        if (!director) return;
        director.Stop(); Restore(); director.time=0; director.Play();
    }
    [ContextMenu("Stop and restore scene")]
    public void StopAndRestore()
    {
        if (director) director.Stop(); Restore();
    }
    void OnDisable() => Restore();
    void OnDestroy() => Restore();
 static float RampIntegral(float q,float a,float w){float x=Mathf.Clamp01((q-a)/w);return w*(x*x*x-.5f*x*x*x*x)+Mathf.Max(0,q-a-w);}
 float MoveVehicles(float time){
 
 
 float t=15;for(int i=0;i<7;i++){if(time<newer[i+1]){t=Mathf.Lerp(previous[i],previous[i+1],(time-newer[i])/(newer[i+1]-newer[i]));break;}}

 float distance=-24;
 if(t>=4.5f&&t<12)distance=-24+60*(t-4.5f);
 else if(t>=12){float q=t-12;distance=426+60*q-42*RampIntegral(q,1.25f,.3f)+72*RampIntegral(q,1.7f,.3f);}
 car.position=new Vector3(portal.x+distance,start.y+.1f,start.z);
 if(red!=null)red.position=new Vector3(portal.x+distance-17,redStart.y+.3f,redStart.z);
 return t;
 }
 void Pose(float time){
 float t=MoveVehicles(time);
 Vector3 p,target;Vector3 up=Vector3.up;float roll=0;
 if(t<2){float s=E(t/2);p=portal+new Vector3(600-25*s,85,-360+20*s);target=portal+Vector3.Lerp(new Vector3(-190,85,0),new Vector3(-270,115,200),s);cam.fieldOfView=58;}
 else if(t<5.2f){float u=Mathf.Clamp01((t-2)/3.2f);float s=u*u*u*(u*(u*6-15)+10);p=portal+new Vector3(210-16*s,1.1f+2*s,0);
 var low=Quaternion.LookRotation(new Vector3(-3,-1,0),Vector3.up);var high=Quaternion.LookRotation(portal+new Vector3(-125,70,0)-p,Vector3.up);
 cam.transform.position=p;cam.transform.rotation=Quaternion.Slerp(low,high,s);cam.fieldOfView=57;return;}
 else if(t<6.3f){p=red.position+new Vector3(0,180,0);target=red.position;up=Vector3.forward;cam.fieldOfView=63;}
 else if(t<8.3f){float s=E((t-6.3f)/2);p=red.position+Vector3.Lerp(new Vector3(-30,60,-75),new Vector3(-10,34,-48),s);target=red.position+new Vector3(8,0,3);cam.fieldOfView=53;}
 else if(t<10){float s=E((t-8.3f)/1.7f);p=red.position+new Vector3(12-4*s,1.0f,-9);target=red.position+new Vector3(1,.1f,0);cam.fieldOfView=50;}
 else if(t<12){float s=E((t-10)/2);p=red.position+Vector3.Lerp(new Vector3(-31,13,-3),new Vector3(-21,4,-3),s);target=red.position+new Vector3(7,1,0);cam.fieldOfView=67;roll=180*(1-s);}
 else{float q=t-12;float u=Mathf.Clamp01((q-.9f)/1.25f);float rotate=u*u*u*(u*(u*6-15)+10);float drift=E((q-1.65f)/1.35f);
 p=new Vector3(portal.x+493+1.2f*drift,portal.y+.16f+.12f*drift,redStart.z);
 cam.transform.position=p;cam.fieldOfView=76;
 cam.transform.rotation=Quaternion.Euler(rotate<.5f ? -3-174*rotate : -177+174*rotate,rotate<.5f ? -90 : 90,0);return;}

 cam.transform.position=p;cam.transform.rotation=Quaternion.LookRotation(target-p,up)*Quaternion.AngleAxis(roll,Vector3.forward);
 }

}
}
