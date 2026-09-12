using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public sealed class TrailerCruiseClip : PlayableAsset, ITimelineClipAsset
{
    [Range(0f, 600f)] public float wheelSpeedKph = 240f;
    public TrailerSteeringSettings steering = new TrailerSteeringSettings();
    public override double duration => steering.Duration;
    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) =>
        Playable.Create(graph);
}
