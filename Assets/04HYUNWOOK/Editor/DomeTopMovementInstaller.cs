using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeTopMovementInstaller
{
    const string Request="Library/DomeTopMovement.request";
    static DomeTopMovementInstaller(){EditorApplication.delayCall+=Check;}
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
            EditorUtility.SetDirty(aim);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeTopMovement.success.txt","Saved "+scene.path+"\nCamera: "+aim.name+"\n카메라 이동: "+aim.topViewMovement+"\nNo Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeTopMovement.error.txt",e.ToString());}
    }
}
