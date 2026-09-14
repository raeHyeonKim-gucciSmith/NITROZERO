using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomePanEstablishing
{
    const string Request="Library/PanDomeEstablishing.request";
    static DomePanEstablishing(){EditorApplication.delayCall+=Check;}
    static void Check()
    {
        if(!File.Exists(Request))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        {EditorApplication.delayCall+=Check;return;}
        File.Delete(Request);
        try
        {
            var scene=SceneManager.GetSceneByPath("Assets/04HYUNWOOK/Scenes/HW_domeInTheMoon.unity");
            if(!scene.isLoaded)throw new InvalidOperationException("Open the dome scene first.");
            var seq=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DomeCinematicSequence>(true)).Single();
            if(!seq.equalShotDurations||seq.dollies.Length!=5)throw new InvalidOperationException("Expected five equal shots.");
            seq.StopAndRestore();
            var backup="Documentation/DomeCameraPreview/BeforeRightPan-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
            var aim=seq.dollyAim[0];
            var rotations=new Quaternion[65];
            for(int j=0;j<rotations.Length;j++)
                seq.GetSixShotPose(Mathf.Min(j/64f,.9999f),out _,out rotations[j],out _);
            aim.rotations=rotations;aim.trackRed=false;
            EditorUtility.SetDirty(aim);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomePanEstablishing.success.txt","Saved "+scene.path+"\nEstablishing smooth right pan: additional 12 degrees. Duration and lens unchanged.\nBackup: "+backup+"\nNo Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomePanEstablishing.error.txt",e.ToString());Debug.LogException(e);}
    }
}
