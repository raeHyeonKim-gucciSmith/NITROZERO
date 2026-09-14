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
public static class DomeFourEqualInstaller
{
    const string Request="Library/FourEqualDome.request";
    static DomeFourEqualInstaller(){EditorApplication.delayCall+=Check;}
    static void Check()
    {
        if(!File.Exists(Request))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        {EditorApplication.delayCall+=Check;return;}
        File.Delete(Request);
        try{Apply();}catch(Exception e){File.WriteAllText("Library/DomeFourEqual.error.txt",e.ToString());Debug.LogException(e);}
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
        string backup="Documentation/DomeCameraPreview/BeforeFourEqual-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);
        if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
        var path=AssetDatabase.GenerateUniqueAssetPath("Assets/04HYUNWOOK/Cinematics/HW_Dome_4Shots_300kph_5s.playable");
        if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(oldTimeline),path))throw new IOException("Timeline copy failed");
        var timeline=AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
        var tracks=timeline.GetOutputTracks().ToArray();
        var bindings=oldTracks.Select(t=>seq.director.GetGenericBinding(t)).ToArray();
        seq.director.playableAsset=timeline;
        for(int i=0;i<tracks.Length;i++)
        {
            seq.director.SetGenericBinding(tracks[i],bindings[i]);
            foreach(var removed in tracks[i].GetClips().Where(c=>c.displayName.Contains("Oblique_Track")).ToArray())
            {
                if(removed.asset is Unity.Cinemachine.CinemachineShot shot)seq.director.ClearReferenceValue(shot.VirtualCamera.exposedName);
                timeline.DeleteClip(removed);
            }
            if(tracks[i] is Unity.Cinemachine.CinemachineTrack)tracks[i].name="Four equal cameras";
            var clips=tracks[i].GetClips().OrderBy(c=>c.start).ToArray();
            for(int j=0;j<clips.Length;j++)
            {
                clips[j].start=j*1.25;clips[j].duration=1.25;clips[j].clipIn=0;clips[j].timeScale=1;
                if(clips[j].asset is DomeCinematicClip motion)
                {motion.sequenceStart=j*1.25f;EditorUtility.SetDirty(motion);}
            }
            EditorUtility.SetDirty(tracks[i]);
        }
        var removedCamera=seq.dollies[3];var removedSpline=removedCamera.Spline;
        seq.dollies=seq.dollies.Where((d,i)=>i!=3).ToArray();
        seq.dollyAim=seq.dollyAim.Where((a,i)=>i!=3).ToArray();
        UnityEngine.Object.DestroyImmediate(removedCamera.gameObject);
        UnityEngine.Object.DestroyImmediate(removedSpline.gameObject);
        seq.equalShotDurations=true;seq.omitObliqueShot=true;seq.fastSpeedMetresPerSecond=300f/3.6f;seq.continueVehiclesAfterCamera=true;
        seq.gameObject.name="HW_Dome_4Shots_300kph_5s";
        for(int i=0;i<4;i++)
        {
            var dolly=seq.dollies[i];var aim=seq.dollyAim[i];
            int count=i==3?2:65;
            var points=new Vector3[count];var rotations=new Quaternion[count];
            for(int j=0;j<count;j++)seq.GetSixShotPose((i+Mathf.Min(j/(float)(count-1),.9999f))*1.25f,out points[j],out rotations[j],out _);
            if(i==3)points[1]=points[0]+Vector3.right*.01f;
            dolly.Spline.Spline=new Spline(points.Select(p=>(float3)p),TangentMode.AutoSmooth);
            dolly.CameraPosition=0;aim.rotations=rotations;aim.position=0;
            dolly.transform.SetPositionAndRotation(points[0],rotations[0]);
            EditorUtility.SetDirty(dolly.Spline);EditorUtility.SetDirty(dolly);EditorUtility.SetDirty(aim);
        }
        seq.director.time=0;
        EditorUtility.SetDirty(seq);EditorUtility.SetDirty(seq.director);EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
        File.WriteAllText("Library/DomeFourEqual.success.txt","Saved "+scene.path+"\nTimeline: "+path+"\nFour 1.25-second shots. Speed 300 km/h (83.333 m/s). Vehicles continue after 5 seconds. Car passes at 4.375 seconds.\nBackup: "+backup+"\nNo Play Mode or tests.");
    }
}


