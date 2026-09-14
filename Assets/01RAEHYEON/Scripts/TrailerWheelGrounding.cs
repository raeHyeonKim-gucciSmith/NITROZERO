using UnityEngine;

/// <summary>
/// Plants a cruise vehicle on the road or terrain underneath it.
/// The path owns this transform; only the dedicated ground pivot is written here.
/// Probes live outside the spinning wheel hierarchy, so wheel rotation never reaches the query.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(500)]
public sealed class TrailerWheelGrounding : MonoBehaviour
{
    [Header("Rig (FL, FR, RL, RR)")]
    [Tooltip("Child pivot that carries the correction. The vehicle root must be its child.")]
    [SerializeField] private Transform groundPivot;
    [Tooltip("Cast origins at the wheel centres. Must never be children of a rolling or body-motion transform.")]
    [SerializeField] private Transform[] probes = new Transform[4];
    [Tooltip("World-space tyre radius per corner, copied from TrailerCruiseMotion.")]
    [SerializeField] private float[] wheelRadii = new float[4];

    [Header("Surface query")]
    [Tooltip("Leave as Everything: hits on this vehicle are rejected by ownership, not by layer.")]
    public LayerMask groundMask = ~0;
    [Tooltip("Metres above the wheel centre where the downward cast starts.")]
    [Min(0f)] public float castStartHeight = 3f;
    [Min(0.1f)] public float castDistance = 12f;
    [Tooltip("Sphere radius as a fraction of the tyre radius. Smooths terrain triangle edges.")]
    [Range(0.05f, 1f)] public float probeRadiusScale = 0.3f;

    [Header("Response")]
    public bool applyTilt = true;
    [Range(0f, 30f)] public float maxTiltDegrees = 12f;
    [Min(0f)] public float heightSmoothTime = 0.08f;
    [Min(0f)] public float tiltSmoothTime = 0.12f;
    [Tooltip("Snap to the surface instead of easing on the first evaluated frame.")]
    public bool snapOnEnable = true;

    private static readonly RaycastHit[] HitBuffer = new RaycastHit[16];

    private readonly float[] corner = new float[4];
    private Vector3 pivotLocal;
    private float wheelbase, track;
    private float height, heightVelocity;
    private float pitch, pitchVelocity;
    private float roll, rollVelocity;
    private bool measured, settled;

    public bool IsGrounded { get; private set; }

    /// <summary>Called by the installer so the rig never depends on inspector wiring.</summary>
    public void ConfigureRig(Transform pivot, Transform[] castOrigins, float[] radii)
    {
        groundPivot = pivot;
        probes = castOrigins;
        wheelRadii = radii;
        measured = false;
    }

    private void OnEnable()
    {
        settled = !snapOnEnable;
        heightVelocity = pitchVelocity = rollVelocity = 0f;
        Measure();
    }

    private bool Measure()
    {
        if (measured) return true;
        if (groundPivot == null || groundPivot.parent != transform ||
            probes == null || probes.Length != 4 || wheelRadii == null || wheelRadii.Length != 4)
        {
            Debug.LogError("TrailerWheelGrounding: assign the ground pivot, four probes and four radii.", this);
            return false;
        }
        for (int i = 0; i < 4; i++)
            if (probes[i] == null || probes[i].IsChildOf(groundPivot)) return false;

        // Probe placement is authored once; the spline only ever moves this root.
        Vector3 fl = probes[0].localPosition, fr = probes[1].localPosition;
        Vector3 rl = probes[2].localPosition, rr = probes[3].localPosition;
        pivotLocal = (fl + fr + rl + rr) * 0.25f;
        wheelbase = Mathf.Max(0.01f, Mathf.Abs((fl.z + fr.z) * 0.5f - (rl.z + rr.z) * 0.5f));
        track = Mathf.Max(0.01f, Mathf.Abs((fr.x + rr.x) * 0.5f - (fl.x + rl.x) * 0.5f));
        measured = true;
        return true;
    }

    private void LateUpdate()
    {
        if (!Measure()) return;

        int found = 0;
        for (int i = 0; i < 4; i++)
        {
            float radius = Mathf.Max(0.01f, wheelRadii[i]);
            Vector3 origin = probes[i].position + Vector3.up * castStartHeight;
            if (TrySurfaceHeight(origin, radius * probeRadiusScale, out float surfaceY))
            {
                // The probe sits at the rest wheel centre, so this is the correction that corner needs.
                corner[i] = surfaceY + radius - probes[i].position.y;
                found++;
            }
            // A miss keeps the previous correction for that corner rather than dropping the car.
        }
        IsGrounded = found > 0;
        if (found == 0) return;

        float front = (corner[0] + corner[1]) * 0.5f;
        float rear = (corner[2] + corner[3]) * 0.5f;
        float left = (corner[0] + corner[2]) * 0.5f;
        float right = (corner[1] + corner[3]) * 0.5f;

        float targetHeight = (corner[0] + corner[1] + corner[2] + corner[3]) * 0.25f;
        float targetPitch = 0f, targetRoll = 0f;
        if (applyTilt)
        {
            // Nose up is a negative X rotation; right side up is a positive Z rotation.
            targetPitch = Mathf.Clamp(-Mathf.Atan2(front - rear, wheelbase) * Mathf.Rad2Deg,
                -maxTiltDegrees, maxTiltDegrees);
            targetRoll = Mathf.Clamp(-Mathf.Atan2(left - right, track) * Mathf.Rad2Deg,
                -maxTiltDegrees, maxTiltDegrees);
        }

        if (!settled)
        {
            height = targetHeight;
            pitch = targetPitch;
            roll = targetRoll;
            settled = true;
        }
        else
        {
            height = Mathf.SmoothDamp(height, targetHeight, ref heightVelocity, heightSmoothTime);
            pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, tiltSmoothTime);
            roll = Mathf.SmoothDamp(roll, targetRoll, ref rollVelocity, tiltSmoothTime);
        }

        // Rotate about the wheel centre so the tyres stay put while the chassis levels.
        Quaternion rotation = Quaternion.Euler(pitch, 0f, roll);
        groundPivot.SetLocalPositionAndRotation(
            new Vector3(0f, height, 0f) + pivotLocal - rotation * pivotLocal, rotation);
    }

    private bool TrySurfaceHeight(Vector3 origin, float radius, out float surfaceY)
    {
        surfaceY = 0f;
        int count = Physics.SphereCastNonAlloc(origin, Mathf.Max(0.01f, radius), Vector3.down,
            HitBuffer, castDistance, groundMask, QueryTriggerInteraction.Ignore);
        bool found = false;
        float best = float.NegativeInfinity;
        for (int i = 0; i < count; i++)
        {
            // Zero distance means the sphere started inside a collider; its point is meaningless.
            if (HitBuffer[i].distance <= 0f) continue;
            Collider collider = HitBuffer[i].collider;
            if (collider == null || collider.transform.IsChildOf(transform)) continue;
            float y = HitBuffer[i].point.y;
            if (y > best) { best = y; found = true; }
        }
        if (found) surfaceY = best;
        return found;
    }

    private void OnDisable()
    {
        if (groundPivot != null)
            groundPivot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        measured = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (probes == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < probes.Length; i++)
        {
            if (probes[i] == null) continue;
            float radius = wheelRadii != null && wheelRadii.Length == probes.Length ? wheelRadii[i] : 0.5f;
            Vector3 origin = probes[i].position + Vector3.up * castStartHeight;
            Gizmos.DrawLine(origin, origin + Vector3.down * castDistance);
            Gizmos.DrawWireSphere(probes[i].position, radius);
        }
    }
}
