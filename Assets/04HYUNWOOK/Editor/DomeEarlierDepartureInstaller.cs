using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeEarlierDepartureInstaller
{
    const string Request="Library/DomeEarlierDeparture.request";
    static DomeEarlierDepartureInstaller(){EditorApplication.delayCall+=Check;}
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
            var backup="Documentation/DomeCameraPreview/BeforeEarlierDeparture-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
            seq.vehicleDepartureAdvanceSeconds=.5f;
            EditorUtility.SetDirty(seq);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeEarlierDeparture.success.txt","Saved "+scene.path+"\nDeparture advanced 0.5 seconds. Red root exit: "+(2.5f+(41f-seq.redVehicleOffset.x)/seq.fastSpeedMetresPerSecond-.5f)+"s. Camera timings unchanged.\nBackup: "+backup+"\nNo Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeEarlierDeparture.error.txt",e.ToString());}
    }
}
