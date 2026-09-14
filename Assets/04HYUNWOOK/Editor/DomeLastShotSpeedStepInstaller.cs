using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeLastShotSpeedStepInstaller
{
    const string Request="Library/DomeLastShotSpeedStep.request";
    static DomeLastShotSpeedStepInstaller(){EditorApplication.delayCall+=Check;}
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
            DomeCameraPreviewSession.Stop();
            seq.StopAndRestore();
            var backup="Documentation/DomeCameraPreview/BeforeLastShotSpeedStep-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
            Undo.RecordObject(seq,"4번 컷 차량 속도 변경");
            seq.accelerateLastShot=true;
            seq.overrideLastShotStartSpeed=true;
            seq.lastShotStartSpeedKmh=400;
            seq.lastShotStartHoldSeconds=.5f;
            seq.lastShotSpeedKmh=600;
            seq.lastShotAccelerationSeconds=0;
            EditorUtility.SetDirty(seq);
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeLastShotSpeedStep.success.txt","Saved "+scene.path+"\nLast cut: first 0.5 seconds = 400 km/h; remaining = 600 km/h. Transition duration = 0.\nOther shots' base speed preserved: "+seq.fastSpeedMetresPerSecond*3.6f+" km/h\nBackup: "+backup+"\nNo Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeLastShotSpeedStep.error.txt",e.ToString());}
    }
}
