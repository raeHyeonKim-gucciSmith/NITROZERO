using System.Collections.Generic;
using UnityEngine;

public sealed class BoosterPetalPulse : MonoBehaviour
{
    [SerializeField] private BoosterDeploymentController deployment;
    [SerializeField] private float maximumOpenAngle = 18f;
    [SerializeField, Range(0f, 1f)] private float openingStart = 0.72f;
    [SerializeField, Range(0f, 1f)] private float fullyOpenAt = 0.84f;
    [SerializeField, Range(0f, 1f)] private float closingStart = 0.88f;
    [SerializeField, Range(0f, 1f)] private float closedAgainAt = 1f;

    private readonly List<Transform> petalPivots = new List<Transform>();
    private readonly List<Quaternion> closedRotations = new List<Quaternion>();

    private void Awake()
    {
        if (deployment == null)
            deployment = GetComponentInParent<BoosterDeploymentController>();

        petalPivots.Clear();
        closedRotations.Clear();

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (!child.name.StartsWith("Nozzle_PetalPivot"))
                continue;

            petalPivots.Add(child);
            closedRotations.Add(child.localRotation);
        }
    }

    private void LateUpdate()
    {
        if (deployment == null)
            return;

        float amount = deployment.DeployAmount;
        float opening = SmoothRange(openingStart, fullyOpenAt, amount);
        float closing = 1f - SmoothRange(closingStart, closedAgainAt, amount);
        float pulse = Mathf.Min(opening, closing);
        Quaternion flare = Quaternion.Euler(maximumOpenAngle * pulse, 0f, 0f);

        for (int i = 0; i < petalPivots.Count; i++)
            petalPivots[i].localRotation = closedRotations[i] * flare;
    }

    private static float SmoothRange(float start, float end, float value)
    {
        float t = Mathf.InverseLerp(start, end, value);
        return Mathf.SmoothStep(0f, 1f, t);
    }
}
