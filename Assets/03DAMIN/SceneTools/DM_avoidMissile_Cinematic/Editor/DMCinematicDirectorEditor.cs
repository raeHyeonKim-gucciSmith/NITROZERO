using UnityEngine;
using UnityEditor;
using Damin.CinematicCopy;
[CustomEditor(typeof(DMCinematicDirector))]
public sealed class DMCopyDirectorEditor:Editor
{
 public override void OnInspectorGUI(){
  var d=(DMCinematicDirector)target;
  serializedObject.Update();
  EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraPlaybackMode"));
  if(serializedObject.FindProperty("cameraPlaybackMode").enumValueIndex==1){
   EditorGUILayout.PropertyField(serializedObject.FindProperty("takeCameraNumber"));
   bool ground=serializedObject.FindProperty("takeCameraNumber").intValue==1;
   if(ground){EditorGUILayout.PropertyField(serializedObject.FindProperty("independentGroundPass"));ground=serializedObject.FindProperty("independentGroundPass").boolValue;}
   if(ground)EditorGUILayout.PropertyField(serializedObject.FindProperty("groundPassTake"),true);
   bool duel=serializedObject.FindProperty("takeCameraNumber").intValue==2;
   if(duel){EditorGUILayout.PropertyField(serializedObject.FindProperty("independentDuel"));duel=serializedObject.FindProperty("independentDuel").boolValue;}
   if(duel)EditorGUILayout.PropertyField(serializedObject.FindProperty("duelTake"),true);
   ground=ground||duel;
   if(!ground){
   EditorGUILayout.PropertyField(serializedObject.FindProperty("useShotTakeTimes"));
   EditorGUILayout.PropertyField(serializedObject.FindProperty("repositionForTake"));
   }
   var profiles=serializedObject.FindProperty("shotTakes");int index=serializedObject.FindProperty("takeCameraNumber").intValue-1;
   bool perShot=serializedObject.FindProperty("useShotTakeTimes").boolValue;
   if(index>=0&&index<profiles.arraySize){var setup=profiles.GetArrayElementAtIndex(index);
    EditorGUILayout.LabelField("이 SHOT 설정",setup.FindPropertyRelative("label").stringValue);
    if(perShot&&!ground){EditorGUILayout.PropertyField(setup.FindPropertyRelative("startTime"));EditorGUILayout.PropertyField(setup.FindPropertyRelative("endTime"));}
    if(ground||serializedObject.FindProperty("repositionForTake").boolValue)EditorGUILayout.PropertyField(setup.FindPropertyRelative("redStartWorldX"));
   }else if(perShot)EditorGUILayout.HelpBox("SHOT 설정이 없어 공통 촬영 구간을 사용합니다.",MessageType.Warning);
   if(!ground&&(!perShot||index<0||index>=profiles.arraySize)){EditorGUILayout.PropertyField(serializedObject.FindProperty("takeStartTime"));EditorGUILayout.PropertyField(serializedObject.FindProperty("takeEndTime"));}
   EditorGUILayout.PropertyField(serializedObject.FindProperty("loopTake"));
   if(serializedObject.FindProperty("loopTake").boolValue)EditorGUILayout.PropertyField(serializedObject.FindProperty("takeLoopDelay"));
  }
  serializedObject.ApplyModifiedProperties();
  bool single=d.cameraPlaybackMode==DMCinematicDirector.CameraPlaybackMode.SingleCamera;
  EditorGUILayout.HelpBox(single?"선택한 카메라만 유지합니다. 시작 시간까지는 연기·미사일 상태를 맞추는 준비 재생, 시작~종료가 사용할 촬영 구간입니다. 영상 파일은 Unity Recorder 또는 화면 녹화 도구로 별도 저장하세요. Play 중 설정 변경은 아래 다시 재생 버튼으로 적용합니다.":"전체 모드: 13개 연출 구간에 맞춰 카메라가 자동 전환됩니다. 개별 카메라 모드를 선택해도 차량 연출 시간표는 바뀌지 않습니다.",MessageType.Info);
  if(single&&d.cameras&&d.cameras.shots!=null&&d.takeCameraNumber>=1&&d.takeCameraNumber<=d.cameras.shots.Length)EditorGUILayout.LabelField("선택 카메라",d.cameras.shots[d.takeCameraNumber-1].label);
  if(single&&d.repositionForTake&&!d.GroundPassConfigured&&!d.DuelConfigured)EditorGUILayout.HelpBox("선택 SHOT의 촬영 시작 시 빨간차가 지정 X에 오도록 전체 동선을 한 번 이동합니다. 높이·차선·차량 간격·맵은 그대로입니다. 준비 재생은 맵 밖일 수 있으니 '촬영 구간 재생 중'부터 사용하세요. 번호/위치/시간 변경 후 다시 재생 버튼을 누릅니다.",MessageType.Info);
  if(d.DuelConfigured)EditorGUILayout.HelpBox("02 독립 테이크: 파랑 접근 → 추월 시도 → 빨강 진로 견제 → 파랑 감속 → 카키 등장. 02 전용 길이/속도/렌즈/카메라 높이/반응 지연/진동을 조절합니다. 다른 SHOT과 전체 시간표에는 적용하지 않습니다. 변경 후 다시 재생하세요.",MessageType.Info);
  if(d.GroundPassConfigured)EditorGUILayout.HelpBox("01 독립 테이크: 시작부터 주행 → 빨강 뒤에서 파랑 등장 → 시간차 통과 → 빈 도로 여운. 위 01 설정의 길이/속도/통과 시각을 사용하며 보넷·미사일 사건은 재생하지 않습니다. 다른 SHOT의 시간표·8배 주행은 유지합니다. 출발 X는 이 테이크의 빨간차 기준입니다.",MessageType.Info);
  if(single&&d.takeCameraNumber==7)EditorGUILayout.HelpBox("07은 발사 전에는 장착 미사일을, 발사 후에는 실제 미사일을 봅니다.",MessageType.Info);
  if(single&&d.takeCameraNumber==10&&!d.interiorCameraReady)EditorGUILayout.HelpBox("10번 실내 카메라는 아직 미완성 위치입니다. 개별 모드는 선택한 10번을 그대로 보여주며 지붕/차체에 가릴 수 있습니다. 자동 모드만 탑뷰로 대체합니다.",MessageType.Warning);
  if(Application.isPlaying){
   EditorGUILayout.LabelField("현재 재생",d.FilmTime.ToString("F2")+" / "+d.FilmDuration.ToString("F2")+"초 · "+DMCinematicDirector.Labels[Mathf.Clamp(d.Phase,0,12)]);
   if(d.SingleCameraTakeActive){EditorGUILayout.LabelField("실행 중 카메라",d.ActiveTakeCamera.ToString("00"));EditorGUILayout.LabelField("실행 중 촬영 구간",d.ActiveTakeStart.ToString("F2")+" ~ "+d.ActiveTakeEnd.ToString("F2")+"초");EditorGUILayout.HelpBox(d.TakePreparing?"준비 재생 중 — 촬영 시작까지 "+(d.ActiveTakeStart-d.FilmTime).ToString("F2")+"초":d.TakeReady?"촬영 구간 재생 중 · "+d.TakeElapsed.ToString("F2")+"초":"촬영 구간 종료 — 반복 설정 시 준비 재생부터 다시 시작합니다.",MessageType.Info);}
   if(!string.IsNullOrEmpty(d.Error))EditorGUILayout.HelpBox(d.Error,MessageType.Error);
   if(d.SingleCameraTakeActive&&d.repositionForTake)EditorGUILayout.LabelField("이번 촬영 동선 이동 X",d.ActiveTakeOffset.x.ToString("F1")+" m");
   if(!string.IsNullOrEmpty(d.TakePlacementWarning))EditorGUILayout.HelpBox(d.TakePlacementWarning,MessageType.Warning);
   if(GUILayout.Button("변경값으로 처음부터 다시 재생"))d.Replay();
   if(d.Completed&&!d.SingleCameraTakeActive)EditorGUILayout.HelpBox("마지막 구간까지 재생했습니다. 다시 재생 버튼으로 반복 확인하세요.",MessageType.Info);
   Repaint();
  }
  if(!d.GroundPassConfigured&&!d.DuelConfigured){
  float end=0;var times=d.timing.Values();
  EditorGUILayout.LabelField("설정 시간표 (변경 시 자동 계산)",EditorStyles.boldLabel);
  for(int i=0;i<times.Length;i++){EditorGUILayout.LabelField((i+1).ToString("00")+" "+DMCinematicDirector.Labels[i],end.ToString("F2")+" ~ "+(end+times[i]).ToString("F2")+"초");end+=times[i];}
  EditorGUILayout.LabelField("총 길이",end.ToString("F2")+"초");
  if(single&&(d.ConfiguredTakeEnd>end||d.ConfiguredTakeStart>=d.ConfiguredTakeEnd))EditorGUILayout.HelpBox("촬영 구간은 전체 길이 안으로 제한됩니다. 시작은 종료보다 앞이어야 합니다. 긴 액션이 필요하다면 별도의 시간표 조정이 필요하며, 이 옵션은 액션을 늘리지 않습니다.",MessageType.Warning);
  EditorGUILayout.LabelField("설정상 기본 주행",d.ConfiguredRaceSpeedKph.ToString("F0")+" km/h");
  }else if(d.DuelConfigured)EditorGUILayout.LabelField("02 독립 녹화",d.duelTake.duration.ToString("F1")+"초 / "+d.duelTake.speedKph.ToString("F0")+" km/h");
  else EditorGUILayout.LabelField("01 독립 녹화",d.groundPassTake.duration.ToString("F1")+"초 / "+d.groundPassTake.speedKph.ToString("F0")+" km/h");
  EditorGUILayout.Space();serializedObject.Update();DrawPropertiesExcluding(serializedObject,"m_Script","cameraPlaybackMode","takeCameraNumber","takeStartTime","takeEndTime","loopTake","takeLoopDelay","useShotTakeTimes","repositionForTake","shotTakes","independentGroundPass","groundPassTake","independentDuel","duelTake");serializedObject.ApplyModifiedProperties();
  if(!d.interiorCameraReady)EditorGUILayout.HelpBox("실내 구간은 현재 탑뷰 대체입니다. 운전자 눈빛 애니메이션은 아직 없습니다. 창문 통과 조절점은 경로 초안이며 차체/유리 메시는 수정하지 않습니다.",MessageType.Warning);
 }
}
