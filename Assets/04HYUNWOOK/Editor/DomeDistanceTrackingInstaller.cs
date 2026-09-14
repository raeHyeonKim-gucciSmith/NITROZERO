using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeDistanceTrackingInstaller
{
    const string Request="Library/DomeDistanceTracking.request";
    static DomeDistanceTrackingInstaller(){EditorApplication.delayCall+=Check;}
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
            var aim=seq.dollyAim.Single(a=>a && a.name.StartsWith("CM_Dolly_04_"));
            DomeCameraPreviewSession.Stop();seq.StopAndRestore();
            var backup="Documentation/DomeCameraPreview/BeforeDistanceTracking-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
            Undo.RecordObjects(new UnityEngine.Object[]{seq,aim},"4번 거리 기반 카메라 회전");
            seq.fastSpeedMetresPerSecond=400f/3.6f;
            seq.accelerateLastShot=false;
            seq.overrideLastShotStartSpeed=false;
            aim.distanceBasedTracking=true;aim.trackingSequence=seq;
            aim.initialTrackingWeight=.12f;aim.trackingCatchupFraction=.85f;
            aim.trackRed=true;aim.redTarget=seq.red;
            aim.useTransformPosition=true;aim.useTransformRotation=false;
            aim.timelineDrivesAim=true;
            var camera=aim.transform.position;
            var start=seq.GetRedPositionAtTime(seq.LastShotStartTime);
            var offset=start+aim.targetOffsetWorld-camera;
            float pass=-offset.x/seq.fastSpeedMetresPerSecond;
            float lateral=new Vector2(offset.y,offset.z).magnitude;
            var report="Camera world position: "+camera.ToString("F3")+"\nRed at cut start: "+start.ToString("F3")
                +"\nConstant vehicle speed: 400 km/h ("+seq.fastSpeedMetresPerSecond.ToString("F3")+" m/s)"
                +"\nCut start: "+seq.LastShotStartTime+" s; duration: "+(seq.PlaybackDuration-seq.LastShotStartTime)+" s"
                +"\nInitial camera-to-aim-point distance: "+offset.magnitude.ToString("F3")+" m"
                +"\nClosest approach to aim-point path: "+lateral.ToString("F3")+" m"
                +"\nClosest approach after cut start: "+pass.ToString("F3")+" s"
                +"\nCatchup complete after cut start: "+(pass*.85f).ToString("F3")+" s"
                +"\nGeometric peak tracking angular speed: "+(seq.fastSpeedMetresPerSecond/Mathf.Max(.001f,lateral)*Mathf.Rad2Deg).ToString("F1")+" deg/s"
                +"\nCalculated from scene coordinates and the scripted path; no playback measurement or tests.";
            EditorUtility.SetDirty(seq);EditorUtility.SetDirty(aim);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText(backup+"/Geometry.txt",report);
            File.WriteAllText("Library/DomeDistanceTracking.success.txt",report+"\nSaved: "+scene.path+"\nBackup: "+backup);
        }
        catch(Exception e){File.WriteAllText("Library/DomeDistanceTracking.error.txt",e.ToString());}
    }
}
