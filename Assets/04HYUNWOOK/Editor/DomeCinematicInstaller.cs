using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.Rendering.Universal;
using Nitrozero.Cinematics;

[InitializeOnLoad]
public static class DomeCinematicInstaller
{
    const string ScenePath = "Assets/04HYUNWOOK/Scenes/HW_domeInTheMoon.unity";
    const string TimelinePath = "Assets/04HYUNWOOK/Cinematics/HW_Dome_20s.playable";
    const string Request = "Library/InstallDomeCinematic.request";
    static DomeCinematicInstaller()
    {
        EditorApplication.delayCall += CheckRequest;
        AssemblyReloadEvents.beforeAssemblyReload += RestorePreviews;
        EditorApplication.quitting += RestorePreviews;
        EditorSceneManager.sceneSaving += (scene, path) => RestorePreviews();
    }
    static void RestorePreviews()
    {
        if (Application.isPlaying) return;
        foreach (var sequence in UnityEngine.Object.FindObjectsByType<DomeCinematicSequence>(FindObjectsSortMode.None))
            sequence.StopAndRestore();
    }
    static void CheckRequest()
    {
        if (!File.Exists(Request)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.delayCall += CheckRequest; return; }
        File.Delete(Request);
        try { Install(); }
        catch (Exception error) { File.WriteAllText("Library/DomeCinematicInstall.error.txt", error.ToString()); Debug.LogException(error); }
    }
    [MenuItem("Tools/HYUNWOOK/Install Dome 20s Cinematic")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before installing the cinematic.");
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var all = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var red = all.Single(t => t.name == "Red_Car");
        var blue = all.Single(t => t.name == "Blue_Car");
        var folder = "Documentation/DomeCameraPreview/UnityApplication-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(folder);
        File.Copy(ScenePath, folder + "/HW_domeInTheMoon.disk-before.unity");
        if (!EditorSceneManager.SaveScene(scene, folder + "/HW_domeInTheMoon.live-before.unity", true))
            throw new IOException("Could not back up the live scene.");
        var sequence = all.Select(t => t.GetComponent<DomeCinematicSequence>()).FirstOrDefault(s => s);
        if (!sequence)
        {
            var root = new GameObject("HW_Dome_Cinematic_20s");
            SceneManager.MoveGameObjectToScene(root, scene);
            sequence = root.AddComponent<DomeCinematicSequence>();
            sequence.director = root.AddComponent<PlayableDirector>();
        }
        string[] shotNames={"01_Dome_Establishing","02_Road_Dome_Tilt","03_Dome_Top_Track","04_Red_Oblique_Track","05_Red_Low_Hero","06_Red_Roll_Track","07_Red_Ground_Pass"};
        if(sequence.shotCameras.Length!=7)
        {
            var shotCameras=new List<Camera>();
            foreach(var name in shotNames)
            {
                var cameraObject=new GameObject(name);
                cameraObject.transform.SetParent(sequence.transform,false);
                cameraObject.tag="MainCamera";
                var camera=cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>().enabled=false;
                camera.nearClipPlane=.025f;camera.farClipPlane=15000;camera.depth=100;
                var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;
                data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                camera.enabled=false;shotCameras.Add(camera);
            }
            sequence.shotCameras=shotCameras.ToArray();sequence.cam=sequence.shotCameras[0];
        }
        sequence.StopAndRestore();
        sequence.red = red; sequence.car = blue;
        sequence.start = blue.position; sequence.redStart = red.position;
        var suspend = new HashSet<Behaviour>();
        foreach (var t in all)
        {
            foreach (var camera in t.GetComponents<Camera>()) if (!sequence.shotCameras.Contains(camera)) suspend.Add(camera);
            foreach (var listener in t.GetComponents<AudioListener>()) if (!sequence.shotCameras.Any(c => c.gameObject == listener.gameObject)) suspend.Add(listener);
            foreach (var director in t.GetComponents<PlayableDirector>()) if (director != sequence.director) suspend.Add(director);
        }
        foreach (var root in new[] {red, blue})
        {
            foreach (var behaviour in root.GetComponentsInChildren<Behaviour>(true))
                if (behaviour is Animator || behaviour is MonoBehaviour) suspend.Add(behaviour);
            foreach (var behaviour in root.GetComponentsInParent<Behaviour>(true))
                if (behaviour && behaviour.GetType().Name.Contains("SplineAnimate")) suspend.Add(behaviour);
        }
        sequence.suspendDuringSequence = suspend.Where(b => b && b != sequence).ToArray();
        var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelinePath);
        if (!timeline)
        {
            timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, TimelinePath);
            var track = timeline.CreateTrack<DomeCinematicTrack>(null, "Dome camera + red car / 20 seconds");
            double[] cuts={0,2.4,6.2,8,10.4,12.5,16,20};
            for(int i=0;i<7;i++)
            {
                var clip=track.CreateClip<DomeCinematicClip>();
                clip.displayName=shotNames[i];clip.start=cuts[i];clip.duration=cuts[i+1]-cuts[i];
                ((DomeCinematicClip)clip.asset).sequenceStart=(float)cuts[i];
            }
            timeline.editorSettings.frameRate = 30;
        }
        sequence.director.playableAsset = timeline;
        sequence.director.playOnAwake = true;
        sequence.director.extrapolationMode = DirectorWrapMode.Hold;
        sequence.director.timeUpdateMode = DirectorUpdateMode.GameTime;
        foreach (var track in timeline.GetOutputTracks())
            if (track is DomeCinematicTrack) sequence.director.SetGenericBinding(track, sequence);
        // Set only the new camera's edit-mode pose; vehicle authoring poses stay intact.
        sequence.Evaluate(0);
        var cameraPosition = sequence.cam.transform.position;
        var cameraRotation = sequence.cam.transform.rotation;
        var cameraFov = sequence.cam.fieldOfView;
        sequence.Restore();
        sequence.cam.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        sequence.cam.fieldOfView = cameraFov;
        EditorUtility.SetDirty(sequence); EditorUtility.SetDirty(sequence.director); EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the cinematic scene.");
        Selection.activeGameObject = sequence.gameObject;
        File.WriteAllText("Library/DomeCinematicInstall.success.txt", "Scene: " + scene.path + "\nTimeline: " + TimelinePath + "\nBackup: " + folder + "\nRed: " + red.position + "\nBlue: " + blue.position);
        Debug.Log("Dome 20-second cinematic installed. Select HW_Dome_Cinematic_20s and open Timeline, or enter Play Mode.", sequence);
    }
}

[CustomEditor(typeof(DomeCinematicSequence))]
public sealed class DomeCinematicSequenceEditor : Editor
{
    float time;
    public override void OnInspectorGUI()
    {
        DomeShotTimingEditor.Draw((DomeCinematicSequence)target);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            var previewSequence = (DomeCinematicSequence)target;
            for (int i = 0; i < previewSequence.dollyAim.Length; i++)
                if (previewSequence.dollyAim[i] && GUILayout.Button(previewSequence.dollyAim[i].name + " 미리보기"))
                    DomeCameraPreviewWindow.ShowShot(previewSequence, i);
            if (GUILayout.Button("미리보기 종료 / 원상복구")) DomeCameraPreviewSession.Stop();
        }
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject,"fastSpeedMetresPerSecond");
        var speed=serializedObject.FindProperty("fastSpeedMetresPerSecond");
        speed.floatValue=Mathf.Max(0,EditorGUILayout.FloatField("Vehicle Speed (km/h)",speed.floatValue*3.6f))/3.6f;
        if(serializedObject.ApplyModifiedProperties())
        {EditorApplication.QueuePlayerLoopUpdate();SceneView.RepaintAll();}
        var sequence = (DomeCinematicSequence)target;
        EditorGUILayout.HelpBox(sequence.useSplineDolly ? sequence.PlaybackDuration + "초 · Cinemachine Dolly " + sequence.dollies.Length + "대. Timeline과 Spline에서 편집하세요." : "20초 · 빨간 차량 중심 시네마틱", MessageType.Info);
        if (GUILayout.Button("Replay " + sequence.PlaybackDuration + "s")) sequence.Replay();
        if (GUILayout.Button("Stop / Restore")) { sequence.StopAndRestore(); SceneView.RepaintAll(); }
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            EditorGUI.BeginChangeCheck();
            time = EditorGUILayout.Slider("Preview time", time, 0, sequence.PlaybackDuration);
            if (EditorGUI.EndChangeCheck()) { if(sequence.useSplineDolly){sequence.director.time=time;sequence.director.Evaluate();}else sequence.Evaluate(time); EditorApplication.QueuePlayerLoopUpdate(); SceneView.RepaintAll(); }
        }
    }
    // Changing Inspector selection must not cancel a user-controlled Timeline preview.
}
