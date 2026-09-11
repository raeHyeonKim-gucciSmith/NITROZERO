using UnityEditor;
using UnityEngine;
namespace Damin.Trailer.MissileCar.Editor {
 [CustomEditor(typeof(TrailerMissileLauncher))] public sealed class TrailerMissileLauncherEditor:UnityEditor.Editor {
  public override void OnInspectorGUI(){serializedObject.Update();
   var unified=((TrailerMissileLauncher)target).GetComponentInParent<HoodMissileSequenceController>();
   if(unified){EditorGUILayout.HelpBox("차량 최상위의 보넷 미사일 통합 연출에서 시간·속도를 조절하세요. 이 컴포넌트는 발사·재장전 내부 담당입니다.",MessageType.Info);if(GUILayout.Button("통합 컨트롤러 선택"))Selection.activeGameObject=unified.gameObject;return;}
   EditorGUILayout.HelpBox("Shift: 보넷 열림 → 발사대 상승 → 선택 미사일만 상승 → 대기 → 발사. 기본 첫 발은 4번이며, 나머지는 장착 상태를 유지합니다. 다음 Shift는 남은 다음 1발을 발사합니다.",MessageType.Info);
   P("deployment","보넷 / 발사대 제어");P("vehicleFrame","차량 기준 Transform");P("flightDirection","비행 방향");P("missilePrefab","재장전용 미사일 프리팹");P("slots","장착 미사일 4개");
   P("useShiftInput","Shift 키 사용");P("playOnStart","Play 시작 시 자동 연출");P("sequenceStartDelay","연출 시작 지연 (초)");P("launchDelayAfterDeployment","발사대 전개 후 준비 시작까지 (초)");P("intervalBetweenMissiles","다음 발사 준비까지 간격 (초)");P("missilesPerSequence","한 번에 발사할 수 (1~4)");P("launchOrder","발사 순서 (1~4번)");
   P("enablePreLaunchLift","발사 전 개별 미사일 상승 사용");
   P("minimumFirstLaunchTime","보넷 시작 후 첫 발사 최소 시간 (초)");
   EditorGUILayout.HelpBox("연기를 충분히 쌓고 발사하려면 첫 발사 최소 시간을 늘리세요. 전개·상승·대기의 합이 더 길면 그 시간이 우선합니다. 미사일을 천천히 날리려면 초기 속도와 가속도를 함께 낮추세요.",MessageType.None);
   using(new EditorGUI.DisabledScope(!serializedObject.FindProperty("enablePreLaunchLift").boolValue)){
    P("preLaunchMissileNumber","별도 상승시킬 미사일 번호 (1~4)");P("preLaunchLiftHeightCm","미사일 추가 상승 높이 (cm)");P("preLaunchLiftDuration","미사일 상승 시간 (초)");P("preLaunchHoldDuration","올라온 뒤 대기 시간 (초)");
   }
   EditorGUILayout.HelpBox("추가 상승은 미사일 통 전체 상승과 별개입니다. 선택 번호에만 적용되며 발사 순서는 위 목록으로 지정합니다. 상승·대기 중에는 차량을 따라가고, 실제 발사 때 분리됩니다. cm는 Unity 1단위=1m 기준입니다.",MessageType.None);
   P("clearanceHeight","발사 후 이탈 추가 상승 높이 (m)");P("clearanceForwardDistance","발사 후 이탈 전진 거리 (m)");P("clearanceTime","발사 후 이탈 시간 (초)");P("missileSpeed","이탈 후 초기 속도 (m/s)");P("missileAcceleration","가속도 (m/s²)");P("missileLifetime","발사 후 유지 시간 (초)");
   foreach(var n in new[]{"onSequenceStarted","onBayReady","onMissileLaunched","onSequenceFinished","onReset"})P(n,n);
   serializedObject.ApplyModifiedProperties();var c=(TrailerMissileLauncher)target;
   EditorGUILayout.LabelField("첫 발사 시점",c.FirstLaunchTime.ToString("F2")+"초 (현재 남은 첫 발 / 보넷 전개부터)");EditorGUILayout.LabelField("남은 미사일",c.LoadedCount.ToString());
   if(Application.isPlaying)EditorGUILayout.LabelField("개별 상승 / 대기 중",c.IsPreparingMissile?"예":"아니오");
   using(new EditorGUI.DisabledScope(!Application.isPlaying)){if(GUILayout.Button("연출 시작 / 다음 미사일 발사"))c.PlaySequence();if(GUILayout.Button("처음 상태로 복원 + 4발 재장전"))c.ResetAndReload();}
  }
  void P(string name,string label){var p=serializedObject.FindProperty(name);if(p!=null)EditorGUILayout.PropertyField(p,new GUIContent(label),true);}
 }
}
