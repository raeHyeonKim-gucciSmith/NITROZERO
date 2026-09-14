using System;
using System.Linq;
using UnityEngine;

namespace Damin.CinematicCopy
{
    // Scene-owned choreography; borrows existing wheel rigs without editing their source/prefabs.
    [DisallowMultipleComponent,DefaultExecutionOrder(1400)]
    public sealed class DMRacePerformance : MonoBehaviour
    {
        [Serializable] public sealed class Car
        {
            public Transform vehicle;
            public TrailerCruiseMotion cruise;
            public DMRaceWheelVisual visual;
            [Tooltip("촬영 앞부분 0~1 진행도에 따른 앞뒤 거리(m). 음수는 뒤처짐.")]
            public AnimationCurve advance = AnimationCurve.Linear(0,0,1,0);
            [Tooltip("같은 진행도에서 우측 이동 거리(m). 음수는 좌측.")]
            public AnimationCurve lane = AnimationCurve.Linear(0,0,1,0);
            [NonSerialized] public float frontWheelDegrees;
            [NonSerialized] public float distanceMetres;
            [NonSerialized] internal AnimationCurve runAdvance,runLane;
            [NonSerialized] internal float pitch,roll;
        }
        public Car[] cars = Array.Empty<Car>();
        [Range(1,2),Tooltip("실제 이동 거리 기반 회전을 촬영용으로 조금 강조합니다.")]
        public float wheelSpinMultiplier=1.35f;
        [Range(1,10)] public float steeringEmphasis=5;
        [Range(1,20)] public float maximumSteerDegrees=14;
        [Range(0,25)] public float driftCounterSteerDegrees=18;
        [Header("초반 견제 동작의 차체 반응 — 바퀴는 접지 유지")]
        public bool openingWeightTransfer;
        [Range(0,2)] public float maximumBodyRoll=1.1f;
        [Range(0,1)] public float maximumBodyPitch=.45f;
        float runSpin,runSteer,runMax,runDrift;
        bool captured;
        public void Begin()
        {
            if(!captured){
                foreach(var c in cars)if(c.visual)c.visual.Begin();
                captured=true;
            }
            runSpin=Mathf.Clamp(wheelSpinMultiplier,1,2);runSteer=Mathf.Clamp(steeringEmphasis,1,10);
            runMax=Mathf.Clamp(maximumSteerDegrees,1,20);runDrift=Mathf.Clamp(driftCounterSteerDegrees,0,25);
            foreach(var c in cars){c.runAdvance=Copy(c.advance);c.runLane=Copy(c.lane);c.distanceMetres=0;c.pitch=c.roll=0;}
        }
        static AnimationCurve Copy(AnimationCurve c)=>c==null?AnimationCurve.Linear(0,0,1,0):new AnimationCurve(c.keys);
        public Vector3 RoadOffset(Transform vehicle,float filmTime,float openingEnd,Vector3 forward,Vector3 right){var c=Array.Find(cars,x=>x.vehicle==vehicle);if(c==null)return Vector3.zero;float u=Mathf.Clamp01(filmTime/Mathf.Max(.1f,openingEnd));return forward*(c.runAdvance??c.advance).Evaluate(u)+right*(c.runLane??c.lane).Evaluate(u);}
        public void Sample(float filmTime,float openingEnd,float dropDuration,float worldTime,float baseSpeed,Vector3 forward,Vector3 right,Transform red,Transform khaki,int phase,float driftProgress)
        {
            if(!captured)Begin();
            float duration=Mathf.Max(.1f,openingEnd),u=Mathf.Clamp01(filmTime/duration),h=.005f;
            foreach(var c in cars){
                if(!c.vehicle||!c.visual)continue;
                float a=c.runAdvance.Evaluate(u),l=c.runLane.Evaluate(u);
                c.vehicle.position+=forward*a+right*l;
                float before=Mathf.Max(0,u-h),after=Mathf.Min(1,u+h),span=Mathf.Max(.00001f,(after-before)*duration);
                float va=(c.runAdvance.Evaluate(after)-c.runAdvance.Evaluate(before))/span;
                float khakiShift=c.vehicle==khaki?-Vector3.Dot(startPosition(khaki)-startPosition(red),right):0;
                float sb=Lateral(c,before,duration,dropDuration,khakiShift),sa=Lateral(c,after,duration,dropDuration,khakiShift),sc=Lateral(c,u,duration,dropDuration,khakiShift);
                float vl=(sa-sb)/span;
                float lateralAcceleration=(sa-2*sc+sb)/(h*h*duration*duration);
                float longitudinalAcceleration=(c.runAdvance.Evaluate(after)-2*a+c.runAdvance.Evaluate(before))/(h*h*duration*duration);
                if(u<h||u>1-h)lateralAcceleration=longitudinalAcceleration=0;
                if(filmTime>=duration){va=vl=lateralAcceleration=0;}
                float weightFade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(duration-.5f,duration,filmTime));
                c.roll=openingWeightTransfer?Mathf.Clamp(lateralAcceleration*.12f,-maximumBodyRoll,maximumBodyRoll)*weightFade:0;
                c.pitch=openingWeightTransfer?Mathf.Clamp(-longitudinalAcceleration*.06f,-maximumBodyPitch,maximumBodyPitch)*weightFade:0;
                float longitudinal=Mathf.Max(1,baseSpeed+va);
                float yaw=Mathf.Clamp(Mathf.Atan2(vl,longitudinal)*Mathf.Rad2Deg,-9,9);
                bool drifting=c.vehicle==red&&phase>=7&&phase<=11;
                if(drifting&&openingWeightTransfer){c.roll=-maximumBodyRoll*.7f*Mathf.Sin(driftProgress*Mathf.PI*2);c.pitch=maximumBodyPitch*Mathf.Sin(driftProgress*Mathf.PI)*.65f;}
                float wheelbase=2.5f;
                var w=c.cruise?c.cruise.Wheels:null;
                if(w!=null&&w.Length==4&&w.All(v=>v))wheelbase=Vector3.Distance((w[0].position+w[1].position)*.5f,(w[2].position+w[3].position)*.5f);
                float steer=Mathf.Clamp(Mathf.Atan(wheelbase*lateralAcceleration/(longitudinal*longitudinal))*Mathf.Rad2Deg*runSteer,-runMax,runMax);
                if(drifting)steer=-runDrift*Mathf.Sin(Mathf.PI*driftProgress);
                else c.vehicle.rotation=Quaternion.AngleAxis(yaw,Vector3.up)*c.vehicle.rotation;
                c.frontWheelDegrees=steer;
                // World forward distance includes the director's dropback/boost, not just common convoy speed.
                float travelled=Vector3.Dot(c.vehicle.position-startPosition(c.vehicle),forward);
                c.distanceMetres=Mathf.Max(0,travelled);
                if(c.cruise){float handle=steer/Mathf.Max(.01f,c.cruise.wheelAngleAt360)*360;c.cruise.ApplyTimelinePose(this,worldTime,c.distanceMetres*runSpin,handle);}
                c.visual.Sample(c.distanceMetres*runSpin,steer);
            }
        }
        Transform[] startCars; Vector3[] positions;
        // The shared cruise controller resets its base body pose at order 1000.
        // Add scene-only weight transfer afterwards, restoring all hubs in world space.
        void LateUpdate(){
            if(!Application.isPlaying||!captured||!openingWeightTransfer)return;
            foreach(var c in cars){if(!c.cruise||!c.cruise.IsTimelineControlled||!c.cruise.BodyMotionRoot)continue;
                var hubs=c.cruise.Wheels;if(hubs==null||hubs.Length!=4||hubs.Any(w=>!w))continue;
                var p=hubs.Select(w=>w.position).ToArray();var q=hubs.Select(w=>w.rotation).ToArray();
                var body=c.cruise.BodyMotionRoot;Vector3 pivot=p.Aggregate(Vector3.zero,(a,b)=>a+b)*.25f;
                Quaternion tilt=c.vehicle.rotation*Quaternion.Euler(c.pitch,0,-c.roll)*Quaternion.Inverse(c.vehicle.rotation);
                body.SetPositionAndRotation(pivot+tilt*(body.position-pivot),tilt*body.rotation);
                for(int i=0;i<4;i++)hubs[i].SetPositionAndRotation(p[i],q[i]);
            }
        }
        static float Lateral(Car c,float u,float end,float drop,float shift){float k=Mathf.InverseLerp(.35f,1,Mathf.Clamp01((u*end-(end-drop))/Mathf.Max(.1f,drop)));return c.runLane.Evaluate(u)+shift*k*k*(3-2*k);}
        public void CapturePositions(){startCars=cars.Select(c=>c.vehicle).ToArray();positions=startCars.Select(t=>t?t.position:Vector3.zero).ToArray();}
        public void SampleGroundPass(float time,float distance,Transform blue,float yaw){
            if(!captured)Begin();
            foreach(var c in cars){
                float steer=c.vehicle==blue?yaw*.8f:0;c.frontWheelDegrees=steer;c.distanceMetres=distance;
                c.pitch=0;c.roll=c.vehicle==blue?-Mathf.Clamp(yaw*.25f,-maximumBodyRoll,maximumBodyRoll):0;
                if(c.cruise)c.cruise.ApplyTimelinePose(this,time,distance*runSpin,steer/Mathf.Max(.01f,c.cruise.wheelAngleAt360)*360);
                if(c.visual)c.visual.Sample(distance*runSpin,steer);
            }
        }
        public void SampleDuelCar(Transform vehicle,float time,float distance,float steer,float roll,float pitch){
            if(!captured)Begin();
            var c=Array.Find(cars,v=>v.vehicle==vehicle);if(c==null)return;
            c.frontWheelDegrees=Mathf.Clamp(steer,-runMax,runMax);c.distanceMetres=Mathf.Max(0,distance);
            c.roll=Mathf.Clamp(roll,-maximumBodyRoll,maximumBodyRoll);c.pitch=Mathf.Clamp(pitch,-maximumBodyPitch,maximumBodyPitch);
            if(c.cruise)c.cruise.ApplyTimelinePose(this,time,c.distanceMetres*runSpin,c.frontWheelDegrees/Mathf.Max(.01f,c.cruise.wheelAngleAt360)*360);
            if(c.visual)c.visual.Sample(c.distanceMetres*runSpin,c.frontWheelDegrees);
        }
        Vector3 startPosition(Transform t){int i=Array.IndexOf(startCars,t);return i<0? t.position:positions[i];}
        public void Release()
        {
            if(!captured)return;
            foreach(var c in cars)if(c.cruise)c.cruise.ReleaseTimeline(this);
            foreach(var c in cars)if(c.visual)c.visual.Release();
            captured=false;
        }
        void OnDisable(){if(Application.isPlaying)Release();}
    }
}
