using UnityEditor;
using UnityEngine;
using Damin.VFX.AirDistortion.Reusable;

[CustomEditor(typeof(ReusableBulletAirDistortion)),CanEditMultipleObjects]
public sealed class ReusableBulletAirDistortionEditor : Editor
{
    bool advanced;
    void Field(string name,string title,string tip="")=>EditorGUILayout.PropertyField(serializedObject.FindProperty(name),new GUIContent(title,tip));
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("1. 씬의 총알 연결 → 2. 실제 촬영 Camera 연결 → Play\n배경은 자동 촬영합니다. 총알 이동·카메라 추적은 변경하지 않습니다.",MessageType.Info);
        Field("Target","따라갈 총알");Field("ViewCamera","촬영 카메라");
        EditorGUILayout.Space();Field("DistortionStrength","왜곡 강도");Field("SizeMultiplier","효과 크기 배율");
        Field("WakeLengthMultiplier","꼬리 길이 배율");Field("TrailFade","잔상 유지 시간 (초)");
        advanced=EditorGUILayout.Foldout(advanced,"고급 설정",true);
        if(advanced)
        {
            EditorGUI.indentLevel++;
            Field("AutoSize","총알 크기 자동 맞춤");
            Field("ManualBulletLength","수동/대체 총알 길이 (m)");Field("ManualBulletDiameter","수동/대체 총알 지름 (m)");
            Field("CenterOffset","중심 보정 (총알 로컬)");Field("AutoForwardAxis","초기 방향 자동 감지");Field("ForwardAxis","수동 초기 앞 방향 (총알 로컬)");
            Field("WidthMultiplier","폭 배율");Field("RefractionPixels","굴절 거리 (화면 픽셀)");Field("OpacityMask","왜곡 영역 가중치");
            Field("NoiseScale","노이즈 크기");Field("NoiseSpeed","노이즈 속도");Field("FineDetail","미세 흐름");
            Field("MeasureTargetSpeed","이동 속도에 강도 연동");Field("FullStrengthSpeed","최대 강도 도달 속도 (m/s)");Field("TeleportDistance","순간이동 판정 거리 (m)");
            Field("DistortionMaterial","전용 재질 (기본값 유지)");
            EditorGUI.indentLevel--;
        }
        serializedObject.ApplyModifiedProperties();
        if(targets.Length!=1)return;
        var fx=(ReusableBulletAirDistortion)target;
        string issue=fx.ConfigurationIssue();
        if(!string.IsNullOrEmpty(issue))EditorGUILayout.HelpBox(issue,MessageType.Warning);
        else if(!fx.Target)EditorGUILayout.HelpBox("따라갈 총알이 비어 있습니다. 런타임 생성 총알은 Bind(target, camera)로 연결할 수도 있습니다.",MessageType.Warning);
        else EditorGUILayout.HelpBox("연결 완료 · Play 시 왜곡만 적용합니다.",MessageType.Info);
        if(fx.Target&&!fx.Target.GetComponentInChildren<MeshRenderer>(true)&&!fx.Target.GetComponentInChildren<SkinnedMeshRenderer>(true))
            EditorGUILayout.HelpBox("연결된 총알 아래에 MeshRenderer가 없습니다. 자동 크기 대신 수동 크기를 사용합니다.",MessageType.Warning);
        if(Application.isPlaying)
        {
            if(!string.IsNullOrEmpty(fx.RuntimeIssue))EditorGUILayout.HelpBox(fx.RuntimeIssue,MessageType.Warning);
            EditorGUILayout.LabelField("측정 길이 / 지름",$"{fx.CurrentBulletLength:F3} m / {fx.CurrentBulletDiameter:F3} m");
            if(GUILayout.Button("총알 모형 다시 읽기 / 잔상 초기화")){fx.RefreshTarget();fx.ClearTrail();}
            Repaint();
        }
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("지원: URP 3D · 단일 Base 카메라 · 원근/직교\n제외: XR, 카메라 Stack, 분할 Viewport, TAA, Screen Space Overlay UI 굴절\n왜곡 1개당 추가 화면 촬영 1회/프레임. 다수 총알에는 성능 점검이 필요합니다.",MessageType.None);
    }
}
