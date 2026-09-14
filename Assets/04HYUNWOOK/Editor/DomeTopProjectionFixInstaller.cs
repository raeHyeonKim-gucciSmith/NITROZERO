using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeTopProjectionFixInstaller
{
    const string Request="Library/DomeTopProjectionFix.request";
    static DomeTopProjectionFixInstaller(){EditorApplication.delayCall+=Check;}
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
            var aim=seq.dollyAim.Single(a=>a && a.name.StartsWith("CM_Dolly_03_"));
            DomeCameraPreviewSession.Stop();seq.StopAndRestore();
            var cam=aim.GetComponent<CinemachineCamera>();var brain=seq.cam.GetComponent<CinemachineBrain>();
            var before=cam.Lens.ModeOverride;
            Undo.RecordObjects(new UnityEngine.Object[]{aim,cam,brain},"3번 직교 투영 유지");
            aim.keepOverheadOrthographic=true;
            cam.Lens.ModeOverride=LensSettings.OverrideModes.Orthographic;
            brain.LensModeOverride.Enabled=true;
            EditorUtility.SetDirty(aim);EditorUtility.SetDirty(cam);EditorUtility.SetDirty(brain);
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeTopProjectionFix.success.txt","Camera 3 projection: "+before+" -> "+cam.Lens.ModeOverride+". Keep orthographic enabled. Size preserved: "+cam.Lens.OrthographicSize+". Scene saved. No Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeTopProjectionFix.error.txt",e.ToString());}
    }
}
