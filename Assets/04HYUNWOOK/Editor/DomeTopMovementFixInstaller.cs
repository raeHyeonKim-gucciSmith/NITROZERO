using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeTopMovementFixInstaller
{
    const string Request="Library/DomeTopMovementFix.request";
    static DomeTopMovementFixInstaller(){EditorApplication.delayCall+=Check;}
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
            var aim=seq.dollyAim.Single(a=>a && a.overhead);
            aim.SyncTopViewMovement();var dolly=aim.GetComponent<Unity.Cinemachine.CinemachineSplineDolly>();
            EditorUtility.SetDirty(aim);EditorUtility.SetDirty(dolly);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeTopMovementFix.success.txt","Saved "+scene.path+"\nMovement="+aim.topViewMovement+", DirectPosition="+aim.useTransformPosition+", TimelineDrivesDolly="+aim.timelineDrivesDolly+", DollyEnabled="+dolly.enabled+"\nNo Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeTopMovementFix.error.txt",e.ToString());}
    }
}
