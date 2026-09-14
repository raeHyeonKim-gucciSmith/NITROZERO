using UnityEngine;
using UnityEngine.Timeline;
namespace Nitrozero.Cinematics
{
[TrackColor(0.85f, 0.25f, 0.12f)]
[TrackClipType(typeof(DomeCinematicClip))]
[TrackBindingType(typeof(DomeCinematicSequence))]
public sealed class DomeCinematicTrack : TrackAsset { }
}
