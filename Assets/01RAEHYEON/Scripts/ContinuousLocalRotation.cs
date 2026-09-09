using UnityEngine;

public sealed class ContinuousLocalRotation : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private Vector3 localAxis = Vector3.right;
    [SerializeField] private float degreesPerSecond = 180f;
    [SerializeField, Range(0f, 1f)] private float speedMultiplier = 1f;

    private Vector3 localPivot;

    private void Awake()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            localPivot = Vector3.zero;
            return;
        }

        Bounds visualBounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            visualBounds.Encapsulate(renderers[i].bounds);
        }

        localPivot = transform.InverseTransformPoint(visualBounds.center);
    }

    private void Update()
    {
        if (localAxis.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 worldPivot = transform.TransformPoint(localPivot);
        Vector3 worldAxis = transform.TransformDirection(localAxis.normalized);

        transform.RotateAround(
            worldPivot,
            worldAxis,
            degreesPerSecond * speedMultiplier * Time.deltaTime);
    }

    public void SetSpeedMultiplier(float value)
    {
        speedMultiplier = Mathf.Clamp01(value);
    }
}
