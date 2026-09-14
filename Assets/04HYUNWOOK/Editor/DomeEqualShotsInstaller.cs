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
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeEqualShotsInstaller
{
    const string Request="Library/EqualDomeShots.request";
    static DomeEqualShotsInstaller(){EditorApplication.delayCall+=Check;}
    static void Check()
    {
        if(!File.Exists(Request))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        {EditorApplication.delayCall+=Check;return;}
        File.Delete(Request);
        try{Apply();}catch(Exception e){File.WriteAllText("Library/DomeEqualShots.error.txt",e.ToString());Debug.LogException(e);}
    }
    static void Apply()
    {
        var scene=SceneManager.GetSceneByPath("Assets/04HYUNWOOK/Scenes/HW_domeInTheMoon.unity");
        if(!scene.isLoaded)throw new InvalidOperationException("Open HW_domeInTheMoon first.");
        var seq=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DomeCinematicSequence>(true)).Single();
        if(!seq.sixShot||!seq.omitRollShot||seq.dollies.Length!=5)throw new InvalidOperationException("Expected five-shot sequence.");
        seq.StopAndRestore();
        var oldTimeline=(TimelineAsset)seq.director.playableAsset;
        var oldTracks=oldTimeline.GetOutputTracks().ToArray();
        if(oldTracks.Any(t=>t.GetClips().Count()!=5))throw new InvalidOperationException("Expected five clips per track.");
        string backup="Documentation/DomeCameraPreview/BeforeEqualShots-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);
        if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
        var path=AssetDatabase.GenerateUniqueAssetPath("Assets/04HYUNWOOK/Cinematics/HW_Dome_5Shots_Equal_5s.playable");
        if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(oldTimeline),path))throw new IOException("Timeline copy failed");
        var timeline=AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
        var tracks=timeline.GetOutputTracks().ToArray();
        var bindings=oldTracks.Select(t=>seq.director.GetGenericBinding(t)).ToArray();
        seq.director.playableAsset=timeline;
        for(int i=0;i<tracks.Length;i++)
        {
            seq.director.SetGenericBinding(tracks[i],bindings[i]);
            var clips=tracks[i].GetClips().OrderBy(c=>c.start).ToArray();
            for(int j=0;j<clips.Length;j++)
            {
                clips[j].start=j;clips[j].duration=1;clips[j].clipIn=0;clips[j].timeScale=1;
                if(clips[j].asset is DomeCinematicClip motion)
                {motion.sequenceStart=j;EditorUtility.SetDirty(motion);}
            }
            EditorUtility.SetDirty(tracks[i]);
        }
        seq.equalShotDurations=true;
        for(int i=0;i<5;i++)
        {
            var dolly=seq.dollies[i];var aim=seq.dollyAim[i];
            int count=i==4?2:65;
            var points=new Vector3[count];var rotations=new Quaternion[count];
            for(int j=0;j<count;j++)seq.GetSixShotPose(i+Mathf.Min(j/(float)(count-1),.9999f),out points[j],out rotations[j],out _);
            if(i==4)points[1]=points[0]+Vector3.right*.01f;
            dolly.Spline.Spline=new Spline(points.Select(p=>(float3)p),TangentMode.AutoSmooth);
            dolly.CameraPosition=0;aim.rotations=rotations;aim.position=0;
            dolly.transform.SetPositionAndRotation(points[0],rotations[0]);
            EditorUtility.SetDirty(dolly.Spline);EditorUtility.SetDirty(dolly);EditorUtility.SetDirty(aim);
        }
        seq.director.time=0;
        EditorUtility.SetDirty(seq);EditorUtility.SetDirty(seq.director);EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
        File.WriteAllText("Library/DomeEqualShots.success.txt","Saved "+scene.path+"\nTimeline: "+path+"\nFive 1-second shots, total 5 seconds. Car passes at 4.5 seconds.\nBackup: "+backup+"\nNo Play Mode or tests.");
    }
}
