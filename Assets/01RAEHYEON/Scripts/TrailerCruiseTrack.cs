using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.8f, 0.18f, 0.12f)]
[TrackClipType(typeof(TrailerCruiseClip))]
[TrackBindingType(typeof(TrailerCruiseMotion))]
public sealed class TrailerCruiseTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        var playable = ScriptPlayable<TrailerCruiseMixer>.Create(graph, inputCount);
        playable.GetBehaviour().Configure(GetClips(), graph.GetResolver() as PlayableDirector);
        return playable;
    }

    public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
    {
        var car = director.GetGenericBinding(this) as TrailerCruiseMotion;
        if (car != null)
        {
            RegisterTransform(car.BodyMotionRoot, driver);
            if (car.Wheels != null)
                foreach (var wheel in car.Wheels) RegisterTransform(wheel, driver);
        }
        base.GatherProperties(director, driver);
    }

    private static void RegisterTransform(Transform target, IPropertyCollector collector)
    {
        if (target == null) return;
        foreach (string axis in new[] { "x", "y", "z" })
            collector.AddFromName<Transform>(target.gameObject, "m_LocalPosition." + axis);
        foreach (string axis in new[] { "x", "y", "z", "w" })
            collector.AddFromName<Transform>(target.gameObject, "m_LocalRotation." + axis);
    }
}

// Track-level sampling integrates every earlier speed segment. Seeking backwards or directly
// into a later clip gives the same wheel phase as playing from the beginning.
public sealed class TrailerCruiseMixer : PlayableBehaviour
{
    private List<TimelineClip> clips;
    private readonly List<double> boundaries = new List<double>();
    private PlayableDirector director;
    private TrailerCruiseMotion boundCar;

    public void Configure(IEnumerable<TimelineClip> source, PlayableDirector owner)
    {
        clips = new List<TimelineClip>(source);
        director = owner;
        if (director != null) director.stopped += OnDirectorStopped;
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var car = playerData as TrailerCruiseMotion;
        if (boundCar != car)
        {
            if (boundCar != null) boundCar.ReleaseTimeline(this);
            boundCar = car;
        }
        if (car == null || !car.isActiveAndEnabled)
        {
            if (boundCar != null) boundCar.ReleaseTimeline(this);
            return;
        }
        double time = director != null ? director.time : playable.GetTime();
        Sample(time, car, out double distance, out float handleAngle);
        TimelineClip active = ActiveAt(time);
        float manualWeight = active == null ? car.manualSteeringWeight :
            ((TrailerCruiseClip)active.asset).SampleManualWeight(time - active.start, active.duration);
        car.ApplyTimelinePose(this, time, distance, handleAngle, manualWeight);
    }

    public void Sample(double seconds, TrailerCruiseMotion car, out double distance, out float handleAngle)
    {
        double time = Math.Max(0, seconds);
        boundaries.Clear();
        boundaries.Add(0);
        boundaries.Add(time);
        foreach (var clip in clips)
        {
            if (clip.start > 0 && clip.start < time) boundaries.Add(clip.start);
            if (clip.end > 0 && clip.end < time) boundaries.Add(clip.end);
        }
        boundaries.Sort();
        distance = 0;
        for (int i = 1; i < boundaries.Count; i++)
        {
            double from = boundaries[i - 1], to = boundaries[i];
            TimelineClip active = ActiveAt((from + to) * 0.5);
            float speed = active == null ? car.wheelSpeedKph : ((TrailerCruiseClip)active.asset).wheelSpeedKph;
            distance += (to - from) * Mathf.Clamp(speed, 0, 600) / 3.6;
        }
        TimelineClip steeringClip = ActiveAt(time);
        if (steeringClip == null)
        {
            // In gaps, hold the most recently completed pose. Speed returns to the car default.
            foreach (var clip in clips)
                if (clip.end <= time && (steeringClip == null || clip.end >= steeringClip.end))
                    steeringClip = clip;
        }
        handleAngle = steeringClip == null ? car.steeringWheelAngle :
            ((TrailerCruiseClip)steeringClip.asset).steering.Sample(
                Math.Min(time - steeringClip.start, steeringClip.duration), car.minHandleAngle, car.maxHandleAngle);
    }

    private TimelineClip ActiveAt(double time)
    {
        TimelineClip active = null;
        foreach (var clip in clips)
            if (clip.start <= time && time < clip.end && (active == null || clip.start >= active.start))
                active = clip;
        return active;
    }

    public override void OnGraphStop(Playable playable) => Release();
    public override void OnPlayableDestroy(Playable playable)
    {
        if (director != null) director.stopped -= OnDirectorStopped;
        Release();
    }

    private void OnDirectorStopped(PlayableDirector stoppedDirector) => Release();

    private void Release()
    {
        if (boundCar != null) boundCar.ReleaseTimeline(this);
        boundCar = null;
    }
}
