using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Splines;
using Unity.Mathematics;
using Unity.Cinemachine;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeSixShotInstaller
{
    const string Request="Library/ApplyDomeSixShot.request";
    static DomeSixShotInstaller(){EditorApplication.delayCall+=Check;}
    static void Check()
    {
        if(!File.Exists(Request))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        {EditorApplication.delayCall+=Check;return;}
        File.Delete(Request);
        try{Apply();}catch(Exception e){File.WriteAllText("Library/DomeSixShot.error.txt",e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Tools/HYUNWOOK/Apply Six Shot Car Over Sequence")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before applying.");
        var scene=SceneManager.GetSceneByPath("Assets/04HYUNWOOK/Scenes/HW_domeInTheMoon.unity");
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/04HYUNWOOK/Scenes/HW_domeInTheMoon.unity",OpenSceneMode.Additive);
        var seq=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DomeCinematicSequence>(true)).Single();
        seq.StopAndRestore();
        if(seq.sixShot)throw new InvalidOperationException("Six-shot version already applied.");
        string backup="Documentation/DomeCameraPreview/BeforeSixShots-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);
        if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
        string[] names={"01_Dome_Establishing","02_Road_Dome_Tilt","03_Dome_Top_Track","04_Red_Oblique_Track","05_Red_Roll_Track","06_Red_Car_Over"};
        double[] cuts={0,.6,1.4,2.2,3,3.7,5};
        var previous=seq.GetComponentsInChildren<CinemachineSplineDolly>(true);
        var selected=new[]{seq.dollies[0],seq.dollies[1],seq.dollies[2],
            previous.Single(d=>d.name.Contains("04_Red_Oblique_Track")),
            previous.Single(d=>d.name.Contains("06_Red_Roll_Track")),seq.dollies[3]};
        var cameras=new List<CinemachineSplineDolly>();var aims=new List<DomeDollyAim>();
        seq.fastSpeedMetresPerSecond=100;
        var timeline=ScriptableObject.CreateInstance<TimelineAsset>();
        var path=AssetDatabase.GenerateUniqueAssetPath("Assets/04HYUNWOOK/Cinematics/HW_Dome_6Shots_CarOver_5s.playable");
        AssetDatabase.CreateAsset(timeline,path);timeline.editorSettings.frameRate=30;
        var motion=timeline.CreateTrack<DomeCinematicTrack>(null,"Constant-speed vehicles + dolly motion");
        var cameraTrack=timeline.CreateTrack<CinemachineTrack>(null,"Six requested cameras");
        seq.director.playableAsset=timeline;
        for(int i=0;i<6;i++)
        {
            var dolly=selected[i];
            dolly.transform.SetParent(seq.transform,true);dolly.gameObject.SetActive(true);
            dolly.Spline.transform.SetParent(seq.transform,true);dolly.Spline.gameObject.SetActive(true);var vcam=dolly.GetComponent<CinemachineCamera>();var aim=dolly.GetComponent<DomeDollyAim>();
            dolly.name="CM_Dolly_"+names[i];dolly.Spline.name="Spline_"+names[i];
            int count=i<5?65:2;
            var points=new Vector3[count];var rotations=new Quaternion[count];float fov=58;
            for(int j=0;j<count;j++)
            {
                float time=(float)(cuts[i]+(cuts[i+1]-cuts[i])*j/(count-1));
                if(j==count-1)time-=.0001f;
                seq.GetSixShotPose(time,out points[j],out rotations[j],out fov);
            }
            // A minimal rail is retained for the two fixed cameras; their dolly position remains zero.
            if(i==5)points[1]=points[0]+Vector3.right*.01f;
            dolly.Spline.Spline=new Spline(points.Select(p=>(float3)p),TangentMode.AutoSmooth);
            dolly.PositionUnits=PathIndexUnit.Knot;dolly.CameraPosition=0;
            dolly.Damping=new CinemachineSplineDolly.DampingSettings{Enabled=false};
            dolly.CameraRotation=CinemachineSplineDolly.RotationMode.Default;
            dolly.AutomaticDolly=default;
            vcam.Lens.FieldOfView=fov;vcam.Follow=seq.red;vcam.LookAt=seq.red;
            vcam.transform.SetPositionAndRotation(points[0],rotations[0]);
            aim.rotations=rotations;aim.position=0;aim.trackRed=i==2||i==3||i==5;aim.overhead=i==2;
            aim.redTarget=seq.red;aim.targetOffsetWorld=i==5?new Vector3(0,.25f,0):Vector3.zero;
            cameras.Add(dolly);aims.Add(aim);
            var move=motion.CreateClip<DomeCinematicClip>();move.displayName=names[i];
            move.start=cuts[i];move.duration=cuts[i+1]-cuts[i];move.timeScale=1;
            ((DomeCinematicClip)move.asset).sequenceStart=(float)cuts[i];
            var clip=cameraTrack.CreateClip<CinemachineShot>();clip.displayName=names[i];clip.start=cuts[i];clip.duration=cuts[i+1]-cuts[i];
            var shot=(CinemachineShot)clip.asset;shot.DisplayName=names[i];shot.VirtualCamera.exposedName=new PropertyName(Guid.NewGuid().ToString("N"));
            seq.director.SetReferenceValue(shot.VirtualCamera.exposedName,vcam);
            EditorUtility.SetDirty(dolly.Spline);EditorUtility.SetDirty(dolly);EditorUtility.SetDirty(vcam);EditorUtility.SetDirty(aim);
        }
        var archive=new GameObject("Unused_Shots_Inactive");archive.transform.SetParent(seq.transform,false);
        foreach(var old in previous)
            if(!cameras.Contains(old))
            {
                old.transform.SetParent(archive.transform,true);
                old.Spline.transform.SetParent(archive.transform,true);
            }
        archive.SetActive(false);
        seq.dollies=cameras.ToArray();seq.dollyAim=aims.ToArray();seq.fastFourShot=false;seq.sixShot=true;seq.useSplineDolly=true;
        seq.gameObject.name="HW_Dome_6Shots_CarOver_5s";
        seq.director.SetGenericBinding(motion,seq);
        seq.director.SetGenericBinding(cameraTrack,seq.cam.GetComponent<CinemachineBrain>());
        seq.director.time=0;seq.director.playOnAwake=true;seq.director.extrapolationMode=DirectorWrapMode.Hold;
        seq.GetSixShotPose(0,out var position,out var rotation,out var initialFov);
        seq.cam.transform.SetPositionAndRotation(position,rotation);seq.cam.fieldOfView=initialFov;
        EditorUtility.SetDirty(seq);EditorUtility.SetDirty(seq.director);EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
        Selection.activeGameObject=seq.gameObject;
        File.WriteAllText("Library/DomeSixShot.success.txt","Saved "+scene.path+"\nTimeline: "+path+"\n6 shots / 5 seconds / tracking top / car-over finish / 100 m/s\nBackup: "+backup+"\nNo Play Mode or test rendering.");
    }
}
