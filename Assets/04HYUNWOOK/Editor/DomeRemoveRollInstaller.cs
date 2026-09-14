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
public static class DomeRemoveRollInstaller
{
    const string Request="Library/RemoveDomeRoll.request";
    static DomeRemoveRollInstaller(){EditorApplication.delayCall+=Check;}
    static void Check()
    {
        if(!File.Exists(Request))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        {EditorApplication.delayCall+=Check;return;}
        File.Delete(Request);
        try{Apply();}catch(Exception e){File.WriteAllText("Library/DomeRemoveRoll.error.txt",e.ToString());Debug.LogException(e);}
    }
    static void Apply()
    {
        var scene=SceneManager.GetSceneByPath("Assets/04HYUNWOOK/Scenes/HW_domeInTheMoon.unity");
        if(!scene.isLoaded)throw new InvalidOperationException("Open HW_domeInTheMoon first.");
        var seq=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DomeCinematicSequence>(true)).Single();
        if(!seq.sixShot||seq.omitRollShot||seq.dollies.Length!=6)throw new InvalidOperationException("Expected six-shot sequence.");
        seq.StopAndRestore();
        string backup="Documentation/DomeCameraPreview/BeforeRemoveRoll-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);
        if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
        var oldPath=AssetDatabase.GetAssetPath(seq.director.playableAsset);
        var path=AssetDatabase.GenerateUniqueAssetPath("Assets/04HYUNWOOK/Cinematics/HW_Dome_5Shots_NoRoll_5s.playable");
        if(!AssetDatabase.CopyAsset(oldPath,path))throw new IOException("Timeline copy failed");
        var timeline=AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
        var oldTimeline=(TimelineAsset)seq.director.playableAsset;
        var oldTracks=oldTimeline.GetOutputTracks().ToArray();
        var tracks=timeline.GetOutputTracks().ToArray();
        var bindings=oldTracks.Select(t=>seq.director.GetGenericBinding(t)).ToArray();
        seq.director.playableAsset=timeline;
        for(int i=0;i<tracks.Length;i++)
        {
            seq.director.SetGenericBinding(tracks[i],bindings[i]);
            foreach(var clip in tracks[i].GetClips().ToArray())
            {
                if(clip.displayName.Contains("Roll_Track"))
                {
                    if(clip.asset is CinemachineShot shot)seq.director.ClearReferenceValue(shot.VirtualCamera.exposedName);
                    timeline.DeleteClip(clip);
                }
                else if(clip.displayName.Contains("Oblique_Track"))clip.duration=1.5;
            }
            if(tracks[i] is CinemachineTrack)tracks[i].name="Five cameras - no roll";
            EditorUtility.SetDirty(tracks[i]);
        }
        var roll=seq.dollies[4];var rollSpline=roll.Spline;
        seq.dollies=seq.dollies.Where((d,i)=>i!=4).ToArray();
        seq.dollyAim=seq.dollyAim.Where((a,i)=>i!=4).ToArray();
        seq.omitRollShot=true;
        var dolly=seq.dollies[3];var aim=seq.dollyAim[3];
        var points=new Vector3[65];var rotations=new Quaternion[65];
        for(int j=0;j<65;j++)seq.GetSixShotPose(Mathf.Lerp(2.2f,3.7f-.0001f,j/64f),out points[j],out rotations[j],out _);
        dolly.Spline.Spline=new Spline(points.Select(p=>(float3)p),TangentMode.AutoSmooth);
        dolly.CameraPosition=0;aim.rotations=rotations;aim.position=0;
        UnityEngine.Object.DestroyImmediate(roll.gameObject);
        UnityEngine.Object.DestroyImmediate(rollSpline.gameObject);
        seq.gameObject.name="HW_Dome_5Shots_NoRoll_5s";
        seq.director.time=0;
        EditorUtility.SetDirty(dolly.Spline);EditorUtility.SetDirty(dolly);EditorUtility.SetDirty(aim);
        EditorUtility.SetDirty(seq);EditorUtility.SetDirty(seq.director);EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
        File.WriteAllText("Library/DomeRemoveRoll.success.txt","Saved "+scene.path+"\nTimeline: "+path+"\n5 shots / 5 seconds / oblique 2.2-3.7 / car-over 3.7-5\nBackup: "+backup+"\nNo Play Mode or tests.");
    }
}
