using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeFixedTopFinalInstaller
{
    const string Request="Library/DomeFixedTopFinal.request";
    static DomeFixedTopFinalInstaller(){EditorApplication.delayCall+=Check;}
    static void Check()
    {
        if(!File.Exists(Request))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        {EditorApplication.delayCall+=Check;return;}
        File.Delete(Request);
        try{Apply();}catch(Exception e){File.WriteAllText("Library/DomeFixedTopFinal.error.txt",e.ToString());}
    }
    static void Apply()
    {
        var scene=SceneManager.GetSceneByPath("Assets/04HYUNWOOK/Scenes/HW_domeInTheMoon.unity");
        if(!scene.isLoaded)throw new InvalidOperationException("Open dome scene first.");
        var seq=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DomeCinematicSequence>(true)).Single();
        if(seq.dollies.Length!=4)throw new InvalidOperationException("Expected four cameras.");
        seq.StopAndRestore();
        var old=(TimelineAsset)seq.director.playableAsset;
        var oldTracks=old.GetOutputTracks().ToArray();
        if(oldTracks.Any(t=>t.GetClips().Count()!=4))throw new InvalidOperationException("Expected four clips per track.");
        var backup="Documentation/DomeCameraPreview/BeforeFixedTopFinal-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);
        if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
        var path=AssetDatabase.GenerateUniqueAssetPath("Assets/04HYUNWOOK/Cinematics/HW_Dome_FixedTop_Final2s_525kph.playable");
        if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(old),path))throw new IOException("Timeline copy failed");
        var timeline=AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
        var bindings=oldTracks.Select(t=>seq.director.GetGenericBinding(t)).ToArray();
        var tracks=timeline.GetOutputTracks().ToArray();
        seq.director.playableAsset=timeline;
        double[] cuts={0,1,2,3,5};
        for(int i=0;i<tracks.Length;i++)
        {
            seq.director.SetGenericBinding(tracks[i],bindings[i]);
            var clips=tracks[i].GetClips().OrderBy(c=>c.start).ToArray();
            for(int j=0;j<4;j++)
            {
                clips[j].start=cuts[j];clips[j].duration=cuts[j+1]-cuts[j];
                if(clips[j].asset is DomeCinematicClip motion){motion.sequenceStart=(float)cuts[j];EditorUtility.SetDirty(motion);}
            }
            EditorUtility.SetDirty(tracks[i]);
        }
        var top=seq.dollyAim[2];
        top.trackRed=false;top.useEulerAngles=true;
        top.startEuler=top.endEuler=Quaternion.LookRotation(Vector3.down,Vector3.forward).eulerAngles;
        top.rotationOffset=Vector3.zero;top.rightPanDegrees=0;top.holdOrientation=false;
        seq.lastShotSpeedKmh=525;seq.accelerateLastShot=true;
        seq.stopPlayAtEnd=true;seq.continueVehiclesAfterCamera=false;
        seq.director.time=0;
        EditorUtility.SetDirty(top);EditorUtility.SetDirty(seq);EditorUtility.SetDirty(seq.director);EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
        File.WriteAllText("Library/DomeFixedTopFinal.success.txt","Saved "+scene.path+"\nCuts: 0-1, 1-2, 2-3, 3-5. Top aim fixed down; movement checkbox preserved: "+top.topViewMovement+". Final speed 525 km/h. Auto stop at 5s.\nTimeline: "+path+"\nBackup: "+backup+"\nNo Play Mode or tests.");
    }
}
