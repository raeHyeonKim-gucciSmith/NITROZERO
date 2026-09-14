using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeFinishInstaller
{
    const string Request="Library/DomeFinish.request";
    static DomeFinishInstaller(){EditorApplication.delayCall+=Check;}
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
            seq.StopAndRestore();
            var backup="Documentation/DomeCameraPreview/BeforeAutoFinish-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
            seq.accelerateLastShot=true;seq.lastShotSpeedKmh=450;seq.lastShotAccelerationSeconds=.15f;
            seq.stopPlayAtEnd=true;seq.continueVehiclesAfterCamera=false;
            seq.director.extrapolationMode=DirectorWrapMode.Hold;
            EditorUtility.SetDirty(seq);EditorUtility.SetDirty(seq.director);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeFinish.success.txt","Saved "+scene.path+"\nLast shot accelerates to 450 km/h over 0.15s. Auto exits Play Mode at timeline end ("+seq.PlaybackDuration+"s).\nBackup: "+backup+"\nNo Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeFinish.error.txt",e.ToString());}
    }
}
