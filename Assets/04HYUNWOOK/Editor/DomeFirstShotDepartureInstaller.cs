using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeFirstShotDepartureInstaller
{
    const string Request="Library/DomeFirstShotDeparture.request";
    static DomeFirstShotDepartureInstaller(){EditorApplication.delayCall+=Check;}
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
            var seq=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DomeCinematicSequence>(true)).Single();
            DomeCameraPreviewSession.Stop();seq.StopAndRestore();
            Undo.RecordObject(seq,"1번 컷 종료 0.5초 전 출발");
            seq.departBeforeFirstShotEnds=true;
            seq.firstShotDepartureLeadSeconds=.5f;
            EditorUtility.SetDirty(seq);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
            File.WriteAllText("Library/DomeFirstShotDeparture.success.txt","Saved. Departure: "+seq.VehicleDepartureTime+" seconds. Lead: "+seq.firstShotDepartureLeadSeconds+" seconds. Speed preserved: "+seq.fastSpeedMetresPerSecond*3.6f+" km/h. No Play Mode or tests.");
        }
        catch(Exception e){File.WriteAllText("Library/DomeFirstShotDeparture.error.txt",e.ToString());}
    }
}
