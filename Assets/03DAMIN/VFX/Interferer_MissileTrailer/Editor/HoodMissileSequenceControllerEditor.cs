using UnityEditor;
using UnityEngine;
namespace Damin.Trailer.MissileCar.Editor
{
    [CustomEditor(typeof(HoodMissileSequenceController))]
    public sealed class HoodMissileSequenceControllerEditor : UnityEditor.Editor
    {
        bool hood=true,bay,shot=true,flight,inside,sides,trail,fade,pressure=true,details,connections;
        HoodMissileSequenceController C=>(HoodMissileSequenceController)target;
        bool TimingLocked=>Application.isPlaying&&(C.State==HoodMissileSequenceController.SequenceState.Playing||C.State==HoodMissileSequenceController.SequenceState.Paused);
        void P(string path,string label){var p=serializedObject.FindProperty(path);if(p!=null)EditorGUILayout.PropertyField(p,new GUIContent(label),true);}
        void Timed(string path,string label){using(new EditorGUI.DisabledScope(TimingLocked))P(path,label);}
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("보넷 · 미사일 통합 연출",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("이 Inspector 한 곳에서 보넷·미사일 통·미사일·연기를 조절합니다. 모든 시작 시각은 전체 시작 지연 이후의 0초 기준입니다. 각 소요 시간은 시작부터 완료까지 걸리는 시간입니다. 발사 후 연기 감소·추진 시작만 실제 발사 기준입니다.",MessageType.Info);
            P("playback.useShift","Shift로 재생 / 일시정지 후 계속");P("playback.playOnStart","Play 시작 시 자동 재생");Timed("playback.startDelay","전체 연출 시작 지연 (초)");
            if(Application.isPlaying){EditorGUILayout.LabelField("현재 상태",C.State.ToString());EditorGUILayout.LabelField("연출 시간",C.TimeOnSequence.ToString("F2")+"초");}
            using(new EditorGUI.DisabledScope(!Application.isPlaying)){
                EditorGUILayout.BeginHorizontal();
                if(GUILayout.Button("재생")){serializedObject.ApplyModifiedProperties();C.Play();}
                if(GUILayout.Button(C.State==HoodMissileSequenceController.SequenceState.Paused?"계속":"일시정지")){if(C.State==HoodMissileSequenceController.SequenceState.Paused)C.Resume();else C.Pause();}
                if(GUILayout.Button("처음으로 / 4발 재장전"))C.ResetAll();
                EditorGUILayout.EndHorizontal();
            }
            if(!Application.isPlaying)EditorGUILayout.HelpBox("미리보기 버튼은 Unity Play 모드에서 사용하세요. 시간표·비행값은 재생 중 고정되며, 처음으로 돌아간 뒤 변경합니다. 연기 양·모양은 재생 중 조절할 수 있습니다.",MessageType.None);
            string error=C.ValidationError();if(error!=null)EditorGUILayout.HelpBox(error,MessageType.Warning);
            hood=EditorGUILayout.Foldout(hood,"01  보넷 열림",true);if(hood){Timed("motion.hoodStart","열림 시작 시각 (초)");Timed("motion.hoodDuration","열리는 데 걸리는 시간 (초)");Timed("motion.hoodAngle","열림 각도 (도)");}
            bay=EditorGUILayout.Foldout(bay,"02  미사일 통 상승",true);if(bay){Timed("motion.bayStart","상승 시작 시각 (초)");Timed("motion.bayDuration","상승 소요 시간 (초)");Timed("motion.bayHeightCm","상승 높이 (cm / 차량 로컬)");}
            shot=EditorGUILayout.Foldout(shot,"03  미사일 상승 · 발사",true);if(shot){
                Timed("motion.missileNumber","이번 연출에서 발사할 번호 (1~4)");Timed("motion.liftMissile","선택한 미사일만 먼저 올리기");
                using(new EditorGUI.DisabledScope(!C.motion.liftMissile)){Timed("motion.missileLiftStart","미사일 상승 시작 시각 (초)");Timed("motion.missileLiftDuration","미사일 상승 소요 시간 (초)");Timed("motion.missileLiftHeightCm","미사일 추가 상승 높이 (cm)");}
                Timed("motion.fireTime","실제 발사 시각 (초)");
                if(C.motion.liftMissile)EditorGUILayout.LabelField("상승 완료 후 대기",(C.motion.fireTime-C.motion.missileLiftStart-C.motion.missileLiftDuration).ToString("F2")+"초 (위 시간표에서 계산)");
            }
            flight=EditorGUILayout.Foldout(flight,"04  미사일 비행",true);if(flight){Timed("motion.speed","이탈 후 초기 속도 (m/s)");Timed("motion.acceleration","가속도 (m/s²)");Timed("motion.flightLifetime","발사 후 비행 유지 시간 (초)");Timed("motion.clearanceDuration","발사 직후 이탈 소요 시간 (초)");Timed("motion.clearanceForward","발사 직후 이탈 전진 거리 (m)");Timed("motion.clearanceHeight","발사 직후 이탈 추가 높이 (m)");}
            P("bodySmoke.enabled","보넷 연기 전체 사용");
            inside=EditorGUILayout.Foldout(inside,"05  내부 연기만 — 작은 입자가 서서히 누적",true);if(inside){
                Timed("interiorSmoke.InteriorSmokeStartTime","InteriorSmokeStartTime / 시작 시각 (초)");Timed("interiorSmoke.InteriorBuildUpDuration","InteriorBuildUpDuration / 축적 시간 (초)");
                P("interiorSmoke.InteriorSpawnRateMin","InteriorSpawnRateMin / 초기 생성량 (개/초)");P("interiorSmoke.InteriorSpawnRateMax","InteriorSpawnRateMax / 최대 생성량 (개/초)");
                P("interiorSmoke.InteriorLifetimeMin","InteriorLifetimeMin / 최소 수명 (초)");P("interiorSmoke.InteriorLifetimeMax","InteriorLifetimeMax / 최대 수명 (초)");
                P("interiorSmoke.InteriorStartSize","InteriorStartSize / 생성 크기 (m)");P("interiorSmoke.InteriorEndSize","InteriorEndSize / 최종 크기 (m)");
                P("interiorSmoke.InteriorOpacity","InteriorOpacity / 입자 최대 불투명도");P("interiorSmoke.InteriorTurbulence","InteriorTurbulence / 작은 흔들림 (m)");P("interiorSmoke.InteriorUpwardForce","InteriorUpwardForce / 약한 상승 가속 (m/s²)");P("interiorSmoke.InteriorVolumeSize","InteriorVolumeSize / 내부 범위 (m)");
                string ie=C.interiorSmoke.Error();if(ie!=null)EditorGUILayout.HelpBox(ie,MessageType.Warning);
                P("interiorSmoke.InteriorColor","내부 연기 색 / 바퀴 연기 계열");
                EditorGUILayout.HelpBox("작은 입자가 미사일 사이·아래에서 쌓입니다. 바퀴 03_TrailingSmoke의 부드러운 질감이며 미사일 추진 연기와 별개입니다. 내부 축적 시간이 끝나기 전에는 좌우 생성량이 0입니다.",MessageType.Info);
                if(Application.isPlaying&&C.hoodVFX){EditorGUILayout.LabelField("현재 내부 생성량",C.hoodVFX.InteriorActualSpawnRate.ToString("F1")+" /초");EditorGUILayout.LabelField("내부 축적 진행",C.hoodVFX.InteriorBuildProgress.ToString("P0"));}
            }
            sides=EditorGUILayout.Foldout(sides,"06  틈에서 새어 나온 뒤 위로 퍼지는 연기",true);if(sides){
                Timed("bodySmoke.sideStart","희망 시작 시각 (초 / 내부 축적 완료 이후)");
                EditorGUILayout.LabelField("실제 좌우 시작 시각",Mathf.Max(C.bodySmoke.sideStart,C.interiorSmoke.InteriorSmokeStartTime+C.interiorSmoke.InteriorBuildUpDuration).ToString("F2")+"초");
                Timed("bodySmoke.sideRiseDuration","서서히 풍성해지는 시간 (초)");
                P("bodySmoke.sideDensity","입자 최대 불투명도");P("bodySmoke.sideSpawnRate","연속 생성량 (개/초)");
                P("bodySmoke.sideParticleSize","처음 작은 입자 크기 (m)");P("bodySmoke.sideExpansion","수명 동안 커지는 양 (m)");P("bodySmoke.sideLifetime","입자 수명 (초)");
                P("bodySmoke.sideSpeed","틈에서 처음 옆으로 새는 속도 (m/s)");P("bodySmoke.maxLateralDistance","옆 이동 한도 (m)");
                P("bodySmoke.sideDrag","수평 속도 감쇠 / 클수록 빨리 멈춤");P("bodySmoke.riseAfterDistance","위로 뜨기 시작하는 거리 (m)");
                P("bodySmoke.outsideRiseSpeed","상승 속도 (m/s)");P("bodySmoke.sideBuoyancy","부력 / 상승 가속도 (m/s²)");
                P("bodySmoke.sideTurbulence","회오리 반경 (m)");P("bodySmoke.sideSwirlSpeed","말려 올라가는 회전 속도 (rad/s)");P("bodySmoke.sideSpread","틈의 가는 연기 발생 폭 (m)");P("bodySmoke.sideAsymmetry","좌우 생성량 차이");P("bodySmoke.sideColor","좌우 연기 색");
                EditorGUILayout.HelpBox("내부 축적 완료 + 희망 시각을 모두 만족한 뒤 가는 입자를 연속 생성합니다. 가까운 시각에 태어난 입자가 같은 흐름을 따라 짧게 새고 말려 올라갑니다. 회오리 반경·회전 속도·발생 폭으로 조절하며 미사일/잔연기와 독립입니다.",MessageType.None);
            }
            trail=EditorGUILayout.Foldout(trail,"07  미사일 뒤 흰 연기 하나",true);if(trail){P("missileSmoke.enabled","미사일 연기 사용");Timed("missileSmoke.exhaustStartDelay","실제 발사 후 연기 시작 지연 (초)");P("missileSmoke.trailDensity","흰 연기 밀도");P("missileSmoke.trailWidth","연기 시작 굵기 (m)");P("missileSmoke.trailExpansion","뒤로 갈수록 굵어지는 양");P("missileSmoke.trailLength","연기 꼬리 목표 길이 (m)");P("missileSmoke.trailTurbulence","옆으로 흩어지는 정도");P("missileSmoke.trailColor","미사일 연기 색");}
            fade=EditorGUILayout.Foldout(fade,"08  발사 순간 · 발사 후 잔연기",true);if(fade){P("bodySmoke.launchBurstIntensity","발사 순간 연기·섬광 강도 (0이면 끔)");Timed("bodySmoke.interiorFadeDelay","발사 후 내부 연기 유지 (초)");Timed("bodySmoke.interiorFadeDuration","내부 연기 감소 소요 시간 (초)");Timed("bodySmoke.sideFadeDelay","발사 후 좌우 연기 유지 (초)");Timed("bodySmoke.sideFadeDuration","좌우 연기 감소 소요 시간 (초)");Timed("bodySmoke.residualFadeDelay","발사 후 잔연기 유지 (초)");Timed("bodySmoke.residualFadeDuration","잔연기 감소 소요 시간 (초)");P("bodySmoke.residualDensity","잔연기 밀도");P("bodySmoke.residualSpawnRate","잔연기 발생량");}
            pressure=EditorGUILayout.Foldout(pressure,"09  내부 축적 → 틈 넘침 → 상승 / 조밀한 미사일 꼬리",true);if(pressure){
                Timed("motion.holdHoodGap","보넷을 살짝 열고 연기 축적 대기");Timed("motion.hoodGapAngle","처음 살짝 여는 각도 (도)");Timed("motion.hoodFullOpenStart","완전히 열기 시작하는 시각 (초)");
                P("bodySmoke.interiorGrowth","발사 후 잔연기 성장량 (m)");
                P("missileSmoke.tailFadeStart","꼬리 투명해짐 시작 (입자 수명 비율)");P("missileSmoke.brightCoreIntensity","노즐 바로 뒤 밝은 코어 강도");
                EditorGUILayout.HelpBox("좌우 연기는 05 내부 축적 완료 이후에만 시작합니다. 06에서 짧은 옆 이동과 상승을 조절하세요. 이 항목의 미사일 코어/꼬리 설정은 그대로 유지됩니다.",MessageType.None);
            }
            details=EditorGUILayout.Foldout(details,"고급  연기 모양 · 입자 예산",true);if(details){
                P("bodySmoke.smokeColor","발사 임팩트·잔연기 색 (내부/좌우 별도)");P("bodySmoke.smokeLifetime","발사 후 잔연기 입자 수명 (초)");P("bodySmoke.smokeExpansion","기존 잔연기 팽창값");P("bodySmoke.smokeRoundness","잔연기 세로 볼륨");P("bodySmoke.interiorTurbulence","발사 후 잔연기 흔들림");P("bodySmoke.collisionMargin","차체 충돌 여유 거리");
                P("missileSmoke.trailSpawnRate","미사일 연기 기본 발생량");P("missileSmoke.trailSpeed","미사일 연기 배출 속도");P("missileSmoke.maxSmokeLifetime","미사일 연기 최대 수명");P("missileSmoke.extraParticlesPerMeter","미사일 이동 1m당 추가 발생량");P("missileSmoke.maxTrailSpawnRate","미사일 연기 발생량 상한");P("missileSmoke.tailCleanupPadding","잔여 연기 정리 여유 (초)");
            }
            connections=EditorGUILayout.Foldout(connections,"고급  내부 연결 (이미 연결됨)",true);if(connections){using(new EditorGUI.DisabledScope(TimingLocked)){P("launcher","기존 미사일 발사 담당");P("deployment","기존 보넷·발사대 담당");P("hoodVFX","보넷 연기 묶음");}EditorGUILayout.HelpBox("VFX 발생 위치와 충돌 프록시는 기존 08_보넷미사일VFX 안에 있습니다. 이 컨트롤러는 차량·바퀴·미사일 모델의 계층을 바꾸지 않습니다.",MessageType.None);}
            serializedObject.ApplyModifiedProperties();if(Application.isPlaying)Repaint();
        }
    }
    [CustomEditor(typeof(TrailerHoodDeployment))]
    public sealed class UnifiedHoodDeploymentEditor:UnityEditor.Editor
    {
        public override void OnInspectorGUI(){var d=((TrailerHoodDeployment)target).GetComponentInParent<HoodMissileSequenceController>();if(!d){DrawDefaultInspector();return;}EditorGUILayout.HelpBox("차량 최상위의 보넷 미사일 통합 연출에서 조절하세요. 이 컴포넌트는 내부 동작 담당입니다.",MessageType.Info);if(GUILayout.Button("통합 컨트롤러 선택"))Selection.activeGameObject=d.gameObject;}
    }
}
