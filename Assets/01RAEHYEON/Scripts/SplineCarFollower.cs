using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Moves a vehicle root along a spline. The spline owns position and heading;
/// TrailerCruiseMotion keeps owning body sway and the steering wheel angle.
/// Runs before TrailerCruiseMotion so its steering read sees the placed position.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[DefaultExecutionOrder(900)]
public sealed class SplineCarFollower : MonoBehaviour
{
    [Header("Path")]
    public SplineContainer spline;
    [Min(0)] public int splineIndex;

    [Header("Motion")]
    [Min(0f)] public float speedKph = 240f;
    [Tooltip("Metres along the path at start. Use it to stagger cars front to back.")]
    public float startDistance;
    [Tooltip("Wrap back to the start when the end is reached. Off clamps at the end.")]
    public bool loop = true;

    [Header("Placement")]
    [Tooltip("Static lift above the path, in metres. No raycast grounding.")]
    public float heightOffset;
    public bool alignToPath = true;

    [Header("Timeline")]
    [Tooltip("Drive the car from 'progress' instead of speed, so Timeline or Animation can key it.")]
    public bool useManualProgress;
    [Range(0f, 1f)] public float progress;

    [Header("Editor")]
    [Tooltip("Snap the car onto the path in the scene view while the handles are moved.")]
    public bool previewInEditMode = true;

    private float m_Distance;

    private bool HasPath =>
        spline != null && spline.Splines != null &&
        splineIndex >= 0 && splineIndex < spline.Splines.Count &&
        spline.Splines[splineIndex].Count > 1;

    /// <summary>Path length in metres. The container is expected to stay unscaled.</summary>
    public float PathLength => HasPath ? spline.Splines[splineIndex].GetLength() : 0f;

    private void OnEnable()
    {
        m_Distance = startDistance;
        Place();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            if (!previewInEditMode) return;
            m_Distance = startDistance;
            Place();
            return;
        }

        if (!useManualProgress) m_Distance += speedKph / 3.6f * Time.deltaTime;
        Place();
    }

    /// <summary>Places the car at the current distance or progress along the path.</summary>
    public void Place()
    {
        if (!HasPath) return;

        float length = PathLength;
        if (length <= 0.0001f) return;

        float distance = useManualProgress ? progress * length : m_Distance;
        distance = loop
            ? Mathf.Repeat(distance, length)
            : Mathf.Clamp(distance, 0f, length);
        if (!useManualProgress) m_Distance = distance;

        float t = spline.Splines[splineIndex]
            .ConvertIndexUnit(distance, PathIndexUnit.Distance, PathIndexUnit.Normalized);

        if (!spline.Evaluate(splineIndex, t, out float3 p, out float3 tangent, out float3 upVector))
            return;

        Vector3 up = ((Vector3)upVector).normalized;
        if (up.sqrMagnitude < 1e-6f) up = Vector3.up;

        Vector3 position = (Vector3)p + up * heightOffset;
        Quaternion rotation = transform.rotation;

        if (alignToPath)
        {
            Vector3 forward = ((Vector3)tangent).normalized;
            if (forward.sqrMagnitude > 1e-6f) rotation = Quaternion.LookRotation(forward, up);
        }

        transform.SetPositionAndRotation(position, rotation);
    }
}
