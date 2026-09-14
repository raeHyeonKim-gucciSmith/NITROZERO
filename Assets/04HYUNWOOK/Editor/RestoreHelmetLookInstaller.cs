using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RestoreHelmetLookInstaller
{
    const string Request="Library/RestoreHelmetLook.request";
    static RestoreHelmetLookInstaller(){EditorApplication.delayCall+=Check;}
    static void Apply(GameObject root)
    {
        foreach(var c in root.GetComponentsInChildren<CarCinemachineSetup>(true))
        {
            Undo.RecordObject(c,"기존 헬멧 효과 복원");
            c.enableHelmetHudCurvature=true;c.enableHelmetVignette=true;c.enableHelmetLensDistortion=true;
            c.helmetLensDistortionIntensity=.7f;c.helmetLensDistortionScale=.95f;
            c.helmetVignetteIntensity=.5f;c.helmetVignetteSmoothness=.5f;
            if(!c.GetComponent<RacingHelmetVignette>())c.gameObject.AddComponent<RacingHelmetVignette>();
            EditorUtility.SetDirty(c);PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        }
        foreach(var c in root.GetComponentsInChildren<RacingHudCurvature>(true))
        {Undo.RecordObject(c,"UI 곡률 복원");c.curvature=.095f;c.enabled=true;EditorUtility.SetDirty(c);PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
    }
    static void Check()
    {
        if(!File.Exists(Request))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        {EditorApplication.delayCall+=Check;return;}
        File.Delete(Request);
        try
        {
            foreach(var path in new[]{"Assets/04HYUNWOOK/Prefab/Car.prefab","Assets/04HYUNWOOK/Prefab/UI.prefab"})
            {
                var root=PrefabUtility.LoadPrefabContents(path);
                try{Apply(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i);if(!scene.isLoaded||string.IsNullOrEmpty(scene.path))continue;
                foreach(var root in scene.GetRootGameObjects())Apply(root);
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            }
            File.WriteAllText("Library/RestoreHelmetLook.success.txt","Restored UI curvature .095, lens intensity .7 / scale .95, vignette .5 / smoothness .5. Prefabs and loaded scenes saved. Play-only helmet gate retained. No tests.");
        }
        catch(Exception e){File.WriteAllText("Library/RestoreHelmetLook.error.txt",e.ToString());}
    }
}
