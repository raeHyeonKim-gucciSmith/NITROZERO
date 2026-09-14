using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Lets Timeline animate a simple float instead of driving a rig constraint
/// through a second Animator that is outside the driver's rig stream.
/// </summary>
[DefaultExecutionOrder(-350)]
public sealed class DriverHeadAimBlend : MonoBehaviour
{
    [SerializeField] private MultiAimConstraint headAim;
    [SerializeField, Range(0f, 1f)] private float headAimBlend;

    private void Update()
    {
        if (!Application.isPlaying || headAim == null)
        {
            return;
        }

        headAim.weight = Mathf.Clamp01(headAimBlend);
    }
}
