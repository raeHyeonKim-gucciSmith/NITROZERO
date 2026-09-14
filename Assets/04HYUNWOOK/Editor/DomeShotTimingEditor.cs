using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;
using Unity.Cinemachine;
using Nitrozero.Cinematics;

// Edit the actual Timeline clips; no second set of duration fields to drift out of sync.
static class DomeShotTimingEditor
{
    public static void DrawForAim(DomeDollyAim aim)
    {
        var sequence = UnityEngine.Object.FindObjectsByType<DomeCinematicSequence>(FindObjectsSortMode.None)
            .FirstOrDefault(s => s.dollyAim.Contains(aim));
        if (sequence)
        {
            Draw(sequence, Array.IndexOf(sequence.dollyAim, aim));
            if (aim.overhead) DrawDeparture(sequence);
        }
    }

    static void DrawDeparture(DomeCinematicSequence sequence)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("3번 카메라 차량 출발", EditorStyles.boldLabel);
        using (var settings = new SerializedObject(sequence))
        {
            settings.Update();
            var lead = settings.FindProperty("firstShotDepartureLeadSeconds");
            EditorGUI.BeginChangeCheck();
            float value = EditorGUILayout.DelayedFloatField(new GUIContent("출발 앞당김 시간 (초)",
                "1번 컷 종료(3번 컷 시작)보다 차량을 몇 초 먼저 출발시킬지 설정합니다. 0이면 컷 전환과 동시에 출발합니다."), lead.floatValue);
            if (EditorGUI.EndChangeCheck() && !float.IsNaN(value) && !float.IsInfinity(value))
            {
                lead.floatValue = Mathf.Max(0, value);
                settings.FindProperty("departBeforeFirstShotEnds").boolValue = true;
                settings.ApplyModifiedProperties();
                EditorApplication.QueuePlayerLoopUpdate();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }
        EditorGUILayout.LabelField("연출 시작 기준 출발 시점", sequence.VehicleDepartureTime.ToString("0.###") + "초");
        EditorGUILayout.HelpBox("0.5 = 3번 컷 시작 0.5초 전 출발, 0 = 3번 컷 시작과 동시에 출발. 1번 컷 시간을 변경하면 자동으로 다시 계산됩니다. 연출 시작보다 빠를 수는 없습니다. 기존 연출 오브젝트의 출발 설정과 같은 값입니다.", MessageType.Info);
    }

    public static void Draw(DomeCinematicSequence sequence, int onlyShot = -1)
    {
        if (!sequence.director || !(sequence.director.playableAsset is TimelineAsset timeline)) return;
        var tracks = timeline.GetOutputTracks()
            .Where(t => t is DomeCinematicTrack || t is CinemachineTrack).ToArray();
        var motion = tracks.OfType<DomeCinematicTrack>().FirstOrDefault();
        if (!motion) return;
        var clips = motion.GetClips().OrderBy(c => c.start).ToArray();
        if (clips.Length == 0 || clips.Length != sequence.dollyAim.Length ||
            tracks.Any(t => t.GetClips().Count() != clips.Length))
        {
            EditorGUILayout.HelpBox("카메라와 이동 트랙의 컷 개수가 달라 시간 편집을 사용할 수 없습니다.", MessageType.Warning);
            return;
        }
        EditorGUILayout.LabelField("카메라별 지속 시간", EditorStyles.boldLabel);
        var durations = clips.Select(c => c.duration).ToArray();
        bool changed = false;
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            for (int i = 0; i < clips.Length; i++)
            {
                if (onlyShot >= 0 && i != onlyShot) continue;
                var aim = sequence.dollyAim[i];
                string label = aim ? aim.name : "컷 " + (i + 1);
                var parts = label.Split('_');
                if (parts.Length > 2 && int.TryParse(parts[2], out int number)) label = number + "번 카메라";
                EditorGUI.BeginChangeCheck();
                double duration = EditorGUILayout.DelayedDoubleField(label + " (초)", durations[i]);
                if (EditorGUI.EndChangeCheck() && !double.IsNaN(duration) && !double.IsInfinity(duration))
                { durations[i] = Math.Max(.1, duration); changed = true; }
                EditorGUILayout.LabelField("재생 구간", clips[i].start.ToString("0.###") + " ~ " + clips[i].end.ToString("0.###") + "초");
            }
        }
        EditorGUILayout.LabelField("전체 연출 시간", durations.Sum().ToString("0.###") + "초");
        EditorGUILayout.HelpBox("초를 입력하고 Enter를 누르면 반영됩니다. 다음 컷의 시작 시점과 자동 종료 시간도 합계에 맞춰 변경됩니다. 최소 0.1초입니다.", MessageType.Info);
        if (changed) Apply(sequence, timeline, tracks, durations);
    }

    static void Apply(DomeCinematicSequence sequence, TimelineAsset timeline, TrackAsset[] tracks, double[] durations)
    {
        DomeCameraPreviewSession.Stop();
        sequence.StopAndRestore();
        // Capture clip references before changing their start times, which reorder tracks.
        var ordered = tracks.Select(t => t.GetClips().OrderBy(c => c.start).ToArray()).ToArray();
        var objects = tracks.Cast<UnityEngine.Object>().Concat(new UnityEngine.Object[] { timeline })
            .Concat(ordered.SelectMany(c => c).Select(c => c.asset)).Where(o => o).Distinct().ToArray();
        Undo.RegisterCompleteObjectUndo(objects, "카메라별 지속 시간 변경");
        foreach (var clips in ordered)
        {
            double start = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].start = start;
                clips[i].duration = durations[i];
                if (clips[i].asset is DomeCinematicClip motion) motion.sequenceStart = (float)start;
                start += durations[i];
            }
        }
        timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
        timeline.fixedDuration = durations.Sum();
        foreach (var obj in objects) EditorUtility.SetDirty(obj);
        AssetDatabase.SaveAssetIfDirty(timeline);
        sequence.director.time = 0;
        EditorApplication.QueuePlayerLoopUpdate();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }
}
