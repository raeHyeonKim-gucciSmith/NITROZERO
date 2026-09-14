using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using Unity.Cinemachine;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeThreeShotsInstaller
{
    const string Request="Library/DomeThreeShots.request";
    static DomeThreeShotsInstaller(){EditorApplication.delayCall+=Check;}
    static void Check()
    {
        if(!File.Exists(Request))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        {EditorApplication.delayCall+=Check;return;}
        File.Delete(Request);
        try{Apply();}catch(Exception e){File.WriteAllText("Library/DomeThreeShots.error.txt",e.ToString());}
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
        var backup="Documentation/DomeCameraPreview/BeforeThreeShots-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);
        if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
        var path=AssetDatabase.GenerateUniqueAssetPath("Assets/04HYUNWOOK/Cinematics/HW_Dome_Shots_1_3_4_5s.playable");
        if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(old),path))throw new IOException("Timeline copy failed");
        var timeline=AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
        var bindings=oldTracks.Select(t=>seq.director.GetGenericBinding(t)).ToArray();
        seq.director.playableAsset=timeline;
        var tracks=timeline.GetOutputTracks().ToArray();double[] cuts={0,1.5,3,5};
        for(int i=0;i<tracks.Length;i++)
        {
            seq.director.SetGenericBinding(tracks[i],bindings[i]);
            var removed=tracks[i].GetClips().OrderBy(c=>c.start).ElementAt(1);
            if(removed.asset is CinemachineShot shot)seq.director.ClearReferenceValue(shot.VirtualCamera.exposedName);
            timeline.DeleteClip(removed);
            var clips=tracks[i].GetClips().OrderBy(c=>c.start).ToArray();
            for(int j=0;j<3;j++)
            {
                clips[j].start=cuts[j];clips[j].duration=cuts[j+1]-cuts[j];
                if(clips[j].asset is DomeCinematicClip motion){motion.sequenceStart=(float)cuts[j];EditorUtility.SetDirty(motion);}
            }
            if(tracks[i] is CinemachineTrack)tracks[i].name="Cameras 1, 3, 4";
            EditorUtility.SetDirty(tracks[i]);
        }
        var unused=seq.dollies[1];
        unused.gameObject.SetActive(false);unused.Spline.gameObject.SetActive(false);
        seq.dollies=seq.dollies.Where((d,i)=>i!=1).ToArray();
        seq.dollyAim=seq.dollyAim.Where((a,i)=>i!=1).ToArray();
        seq.gameObject.name="HW_Dome_Shots_1_3_4_5s";
        seq.director.time=0;
        EditorUtility.SetDirty(seq);EditorUtility.SetDirty(seq.director);EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
        File.WriteAllText("Library/DomeThreeShots.success.txt","Saved "+scene.path+"\nCamera 1: 0-1.5; Camera 3: 1.5-3; Camera 4: 3-5. Camera 2 inactive.\nTimeline: "+path+"\nBackup: "+backup+"\nNo Play Mode or tests.");
    }
}
