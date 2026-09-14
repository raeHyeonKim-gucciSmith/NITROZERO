using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeAll525Installer
{
    const string Request="Library/DomeAll525.request";
    static DomeAll525Installer(){EditorApplication.delayCall+=Check;}
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
            var backup="Documentation/DomeCameraPreview/BeforeAll525-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
            seq.fastSpeedMetresPerSecond=525f/3.6f;seq.lastShotSpeedKmh=525;
            seq.stopPlayAtEnd=true;seq.continueVehiclesAfterCamera=false;
            seq.director.extrapolationMode=DirectorWrapMode.Hold;
            EditorUtility.SetDirty(seq);EditorUtility.SetDirty(seq.director);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeAll525.success.txt","Saved "+scene.path+"\nBase and last-shot vehicle speeds are both 525 km/h. Auto exits Play Mode at timeline end ("+seq.PlaybackDuration+"s).\nBackup: "+backup+"\nNo Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeAll525.error.txt",e.ToString());}
    }
}

