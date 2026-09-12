using System;
using UnityEngine;

public enum TrailerSteeringMode { Normal, DriftCounter, Recovery }

[Serializable]
public sealed class TrailerSteeringSettings
{
    public TrailerSteeringMode mode;
    public float startHandleAngle;
    public float targetHandleAngle;
    public bool overrideDuration;
    [Min(0.01f)] public float customDuration = 0.8f;
    public AnimationCurve easing = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public float Duration => overrideDuration ? Mathf.Max(0.01f, customDuration) :
        mode == TrailerSteeringMode.DriftCounter ? 0.35f :
        mode == TrailerSteeringMode.Recovery ? 1f : 0.8f;

    public float Sample(double time, float minAngle, float maxAngle)
    {
        float lo = Mathf.Min(minAngle, maxAngle);
        float hi = Mathf.Max(minAngle, maxAngle);
        float progress = Mathf.Clamp01((float)(time / Duration));
        float eased = progress <= 0 ? 0 : progress >= 1 ? 1 :
            easing != null && easing.length > 0 ? Mathf.Clamp01(easing.Evaluate(progress)) :
            Mathf.SmoothStep(0, 1, progress);
        return Mathf.Lerp(Mathf.Clamp(startHandleAngle, lo, hi),
            Mathf.Clamp(targetHandleAngle, lo, hi), eased);
    }
}
