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
            [Range(.7f,2.5f),InspectorName("노면 위 카메라 높이 (m)")] public float cameraHeight=1.1f;
            [Range(0,.35f),InspectorName("견제 반응 지연 (초)")] public float reactionDelay=.16f;
            [Range(0,1),InspectorName("진동 강도")] public float vibration=.35f;
        }
        [Header("02 독립 촬영 — 파랑 추월 시도 / 빨강 견제")]
        [InspectorName("02를 독립 테이크로 촬영")] public bool independentDuel;
        public DuelSettings duelTake=new DuelSettings();
        public bool DuelConfigured=>independentDuel&&cameraPlaybackMode==CameraPlaybackMode.SingleCamera&&takeCameraNumber==2;
        public bool DuelTakeActive=>runDuel;
        bool runDuel;
        float duelSpeed,duelLens,duelHeight,duelDelay,duelVibration;
        Vector3[] duelStarts;
        static float Beat(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        // Authored action/reaction in an eight-second score; duration retimes every participant together.
        Vector2 DuelOffset(Transform car,float seconds){
            float t=Mathf.Clamp(seconds*8/FilmDuration,0,8);
            // Blue reaches the rear quarter before conceding. Red's small initial move is the
            // warning; the full lane closure happens only after a longitudinal gap reopens.
            if(car==redCar)return new Vector2(5*Beat(t,2.9f,3.8f)+3*Beat(t,4.3f,5.7f),
                .45f*Beat(t,2.75f,3.2f)+3.35f*Beat(t,3.65f,4.2f)-.55f*Beat(t,4.6f,5.35f));
            if(car==blueCar)return new Vector2(18*Beat(t,.5f,2.9f)-22*Beat(t,3.25f,5.2f),
                7*Beat(t,.35f,1.35f)+.15f*Beat(t,3.35f,3.7f)-.3f*Beat(t,4.1f,4.8f));
            if(car==khakiCar)return new Vector2(2*Beat(t,5.6f,7.5f),0);
            return Vector2.zero;
        }
        Vector3 DuelPosition(int i,float t)=>duelStarts[i]+forward*(duelSpeed*t+DuelOffset(opening.vehicles[i],t).x)+right*DuelOffset(opening.vehicles[i],t).y;
        void PrepareDuel(){
            duelSpeed=Safe(duelTake.speedKph,560,120,900)/3.6f;duelLens=Safe(duelTake.focalLength,28,24,35);
            duelHeight=Safe(duelTake.cameraHeight,1.1f,.7f,2.5f);duelDelay=Safe(duelTake.reactionDelay,.16f,0,.35f);
            duelVibration=Safe(duelTake.vibration,.35f,0,1);
            float anchor=SelectedShotTake!=null?Safe(SelectedShotTake.redStartWorldX,1200,-10000,10000):1200;
            int r=Array.IndexOf(opening.vehicles,redCar);runTakeOffset=Vector3.right*(anchor-initialPositions[r].x);
            duelStarts=new Vector3[opening.vehicles.Length];int extra=0;
            for(int i=0;i<duelStarts.Length;i++){
                var car=opening.vehicles[i];float gap,lane;
                if(car==redCar){gap=0;lane=-3.5f;}
                else if(car==blueCar){gap=26;lane=-3.5f;}
                else if(car==khakiCar){gap=65;lane=3.8f;}
                else{gap=105+extra*35;lane=new[]{-4.5f,0,4.5f}[Mathf.Min(2,extra++)];}
                duelStarts[i]=new Vector3(anchor+gap,initialPositions[i].y,lane);
                car.SetPositionAndRotation(duelStarts[i],initialRotations[i]);
            }
            // Fail visibly instead of changing the map or silently reducing the requested speed.
            if(anchor+190>1999||anchor-duelSpeed*FilmDuration-20< -1999){
                TakePlacementWarning="02 속도/길이/출발 X가 도로 범위를 벗어납니다. 02 설정을 조정하고 다시 재생하세요.";
                Error=TakePlacementWarning;Playing=false;
            }
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
                // Cinematic steering accent leads the lateral response; body roll does not lift wheel hubs.
                float anticipation=(DuelOffset(car,t+.12f).y-offset.y)/.12f;
                float steer=Mathf.Clamp(anticipation*.8f,-6,6);
                if(racePerformance)racePerformance.SampleDuelCar(car,t,duelSpeed*t+offset.x,steer,accel.y*.08f,-accel.x*.035f);
            }
            int ri=Array.IndexOf(opening.vehicles,redCar),bi=Array.IndexOf(opening.vehicles,blueCar),ki=Array.IndexOf(opening.vehicles,khakiCar);
            float beat=t*8/FilmDuration,delayed=Mathf.Max(0,t-duelDelay);
            // Independent tracking-vehicle trajectory: Blue can enter, pass and fall behind
            // the frame. Only the aim reacts to Blue; position never inherits Blue's braking.
            Vector3 b=DuelPosition(bi,delayed)+forward*(duelSpeed*(t-delayed));
            Vector3 r=DuelPosition(ri,delayed)+forward*(duelSpeed*(t-delayed));
            Vector3 k=DuelPosition(ki,delayed)+forward*(duelSpeed*(t-delayed));
            float swing=Beat(beat,1.15f,2.8f),reaction=Beat(beat,3.35f,4.1f),release=Beat(beat,5.5f,8);
            Vector3 rail=duelStarts[ri]+forward*(duelSpeed*t);
            // Carry the fixed-pass shot's speed into this take: initially lose 12 metres
            // to the convoy, then catch up. This is camera travel, not an FOV zoom.
            float openingLag=12*Beat(beat,0,1.2f)*(1-Beat(beat,1.4f,2.8f));
            float railAdvance=Mathf.Lerp(-10,-1,swing)-openingLag-6*Beat(beat,3.2f,3.75f)+11*Beat(beat,4.05f,5.05f);
            float railSide=Mathf.Lerp(12,17,Beat(beat,.4f,1.6f))+3*reaction;
            Vector3 closePosition=rail+right*railSide+forward*railAdvance;
            Vector3 revealPosition=rail+right*16-forward*82;
            Vector3 position=Vector3.Lerp(closePosition,revealPosition,release);
            position.y=-29.65f+duelHeight+.55f*swing+.65f*release;
            // Start looking at Red. Pan to the threatened rear-quarter/front-wheel gap,
            // then let Blue leave frame instead of keeping both cars permanently centred.
            Vector3 threat=Vector3.Lerp(r-forward*3.8f,b+forward*4,.5f);
            Vector3 aim=Vector3.Lerp(r-forward*1.5f,threat,Beat(beat,.95f,2.55f));
            aim=Vector3.Lerp(aim,r-forward*1.5f,.35f*reaction+.65f*Beat(beat,4.25f,5.1f));
            aim=Vector3.Lerp(aim,Vector3.Lerp(b,k,.45f),release);
            aim.y=Mathf.Lerp(r.y+.2f,b.y+.25f,release);
            float impulse=Mathf.Exp(-Mathf.Pow((beat-3.9f)/.24f,2));
            Vector3 shake=new Vector3(Mathf.Sin(t*29)*.055f,Mathf.Sin(t*37)*.04f,Mathf.Sin(t*23)*.065f)*duelVibration;
            Vector3 kick=(shake+new Vector3(-.18f,.22f,.65f)*impulse*duelVibration)*(1-.7f*release);
            var shot=cameras.shots[1];shot.independentPose=true;shot.independentPosition=position;
            shot.independentRotation=Quaternion.LookRotation(aim-position,Vector3.up)*Quaternion.Euler(kick);
            shot.independentLens=duelLens;
        }
    }
}
