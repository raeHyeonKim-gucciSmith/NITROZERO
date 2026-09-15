using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class DisableCameraLensDistortionInstaller
{
    const string Request="Library/DisableCameraLensDistortion.request";
    static DisableCameraLensDistortionInstaller(){EditorApplication.delayCall+=Check;}
    static void Check()
    {
        if(!File.Exists(Request))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        {EditorApplication.delayCall+=Check;return;}
        File.Delete(Request);
        try
        {
            var backup="Documentation/DomeCameraPreview/BeforeAllLensDistortionOff-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var report=new List<string>();
            foreach(var guid in AssetDatabase.FindAssets("t:VolumeProfile",new[]{"Assets"}))
            {
                var path=AssetDatabase.GUIDToAssetPath(guid);
                var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                if(!profile || !profile.TryGet<LensDistortion>(out var lens))continue;
                if(lens.intensity.value==0 && lens.scale.value==1 && lens.intensity.overrideState && lens.scale.overrideState)
                {report.Add("Already neutral: "+path);continue;}
                var copy=Path.Combine(backup,path);
                Directory.CreateDirectory(Path.GetDirectoryName(copy));File.Copy(path,copy);
                Undo.RecordObject(lens,"전체 카메라 렌즈 왜곡 제거");
                report.Add("Neutralized: "+path+" (intensity "+lens.intensity.value+", scale "+lens.scale.value+")");
                lens.intensity.Override(0);lens.scale.Override(1);
                EditorUtility.SetDirty(lens);EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
            }
            File.WriteAllText("Library/DisableCameraLensDistortion.success.txt",string.Join("\n",report)+"\nBackup: "+backup+"\nNo Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DisableCameraLensDistortion.error.txt",e.ToString());}
    }
}
