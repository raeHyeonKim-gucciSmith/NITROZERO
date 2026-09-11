using UnityEditor;
using UnityEngine;
using Damin.VFX.TireSmoke.Progressive;

[CustomEditor(typeof(ProgressiveTireSmokeController))]
public sealed class ProgressiveTireSmokeControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("독립 Progressive 버전입니다. 기존 Burnout/Final은 변경하지 않습니다.\n연기 부모에 붙이는 스크립트이며 뒷바퀴 두 개만 연결합니다. 연결하지 않으면 현재 위치에서 단독 연기를 미리 봅니다.",MessageType.Info);
        serializedObject.Update();
        EditorGUILayout.LabelField("트레일러 시간표 / Play 또는 시작 버튼 기준",EditorStyles.boldLabel);
        string[] timing={"previewOnPlay","sequenceStartDelay","previewBuildTime","previewHoldTime","previewFadeTime","previewRamp","loopPreview","previewRestTime"};
        string[] labels={"Play 시작 시 자동 실행","연기 시작 지연 (초)","연기가 커지는 시간 (초)","최대 연기 유지 (초)","발산 감소 시간 (초)","증가 곡선","반복 재생","반복 전 대기 (초)"};
        for(int i=0;i<timing.Length;i++)EditorGUILayout.PropertyField(serializedObject.FindProperty(timing[i]),new GUIContent(labels[i]));
        EditorGUILayout.Space();DrawPropertiesExcluding(serializedObject,"m_Script","previewOnPlay","sequenceStartDelay","previewBuildTime","previewHoldTime","previewFadeTime","previewRamp","loopPreview","previewRestTime");
        serializedObject.ApplyModifiedProperties();var c=(ProgressiveTireSmokeController)target;
        float a=c.sequenceStartDelay,b=a+c.previewBuildTime,d=b+c.previewHoldTime,e=d+c.previewFadeTime;
        EditorGUILayout.HelpBox($"{a:0.0}s 생성 시작 → {b:0.0}s 최대 강도 → {d:0.0}s 감소 시작 → {e:0.0}s 새 연기 생성 종료\n남아 있는 연기는 그 이후에도 수명에 따라 소멸합니다. 시간표 재생 중에는 수동 Build Up/Fade Out 지연을 중복 적용하지 않습니다.",MessageType.Info);
        EditorGUILayout.LabelField("연출 경과 시간",c.SequenceTime.ToString("0.00")+" s");
        EditorGUILayout.LabelField("실제 연기 강도",c.CurrentPower.ToString("0.00"));
        using(new EditorGUI.DisabledScope(!Application.isPlaying)){
            if(GUILayout.Button("트레일러 연출 시작 / 시간표 재시작"))c.StartPreview();
            if(GUILayout.Button("발산 중지 / 남은 연기 자연 소멸"))c.StopPreview();
            if(GUILayout.Button("뒷바퀴 연결 다시 적용"))c.RebuildBindings();
        }
        EditorGUILayout.HelpBox("Play에서 Smoke Power를 0~1로 조절하세요. Build Up/Fade Out은 반응 시간, Layers의 Response는 레이어별 증가 곡선입니다. 미리보기 실행 중에는 자동 재생이 Smoke Power를 제어합니다.\nUse Slip Amount를 켜면 외부 입력 Slip Amount도 곱합니다. 속도/슬립/접지는 자동 측정하지 않습니다. 차체 Collision/SDF 및 이동 후 World-space 잔류 연기는 아직 포함하지 않았습니다.",MessageType.None);
        if(Application.isPlaying)Repaint();
    }
}
