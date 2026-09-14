using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeTopLensFixInstaller
{
    const string Request="Library/DomeTopLensFix.request";
    static DomeTopLensFixInstaller(){EditorApplication.delayCall+=Check;}
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
            DomeCameraPreviewSession.Stop();
            var backup="Documentation/DomeCameraPreview/BeforeTopLensFix-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            if(!EditorSceneManager.SaveScene(scene,backup+"/HW_domeInTheMoon.unity",true))throw new IOException("Backup failed");
            var extension=aim.GetComponent<CinemachineVolumeSettings>();
            if(!extension)extension=Undo.AddComponent<CinemachineVolumeSettings>(aim.gameObject);
            const string path="Assets/04HYUNWOOK/Cinematics/DomeTopNoDistortion.asset";
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if(!profile)
            {
                if(extension.Profile && AssetDatabase.Contains(extension.Profile))
                {
                    if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(extension.Profile),path))throw new IOException("Profile copy failed");
                    profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                }
                else
                {
                    profile=ScriptableObject.CreateInstance<VolumeProfile>();
                    AssetDatabase.CreateAsset(profile,path);
                }
            }
            if(!profile.TryGet<LensDistortion>(out var lens))
            {lens=profile.Add<LensDistortion>();AssetDatabase.AddObjectToAsset(lens,profile);}
            lens.active=true;lens.intensity.Override(0);lens.scale.Override(1);
            Undo.RecordObject(extension,"3번 카메라 렌즈 왜곡 제거");
            extension.Profile=profile;extension.enabled=true;extension.InvalidateCachedProfile();
            EditorUtility.SetDirty(lens);EditorUtility.SetDirty(profile);EditorUtility.SetDirty(extension);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeTopLensFix.success.txt","Saved. Camera 3 volume override: Lens Distortion intensity=0, scale=1.\nProfile: "+path+"\nOther camera profiles unchanged. No Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeTopLensFix.error.txt",e.ToString());}
    }
}
