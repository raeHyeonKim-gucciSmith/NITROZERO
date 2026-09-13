using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Splines;

/// <summary>Repeatable visual cruise pose. The vehicle root remains owned by the path.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public sealed class TrailerCruiseMotion : MonoBehaviour
{
    [Header("Rig (FL, FR, RL, RR)")]
    [SerializeField] private Transform bodyMotionRoot;
    [SerializeField] private Transform[] wheels = new Transform[4];

    [Header("Body motion speed")]
    [Tooltip("Controls suspension/vibration only. The spline owns vehicle movement.")]
    [Min(1f)] public float speedKph = 240f;

    [Header("Wheel rotation")]
    [Tooltip("Independent visual wheel speed. Try 0.1 for slow motion, up to 600 km/h.")]
    [Range(0f, 600f)] public float wheelSpeedKph = 240f;
    [Min(0.01f)] public float wheelRadius = 0.19f;
    public bool reverseWheelSpin;

    [Header("Front steering: handle degrees")]
    public float minHandleAngle = -360f;
    public float maxHandleAngle = 360f;
    [Tooltip("Actual front wheel angle for a 360-degree handle turn.")]
    [Range(0f, 60f)] public float wheelAngleAt360 = 30f;
    public float steeringWheelAngle;
    public TrailerSteeringSettings steering = new TrailerSteeringSettings();

    [Header("Spline automatic steering")]
    public bool automaticSteering = true;
    public SplineContainer steeringSpline;
    [Min(0)] public int steeringSplineIndex;
    [Tooltip("Enable for crossings or an external path controller. Otherwise uses the nearest point to the car.")]
    public bool useSplineProgress;
    [Range(0f, 1f)] public float splineProgress;
    [Tooltip("Metres ahead of the car to anticipate the bend.")]
    [Min(0f)] public float steeringLookAhead = 0.3f;
    [Tooltip("Spatial averaging distance in metres; independent of playback history.")]
    [Min(0.01f)] public float steeringSmoothingDistance = 1f;
    [Tooltip("0: measure front-to-rear wheel spacing automatically. Otherwise world metres.")]
    [Min(0f)] public float steeringWheelbase;
    [Range(0f, 2f)] public float automaticSteeringStrength = 1f;
    [Range(0f, 60f)] public float maxAutomaticWheelAngle = 30f;
    [Tooltip("0 = spline; 1 = manual handle angle. Used outside a Timeline clip.")]
    [Range(0f, 1f)] public float manualSteeringWeight;

    [Header("Body suspension (metres / degrees)")]
    [Range(0f, 2f)] public float suspensionIntensity = 0.3f;
    [Min(0f)] public float heaveAmplitude = 0.001f;
    [Min(0f)] public float pitchDegrees = 0.035f;
    [Min(0f)] public float rollDegrees = 0.025f;
    [Min(0.01f)] public float suspensionFrequency = 0.9f;

    [Header("High-frequency road vibration")]
    [Range(0f, 2f)] public float vibrationIntensity = 1f;
    [Min(0f)] public float vibrationAmplitude = 0.00025f;
    [Min(0f)] public float vibrationDegrees = 0.008f;
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
    private double lastCruiseTime;
    private double cruiseDistance;
    private bool steeringActive;
    private double steeringStartTime;
    private object timelineOwner;
    private double timelineTime, timelineDistance;
    private float timelineHandle, timelineManualWeight;

    public Transform BodyMotionRoot => bodyMotionRoot;
    public Transform[] Wheels => wheels;
    public float CurrentHandleAngle { get; private set; }
    public float CurrentFrontWheelAngle => CurrentHandleAngle / 360f * wheelAngleAt360;
    public bool IsTimelineControlled => timelineOwner != null;

    private void OnEnable()
    {
        startTime = Time.timeAsDouble;
        lastCruiseTime = 0;
        cruiseDistance = 0;
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
        if (timelineOwner != null)
        {
            // Re-sample after a path controller has moved the root during this frame.
            ApplyPose(timelineTime, timelineDistance, timelineHandle, timelineManualWeight);
            return;
        }
        if (steeringActive)
        {
            double elapsed = Time.timeAsDouble - steeringStartTime;
            steeringWheelAngle = steering.Sample(elapsed, minHandleAngle, maxHandleAngle);
            if (elapsed >= steering.Duration) steeringActive = false;
        }
        double time = useManualTime ? manualTime :
            shotDirector != null ? shotDirector.time : Time.timeAsDouble - startTime;
        if (useManualTime || shotDirector != null)
            EvaluateAtTime(time);
        else
        {
            // Integrate live Inspector changes without jumping the existing wheel phase.
            cruiseDistance += Math.Max(0, time - lastCruiseTime) * Mathf.Clamp(wheelSpeedKph, 0, 600) / 3.6;
            lastCruiseTime = time;
            ApplyPose(time, cruiseDistance, steeringWheelAngle);
        }
    }

    public void StartSteering()
    {
        if (!Application.isPlaying || IsTimelineControlled) return;
        steeringStartTime = Time.timeAsDouble;
        manualSteeringWeight = 1f;
        steeringActive = true;
        steeringWheelAngle = steering.Sample(0, minHandleAngle, maxHandleAngle);
    }

    public void StopSteering() => steeringActive = false;

    // Absolute sampling has no integration, startup settling or frame-rate-dependent randomness.
    public void EvaluateAtTime(double seconds)
    {
        ApplyPose(seconds, seconds * Mathf.Clamp(wheelSpeedKph, 0, 600) / 3.6, steeringWheelAngle);
    }

    public void ApplyTimelinePose(object owner, double seconds, double distance, float handleAngle, float manualWeight = 1f)
    {
        timelineOwner = owner;
        timelineTime = seconds;
        timelineDistance = distance;
        timelineHandle = handleAngle;
        timelineManualWeight = manualWeight;
        ApplyPose(seconds, distance, handleAngle, manualWeight);
    }

    public void ReleaseTimeline(object owner)
    {
        if (!ReferenceEquals(timelineOwner, owner)) return;
        timelineOwner = null;
        RestorePose();
        startTime = Time.timeAsDouble;
        lastCruiseTime = 0;
        cruiseDistance = 0;
    }

    private void ApplyPose(double seconds, double distance, float handleAngle, float manualWeight = -1f)
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

        double degrees = distance /
            (2.0 * Math.PI * Math.Max(0.01f, wheelRadius)) * 360.0;
        Quaternion spin = Quaternion.AngleAxis((float)(degrees % 360.0) *
            (reverseWheelSpin ? -1f : 1f), Vector3.right);
        CurrentHandleAngle = Mathf.Clamp(handleAngle, Mathf.Min(minHandleAngle, maxHandleAngle),
            Mathf.Max(minHandleAngle, maxHandleAngle));
        if (TryGetAutomaticWheelAngle(out float automaticAngle) && wheelAngleAt360 > 0.0001f)
        {
            float autoHandle = automaticAngle / wheelAngleAt360 * 360f;
            CurrentHandleAngle = Mathf.Lerp(Mathf.Clamp(autoHandle, Mathf.Min(minHandleAngle, maxHandleAngle),
                Mathf.Max(minHandleAngle, maxHandleAngle)), CurrentHandleAngle,
                Mathf.Clamp01(manualWeight < 0 ? manualSteeringWeight : manualWeight));
        }
        Quaternion steer = Quaternion.AngleAxis(CurrentFrontWheelAngle, Vector3.up);
        for (int i = 0; i < 4; i++)
        {
            if (wheels[i] == null) continue;
            // Flat-road contact: body moves over planted wheels, creating relative suspension travel.
            // Root-relative poses also work when a spline translates or rotates the vehicle.
            wheels[i].SetPositionAndRotation(transform.TransformPoint(wheelRootPositions[i]),
                transform.rotation * (i < 2 ? steer : Quaternion.identity) * spin * wheelRootRotations[i]);
        }
    }

    public bool TryGetAutomaticWheelAngle(out float angle)
    {
        angle = 0;
        if (!automaticSteering || steeringSpline == null || steeringSplineIndex < 0 ||
            steeringSplineIndex >= steeringSpline.Splines.Count) return false;
        var source = steeringSpline.Splines[steeringSplineIndex];
        if (source == null || source.Count < 2) return false;
        using (var spline = new NativeSpline(source, steeringSpline.transform.localToWorldMatrix))
        {
            float length = spline.GetLength();
            if (length < 0.001f) return false;
            float progress = Mathf.Clamp01(splineProgress);
            if (!useSplineProgress)
                SplineUtility.GetNearestPoint(spline, (Unity.Mathematics.float3)transform.position,
                    out _, out progress, 8, 3);
            float curvature = 0;
            float weightSum = 0;
            // Signed planar curvature = dot(up, tangent x acceleration) / |tangent|^3.
            // Average in space rather than over frames so Timeline seeking is repeatable.
            for (int i = -2; i <= 2; i++)
            {
                float t = progress + (Mathf.Max(0, steeringLookAhead) + i *
                    Mathf.Max(0.01f, steeringSmoothingDistance) * 0.25f) / length;
                t = source.Closed ? Mathf.Repeat(t, 1f) : Mathf.Clamp01(t);
                Vector3 tangent = Vector3.ProjectOnPlane((Vector3)spline.EvaluateTangent(t), transform.up);
                Vector3 acceleration = Vector3.ProjectOnPlane((Vector3)spline.EvaluateAcceleration(t), transform.up);
                float magnitude = tangent.magnitude;
                if (magnitude < 0.0001f) continue;
                float weight = 3 - Mathf.Abs(i);
                curvature += weight * Vector3.Dot(transform.up, Vector3.Cross(tangent, acceleration)) /
                    (magnitude * magnitude * magnitude);
                weightSum += weight;
            }
            if (weightSum == 0) return false;
            float wheelbase = steeringWheelbase;
            if (wheelbase <= 0 && initialized)
                wheelbase = Mathf.Abs(Vector3.Dot(transform.TransformVector((wheelRootPositions[0] +
                    wheelRootPositions[1] - wheelRootPositions[2] - wheelRootPositions[3]) * 0.5f), transform.forward));
            else if (wheelbase <= 0 && wheels != null && wheels.Length == 4 &&
                wheels[0] != null && wheels[1] != null && wheels[2] != null && wheels[3] != null)
                wheelbase = Mathf.Abs(Vector3.Dot((wheels[0].position + wheels[1].position -
                    wheels[2].position - wheels[3].position) * 0.5f, transform.forward));
            if (wheelbase <= 0.001f) return false;
            angle = Mathf.Atan(wheelbase * curvature / weightSum) * Mathf.Rad2Deg * automaticSteeringStrength;
            angle = Mathf.Clamp(angle, -maxAutomaticWheelAngle, maxAutomaticWheelAngle);
            return !float.IsNaN(angle) && !float.IsInfinity(angle);
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
        timelineOwner = null;
        steeringActive = false;
        RestorePose();
    }

    public void RestorePose()
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
