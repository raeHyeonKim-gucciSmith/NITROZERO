using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeTopDirectPositionInstaller
{
    const string Request="Library/DomeTopDirectPosition.request";
    static DomeTopDirectPositionInstaller(){EditorApplication.delayCall+=Check;}
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
            var aim=seq.dollyAim[2];var dolly=seq.dollies[2];
            aim.useTransformPosition=true;aim.timelineDrivesDolly=false;aim.positionFollowTarget=null;
            aim.trackRed=false;aim.holdOrientation=false;
            dolly.enabled=false;
            EditorUtility.SetDirty(aim);EditorUtility.SetDirty(dolly);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeTopDirectPosition.success.txt","Saved "+scene.path+"\n"+aim.name+": Transform position directly used; existing coordinates preserved; fixed downward aim preserved.\nNo Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeTopDirectPosition.error.txt",e.ToString());}
    }
}

