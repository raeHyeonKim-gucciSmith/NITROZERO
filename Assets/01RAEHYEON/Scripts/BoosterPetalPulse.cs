using UnityEngine;

public sealed class BoosterPetalPulse : MonoBehaviour
{
    [SerializeField] private BoosterDeploymentController deployment;
    [SerializeField] private VariableNozzleController nozzleController;
    [SerializeField, Range(-1f, 0f)] private float maximumOpenClosure = -0.5f;
    [SerializeField, Range(0f, 1f)] private float maximumContractedClosure = 0.65f;
    [Tooltip("노즐 점검을 시작하며 블레이드가 축소되기 시작하는 시점")]
    [SerializeField, Range(0f, 1f)] private float openingStart = 0.6875f;
    [SerializeField, Range(0f, 1f)] private float fullyContractedAt = 0.765625f;
    [SerializeField, Range(0f, 1f)] private float fullyOpenAt = 0.875f;
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
        else if (amount < fullyContractedAt)
        {
            float contracting = HeavyPhase(openingStart, fullyContractedAt, amount, 0.055f);
            closure = Mathf.LerpUnclamped(0f, maximumContractedClosure, contracting);
        }
        else if (amount < fullyOpenAt)
        {
            float opening = HeavyPhase(fullyContractedAt, fullyOpenAt, amount, 0.045f);
            closure = Mathf.LerpUnclamped(
                maximumContractedClosure,
                maximumOpenClosure,
                opening);
        }
        else
        {
            float leveling = HeavyPhase(fullyOpenAt, horizontalAgainAt, amount, 0.04f);
            closure = Mathf.LerpUnclamped(maximumOpenClosure, 0f, leveling);
        }

        nozzleController.ApplyClosureImmediate(closure);
    }

    private static float SmoothRange(float start, float end, float value)
    {
        float t = Mathf.InverseLerp(start, end, value);
        return Mathf.SmoothStep(0f, 1f, t);
    }

    private static float HeavyPhase(float start, float end, float value, float overshoot)
    {
        float t = Mathf.Clamp01(Mathf.InverseLerp(start, end, value));
        float driven;

        if (t < 0.18f)
            driven = Mathf.Lerp(0f, 0.07f, Mathf.SmoothStep(0f, 1f, t / 0.18f));
        else if (t < 0.82f)
            driven = Mathf.Lerp(0.07f, 0.94f, Mathf.SmoothStep(0f, 1f, (t - 0.18f) / 0.64f));
        else
            driven = Mathf.Lerp(0.94f, 1f, Mathf.SmoothStep(0f, 1f, (t - 0.82f) / 0.18f));

        float impact = Mathf.Sin(Mathf.InverseLerp(0.72f, 1f, t) * Mathf.PI);
        return driven + impact * overshoot;
    }
}
