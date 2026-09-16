using UnityEngine;

/// <summary>
/// Fine idle vibration for a vehicle object — the tremble of a running engine,
/// not a shake effect.
///
/// Additive by design: every LateUpdate it first removes the offset it added last
/// frame, then applies a fresh one. That means it never accumulates drift and it
/// coexists with anything else driving the same Transform (TrailerCruiseMotion,
/// Timeline, an Animator). Scripts that overwrite localPosition outright would
/// fight each other; this one only ever adds a delta on top of whatever is there.
///
/// Two layers are summed:
///   - Engine:  ~24Hz, very small. The buzz itself.
///   - Chassis: ~3Hz, slightly larger. The body settling on its springs.
/// The slow layer is what stops it reading as a buzzing prop.
///
/// Defaults are deliberately subtle and tuned for this project's cars (root scale ~3.6).
/// </summary>
[AddComponentMenu("NITROZERO/Vehicle Idle Shake")]
[DisallowMultipleComponent]
[ExecuteAlways]
public class VehicleIdleShake : MonoBehaviour
{
    [Tooltip("Master multiplier. 0 turns it off, 1 is the tuned default. " +
             "Start here before touching anything else.")]
    [Range(0f, 3f)]
    public float Intensity = 1f;

    [Header("Engine — high frequency buzz")]
    [Tooltip("Cycles per second. A running idle sits near 22-26Hz. Above ~28 it " +
             "aliases against the frame rate and turns into random jitter.")]
    [Range(1f, 32f)]
    public float EngineFrequency = 24f;

    [Tooltip("Positional tremble in world units. Keep this tiny.")]
    [Range(0f, 0.05f)]
    public float EnginePositionAmplitude = 0.004f;

    [Tooltip("Rotational tremble in degrees. The eye reads rotation far more than position.")]
    [Range(0f, 0.3f)]
    public float EngineRotationAmplitude = 0.02f;

    [Header("Chassis — slow body settle")]
    [Range(0.1f, 12f)]
    public float ChassisFrequency = 3.2f;

    [Range(0f, 0.05f)]
    public float ChassisPositionAmplitude = 0.006f;

    [Range(0f, 0.3f)]
    public float ChassisRotationAmplitude = 0.018f;

    [Header("Axis weighting")]
    [Tooltip("Per-axis scale of the positional tremble in local space " +
             "(x = side to side, y = up/down, z = forward/back).")]
    public Vector3 PositionWeight = new Vector3(0.4f, 1f, 0.3f);

    [Tooltip("Per-axis scale of the rotational tremble (x = pitch, y = yaw, z = roll).")]
    public Vector3 RotationWeight = new Vector3(1f, 0.35f, 0.8f);

    [Header("Misc")]
    [Tooltip("Different values per vehicle so several cars do not tremble in lockstep.")]
    public float Seed = 0f;

    [Tooltip("Also run while the editor is not playing. Off by default so it does not " +
             "nudge the object while you are placing things.")]
    public bool PreviewInEditMode = false;

    Vector3 m_AppliedPosition;
    Quaternion m_AppliedRotation = Quaternion.identity;
    bool m_HasApplied;

    static float Noise(float row, float t)
    {
        // Perlin returns 0..1 and is flat along integer rows, so each axis is sampled
        // on its own fractional row to keep them uncorrelated.
        return Mathf.PerlinNoise(row, t) * 2f - 1f;
    }

    void OnDisable()
    {
        Revert();
    }

    void OnDestroy()
    {
        Revert();
    }

    /// <summary>Removes whatever offset is currently applied, leaving the Transform clean.</summary>
    void Revert()
    {
        if (!m_HasApplied)
            return;
        transform.localPosition -= m_AppliedPosition;
        transform.localRotation = transform.localRotation * Quaternion.Inverse(m_AppliedRotation);
        m_AppliedPosition = Vector3.zero;
        m_AppliedRotation = Quaternion.identity;
        m_HasApplied = false;
    }

    void LateUpdate()
    {
        // Always undo first: the base pose may have been moved by another script this
        // frame, and we must not bake our own offset into it.
        Revert();

        if (Intensity <= 0f)
            return;
        if (!Application.isPlaying && !PreviewInEditMode)
            return;

        float t = Application.isPlaying
            ? Time.time
            : (float)UnityEditor_TimeSinceStartup();

        float s = Seed * 41.27f;
        float te = t * EngineFrequency;
        float tc = t * ChassisFrequency;

        Vector3 pos =
            new Vector3(Noise(s + 0.19f, te), Noise(s + 4.37f, te), Noise(s + 8.51f, te))
                * EnginePositionAmplitude
            + new Vector3(Noise(s + 12.73f, tc), Noise(s + 16.91f, tc), Noise(s + 21.07f, tc))
                * ChassisPositionAmplitude;

        Vector3 rot =
            new Vector3(Noise(s + 25.33f, te), Noise(s + 29.61f, te), Noise(s + 33.89f, te))
                * EngineRotationAmplitude
            + new Vector3(Noise(s + 38.17f, tc), Noise(s + 42.43f, tc), Noise(s + 46.79f, tc))
                * ChassisRotationAmplitude;

        m_AppliedPosition = Vector3.Scale(pos, PositionWeight) * Intensity;
        m_AppliedRotation = Quaternion.Euler(Vector3.Scale(rot, RotationWeight) * Intensity);

        transform.localPosition += m_AppliedPosition;
        transform.localRotation = transform.localRotation * m_AppliedRotation;
        m_HasApplied = true;
    }

    static double UnityEditor_TimeSinceStartup()
    {
#if UNITY_EDITOR
        return UnityEditor.EditorApplication.timeSinceStartup;
#else
        return Time.timeAsDouble;
#endif
    }
}
