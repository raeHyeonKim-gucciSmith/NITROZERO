using System;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>Repeatable visual cruise pose. The vehicle root remains owned by the path.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public sealed class TrailerCruiseMotion : MonoBehaviour
{
    [Header("Rig (FL, FR, RL, RR)")]
    [SerializeField] private Transform bodyMotionRoot;
    [SerializeField] private Transform[] wheels = new Transform[4];

    [Header("Constant high-speed cruise")]
    [Tooltip("Visual speed only. Does not translate the vehicle; the spline owns movement.")]
    [Min(1f)] public float speedKph = 240f;
    [Min(0.01f)] public float wheelRadius = 0.19f;
    public bool reverseWheelSpin;

    [Header("Body suspension (metres / degrees)")]
    [Range(0f, 2f)] public float suspensionIntensity = 1f;
    [Min(0f)] public float heaveAmplitude = 0.006f;
    [Min(0f)] public float pitchDegrees = 0.24f;
    [Min(0f)] public float rollDegrees = 0.18f;
    [Min(0.01f)] public float suspensionFrequency = 1.65f;

    [Header("High-frequency road vibration")]
    [Range(0f, 2f)] public float vibrationIntensity = 1f;
    [Min(0f)] public float vibrationAmplitude = 0.0008f;
    [Min(0f)] public float vibrationDegrees = 0.035f;
    [Min(0.01f)] public float vibrationFrequency = 11f;

    [Header("Repeatable shot timing")]
    [Tooltip("Optional shot Timeline. Its absolute time drives the pose during Play Mode.")]
    public PlayableDirector shotDirector;
    public bool useManualTime;
    [Min(0f)] public float manualTime;
    public float timeOffset;
    public int motionSeed = 17;

    private Vector3[] wheelLocalPositions;
    private Quaternion[] wheelLocalRotations;
    private Vector3[] wheelRootPositions;
    private Quaternion[] wheelRootRotations;
    private double startTime;
    private bool initialized;

    private void OnEnable()
    {
        startTime = Time.timeAsDouble;
        if (InitializeRig()) EvaluateAtTime(0);
    }

    private bool InitializeRig()
    {
        if (initialized) return true;
        if (bodyMotionRoot == null || bodyMotionRoot.parent != transform ||
            wheels == null || wheels.Length != 4)
        {
            Debug.LogError("TrailerCruiseMotion: assign the dedicated body root and four wheels.", this);
            return false;
        }
        for (int i = 0; i < wheels.Length; i++)
            if (wheels[i] == null || !wheels[i].IsChildOf(bodyMotionRoot)) return false;

        bodyMotionRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        wheelLocalPositions = new Vector3[4];
        wheelLocalRotations = new Quaternion[4];
        wheelRootPositions = new Vector3[4];
        wheelRootRotations = new Quaternion[4];
        for (int i = 0; i < 4; i++)
        {
            wheelLocalPositions[i] = wheels[i].localPosition;
            wheelLocalRotations[i] = wheels[i].localRotation;
            wheelRootPositions[i] = transform.InverseTransformPoint(wheels[i].position);
            wheelRootRotations[i] = Quaternion.Inverse(transform.rotation) * wheels[i].rotation;
        }
        initialized = true;
        return true;
    }

    private void LateUpdate()
    {
        double time = useManualTime ? manualTime :
            shotDirector != null ? shotDirector.time : Time.timeAsDouble - startTime;
        EvaluateAtTime(time);
    }

    // Absolute sampling has no integration, startup settling or frame-rate-dependent randomness.
    public void EvaluateAtTime(double seconds)
    {
        if (!InitializeRig()) return;
        double t = seconds + timeOffset;
        float speedRatio = Mathf.Clamp(Mathf.Max(1f, speedKph) / 240f, 0.25f, 2f);
        double slow = t * Mathf.Max(0.01f, suspensionFrequency) * Mathf.Sqrt(speedRatio);
        double fast = t * Mathf.Max(0.01f, vibrationFrequency) * Mathf.Sqrt(speedRatio);
        float suspension = Mathf.Max(0f, suspensionIntensity);
        float vibration = Mathf.Max(0f, vibrationIntensity) * Mathf.Sqrt(speedRatio);
        float heave = Wave(slow, 0.1) * heaveAmplitude * suspension;
        float pitch = Wave(slow * 0.87, 1.7) * pitchDegrees * suspension;
        float roll = Wave(slow * 0.71, 3.2) * rollDegrees * suspension;
        Vector3 position = new Vector3(
            Wave(fast * 0.91, 4.1) * vibrationAmplitude * 0.3f * vibration,
            heave + Wave(fast, 5.3) * vibrationAmplitude * vibration, 0f);
        Quaternion rotation = Quaternion.Euler(
            pitch + Wave(fast * 0.83, 2.2) * vibrationDegrees * vibration,
            0f, roll + Wave(fast * 0.77, 6.4) * vibrationDegrees * vibration);
        bodyMotionRoot.SetLocalPositionAndRotation(position, rotation);

        double degrees = t * (Math.Max(1f, speedKph) / 3.6) /
            (2.0 * Math.PI * Math.Max(0.01f, wheelRadius)) * 360.0;
        Quaternion spin = Quaternion.AngleAxis((float)(degrees % 360.0) *
            (reverseWheelSpin ? -1f : 1f), Vector3.right);
        for (int i = 0; i < 4; i++)
        {
            if (wheels[i] == null) continue;
            // Flat-road contact: body moves over planted wheels, creating relative suspension travel.
            // Root-relative poses also work when a spline translates or rotates the vehicle.
            wheels[i].SetPositionAndRotation(transform.TransformPoint(wheelRootPositions[i]),
                transform.rotation * spin * wheelRootRotations[i]);
        }
    }

    private float Wave(double cycles, double phase)
    {
        double seed = motionSeed * 0.61803398875;
        return (float)(0.62 * Math.Sin(cycles * 2.0 * Math.PI + phase + seed) +
            0.26 * Math.Sin(cycles * 2.0 * Math.PI * 1.731 + phase * 2.3 + seed) +
            0.12 * Math.Sin(cycles * 2.0 * Math.PI * 2.417 + phase * 0.7 + seed));
    }

    private void OnDisable()
    {
        if (!initialized) return;
        if (bodyMotionRoot != null)
            bodyMotionRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        for (int i = 0; i < 4; i++)
            if (wheels[i] != null)
                wheels[i].SetLocalPositionAndRotation(wheelLocalPositions[i], wheelLocalRotations[i]);
        initialized = false;
    }
}
