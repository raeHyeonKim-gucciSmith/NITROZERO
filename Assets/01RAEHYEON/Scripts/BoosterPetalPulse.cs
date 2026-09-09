using UnityEngine;

public sealed class BoosterPetalPulse : MonoBehaviour
{
    [SerializeField] private BoosterDeploymentController deployment;
    [SerializeField] private VariableNozzleController nozzleController;
    [SerializeField, Range(-1f, 0f)] private float maximumOpenClosure = -0.5f;
    [SerializeField, Range(0f, 1f)] private float openingStart = 0.88f;
    [SerializeField, Range(0f, 1f)] private float fullyOpenAt = 0.9f;
    [SerializeField, Range(0f, 1f)] private float neutralAgainAt = 0.94f;
    [SerializeField, Range(0f, 1f)] private float closedAgainAt = 1f;
    [SerializeField, HideInInspector] private Quaternion[] storedNeutralRotations = new Quaternion[0];

    private bool initialized;

    private void Awake()
    {
        InitializeOnce();
    }

    private void InitializeOnce()
    {
        if (initialized)
            return;

        if (deployment == null)
            deployment = GetComponentInParent<BoosterDeploymentController>();

        if (nozzleController == null)
            nozzleController = GetComponent<VariableNozzleController>();

        if (storedNeutralRotations == null || storedNeutralRotations.Length == 0)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            int petalCount = 0;
            for (int i = 0; i < children.Length; i++)
                if (children[i].name.StartsWith("Nozzle_PetalPivot"))
                    petalCount++;

            storedNeutralRotations = new Quaternion[petalCount];
            int petalIndex = 0;
            for (int i = 0; i < children.Length; i++)
            {
                if (!children[i].name.StartsWith("Nozzle_PetalPivot"))
                    continue;

                storedNeutralRotations[petalIndex++] = children[i].localRotation;
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        if (nozzleController != null)
            nozzleController.SetOpenRotations(storedNeutralRotations);

        initialized = true;
    }

    private void LateUpdate()
    {
        if (deployment == null)
            return;

        ApplyAmount(deployment.DeployAmount);
    }

    public void ApplyAmount(float amount)
    {
        InitializeOnce();

        if (nozzleController == null)
            return;

        float closure;
        if (amount < openingStart)
        {
            closure = 1f;
        }
        else if (amount < fullyOpenAt)
        {
            float opening = SmoothRange(openingStart, fullyOpenAt, amount);
            closure = Mathf.LerpUnclamped(1f, maximumOpenClosure, opening);
        }
        else if (amount < neutralAgainAt)
        {
            float returning = SmoothRange(fullyOpenAt, neutralAgainAt, amount);
            closure = Mathf.LerpUnclamped(maximumOpenClosure, 0f, returning);
        }
        else
        {
            float tightening = SmoothRange(neutralAgainAt, closedAgainAt, amount);
            closure = Mathf.LerpUnclamped(0f, 1f, tightening);
        }

        nozzleController.ApplyClosureImmediate(closure);
    }

    private static float SmoothRange(float start, float end, float value)
    {
        float t = Mathf.InverseLerp(start, end, value);
        return Mathf.SmoothStep(0f, 1f, t);
    }
}
