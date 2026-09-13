using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public sealed class TrailerCruiseClip : PlayableAsset, ITimelineClipAsset
{
    [Range(0f, 600f)] public float wheelSpeedKph = 240f;
    public TrailerSteeringSettings steering = new TrailerSteeringSettings();
    [Range(0f, 1f)] public float manualSteeringWeight = 1f;
    [Min(0f)] public float automaticBlendIn = 0.2f;
    [Min(0f)] public float automaticBlendOut = 0.5f;

    public float SampleManualWeight(double localTime, double clipDuration)
    {
        float duration = Mathf.Max(0.0001f, (float)clipDuration);
        float fadeIn = Mathf.Max(0, automaticBlendIn), fadeOut = Mathf.Max(0, automaticBlendOut);
        float scale = Mathf.Min(1, duration / Mathf.Max(0.0001f, fadeIn + fadeOut));
        fadeIn *= scale;
        fadeOut *= scale;
        float enter = fadeIn > 0 ? Mathf.SmoothStep(0, 1, (float)localTime / fadeIn) : 1;
        float leave = fadeOut > 0 ? Mathf.SmoothStep(0, 1, (duration - (float)localTime) / fadeOut) : 1;
        return Mathf.Clamp01(manualSteeringWeight) * Mathf.Min(enter, leave);
    }
    public override double duration => steering.Duration;
    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) =>
        Playable.Create(graph);
}
