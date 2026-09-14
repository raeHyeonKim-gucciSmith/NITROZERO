using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeFirstRotationInstaller
{
    const string Request="Library/DomeFirstRotation.request";
    static DomeFirstRotationInstaller(){EditorApplication.delayCall+=Check;}
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
            var aim=seq.dollyAim[0];var dolly=seq.dollies[0];
            aim.useTransformPosition=true;aim.timelineDrivesDolly=false;aim.positionFollowTarget=null;
            aim.trackRed=false;aim.holdOrientation=false;
            dolly.enabled=false; aim.useTransformRotation=true;
            EditorUtility.SetDirty(aim);EditorUtility.SetDirty(dolly);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeFirstRotation.success.txt","Saved "+scene.path+"\n"+aim.name+": Transform position and rotation directly editable; authored direction preserved, right pan applied only to output without accumulation.\nNo Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeFirstRotation.error.txt",e.ToString());}
    }
}



