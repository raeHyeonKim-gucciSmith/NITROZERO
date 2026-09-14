using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeVehicleTargetInstaller
{
    const string Request="Library/DomeVehicleTarget.request";
    static DomeVehicleTargetInstaller(){EditorApplication.delayCall+=Check;}
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
            var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var vehicle=transforms.Single(t=>t.name=="차량");
            var aim=transforms.Select(t=>t.GetComponent<DomeDollyAim>()).Single(a=>a && a.name.StartsWith("CM_Dolly_03_"));
            DomeCameraPreviewSession.Stop();
            var seq=transforms.Select(t=>t.GetComponent<DomeCinematicSequence>()).Single(s=>s);
            seq.StopAndRestore();
            Undo.RecordObject(aim,"3번 카메라 바라볼 대상 변경");
            aim.redTarget=vehicle;aim.trackRed=true;
            EditorUtility.SetDirty(aim);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeVehicleTarget.success.txt","Camera: "+aim.name+"\nTarget: "+vehicle.name+" (instance "+vehicle.GetInstanceID()+")\nLook-at enabled. Scene saved. No Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeVehicleTarget.error.txt",e.ToString());}
    }
}
