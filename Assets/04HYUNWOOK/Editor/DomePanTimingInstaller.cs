using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using Unity.Mathematics;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomePanTimingInstaller
{
    const string Request="Library/DomePanTiming.request";
    static DomePanTimingInstaller(){EditorApplication.delayCall+=Check;}
    static void Check()
    {
        if(!File.Exists(Request))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        {EditorApplication.delayCall+=Check;return;}
        File.Delete(Request);
        try{Apply();}catch(Exception e){File.WriteAllText("Library/DomePanTiming.error.txt",e.ToString());Debug.LogException(e);}
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
        var oldClips=oldTracks.OfType<DomeCinematicTrack>().Single().GetClips().OrderBy(c=>c.start).ToArray();
        string backup="Documentation/DomeCameraPreview/BeforePanTiming-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);
        if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
        var path=AssetDatabase.GenerateUniqueAssetPath("Assets/04HYUNWOOK/Cinematics/HW_Dome_Pan1_25s_Total5s.playable");
        if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(old),path))throw new IOException("Timeline backup failed");
        var timeline=AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
        var tracks=timeline.GetOutputTracks().ToArray();
        var bindings=oldTracks.Select(t=>seq.director.GetGenericBinding(t)).ToArray();
        double[] cuts={0,1.25,2.5,3.75,5};
        seq.director.playableAsset=timeline;
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
        // Preserve the user's spline shape; shift tracking positions only by the
        // distance the constant-speed vehicle travels due to the changed timing.
        for(int i=2;i<4;i++)
        {
            var container=seq.dollies[i].Spline;var spline=container.Spline;
            for(int j=0;j<spline.Count;j++)
            {
                double u=i==3?.5:j/(double)(spline.Count-1);
                double oldTime=oldClips[i].start+oldClips[i].duration*u;
                double newTime=cuts[i]+(cuts[i+1]-cuts[i])*u;
                var delta=container.transform.InverseTransformVector(Vector3.right*(float)(newTime-oldTime)*seq.fastSpeedMetresPerSecond);
                var knot=spline[j];knot.Position+=(float3)delta;spline[j]=knot;
            }
            EditorUtility.SetDirty(container);
        }
        var aim=seq.dollyAim[0];
        // Fix the baseline, so the total added yaw is exactly the Inspector value.
        if(!aim.useEulerAngles && aim.rotations!=null && aim.rotations.Length>0)aim.startEuler=aim.rotations[0].eulerAngles;
        aim.endEuler=aim.startEuler;aim.useEulerAngles=true;aim.trackRed=false;
        aim.timelineDrivesAim=true;aim.position=0;
        aim.panCurve=AnimationCurve.Linear(0,0,1,1);
        EditorUtility.SetDirty(aim);EditorUtility.SetDirty(seq.director);EditorUtility.SetDirty(timeline);
        seq.director.time=0;
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
        File.WriteAllText("Library/DomePanTiming.success.txt","Saved "+scene.path+"\nPan: 0 to "+aim.rightPanDegrees+" degrees in 1.25 seconds; "+(aim.rightPanDegrees/1.25f)+" degrees/sec. Total 5 seconds.\nBackup: "+backup+"\nNo Play Mode or tests.");
    }
}

