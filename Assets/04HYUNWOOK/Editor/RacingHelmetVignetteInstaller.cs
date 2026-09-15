using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RacingHelmetVignetteInstaller
{
    const string Request="Library/RacingHelmetVignette.request";
    static RacingHelmetVignetteInstaller(){EditorApplication.delayCall+=Check;}
    static void Check()
    {
        if(!File.Exists(Request))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        {EditorApplication.delayCall+=Check;return;}
        File.Delete(Request);
        try
        {
            const string path="Assets/04HYUNWOOK/Prefab/Car.prefab";
            var prefab=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var camera in prefab.GetComponentsInChildren<CarCinemachineSetup>(true))
                    if(!camera.GetComponent<RacingHelmetVignette>())camera.gameObject.AddComponent<RacingHelmetVignette>();
                PrefabUtility.SaveAsPrefabAsset(prefab,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            int count=0;
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i);if(!scene.isLoaded)continue;
                bool changed=false;
                foreach(var root in scene.GetRootGameObjects())
                    foreach(var camera in root.GetComponentsInChildren<CarCinemachineSetup>(true))
                    {
                        if(!camera.GetComponent<RacingHelmetVignette>())Undo.AddComponent<RacingHelmetVignette>(camera.gameObject);
                        changed=true;count++;
                    }
                if(changed && !string.IsNullOrEmpty(scene.path))
                {EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed: "+scene.path);}
            }
            File.WriteAllText("Library/RacingHelmetVignette.success.txt","Car prefab and "+count+" loaded racing cameras configured. Edit mode vignette=0; runtime first person after helmet only. No Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/RacingHelmetVignette.error.txt",e.ToString());}
    }
}
