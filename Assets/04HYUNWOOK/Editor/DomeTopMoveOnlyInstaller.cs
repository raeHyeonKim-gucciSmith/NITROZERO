using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeTopMoveOnlyInstaller
{
    const string Request="Library/DomeTopMoveOnly.request";
    static DomeTopMoveOnlyInstaller(){EditorApplication.delayCall+=Check;}
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
            var dolly=aim.GetComponent<CinemachineSplineDolly>();
            if(!dolly || !dolly.Spline)throw new InvalidOperationException("Missing third-camera spline.");
            DomeCameraPreviewSession.Stop();seq.StopAndRestore();
            Undo.RecordObjects(new UnityEngine.Object[]{aim,dolly},"3번 카메라 이동만 사용");
            aim.trackRed=false;
            aim.topViewMovement=true;
            aim.SyncTopViewMovement();
            EditorUtility.SetDirty(aim);EditorUtility.SetDirty(dolly);
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeTopMoveOnly.success.txt","Camera: "+aim.name+"\nLook at vehicle: "+aim.trackRed+"\nMovement: "+aim.topViewMovement+"; Dolly enabled: "+dolly.enabled+"; Timeline drives dolly: "+aim.timelineDrivesDolly+"\nPath range: "+aim.pathStart+" -> "+aim.pathEnd+"\nSaved. No Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeTopMoveOnly.error.txt",e.ToString());}
    }
}
