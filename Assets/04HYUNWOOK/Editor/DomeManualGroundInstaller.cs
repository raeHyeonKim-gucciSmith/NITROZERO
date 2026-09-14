using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.Splines;
using Unity.Mathematics;
using Unity.Cinemachine;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeManualGroundInstaller
{
    const string Request="Library/DomeManualGround.request";
    static DomeManualGroundInstaller(){EditorApplication.delayCall+=Check;}
    static void Check()
    {
        if(!File.Exists(Request))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        {EditorApplication.delayCall+=Check;return;}
        File.Delete(Request);
        try{Apply();}catch(Exception e){File.WriteAllText("Library/DomeManualGround.error.txt",e.ToString());Debug.LogException(e);}
    }
    static void Apply()
    {
        var scene=SceneManager.GetSceneByPath("Assets/04HYUNWOOK/Scenes/HW_domeInTheMoon.unity");
        if(!scene.isLoaded)throw new InvalidOperationException("Open dome scene first.");
        var seq=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DomeCinematicSequence>(true)).Single();
        if(seq.dollies.Length!=4 || !seq.car || !seq.red)throw new InvalidOperationException("Expected four cameras and two vehicles.");
        seq.StopAndRestore();
        var backup="Documentation/DomeCameraPreview/BeforeManualGround-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);
        if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
        var old=(TimelineAsset)seq.director.playableAsset;
        var oldTracks=old.GetOutputTracks().ToArray();
        var bindings=oldTracks.Select(t=>seq.director.GetGenericBinding(t)).ToArray();
        var path=AssetDatabase.GenerateUniqueAssetPath("Assets/04HYUNWOOK/Cinematics/HW_Dome_ManualGround_RedTrack_5s.playable");
        if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(old),path))throw new IOException("Timeline copy failed");
        var timeline=AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
        seq.director.playableAsset=timeline;
        var tracks=timeline.GetOutputTracks().ToArray();
        for(int i=0;i<tracks.Length;i++)
        {
            seq.director.SetGenericBinding(tracks[i],bindings[i]);
            var last=tracks[i].GetClips().OrderBy(c=>c.start).Last();
            last.displayName="04_Manual_Ground_Red_Track";
            if(last.asset is CinemachineShot shot){shot.DisplayName=last.displayName;EditorUtility.SetDirty(shot);}
            EditorUtility.SetDirty(tracks[i]);
        }
        var dolly=seq.dollies[3];var aim=seq.dollyAim[3];var camera=dolly.GetComponent<CinemachineCamera>();
        dolly.name="CM_Dolly_04_Manual_Ground_Red_Track";
        dolly.Spline.name="Spline_04_Manual_Ground_Red_Track";
        aim.positionFollowTarget=null;aim.positionFollowReference=Vector3.zero;
        aim.positionOffsetWorld=Vector3.zero;
        aim.redTarget=seq.red;aim.trackRed=true;aim.overhead=false;
        aim.targetOffsetWorld=new Vector3(0,.25f,0);
        aim.rightPanDegrees=0;aim.rotationOffset=Vector3.zero;
        aim.pathStart=aim.pathEnd=0;aim.timelineDrivesDolly=false;aim.timelineDrivesAim=true;
        var groundPosition=new Vector3(seq.portal.x-41+seq.fastSpeedMetresPerSecond*(4.375f-2.5f),seq.portal.y+.16f,seq.redStart.z)+seq.redVehicleOffset;
        var p=dolly.Spline.transform.InverseTransformPoint(groundPosition);
        dolly.Spline.Spline=new Spline(new[]{(float3)p,(float3)(p+Vector3.right*.01f)},TangentMode.Linear);
        dolly.CameraPosition=0;dolly.enabled=false;
        camera.Follow=null;camera.LookAt=seq.red;camera.Lens.FieldOfView=76;
        camera.transform.position=groundPosition;
        camera.transform.rotation=Quaternion.LookRotation(seq.red.position+aim.targetOffsetWorld-groundPosition,Vector3.up);
        aim.holdOrientation=false;seq.freezeCameraAfterEnd=false;
        EditorUtility.SetDirty(seq);
        seq.director.time=0;
        EditorUtility.SetDirty(dolly.Spline);EditorUtility.SetDirty(dolly);EditorUtility.SetDirty(aim);EditorUtility.SetDirty(camera);
        EditorUtility.SetDirty(seq.director);EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
        File.WriteAllText("Library/DomeManualGround.success.txt","Saved "+scene.path+"\nFinal shot uses editable camera Transform at road level. Dolly disabled; aim tracks red continuously. Timing unchanged.\nTimeline: "+path+"\nBackup: "+backup+"\nNo Play Mode or tests.");
    }
}

