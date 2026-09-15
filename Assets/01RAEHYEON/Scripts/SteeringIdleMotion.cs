using UnityEngine;

/// <summary>
/// Adds a small procedural steering correction on top of the current pose.
/// Animate Timeline Steering Override to 1 while a Timeline owns the wheel.
/// </summary>
[DefaultExecutionOrder(500)]
public sealed class SteeringIdleMotion : MonoBehaviour
{
    [Header("Idle steering")]
    [SerializeField] private Vector3 localRotationAxis = Vector3.up;
    [SerializeField, Min(0f)] private float maximumAngle = 1.8f;
    [SerializeField, Min(0.05f)] private float secondsPerSide = 0.38f;
    [SerializeField, Range(0f, 1f)] private float irregularity = 0.45f;

    [Header("Timeline")]
    [Tooltip("0: idle steering, 1: Timeline has full control")]
    [SerializeField, Range(0f, 1f)] private float timelineSteeringOverride;
    [SerializeField, Min(0.01f)] private float stopBlendTime = 0.15f;
    [SerializeField, Min(0.01f)] private float resumeBlendTime = 0.3f;

    private Quaternion lastOutputRotation;
    private Quaternion lastProceduralOffset = Quaternion.identity;
    private float proceduralWeight;
    private float phase;
    private bool hasOutput;

    public float TimelineSteeringOverride
    {
        get => timelineSteeringOverride;
        set => timelineSteeringOverride = Mathf.Clamp01(value);
    }

    private void OnEnable()
    {
        lastOutputRotation = transform.localRotation;
        lastProceduralOffset = Quaternion.identity;
        proceduralWeight = 1f - timelineSteeringOverride;
        phase = 0f;
        hasOutput = false;
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || localRotationAxis.sqrMagnitude < 0.000001f)
            return;

        Quaternion current = transform.localRotation;
        Quaternion baseRotation;

        // If Animator/Timeline wrote a new rotation this frame, use that as the
        // base. Otherwise remove only the offset applied by this component.
        if (hasOutput && Quaternion.Angle(current, lastOutputRotation) < 0.001f)
            baseRotation = current * Quaternion.Inverse(lastProceduralOffset);
        else
            baseRotation = current;

        float targetWeight = 1f - timelineSteeringOverride;
        float blendTime = targetWeight < proceduralWeight ? stopBlendTime : resumeBlendTime;
        proceduralWeight = Mathf.MoveTowards(proceduralWeight, targetWeight, Time.deltaTime / blendTime);

        float cycleSeconds = Mathf.Max(0.1f, secondsPerSide * 4f);
        phase += Time.deltaTime * Mathf.PI * 2f / cycleSeconds;
        float mainWave = Mathf.Sin(phase);
        float secondaryWave = Mathf.Sin(phase * 2.17f + 1.31f);
        float angle = maximumAngle * proceduralWeight *
            Mathf.Lerp(mainWave, mainWave * 0.8f + secondaryWave * 0.2f, irregularity);

        lastProceduralOffset = Quaternion.AngleAxis(angle, localRotationAxis.normalized);
        lastOutputRotation = baseRotation * lastProceduralOffset;
        transform.localRotation = lastOutputRotation;
        hasOutput = true;
    }

    private void OnDisable()
    {
        if (Application.isPlaying && hasOutput && Quaternion.Angle(transform.localRotation, lastOutputRotation) < 0.001f)
            transform.localRotation *= Quaternion.Inverse(lastProceduralOffset);
        hasOutput = false;
        lastProceduralOffset = Quaternion.identity;
    }
}
