using System;
using UnityEngine;
namespace Damin.CinematicCopy
{
    public sealed partial class DMCinematicDirector
    {
        [Serializable] public sealed class DuelSettings
        {
            [Range(5,10),InspectorName("02 녹화 길이 (초)")] public float duration=8;
            [Range(120,900),InspectorName("02 촬영용 속도 (km/h)")] public float speedKph=560;
            [Range(24,35),InspectorName("02 렌즈 (mm)")] public float focalLength=28;
            [Range(.25f,2.5f),InspectorName("노면 위 카메라 높이 (m)")] public float cameraHeight=.45f;
            [Range(0,1.25f),InspectorName("카메라 속도 차이 강도"),Tooltip("1=차량이 카메라를 추월하는 촬영, 0=차량과 동속 추적. 차량 속도·동선은 바뀌지 않습니다.")] public float cameraSeparation=1;
            [Range(0,.35f),InspectorName("견제 반응 지연 (초)")] public float reactionDelay=.16f;
            [Range(0,1),InspectorName("진동 강도")] public float vibration=.35f;
            [Range(0,1),InspectorName("서브 차량 추격 강도"),Tooltip("0=간격 유지, 1=서브1 추월·서브2/3 추격. 카키 뒤 안전 간격은 유지합니다. 변경 후 다시 재생하세요.")]
            public float pursuit=1;
        }
        [Header("02 독립 촬영 — 추월 / 견제 / 재추격")]
        [InspectorName("02를 독립 테이크로 촬영")] public bool independentDuel;
        public DuelSettings duelTake=new DuelSettings();
        public bool DuelConfigured=>independentDuel&&cameraPlaybackMode==CameraPlaybackMode.SingleCamera&&takeCameraNumber==2;
        public bool DuelTakeActive=>runDuel;
        bool runDuel;
        float duelSpeed,duelLens,duelHeight,duelDelay,duelVibration,duelPursuit,duelSeparation;
        Vector3[] duelStarts;
        static float Beat(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        int DuelExtraIndex(Transform car){
            int extra=0;
            foreach(var candidate in opening.vehicles){
                if(candidate==redCar||candidate==blueCar||candidate==khakiCar)continue;
                if(candidate==car)return extra;
                extra++;
            }
            return -1;
        }
        // One deterministic score; changing duration retimes cars AND camera together.
        Vector2 DuelOffset(Transform car,float seconds){
            float t=Mathf.Clamp(seconds*8/FilmDuration,0,8);
            if(car==redCar)return new Vector2(
                5*Beat(t,2.9f,3.8f)+3*Beat(t,4.3f,5.7f),
                .45f*Beat(t,2.75f,3.2f)+3.35f*Beat(t,3.65f,4.2f)-.55f*Beat(t,4.6f,5.35f));
            if(car==blueCar)return new Vector2(
                18*Beat(t,.5f,2.9f)-22*Beat(t,3.25f,5.2f)+14*Beat(t,5.25f,7.25f),
                7*Beat(t,.35f,1.35f)+.15f*Beat(t,3.35f,3.7f)-.3f*Beat(t,4.1f,4.8f)-2.4f*Beat(t,5.15f,6.55f));
            if(car==khakiCar)return new Vector2(2*Beat(t,5.6f,7.5f),0);
            switch(DuelExtraIndex(car)){
                // Sub1 first pulls out behind Sub2, THEN accelerates alongside it.
                // No return across Sub2's nose: both lanes remain collision-separated.
                case 0:return new Vector2(40*Beat(t,4.65f,7.55f)*duelPursuit,
                    -8*Beat(t,3.45f,4.85f));
                case 1:return new Vector2(9*Beat(t,5.5f,7.9f)*duelPursuit,
                    .35f*Beat(t,5.4f,6.1f)*duelPursuit);
                case 2:return new Vector2(22*Beat(t,4.3f,7.8f)*duelPursuit,
                    .8f*(Beat(t,4.1f,5.3f)-Beat(t,6.5f,7.8f))*duelPursuit);
            }
            return Vector2.zero;
        }
        Vector3 DuelPosition(int i,float t){
            var offset=DuelOffset(opening.vehicles[i],t);
            return duelStarts[i]+forward*(duelSpeed*t+offset.x)+right*offset.y;
        }
        void PrepareDuel(){
            duelSpeed=Safe(duelTake.speedKph,560,120,900)/3.6f;
            duelLens=Safe(duelTake.focalLength,28,24,35);
            duelHeight=Safe(duelTake.cameraHeight,.45f,.25f,2.5f);
            duelSeparation=Safe(duelTake.cameraSeparation,1,0,1.25f);
            duelDelay=Safe(duelTake.reactionDelay,.16f,0,.35f);
            duelVibration=Safe(duelTake.vibration,.35f,0,1);
            duelPursuit=Safe(duelTake.pursuit,1,0,1);
            float anchor=SelectedShotTake!=null?Safe(SelectedShotTake.redStartWorldX,1200,-10000,10000):1200;
            int r=Array.IndexOf(opening.vehicles,redCar);
            runTakeOffset=Vector3.right*(anchor-initialPositions[r].x);
            duelStarts=new Vector3[opening.vehicles.Length];
            int extra=0;
            for(int i=0;i<duelStarts.Length;i++){
                var car=opening.vehicles[i];float gap,lane;
                if(car==redCar){gap=0;lane=-3.5f;}
                else if(car==blueCar){gap=26;lane=-3.5f;}
                else if(car==khakiCar){gap=45;lane=3.8f;}
                else{
                    int index=Mathf.Min(2,extra++);
                    gap=new[]{116f,96f,145f}[index];
                    lane=new[]{4f,4f,0f}[index];
                }
                duelStarts[i]=new Vector3(anchor+gap,initialPositions[i].y,lane);
                car.SetPositionAndRotation(duelStarts[i],initialRotations[i]);
            }
            // Keep both the start and the last frame on the EXISTING road.
            if(anchor+160>1999||anchor-duelSpeed*FilmDuration-20< -1999){
                TakePlacementWarning="02 속도/길이/출발 X가 도로 범위를 벗어납니다. 02 설정을 조정하고 다시 재생하세요.";
                Error=TakePlacementWarning;Playing=false;
            }
        }
        static readonly float[] DuelRailTimes={0,.75f,1.4f,2.4f,3.3f,4,4.8f,5.5f,6.2f,6.9f,8};
        static readonly float[] DuelRailMetres={12,-28,-40,4,-8,-14,-62,-96,-108,-112,-164};
        static float DuelRailOffset(float beat){
            for(int i=1;i<DuelRailTimes.Length;i++)
                if(beat<=DuelRailTimes[i])
                    return Mathf.Lerp(DuelRailMetres[i-1],DuelRailMetres[i],Beat(beat,DuelRailTimes[i-1],DuelRailTimes[i]));
            return DuelRailMetres[DuelRailMetres.Length-1];
        }
        Vector3 DuelCameraAimPosition(int index,float t){
            float delayed=Mathf.Max(0,t-duelDelay);
            return DuelPosition(index,delayed)+forward*(duelSpeed*(t-delayed));
        }
        void ApplyDuel(float t){
            Phase=1;cameras.activeShot=2;cameras.motionTime=t;
            for(int i=0;i<duelStarts.Length;i++){
                var car=opening.vehicles[i];float h=.01f;
                Vector2 offset=DuelOffset(car,t),prev=DuelOffset(car,Mathf.Max(0,t-h)),next=DuelOffset(car,t+h);
                Vector2 velocity=(next-prev)/(t<h?h:2*h);
                Vector2 accel=t<h?Vector2.zero:(next-2*offset+prev)/(h*h);
                float yaw=Mathf.Atan2(velocity.y,Mathf.Max(1,duelSpeed+velocity.x))*Mathf.Rad2Deg;
                car.SetPositionAndRotation(DuelPosition(i,t),Quaternion.AngleAxis(yaw,Vector3.up)*initialRotations[i]);
                float anticipation=(DuelOffset(car,t+.12f).y-offset.y)/.12f;
                float steer=Mathf.Clamp(anticipation*.8f,-6,6);
                if(racePerformance)racePerformance.SampleDuelCar(car,t,duelSpeed*t+offset.x,steer,accel.y*.08f,-accel.x*.035f);
            }
            int ri=Array.IndexOf(opening.vehicles,redCar),bi=Array.IndexOf(opening.vehicles,blueCar),ki=Array.IndexOf(opening.vehicles,khakiCar);
            float beat=t*8/FilmDuration;
            Vector3 r=DuelCameraAimPosition(ri,t),b=DuelCameraAimPosition(bi,t),k=DuelCameraAimPosition(ki,t);
            // Stay on ONE side of the racing axis. Blue overtakes the camera, not its parent.
            Vector3 rail=duelStarts[ri]+forward*(duelSpeed*t);
            float attack=Beat(beat,1.15f,2.85f),reaction=Beat(beat,3.35f,4.1f);
            float reveal=Beat(beat,4.3f,7.4f);
            // Camera has its own longitudinal score: let the cars pass, briefly recover
            // the duel, then fall back through the pursuing pack. No sustained co-speed lock.
            float railAdvance=12+(DuelRailOffset(beat)-12)*duelSeparation;
            float railSide=Mathf.Lerp(14+attack+reaction,18,reveal);
            Vector3 position=rail+right*railSide+forward*railAdvance;
            position.y=-29.65f+duelHeight+.18f*reveal;
            Vector3 threat=Vector3.Lerp(r-forward*3.8f,b+forward*4,.5f);
            Vector3 aim=Vector3.Lerp(r-forward*1.5f,threat,Beat(beat,.95f,2.55f));
            aim=Vector3.Lerp(aim,r-forward*1.5f,.35f*reaction+.65f*Beat(beat,4.25f,5.1f));
            // Explicitly frame the sub-car overtake instead of aiming only at the main three.
            Vector3 pursuitCenter=Vector3.zero;int subjects=0;
            for(int i=0;i<opening.vehicles.Length;i++){
                int index=DuelExtraIndex(opening.vehicles[i]);
                if(index<0||index>1)continue;
                pursuitCenter+=DuelCameraAimPosition(i,t);subjects++;
            }
            if(subjects>0)pursuitCenter/=subjects;else pursuitCenter=k;
            Vector3 rearGroup=Vector3.Lerp(pursuitCenter,k,.18f);
            float showPursuit=Beat(beat,4.1f,5.65f);
            aim=Vector3.Lerp(aim,rearGroup,showPursuit);
            // Finish looking ahead through the pursuing cars, ready for the Blue/Khaki cut.
            aim=Vector3.Lerp(aim,Vector3.Lerp(b,k,.5f),.35f*Beat(beat,7.25f,8));
            // Frame the lower body and the nearby road, not mainly the sky.
            aim.y=-29.65f+.95f;
            float nearPass=Mathf.Exp(-Mathf.Pow((beat-.7f)/.18f,2));
            float block=Mathf.Exp(-Mathf.Pow((beat-3.9f)/.24f,2));
            Vector3 vibration=new Vector3(Mathf.Sin(t*29)*.055f,Mathf.Sin(t*37)*.04f,Mathf.Sin(t*23)*.065f);
            Vector3 kick=(vibration+new Vector3(.13f,-.18f,.5f)*nearPass+new Vector3(-.18f,.22f,.65f)*block)*duelVibration*(1-.6f*reveal);
            var shot=cameras.shots[1];shot.independentPose=true;shot.independentPosition=position;
            shot.independentRotation=Quaternion.LookRotation(aim-position,Vector3.up)*Quaternion.Euler(kick);
            shot.independentLens=duelLens;
        }
    }
}
