using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeFirstPanHoldInstaller
{
    const string Request="Library/DomeFirstPanHold.request";
    static DomeFirstPanHoldInstaller(){EditorApplication.delayCall+=Check;}
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
            var aim=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DomeDollyAim>(true)).Single(a=>a.name.StartsWith("CM_Dolly_01_"));
            DomeCameraPreviewSession.Stop();
            Undo.RecordObject(aim,"1번 Pan Curve 회전 후 유지");
            aim.panCurve=new AnimationCurve(new Keyframe(0,0,2,2),new Keyframe(.5f,1,2,0),new Keyframe(1,1,0,0));
            EditorUtility.SetDirty(aim);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeFirstPanHold.success.txt","Saved. Camera 1 Pan Curve: (0,0) -> (0.5,1) -> (1,1), linear then hold. Right Pan Degrees preserved: "+aim.rightPanDegrees+". No Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeFirstPanHold.error.txt",e.ToString());}
    }
}
