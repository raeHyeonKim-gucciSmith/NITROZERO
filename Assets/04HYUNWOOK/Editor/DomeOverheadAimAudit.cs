using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeOverheadAimAudit
{
    const string Request="Library/DomeOverheadAimAudit.request";
    static DomeOverheadAimAudit(){EditorApplication.delayCall+=Check;}
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
            var aim=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DomeDollyAim>(true)).Single(a=>a.name.StartsWith("CM_Dolly_03_"));
            var cam=aim.GetComponent<CinemachineCamera>();
            var delta=aim.redTarget ? aim.redTarget.position+aim.targetOffsetWorld-aim.transform.position : Vector3.zero;
            string report="Target: "+(aim.redTarget?aim.redTarget.name:"None")+"\nCamera position: "+aim.transform.position.ToString("F3")
                +"\nTarget position: "+(aim.redTarget?aim.redTarget.position.ToString("F3"):"None")
                +"\nView direction: "+delta.normalized.ToString("F4")
                +"\nOld up-vector alignment: "+Mathf.Abs(Vector3.Dot(delta.normalized,Vector3.forward)).ToString("F4")
                +"\nFOV: "+cam.Lens.FieldOfView+"; Dutch: "+cam.Lens.Dutch
                +"\nRotation offset: "+aim.rotationOffset+"; pan: "+aim.rightPanDegrees
                +"\nMovement: "+aim.topViewMovement+"; Manual rotation: "+aim.useTransformRotation;
            foreach(var volume in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Volume>(true)))
                if(volume.sharedProfile && volume.sharedProfile.TryGet<LensDistortion>(out var distortion))
                    report+="\nVolume "+volume.name+": enabled="+volume.enabled+", weight="+volume.weight+", distortion active="+distortion.active+", intensity="+distortion.intensity.value;
            File.WriteAllText("Library/DomeOverheadAimAudit.success.txt",report+"\nRead-only settings inspection. No Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeOverheadAimAudit.error.txt",e.ToString());}
    }
}
