using UnityEngine;

public sealed class BoosterPetalPulse : MonoBehaviour
{
    [SerializeField] private BoosterDeploymentController deployment;
    [SerializeField] private VariableNozzleController nozzleController;
    [SerializeField, Range(-1f, 0f)] private float maximumOpenClosure = -0.5f;
    [SerializeField, Range(0f, 1f)] private float maximumContractedClosure = 0.65f;
    [SerializeField, Range(0f, 1f)] private float openingStart = 0.6875f;
    [SerializeField, Range(0f, 1f)] private float fullyOpenAt = 0.78125f;
    [SerializeField, Range(0f, 1f)] private float fullyContractedAt = 0.88125f;
    [SerializeField, Range(0f, 1f)] private float horizontalAgainAt = 1f;
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
            closure = 0f;
        }
        else if (amount < fullyOpenAt)
        {
            float opening = SmoothRange(openingStart, fullyOpenAt, amount);
            closure = Mathf.LerpUnclamped(1f, maximumOpenClosure, opening);
        }
        else if (amount < fullyContractedAt)
        {
            float contracting = SmoothRange(fullyOpenAt, fullyContractedAt, amount);
            closure = Mathf.LerpUnclamped(
                maximumOpenClosure,
                maximumContractedClosure,
                contracting);
        }
        else
        {
            float leveling = SmoothRange(fullyContractedAt, horizontalAgainAt, amount);
            closure = Mathf.LerpUnclamped(maximumContractedClosure, 0f, leveling);
        }

        nozzleController.ApplyClosureImmediate(closure);
    }

    private static float SmoothRange(float start, float end, float value)
    {
        float t = Mathf.InverseLerp(start, end, value);
        return Mathf.SmoothStep(0f, 1f, t);
    }
}
