using System;
using Damin.VFX.TireSmoke.Progressive;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Splines;

/// <summary>
/// Drives the whole pack along one shared racing line.
/// Every car's pose is a pure function of shot time, so scrubbing a Timeline back
/// and forth lands on exactly the same frame - no accumulated state to drift.
/// Running order comes from the keyed gap curves; the weave, the lift-off when a
/// car closes on the one ahead, and the corner drift are layered on top.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(800)]
[ExecuteAlways]
public sealed class RaceChoreography : MonoBehaviour
{
    [Serializable]
    public sealed class Racer
    {
        public string label = "";
        public Transform car;

        [Tooltip("Metres behind the leader. Key this to choreograph the running order.")]
        public AnimationCurve gap = AnimationCurve.Constant(0f, 30f, 0f);

        [Tooltip("-1 hugs the left edge of the usable road, +1 the right edge.")]
        public AnimationCurve lane = AnimationCurve.Constant(0f, 30f, 0f);

        [Tooltip("Offsets this car's weave so the pack does not breathe in unison.")]
        public float weavePhase;

        public ProgressiveTireSmokeController smoke;

        [Tooltip("Owns the steering angle and the body sway. Fed this car's live speed.")]
        public TrailerCruiseMotion cruise;

        [Tooltip("Wheel spinners driven from this car's live speed.")]
        public ContinuousLocalRotation[] wheels = new ContinuousLocalRotation[0];

        [Tooltip("Metres from this car's pivot down to the bottom of its body. " +
                 "Measured per car at install, because every model pivots somewhere different.")]
        public float rideHeight;

        [NonSerialized] public float Distance;
        [NonSerialized] public float Lane;
        [NonSerialized] public float Lift;
        [NonSerialized] public float Drift;
        [NonSerialized] public float SpeedKph;
    }

    [Header("Path")]
    public SplineContainer racingLine;
    [Min(0)] public int splineIndex;

    [Header("Shot")]
    [Min(1f)] public float duration = 30f;
    [Min(1f)] public float baseSpeedKph = 220f;
    [Tooltip("Metres along the path where the leader starts, so the corner lands mid-shot.")]
    [Min(0f)] public float startDistance;
    [Tooltip("Multiplies the base speed over the shot. Lift it into the corner exit for punch.")]
    public AnimationCurve speedOverTime = AnimationCurve.Constant(0f, 30f, 1f);

    [Header("Road")]
    [Min(1f)] public float roadWidth = 30f;
    [Min(0f)] public float edgeMargin = 3f;
    [Tooltip("Static lift above the path. No raycast grounding.")]
    public float heightOffset;

    [Header("Jostle")]
    [Min(0f)] public float weaveMetres = 3.5f;
    [Min(0.1f)] public float weavePeriod = 4.5f;
    [Range(0f, 1f)] public float laneWeave = 0.18f;

    [Header("Near miss")]
    [Tooltip("A car lifts off when the one ahead is closer than this.")]
    [Min(0.1f)] public float safetyGap = 9f;
    [Tooltip("Only a threat when the two are within this much of the same lane.")]
    [Min(0.1f)] public float sameLaneWidth = 3.2f;
    [Range(0f, 1f)] public float liftStrength = 0.55f;
    [Tooltip("How far the lifting car also edges sideways to look for a way past.")]
    [Range(0f, 1f)] public float liftSwerve = 0.25f;

    [Header("Lane change steering")]
    [Tooltip("TrailerCruiseMotion only reads spline curvature, so sideways moves need their own steer.")]
    [Range(0f, 2f)] public float laneSteerGain = 1f;
    [Range(0f, 45f)] public float maxLaneSteerDegrees = 18f;

    [Header("Drift")]
    public Transform driftApex;
    [Range(0f, 60f)] public float driftAngle = 35f;
    public bool noseIntoCorner = true;
    [Tooltip("Metres either side of the apex at full drift.")]
    [Min(1f)] public float driftHold = 130f;
    [Tooltip("Metres over which the drift fades in and out past the hold.")]
    [Min(1f)] public float driftFade = 90f;

    [Header("Timeline")]
    public PlayableDirector director;
    public bool useManualTime;
    [Min(0f)] public float manualTime;

    [Header("Editor")]
    public bool previewInEditMode = true;

    public Racer[] racers = new Racer[0];

    private const int SpeedSamples = 256;
    private float[] m_Cumulative;
    private float m_TableDuration = -1f;
    private float m_TableSpeed = -1f;
    private float m_ApexDistance = -1f;

    private bool HasPath =>
        racingLine != null && racingLine.Splines != null &&
        splineIndex >= 0 && splineIndex < racingLine.Splines.Count &&
        racingLine.Splines[splineIndex].Count > 1;

    /// <summary>Usable half width, keeping the cars off the shoulders.</summary>
    public float HalfWidth => Mathf.Max(0f, roadWidth * 0.5f - edgeMargin);

    private void LateUpdate()
    {
        if (!Application.isPlaying && !previewInEditMode) return;
        Evaluate(CurrentTime());
    }

    private float CurrentTime()
    {
        if (useManualTime) return manualTime;
        if (director != null) return (float)director.time;
        return Application.isPlaying ? Time.time : manualTime;
    }

    /// <summary>Places every car for the given shot time.</summary>
    public void Evaluate(float time)
    {
        if (!HasPath || racers == null || racers.Length == 0) return;

        time = Mathf.Clamp(time, 0f, duration);
        float leader = LeaderDistance(time);
        float packSpeed = baseSpeedKph * Mathf.Max(0f, speedOverTime.Evaluate(time));
        float half = HalfWidth;

        // Pass 1 - the choreographed pack, before anyone reacts to anyone else.
        for (int i = 0; i < racers.Length; i++)
        {
            Racer r = racers[i];
            if (r == null) continue;

            float weave = weaveMetres * Mathf.Sin(Mathf.PI * 2f * (time / weavePeriod + r.weavePhase));
            r.Distance = leader + r.gap.Evaluate(time) + weave;

            r.Lane = LaneNormalized(r, time);
            r.Lift = 0f;
        }

        // Pass 2 - a car that has run up on the one ahead in its lane lifts off.
        for (int i = 0; i < racers.Length; i++)
        {
            Racer r = racers[i];
            if (r == null) continue;

            float closest = float.MaxValue;
            float aheadLane = r.Lane;

            for (int j = 0; j < racers.Length; j++)
            {
                if (i == j || racers[j] == null) continue;

                float ahead = racers[j].Distance - r.Distance;
                if (ahead <= 0f || ahead >= safetyGap) continue;
                if (Mathf.Abs((racers[j].Lane - r.Lane) * half) > sameLaneWidth) continue;

                if (ahead < closest) { closest = ahead; aheadLane = racers[j].Lane; }
            }

            if (closest < safetyGap)
                r.Lift = Mathf.Clamp01(1f - closest / safetyGap) * liftStrength;
        }

        // Pass 3 - apply the reaction, then drift and place.
        float apex = ApexDistance();

        for (int i = 0; i < racers.Length; i++)
        {
            Racer r = racers[i];
            if (r == null || r.car == null) continue;

            float distance = r.Distance - r.Lift * safetyGap * 0.35f;
            float lane = Mathf.Clamp(r.Lane + Mathf.Sign(r.Lane == 0f ? 1f : -r.Lane) * r.Lift * liftSwerve, -1f, 1f);

            r.Drift = apex >= 0f ? DriftAmount(distance, apex) : 0f;
            r.SpeedKph = packSpeed * (1f - r.Lift);

            Place(r, time, distance, lane, half);
            DriveCar(r);
        }
    }

    /// <summary>Choreographed lane before anyone reacts, so it can be sampled either side of now.</summary>
    private float LaneNormalized(Racer r, float time)
    {
        float wobble = laneWeave * Mathf.Sin(
            Mathf.PI * 2f * (time / (weavePeriod * 1.37f) + r.weavePhase * 0.5f));
        return Mathf.Clamp(r.lane.Evaluate(time) + wobble, -1f, 1f);
    }

    /// <summary>
    /// Degrees the nose turns into a lane change. The car's real velocity is the path
    /// tangent plus the sideways slide, so the heading follows that, not the tangent alone.
    /// </summary>
    private float LaneSteer(Racer r, float time, float half, float speedKph)
    {
        const float h = 0.08f;
        float lateral = (LaneNormalized(r, time + h) - LaneNormalized(r, time - h)) * half / (2f * h);
        float forward = Mathf.Max(1f, speedKph / 3.6f);

        return Mathf.Clamp(
            Mathf.Atan2(lateral, forward) * Mathf.Rad2Deg * laneSteerGain,
            -maxLaneSteerDegrees, maxLaneSteerDegrees);
    }

    private void Place(Racer r, float time, float distance, float lane, float half)
    {
        Spline spline = racingLine.Splines[splineIndex];
        float length = spline.GetLength();
        if (length <= 0.0001f) return;

        float t = spline.ConvertIndexUnit(
            Mathf.Repeat(distance, length), PathIndexUnit.Distance, PathIndexUnit.Normalized);

        if (!racingLine.Evaluate(splineIndex, t, out float3 p, out float3 tangent, out float3 upVector))
            return;

        Vector3 up = ((Vector3)upVector).normalized;
        if (up.sqrMagnitude < 1e-6f) up = Vector3.up;

        Vector3 forward = ((Vector3)tangent).normalized;
        if (forward.sqrMagnitude < 1e-6f) forward = r.car.forward;

        Vector3 right = Vector3.Cross(up, forward).normalized;
        // The spline runs along the road surface, so every car is lifted by its own
        // pivot-to-floor distance or it ends up buried.
        Vector3 position = (Vector3)p + right * (lane * half) + up * (heightOffset + r.rideHeight);

        float driftYaw = (noseIntoCorner ? -1f : 1f) * driftAngle * r.Drift;
        float laneYaw = LaneSteer(r, time, half, r.SpeedKph);
        Quaternion rotation = Quaternion.LookRotation(forward, up) * Quaternion.Euler(0f, driftYaw + laneYaw, 0f);

        r.car.SetPositionAndRotation(position, rotation);
    }

    /// <summary>Full drift across the hold either side of the apex, easing out over the fade.</summary>
    private float DriftAmount(float distance, float apex)
    {
        float d = Mathf.Abs(distance - apex);
        if (d <= driftHold) return 1f;
        if (d >= driftHold + driftFade) return 0f;
        return 1f - Mathf.SmoothStep(0f, 1f, (d - driftHold) / driftFade);
    }

    /// <summary>Feeds this car's live speed and drift to the parts that already exist on it.</summary>
    private void DriveCar(Racer r)
    {
        if (r.smoke != null)
        {
            r.smoke.smokePower = Mathf.Clamp01(r.Drift + r.Lift * 0.3f);
            r.smoke.vehicleSpeed = r.SpeedKph;
        }

        // TrailerCruiseMotion owns the steering angle and the body sway; it only needs
        // to be told how fast this car is actually going right now.
        if (r.cruise != null)
        {
            r.cruise.speedKph = Mathf.Max(1f, r.SpeedKph);
            r.cruise.wheelSpeedKph = Mathf.Max(0f, r.SpeedKph);
        }

        if (r.wheels == null) return;
        float multiplier = Mathf.Clamp01(r.SpeedKph / Mathf.Max(1f, baseSpeedKph));
        foreach (ContinuousLocalRotation wheel in r.wheels)
            if (wheel != null) wheel.SetSpeedMultiplier(multiplier);
    }

    /// <summary>Distance along the path of the point nearest the apex marker.</summary>
    private float ApexDistance()
    {
        if (driftApex == null) return -1f;
        if (m_ApexDistance >= 0f) return m_ApexDistance;

        Spline spline = racingLine.Splines[splineIndex];
        Vector3 local = racingLine.transform.InverseTransformPoint(driftApex.position);

        SplineUtility.GetNearestPoint(spline, (float3)local, out _, out float t);
        m_ApexDistance = spline.ConvertIndexUnit(t, PathIndexUnit.Normalized, PathIndexUnit.Distance);
        return m_ApexDistance;
    }

    /// <summary>
    /// Metres covered by the leader at this time. The speed curve is integrated once
    /// into a table so the answer stays a pure function of time.
    /// </summary>
    private float LeaderDistance(float time)
    {
        if (m_Cumulative == null || m_TableDuration != duration || m_TableSpeed != baseSpeedKph)
            BuildSpeedTable();

        float u = Mathf.Clamp01(time / duration) * SpeedSamples;
        int i = Mathf.Clamp(Mathf.FloorToInt(u), 0, SpeedSamples - 1);
        return startDistance + Mathf.Lerp(m_Cumulative[i], m_Cumulative[i + 1], u - i);
    }

    private void BuildSpeedTable()
    {
        m_Cumulative = new float[SpeedSamples + 1];
        m_TableDuration = duration;
        m_TableSpeed = baseSpeedKph;

        float dt = duration / SpeedSamples;
        float baseMs = baseSpeedKph / 3.6f;
        float acc = 0f;

        for (int i = 1; i <= SpeedSamples; i++)
        {
            float v0 = baseMs * Mathf.Max(0f, speedOverTime.Evaluate((i - 1) * dt));
            float v1 = baseMs * Mathf.Max(0f, speedOverTime.Evaluate(i * dt));
            acc += (v0 + v1) * 0.5f * dt;
            m_Cumulative[i] = acc;
        }
    }

    /// <summary>Drops the cached apex projection so a moved marker is picked up.</summary>
    public void InvalidateCache()
    {
        m_ApexDistance = -1f;
        m_Cumulative = null;
    }

    private void OnValidate() => InvalidateCache();
}
