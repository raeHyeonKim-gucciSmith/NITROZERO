using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Nitrozero.Cinematics;

[CustomEditor(typeof(DomeDollyAim)), CanEditMultipleObjects]
public sealed class DomeDollyAimInspector : Editor
{
    public override void OnInspectorGUI()
    {
        if (targets.Length == 1) DomeShotTimingEditor.DrawForAim((DomeDollyAim)target);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || targets.Length != 1))
            if (GUILayout.Button("이 카메라를 Game 창에서 미리보기"))
                DomeCameraPreviewWindow.ShowAim((DomeDollyAim)target);
        serializedObject.Update();
        if(targets.OfType<DomeDollyAim>().Any(aim=>aim.overhead))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("topViewMovement"),new GUIContent("카메라 이동"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("trackRed"),new GUIContent("차량 바라보기", "켜면 바라볼 대상에 지정한 GameObject를 바라봅니다. 카메라 이동과 독립적으로 사용할 수 있습니다."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("redTarget"),new GUIContent("바라볼 대상", "추적할 GameObject를 드래그해서 지정하세요."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("keepOverheadOrthographic"),new GUIContent("원근감 제거 (직교 투영)", "차량을 바라볼 때도 직교 투영을 유지해 거리에 따라 도로 폭이 좁아지는 원근감을 제거합니다."));
            EditorGUILayout.HelpBox("체크: 스플라인을 따라 이동 / 해제: 현재 위치 고정 및 Transform 직접 편집. 시선 설정은 유지됩니다.",MessageType.Info);
            EditorGUILayout.HelpBox("차량 바라보기: 체크하면 지정한 GameObject 추적 / 해제하면 설정된 기본 시선 사용. 이동을 꺼도 대상을 바라볼 수 있습니다.",MessageType.Info);
        }
        if(targets.OfType<DomeDollyAim>().All(aim=>aim.overhead))
            DrawPropertiesExcluding(serializedObject,"topViewMovement","trackRed","redTarget","keepOverheadOrthographic","useTransformPosition","timelineDrivesDolly");
        else if(targets.OfType<DomeDollyAim>().Any(aim=>aim.overhead))
            DrawPropertiesExcluding(serializedObject,"topViewMovement","trackRed","redTarget","keepOverheadOrthographic");
        else DrawPropertiesExcluding(serializedObject,"topViewMovement");
        if(serializedObject.ApplyModifiedProperties())
        {
            foreach(var aim in targets.OfType<DomeDollyAim>().Where(a=>a.overhead))
            {
                Undo.RecordObject(aim,"탑뷰 이동 모드 변경");
                var dolly=aim.GetComponent<Unity.Cinemachine.CinemachineSplineDolly>();
                if(dolly)Undo.RecordObject(dolly,"탑뷰 이동 모드 변경");
                aim.SyncTopViewMovement();EditorUtility.SetDirty(aim);
                if(dolly)EditorUtility.SetDirty(dolly);
            }
            EditorApplication.QueuePlayerLoopUpdate();SceneView.RepaintAll();
        }
        EditorGUILayout.HelpBox("Right Pan Degrees: 오른쪽 회전량. Rotation Offset: 회전 보정. Position Offset World: 위치 보정. 수동 진행률은 Timeline Drives Aim/Dolly를 끄고 수정하세요. Track Red가 켜져 있으면 대상 추적이 Euler/샘플 회전보다 우선합니다.",MessageType.Info);
    }
}

[InitializeOnLoad]
public static class DomeInspectorEditing
{
    const string Request="Library/DomeInspectorEditing.request";
    static DomeInspectorEditing(){EditorApplication.delayCall+=Check;}
    static void Check()
    {
        if(!File.Exists(Request))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        {EditorApplication.delayCall+=Check;return;}
        File.Delete(Request);
        try
        {
            var scene=SceneManager.GetSceneByPath("Assets/04HYUNWOOK/Scenes/HW_domeInTheMoon.unity");
            if(!scene.isLoaded)throw new InvalidOperationException("Open dome scene first.");
            var seq=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DomeCinematicSequence>(true)).Single();
            if(seq.dollyAim.Length!=4)throw new InvalidOperationException("Expected four cameras.");
            seq.StopAndRestore();
            var backup="Documentation/DomeCameraPreview/BeforeInspectorEditing-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
            for(int i=0;i<seq.dollyAim.Length;i++)
            {
                var aim=seq.dollyAim[i];
                aim.timelineDrivesAim=true;aim.timelineDrivesDolly=true;
                aim.pathStart=0;aim.pathEnd=i==3?0:1;
                if(i==0 && aim.rotations!=null && aim.rotations.Length>1)
                {
                    // Move the previously baked pan into an editable Inspector parameter.
                    for(int j=0;j<aim.rotations.Length;j++)
                    {
                        float u=Mathf.Min(j/(float)(aim.rotations.Length-1),.9999f);
                        float smooth=u*u*(3-2*u);
                        aim.rotations[j]=Quaternion.AngleAxis(-12f*smooth,Vector3.up)*aim.rotations[j];
                    }
                    aim.rightPanDegrees=12;
                }
                if(aim.rotations!=null && aim.rotations.Length>0)
                {aim.startEuler=aim.rotations[0].eulerAngles;aim.endEuler=aim.rotations[aim.rotations.Length-1].eulerAngles;}
                EditorUtility.SetDirty(aim);
            }
            seq.driveVehicles=true;EditorUtility.SetDirty(seq);
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeInspectorEditing.success.txt","Saved "+scene.path+"\nInspector controls installed on all four cameras.\nBackup: "+backup+"\nNo Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeInspectorEditing.error.txt",e.ToString());Debug.LogException(e);}
    }
}
