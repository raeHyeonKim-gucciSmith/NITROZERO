using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeFiveSecondEndFixInstaller
{
    const string Request = "Library/DomeFiveSecondEndFix.request";
    static DomeFiveSecondEndFixInstaller() { EditorApplication.delayCall += Check; }
    static void Check()
    {
        if (!File.Exists(Request)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.delayCall += Check; return; }
        File.Delete(Request);
        try
        {
            var scene = SceneManager.GetSceneByPath("Assets/04HYUNWOOK/Scenes/HW_domeInTheMoon.unity");
            if (!scene.isLoaded) throw new InvalidOperationException("Open dome scene first.");
            var seq = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DomeCinematicSequence>(true)).Single();
            var timeline = seq.director.playableAsset as TimelineAsset;
            if (!timeline) throw new InvalidOperationException("Missing Timeline.");
            var tracks = timeline.GetOutputTracks().ToArray();
            var report = string.Join("\n", tracks.Select(t => t.name + ": " + string.Join(", ", t.GetClips().Select(c => c.start + "-" + c.end))));
            if (tracks.SelectMany(t => t.GetClips()).Any(c => c.end > 5.0001))
                throw new InvalidOperationException("Clips exceed five seconds; preserve authored timing.\n" + report);
            var backup = "Documentation/DomeCameraPreview/BeforeFiveSecondEndFix-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            File.Copy(AssetDatabase.GetAssetPath(timeline), backup + "/Timeline.playable");
            timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
            timeline.fixedDuration = 5;
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            File.WriteAllText("Library/DomeFiveSecondEndFix.success.txt", "Timeline: " + AssetDatabase.GetAssetPath(timeline) + "\nFixed duration: " + timeline.duration + "\n" + report + "\nNo scene poses changed. No Play Mode or tests.");
        }
        catch (Exception e) { File.WriteAllText("Library/DomeFiveSecondEndFix.error.txt", e.ToString()); }
    }
}
