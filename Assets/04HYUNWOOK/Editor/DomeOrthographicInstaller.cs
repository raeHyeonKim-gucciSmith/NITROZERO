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
public static class DomeOrthographicInstaller
{
    const string Request="Library/DomeOrthographic.request";
    static DomeOrthographicInstaller(){EditorApplication.delayCall+=Check;}
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
            DomeCameraPreviewSession.Stop();seq.StopAndRestore();
            var backup="Documentation/DomeCameraPreview/BeforeOrthographic-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
            var brain=seq.cam.GetComponent<CinemachineBrain>();
            if(!brain)throw new InvalidOperationException("Missing output camera brain");
            var clips=((TimelineAsset)seq.director.playableAsset).GetOutputTracks().OfType<DomeCinematicTrack>().Single().GetClips().OrderBy(c=>c.start).ToArray();
            string report="";
            for(int i=0;i<seq.dollyAim.Length;i++)
            {
                var aim=seq.dollyAim[i];
                if(!aim || !(aim.name.StartsWith("CM_Dolly_03_")||aim.name.StartsWith("CM_Dolly_04_")))continue;
                var cam=aim.GetComponent<CinemachineCamera>();
                Undo.RecordObject(cam,"3번·4번 원근 투영 끄기");
                if(cam.Lens.ModeOverride!=LensSettings.OverrideModes.Orthographic)
                {
                    var target=aim.redTarget?aim.redTarget.position:seq.GetRedPositionAtTime((float)clips[i].start);
                    if(aim.redTarget==seq.red)target=seq.GetRedPositionAtTime((float)clips[i].start);
                    target+=aim.targetOffsetWorld;
                    float distance=Vector3.Distance(cam.transform.position,target);
                    cam.Lens.OrthographicSize=Mathf.Max(1,distance*Mathf.Tan(cam.Lens.FieldOfView*Mathf.Deg2Rad*.5f));
                }
                cam.Lens.ModeOverride=LensSettings.OverrideModes.Orthographic;
                EditorUtility.SetDirty(cam);
                report+=cam.name+": Orthographic, Size="+cam.Lens.OrthographicSize+"\n";
            }
            Undo.RecordObject(brain,"카메라별 투영 방식 적용");
            brain.LensModeOverride.Enabled=true;
            brain.LensModeOverride.DefaultMode=LensSettings.OverrideModes.Perspective;
            EditorUtility.SetDirty(brain);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeOrthographic.success.txt",report+"Saved. Camera 1 remains perspective. Size matched at each cut's initial target distance. No Play Mode or tests.\nBackup: "+backup);
        }
        catch(Exception e){File.WriteAllText("Library/DomeOrthographic.error.txt",e.ToString());}
    }
}
