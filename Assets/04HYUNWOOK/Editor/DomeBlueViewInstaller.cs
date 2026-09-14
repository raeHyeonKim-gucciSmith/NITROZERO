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
public static class DomeBlueViewInstaller
{
    const string Request="Library/DomeBlueView.request";
    static DomeBlueViewInstaller(){EditorApplication.delayCall+=Check;}
    static void Check()
    {
        if(!File.Exists(Request))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        {EditorApplication.delayCall+=Check;return;}
        File.Delete(Request);
        try{Apply();}catch(Exception e){File.WriteAllText("Library/DomeBlueView.error.txt",e.ToString());Debug.LogException(e);}
    }
    static void Apply()
    {
        var scene=SceneManager.GetSceneByPath("Assets/04HYUNWOOK/Scenes/HW_domeInTheMoon.unity");
        if(!scene.isLoaded)throw new InvalidOperationException("Open dome scene first.");
        var seq=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DomeCinematicSequence>(true)).Single();
        if(seq.dollies.Length!=4 || !seq.car || !seq.red)throw new InvalidOperationException("Expected four cameras and two vehicles.");
        seq.StopAndRestore();
        var backup="Documentation/DomeCameraPreview/BeforeBlueView-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);
        if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
        var old=(TimelineAsset)seq.director.playableAsset;
        var oldTracks=old.GetOutputTracks().ToArray();
        var bindings=oldTracks.Select(t=>seq.director.GetGenericBinding(t)).ToArray();
        var path=AssetDatabase.GenerateUniqueAssetPath("Assets/04HYUNWOOK/Cinematics/HW_Dome_BlueView_RedTrack_5s.playable");
        if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(old),path))throw new IOException("Timeline copy failed");
        var timeline=AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
        seq.director.playableAsset=timeline;
        var tracks=timeline.GetOutputTracks().ToArray();
        for(int i=0;i<tracks.Length;i++)
        {
            seq.director.SetGenericBinding(tracks[i],bindings[i]);
            var last=tracks[i].GetClips().OrderBy(c=>c.start).Last();
            last.displayName="04_Blue_Position_Red_Track";
            if(last.asset is CinemachineShot shot){shot.DisplayName=last.displayName;EditorUtility.SetDirty(shot);}
            EditorUtility.SetDirty(tracks[i]);
        }
        var dolly=seq.dollies[3];var aim=seq.dollyAim[3];var camera=dolly.GetComponent<CinemachineCamera>();
        dolly.name="CM_Dolly_04_Blue_Position_Red_Track";
        dolly.Spline.name="Spline_04_Blue_Position_Red_Track";
        aim.positionFollowTarget=seq.car;aim.positionFollowReference=seq.car.position;
        aim.positionOffsetWorld=new Vector3(0,3.2f,0);
        aim.redTarget=seq.red;aim.trackRed=true;aim.overhead=false;
        aim.targetOffsetWorld=new Vector3(0,1.2f,0);
        aim.rightPanDegrees=0;aim.rotationOffset=Vector3.zero;
        aim.pathStart=aim.pathEnd=0;aim.timelineDrivesDolly=true;aim.timelineDrivesAim=true;
        var p=dolly.Spline.transform.InverseTransformPoint(seq.car.position);
        dolly.Spline.Spline=new Spline(new[]{(float3)p,(float3)(p+Vector3.right*.01f)},TangentMode.Linear);
        dolly.CameraPosition=0;camera.Follow=seq.car;camera.LookAt=seq.red;camera.Lens.FieldOfView=60;
        seq.director.time=0;
        EditorUtility.SetDirty(dolly.Spline);EditorUtility.SetDirty(dolly);EditorUtility.SetDirty(aim);EditorUtility.SetDirty(camera);
        EditorUtility.SetDirty(seq.director);EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
        File.WriteAllText("Library/DomeBlueView.success.txt","Saved "+scene.path+"\nFinal shot follows blue car position + 3.2m height, aims at red car. Timing unchanged.\nTimeline: "+path+"\nBackup: "+backup+"\nNo Play Mode or tests.");
    }
}
