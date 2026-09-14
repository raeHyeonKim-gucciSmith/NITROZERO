using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeDirectPositionInstaller
{
    const string Request="Library/DomeDirectPosition.request";
    static DomeDirectPositionInstaller(){EditorApplication.delayCall+=Check;}
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
            var aim=seq.dollyAim[3];var dolly=seq.dollies[3];
            aim.useTransformPosition=true;aim.timelineDrivesDolly=false;aim.positionFollowTarget=null;
            aim.trackRed=true;aim.redTarget=seq.red;aim.holdOrientation=false;
            dolly.enabled=false;
            EditorUtility.SetDirty(aim);EditorUtility.SetDirty(dolly);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeDirectPosition.success.txt","Saved "+scene.path+"\n"+aim.name+": Transform position directly used; existing coordinates preserved; red-only aim.\nNo Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeDirectPosition.error.txt",e.ToString());}
    }
}
