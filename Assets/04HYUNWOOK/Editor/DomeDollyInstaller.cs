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
using UnityEngine.Rendering.Universal;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeDollyInstaller
{
    const string Request="Library/InstallDomeDolly.request";
    static DomeDollyInstaller() { EditorApplication.delayCall+=Check; }
    static void Check()
    {
        if(!File.Exists(Request))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        {EditorApplication.delayCall+=Check;return;}
        File.Delete(Request);
        try {Install();}
        catch(Exception e){File.WriteAllText("Library/DomeDollyInstall.error.txt",e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Tools/HYUNWOOK/Apply 4.8s Dolly Cinematic")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before authoring.");
        var scene=SceneManager.GetSceneByPath("Assets/04HYUNWOOK/Scenes/HW_domeInTheMoon.unity");
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/04HYUNWOOK/Scenes/HW_domeInTheMoon.unity",OpenSceneMode.Additive);
        var sequence=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DomeCinematicSequence>(true)).Single();
        sequence.StopAndRestore();
        string backup="Documentation/DomeCameraPreview/BeforeDolly-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);
        if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Scene backup failed");
        if(sequence.useSplineDolly)throw new InvalidOperationException("Dolly sequence already installed; edit its splines and Timeline directly.");
        string[] names={"01_Dome_Establishing","02_Road_Dome_Tilt","03_Dome_Top_Track","04_Red_Oblique_Track","05_Red_Low_Hero","06_Red_Roll_Track","07_Red_Ground_Pass"};
        double[] old={0,2.4,6.2,8,10.4,12.5,16,20};
        double[] cuts={0,.5,1.4,1.95,2.6,3.2,3.85,4.8};
        const int samples=65;
        var positions=new Vector3[7][];var rotations=new Quaternion[7][];var fovs=new float[7];
        // Author spline knots from the approved mathematical poses. No rendering or playback.
        try
        {
            for(int i=0;i<7;i++)
            {
                positions[i]=new Vector3[samples];rotations[i]=new Quaternion[samples];
                for(int j=0;j<samples;j++)
                {
                    double time=old[i]+(old[i+1]-old[i])*j/(samples-1);
                    if(j==samples-1)time-=.0001;
                    sequence.Evaluate((float)time);
                    positions[i][j]=sequence.cam.transform.position;
                    rotations[i][j]=sequence.cam.transform.rotation;
                    fovs[i]=sequence.cam.fieldOfView;
                }
            }
        }
        finally {sequence.Restore();}
        var root=sequence.gameObject;root.name="HW_Dome_Dolly_4_8s";
        var archive=new GameObject("Previous_20s_Cameras_Inactive");archive.transform.SetParent(root.transform,false);
        foreach(var c in sequence.shotCameras){c.enabled=false;c.transform.SetParent(archive.transform,true);}
        archive.SetActive(false);
        var output=new GameObject("Dolly_Output_Camera");output.transform.SetParent(root.transform,false);
        output.tag="MainCamera";
        var camera=output.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.025f;camera.farClipPlane=15000;camera.depth=100;
        output.AddComponent<AudioListener>().enabled=false;
        var brain=output.AddComponent<CinemachineBrain>();
        brain.UpdateMethod=CinemachineBrain.UpdateMethods.LateUpdate;
        brain.DefaultBlend=new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut,0);
        var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        var timeline=ScriptableObject.CreateInstance<TimelineAsset>();
        string path=AssetDatabase.GenerateUniqueAssetPath("Assets/04HYUNWOOK/Cinematics/HW_Dome_Dolly_4_8s.playable");
        AssetDatabase.CreateAsset(timeline,path);timeline.editorSettings.frameRate=30;
        var motion=timeline.CreateTrack<DomeCinematicTrack>(null,"Dolly position + red vehicle motion");
        var cameraTrack=timeline.CreateTrack<CinemachineTrack>(null,"Cinemachine Dolly Cameras");
        var dollies=new List<CinemachineSplineDolly>();var aims=new List<DomeDollyAim>();
        sequence.director.playableAsset=timeline;
        for(int i=0;i<7;i++)
        {
            var pathObject=new GameObject("Spline_"+names[i]);pathObject.transform.SetParent(root.transform,false);
            var container=pathObject.AddComponent<SplineContainer>();
            container.Spline=new Spline(positions[i].Select(p=>(float3)p),TangentMode.AutoSmooth);
            var cameraObject=new GameObject("CM_Dolly_"+names[i]);cameraObject.transform.SetParent(root.transform,false);
            var vcam=cameraObject.AddComponent<CinemachineCamera>();
            vcam.Follow=sequence.red;vcam.LookAt=sequence.red;
            vcam.Lens.FieldOfView=fovs[i];vcam.Lens.NearClipPlane=.025f;vcam.Lens.FarClipPlane=15000;
            cameraObject.transform.SetPositionAndRotation(positions[i][0],rotations[i][0]);
            var dolly=cameraObject.AddComponent<CinemachineSplineDolly>();
            dolly.Spline=container;dolly.PositionUnits=PathIndexUnit.Knot;dolly.CameraPosition=0;
            dolly.CameraRotation=CinemachineSplineDolly.RotationMode.Default;
            dolly.Damping=new CinemachineSplineDolly.DampingSettings{Enabled=false};
            var aim=cameraObject.AddComponent<DomeDollyAim>();aim.rotations=rotations[i];aim.redTarget=sequence.red;
            aim.trackRed=i>=2&&i<=4;aim.overhead=i==2;
            dollies.Add(dolly);aims.Add(aim);
            var moveClip=motion.CreateClip<DomeCinematicClip>();moveClip.displayName=names[i]+" / Dolly";
            moveClip.start=cuts[i];moveClip.duration=cuts[i+1]-cuts[i];moveClip.timeScale=(old[i+1]-old[i])/moveClip.duration;
            ((DomeCinematicClip)moveClip.asset).sequenceStart=(float)old[i];
            var shot=cameraTrack.CreateClip<CinemachineShot>();shot.start=cuts[i];shot.duration=cuts[i+1]-cuts[i];shot.displayName=names[i];
            var shotAsset=(CinemachineShot)shot.asset;shotAsset.DisplayName=names[i];
            shotAsset.VirtualCamera.exposedName=new PropertyName(Guid.NewGuid().ToString("N"));
            sequence.director.SetReferenceValue(shotAsset.VirtualCamera.exposedName,vcam);
        }
        sequence.dollies=dollies.ToArray();sequence.dollyAim=aims.ToArray();sequence.cam=camera;sequence.shotCameras=new[]{camera};sequence.useSplineDolly=true;
        sequence.director.SetGenericBinding(motion,sequence);sequence.director.SetGenericBinding(cameraTrack,brain);
        sequence.director.playOnAwake=true;sequence.director.extrapolationMode=DirectorWrapMode.Hold;
        sequence.director.time=0;
        camera.transform.SetPositionAndRotation(positions[0][0],rotations[0][0]);camera.fieldOfView=fovs[0];
        EditorUtility.SetDirty(sequence);EditorUtility.SetDirty(sequence.director);EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");
        Selection.activeGameObject=root;
        File.WriteAllText("Library/DomeDollyInstall.success.txt","Saved "+scene.path+"\nTimeline: "+path+"\n7 CinemachineSplineDolly cameras / 7 editable SplineContainers / 4.8 seconds\nBackup: "+backup+"\nNo Play Mode or render test performed.");
    }
}
