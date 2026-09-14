using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Linq;
namespace Nitrozero.Cinematics
{
public sealed class DomeCinematicClip : PlayableAsset, ITimelineClipAsset
{
    public float sequenceStart;
    public override double duration => DomeCinematicSequence.Duration;
    public ClipCaps clipCaps => ClipCaps.ClipIn | ClipCaps.SpeedMultiplier;
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable=ScriptPlayable<DomeCinematicBehaviour>.Create(graph);
        playable.GetBehaviour().sequenceStart=sequenceStart;
        playable.GetBehaviour().sourceAsset=this;
        return playable;
    }
}
public sealed class DomeCinematicBehaviour : PlayableBehaviour
{
    public float sequenceStart;
    public DomeCinematicClip sourceAsset;
    DomeCinematicSequence sequence;
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        sequence = playerData as DomeCinematicSequence;
        if (!sequence || info.effectiveWeight<=0) return;
        if(sequence.director && sequence.director.playableAsset is TimelineAsset timeline)
        {
            foreach(var track in timeline.GetOutputTracks().OfType<DomeCinematicTrack>())
            {
                int index=0;
                foreach(var clip in track.GetClips().OrderBy(c=>c.start))
                {
                    if(clip.asset==sourceAsset)
                    {
                        float progress=Mathf.Clamp01((float)((sequence.director.time-clip.start)/clip.duration));
                        sequence.Evaluate((float)sequence.director.time,index,progress);return;
                    }
                    index++;
                }
            }
        }
        sequence.Evaluate(sequenceStart+(float)playable.GetTime());
    }
    public override void OnGraphStop(Playable playable) { if (sequence) sequence.OnTimelineGraphStopped(); }
    public override void OnPlayableDestroy(Playable playable) { if (sequence) sequence.Restore(); }
}
}
