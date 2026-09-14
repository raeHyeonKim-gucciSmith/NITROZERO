using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Timeline;
using Unity.Cinemachine;
using Nitrozero.Cinematics;

public sealed class DomeCameraPreviewWindow : EditorWindow
{
    DomeCinematicSequence sequence;
    int shot;
    float progress;

    [MenuItem("Tools/HYUNWOOK/Dome Camera Preview")]
    public static void Open() => GetWindow<DomeCameraPreviewWindow>("돔 카메라 미리보기");

    public static void ShowShot(DomeCinematicSequence source, int index)
    {
        var window = GetWindow<DomeCameraPreviewWindow>("돔 카메라 미리보기");
        window.sequence = source;
        window.shot = index;
        window.progress = 0;
        DomeCameraPreviewSession.Show(source, index, 0);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
    }

    public static void ShowAim(DomeDollyAim aim)
    {
        var source = UnityEngine.Object.FindObjectsByType<DomeCinematicSequence>(FindObjectsSortMode.None)
            .FirstOrDefault(s => s.dollyAim.Contains(aim));
        if (source) ShowShot(source, Array.IndexOf(source.dollyAim, aim));
    }

    void OnGUI()
    {
        if (!sequence)
            sequence = UnityEngine.Object.FindObjectsByType<DomeCinematicSequence>(FindObjectsSortMode.None)
                .FirstOrDefault(s => s.gameObject.scene == EditorSceneManager.GetActiveScene());
        EditorGUILayout.HelpBox("플레이 없이 Game 창에서 확인합니다. 카메라 선택 후 컷 진행률을 조절하세요. Scene 창의 위치·회전 수정도 반영됩니다. 저장 또는 플레이 시작 시 미리보기가 해제됩니다.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            EditorGUI.BeginChangeCheck();
            var next = (DomeCinematicSequence)EditorGUILayout.ObjectField("연출", sequence, typeof(DomeCinematicSequence), true);
            if (EditorGUI.EndChangeCheck()) { DomeCameraPreviewSession.Stop(); sequence = next; }
            if (!sequence) return;
            for (int i = 0; i < sequence.dollyAim.Length; i++)
            {
                var aim = sequence.dollyAim[i];
                if (aim && GUILayout.Button(aim.name + " 미리보기")) ShowShot(sequence, i);
            }
            EditorGUI.BeginChangeCheck();
            progress = EditorGUILayout.Slider("컷 진행률", progress, 0, 1);
            if (EditorGUI.EndChangeCheck()) DomeCameraPreviewSession.Show(sequence, shot, progress);
            if (GUILayout.Button("미리보기 종료 / 원상복구")) DomeCameraPreviewSession.Stop();
        }
    }
    void OnDisable() => DomeCameraPreviewSession.Stop();
}

[InitializeOnLoad]
static class DomeCameraPreviewSession
{
    static DomeCinematicSequence sequence;
    static int shot;
    static float progress;
    static double previousTime, nextRefresh;
    static float[] aimPositions, dollyPositions;

    static DomeCameraPreviewSession()
    {
        EditorApplication.update += Update;
        AssemblyReloadEvents.beforeAssemblyReload += Stop;
        EditorApplication.quitting += Stop;
        EditorSceneManager.sceneSaving += (scene, path) => Stop();
        EditorSceneManager.sceneClosing += (scene, removing) => Stop();
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) Stop();
        };
    }

    public static void Show(DomeCinematicSequence source, int index, float value)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !source || !source.director) return;
        if (index < 0 || index >= source.dollyAim.Length) return;
        if (sequence != source)
        {
            Stop();
            source.StopAndRestore();
            sequence = source;
            previousTime = source.director.time;
            aimPositions = source.dollyAim.Select(a => a ? a.position : 0).ToArray();
            dollyPositions = source.dollies.Select(d => d ? d.CameraPosition : 0).ToArray();
        }
        shot = index;
        progress = Mathf.Clamp01(value);
        Refresh();
    }

    static void Update()
    {
        if (!sequence || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        // An external Stop/Restore or Timeline operation must not restart preview.
        if (!sequence.IsPreviewing) { Stop(); return; }
        if (EditorApplication.timeSinceStartup < nextRefresh) return;
        nextRefresh = EditorApplication.timeSinceStartup + 1.0 / 30;
        Refresh();
    }

    static void Refresh()
    {
        if (!sequence || !sequence.director || !(sequence.director.playableAsset is TimelineAsset timeline)) return;
        var track = timeline.GetOutputTracks().OfType<DomeCinematicTrack>().FirstOrDefault();
        var clips = track?.GetClips().OrderBy(c => c.start).ToArray();
        if (clips == null || shot >= clips.Length) { Stop(); return; }
        try
        {
            var clip = clips[shot];
            // Stay inside the selected clip at 100%, avoiding a cut to the next camera.
            sequence.director.time = clip.start + Math.Min(progress * clip.duration, Math.Max(0, clip.duration - .00001));
            sequence.director.Evaluate();
            if (sequence.cam && sequence.cam.TryGetComponent<CinemachineBrain>(out var brain)) brain.ManualUpdate();
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
        catch (Exception error) { Stop(); Debug.LogException(error); }
    }

    public static void Stop()
    {
        var source = sequence;
        sequence = null;
        if (!source) return;
        source.StopAndRestore();
        if (source.director) source.director.time = previousTime;
        for (int i = 0; i < source.dollyAim.Length && i < aimPositions.Length; i++)
            if (source.dollyAim[i]) source.dollyAim[i].position = aimPositions[i];
        for (int i = 0; i < source.dollies.Length && i < dollyPositions.Length; i++)
            if (source.dollies[i]) source.dollies[i].CameraPosition = dollyPositions[i];
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }
}
