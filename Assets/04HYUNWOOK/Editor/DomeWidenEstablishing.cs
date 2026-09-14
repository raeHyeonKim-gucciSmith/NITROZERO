using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeWidenEstablishing
{
    const string Request="Library/WidenDomeEstablishing.request";
    static DomeWidenEstablishing(){EditorApplication.delayCall+=Check;}
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
            var backup="Documentation/DomeCameraPreview/BeforeWiderDome-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
            var camera=seq.dollies[0].GetComponent<CinemachineCamera>();
            camera.Lens.FieldOfView=63;
            if(seq.director.time==0){seq.cam.fieldOfView=63;EditorUtility.SetDirty(seq.cam);}
            EditorUtility.SetDirty(camera);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeWidenEstablishing.success.txt","Saved "+scene.path+"\nEstablishing FOV 58 -> 63. Five 1-second shots unchanged.\nBackup: "+backup+"\nNo Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeWidenEstablishing.error.txt",e.ToString());Debug.LogException(e);}
    }
}
