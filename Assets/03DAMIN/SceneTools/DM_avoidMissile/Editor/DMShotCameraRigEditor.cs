using UnityEngine;
using UnityEditor;
using Damin.SceneOnly;

[CustomEditor(typeof(DMShotCameraRig))]
public sealed class DMShotCameraRigEditor:Editor
{
    static void Record(DMShotCameraRig rig)
    {
        if(Application.isPlaying)return;
        Undo.RecordObject(rig,"DM shot preview");
        if(rig.outputCamera)Undo.RecordObjects(new Object[]{rig.outputCamera,rig.outputCamera.transform},"DM output preview");
        if(rig.shots!=null)foreach(var s in rig.shots)if(s!=null&&s.camera)Undo.RecordObjects(new Object[]{s.camera,s.camera.transform},"DM shot preview");
    }
    public override void OnInspectorGUI()
    {
        var rig=(DMShotCameraRig)target;
        var film=Application.isPlaying?Object.FindFirstObjectByType<DMCinematicDirector>():null;
        if(film && film.isActiveAndEnabled && film.cameras==rig)
        {
            EditorGUILayout.HelpBox("전체 촬영 감독이 카메라를 제어 중입니다. 구간 시간과 다시 재생은 DM_CinematicDirector에서 조절하세요. 개별 카메라 구도는 Play 정지 후 수정할 수 있습니다.",MessageType.Info);
            if(GUILayout.Button("전체 촬영 시간표 선택"))Selection.activeGameObject=film.gameObject;
            EditorGUI.BeginDisabledGroup(true);DrawDefaultInspector();EditorGUI.EndDisabledGroup();return;
        }
        EditorGUILayout.HelpBox("카메라 10개 / Main Camera 1개. 전체 시간표는 DM_CinematicDirector가 제어합니다. 01은 근접 고정, 02는 추월·방어 후측면 추적입니다. 아래 버튼은 정지 상태의 구도 확인용입니다.",MessageType.Info);
        if (Application.isPlaying && rig.opening)
        {
            EditorGUILayout.LabelField("주행 시간 / 거리", rig.opening.ElapsedSeconds.ToString("F2")+"초 / "+rig.opening.TravelledMetres.ToString("F1")+"m");
            EditorGUILayout.LabelField("재생 상태",rig.OpeningPlaybackActive && rig.autoPlayOpening ? "01→02 자동 재생 중" : "수동 샷 확인");
            if (!rig.opening.moveOnPlay || rig.opening.speedKph<=0 || !rig.opening.isActiveAndEnabled)
                EditorGUILayout.HelpBox("주행이 꺼져 있습니다. DMRaceOpening의 활성화 / Move On Play / Speed Kph를 확인하세요.",MessageType.Warning);
            if (Mathf.Approximately(Time.timeScale,0))
                EditorGUILayout.HelpBox("Time Scale이 0이라 주행 시간이 멈춰 있습니다.",MessageType.Warning);
            if(GUILayout.Button("처음부터 다시 보기 (차량 위치 + 01→02)")) rig.RestartOpeningSequence();
            Repaint();
        }
        if(rig.shots!=null)for(int i=0;i<rig.shots.Length;i++)
        {
            var s=rig.shots[i];if(s==null)continue;
            if(GUILayout.Button((i+1).ToString("00")+"  "+s.label+(rig.activeShot==i+1?"  [선택됨]":"")))
            {Record(rig);rig.SelectShot(i+1);if(!Application.isPlaying)EditorUtility.SetDirty(rig);SceneView.RepaintAll();}
        }
        if(rig.shots!=null&&rig.activeShot>0&&rig.activeShot<=rig.shots.Length)
        {
            var s=rig.shots[rig.activeShot-1];
            if(s!=null){
                if(!string.IsNullOrEmpty(s.productionNote))EditorGUILayout.HelpBox(s.productionNote,MessageType.Info);
                if(s.hasEndPose){float p=EditorGUILayout.Slider("구도 이동 (시작 → 끝)",s.moveProgress,0,1);
                    if(!Mathf.Approximately(p,s.moveProgress)){Record(rig);s.moveProgress=p;rig.SelectShot(rig.activeShot);if(!Application.isPlaying)EditorUtility.SetDirty(rig);SceneView.RepaintAll();}}
            }
        }
        EditorGUILayout.Space();DrawDefaultInspector();
        EditorGUILayout.HelpBox("고정 샷: CM_Shot Transform. 추적 샷: Position Offset / Aim Offset. Use Focal Length 사용 시 Focal Length Mm에서 화각 조절(36mm 폭·16:9 환산). 꺼진 샷은 CM_Shot > Lens > FOV. 정지 상태에서 버튼을 다시 눌러 반영하세요. Play 중 수정은 종료 시 되돌아갑니다.",MessageType.None);
    }
}
