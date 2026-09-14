using UnityEngine;
using UnityEditor;
using Damin.SceneOnly;
[CustomEditor(typeof(DMCinematicDirector))]
public sealed class DMCinematicDirectorEditor:Editor
{
 public override void OnInspectorGUI(){
  var d=(DMCinematicDirector)target;
  EditorGUILayout.HelpBox("이 씬 하나에서 13개 촬영 구간이 순서대로 이어집니다. 길이를 바꾸면 뒤 구간 시작 시각은 자동 계산됩니다. Play 중 변경값은 [변경값으로 처음부터 다시 재생]할 때 적용됩니다. Unity의 Pause로 일시정지하세요.",MessageType.Info);
  if(Application.isPlaying){
   EditorGUILayout.LabelField("현재 재생",d.FilmTime.ToString("F2")+" / "+d.FilmDuration.ToString("F2")+"초 · "+DMCinematicDirector.Labels[Mathf.Clamp(d.Phase,0,12)]);
   if(!string.IsNullOrEmpty(d.Error))EditorGUILayout.HelpBox(d.Error,MessageType.Error);
   if(GUILayout.Button("변경값으로 처음부터 다시 재생"))d.Replay();
   if(d.Completed)EditorGUILayout.HelpBox("마지막 구간까지 재생했습니다. 다시 재생 버튼으로 반복 확인하세요.",MessageType.Info);
   Repaint();
  }
  float end=0;var times=d.timing.Values();
  EditorGUILayout.LabelField("설정 시간표 (변경 시 자동 계산)",EditorStyles.boldLabel);
  for(int i=0;i<times.Length;i++){EditorGUILayout.LabelField((i+1).ToString("00")+" "+DMCinematicDirector.Labels[i],end.ToString("F2")+" ~ "+(end+times[i]).ToString("F2")+"초");end+=times[i];}
  EditorGUILayout.LabelField("총 길이",end.ToString("F2")+"초");
  EditorGUILayout.Space();DrawDefaultInspector();
  if(!d.interiorCameraReady)EditorGUILayout.HelpBox("실내 구간은 현재 탑뷰 대체입니다. 운전자 눈빛 애니메이션은 아직 없습니다. 창문 통과 조절점은 경로 초안이며 차체/유리 메시는 수정하지 않습니다.",MessageType.Warning);
 }
}
