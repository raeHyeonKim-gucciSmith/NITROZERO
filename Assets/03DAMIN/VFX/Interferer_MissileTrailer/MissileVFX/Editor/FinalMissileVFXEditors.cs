using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Damin.Trailer.FinalVFX.Editor
{
    internal static class FinalVFXInspector
    {
        internal static bool ShowUnified(MonoBehaviour component){var d=component.GetComponentInParent<Damin.Trailer.MissileCar.HoodMissileSequenceController>();if(!d)return false;EditorGUILayout.HelpBox("이 차량의 연기는 최상위 보넷 미사일 통합 연출에서 조절합니다. 현재 컴포넌트는 내부 VFX 담당입니다.",MessageType.Info);if(GUILayout.Button("통합 컨트롤러 선택"))Selection.activeGameObject=d.gameObject;return true;}
        static readonly Dictionary<string,string> Labels=new Dictionary<string,string>{
            {"sideDensity","좌우 연기 밀도"},{"sideParticleSize","좌우 연기 덩어리 크기 (m)"},{"smokeRoundness","연기 세로 볼륨"},{"trailDensity","흰 연기 밀도 / 불투명도"},
            {"launcher","기존 미사일 발사 컨트롤러"},{"interiorVolume","내부 연기 범위"},{"leftVent","왼쪽 배출 위치"},{"rightVent","오른쪽 배출 위치"},
            {"roofProxy","보넷 지붕 충돌 프록시"},{"floorProxy","내부 바닥 충돌 프록시"},{"bodyBlocker","차체 충돌 박스 프록시"},
            {"interior","내부 연기 VFX"},{"sideLeft","왼쪽 누출 VFX"},{"sideRight","오른쪽 누출 VFX"},{"residual","잔연기 VFX"},{"launchBursts","발사구별 Burst VFX"},
            {"interiorStart","내부 연기 시작 지연 (초)"},{"interiorFillDuration","내부 연기가 차오르는 시간 (초)"},{"sideStart","좌우 누출 시작 지연 (초)"},{"sideRiseDuration","좌우 누출이 최대가 되는 시간 (초)"},
            {"sideFadeDelay","발사 후 좌우 연기 유지 (초)"},{"sideFadeDuration","좌우 연기 감소 시간 (초)"},{"interiorFadeDelay","발사 후 내부 연기 유지 (초)"},{"interiorFadeDuration","내부 연기 감소 시간 (초)"},
            {"residualFadeDelay","발사 후 잔연기 유지 (초)"},{"residualFadeDuration","잔연기 감소 시간 (초)"},{"interiorSpawnRate","내부 연기 발생량"},{"interiorDensity","내부 연기 밀도"},{"interiorTurbulence","내부 연기 흔들림"},{"interiorParticleSize","내부 연기 덩어리 크기"},
            {"sideSpawnRate","좌우 연기 발생량"},{"sideSpeed","좌우 배출 속도"},{"sideSpread","좌우 연기 퍼짐"},{"sideTurbulence","좌우 연기 난류"},{"riseAfterDistance","옆으로 이 거리만큼 나온 뒤 상승 (m)"},{"outsideRiseSpeed","외부 상승 속도"},{"backflowSpeed","차량 뒤쪽 흐름 속도"},{"sideAsymmetry","좌우 강도 차이"},
            {"launchBurstIntensity","발사 순간 Burst 강도"},{"residualSpawnRate","잔연기 발생량"},{"residualDensity","잔연기 밀도"},{"smokeColor","연기 색"},{"smokeLifetime","연기 입자 수명 (초)"},{"smokeExpansion","연기 팽창량"},{"collisionMargin","충돌 여유 거리 (m)"},{"showCollisionGuides","선택 시 충돌 가이드 표시"},
            {"core","추진 코어 VFX"},{"flame","추진 화염 VFX"},{"smokeTrail","월드 연기 Trail VFX"},{"exhaustStartDelay","실제 발사 후 추진 시작 지연 (초)"},{"intensity","추진 화염 강도"},{"coreWidth","코어 폭 (미사일 로컬 단위)"},{"flameWidth","화염 폭 (미사일 로컬 단위)"},{"flameLength","화염 길이 (미사일 로컬 단위)"},{"coreColor","코어 색 / HDR"},{"flameColor","화염 색 / HDR"},
            {"trailSpawnRate","Trail 기본 발생량"},{"trailSpeed","연기 배출 속도 (m/s)"},{"trailLength","Trail 목표 길이 (m / 근사값)"},{"trailWidth","Trail 시작 폭 (m)"},{"trailExpansion","Trail 팽창량"},{"trailTurbulence","Trail 난류"},{"maxSmokeLifetime","Trail 최대 수명 (초)"},{"trailColor","Trail 연기 색"},{"extraParticlesPerMeter","이동 1m당 추가 발생량"},{"maxTrailSpawnRate","Trail 발생량 상한"},{"tailCleanupPadding","잔여 Trail 정리 여유 (초)"}
        };
        internal static void Draw(SerializedObject data, bool smokeOnly = false)
        {
            data.Update();var p=data.GetIterator();bool children=true;
            var flameFields=new HashSet<string>{"core","flame","intensity","coreWidth","flameWidth","flameLength","coreColor","flameColor"};
            while(p.NextVisible(children)){children=false;if(p.name=="m_Script"||(smokeOnly&&flameFields.Contains(p.name)))continue;EditorGUILayout.PropertyField(p,new GUIContent(Labels.TryGetValue(p.name,out var label)?label:p.displayName,p.tooltip),true);}
            data.ApplyModifiedProperties();
        }
    }
    [CustomEditor(typeof(HoodMissileVFXController_Final))]
    public sealed class HoodFinalVFXEditor:UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if(FinalVFXInspector.ShowUnified((HoodMissileVFXController_Final)target))return;
            EditorGUILayout.HelpBox("보넷 시작 기준으로 내부 연기와 좌우 누출을 제어합니다. Burst와 감소 타이밍은 실제 발사 신호를 기준으로 하므로 4번 미사일의 상승·대기를 바꿔도 함께 맞춰집니다. 기존 보넷/발사 시간은 Trailer Missile Launcher 및 Hood Deployment에서 조절하세요.",MessageType.Info);
            FinalVFXInspector.Draw(serializedObject);var c=(HoodMissileVFXController_Final)target;
            if(!c.launcher)EditorGUILayout.HelpBox("다른 차량에 재사용할 때 기존 발사 컨트롤러를 연결하고, 연기 위치와 충돌 프록시를 맞춰 주세요.",MessageType.Warning);
            else EditorGUILayout.LabelField("현재 최초 발사 예상 시간",c.launcher.FirstLaunchTime.ToString("F2")+"초");
            if(GUILayout.Button("현재 지붕 프록시를 보넷에 맞춰 기억")){Undo.RecordObject(c,"Capture VFX roof");c.CaptureRoofAnchor();EditorUtility.SetDirty(c);}
            using(new EditorGUI.DisabledScope(!Application.isPlaying))if(GUILayout.Button("연기만 초기화"))c.ResetVFX();
        }
    }
    [CustomEditor(typeof(MissileExhaustVFXController))]
    public sealed class MissileFinalVFXEditor:UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if(FinalVFXInspector.ShowUnified((MissileExhaustVFXController)target))return;
            var c=(MissileExhaustVFXController)target;bool smokeOnly=!c.core&&!c.flame;
            EditorGUILayout.HelpBox(smokeOnly?"흰 연기 VFX 하나로 구성됩니다. 실제 발사 후 배기구에서 시작해 뒤로 갈수록 굵어집니다. 시작 폭·팽창량·길이로 형태를, 난류로 옆으로 흩어지는 정도를 조절하세요. 이미 생성된 연기는 월드 공간에 남고 비행 종료 후 자연스럽게 사라집니다.":"ExhaustPoint의 +Z가 배기 방향입니다. 실제 발사 때만 켜집니다. 화염은 미사일을 따라가고 이미 생성된 연기는 월드 공간에 남습니다.",MessageType.Info);
            FinalVFXInspector.Draw(serializedObject,smokeOnly);
        }
    }
}
