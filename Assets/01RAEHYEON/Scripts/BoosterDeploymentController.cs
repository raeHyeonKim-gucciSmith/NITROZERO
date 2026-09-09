using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class BoosterDeploymentController : MonoBehaviour
{
    [FormerlySerializedAs("deployAmount")]
    [SerializeField, Range(0f, 1f)] private float masterSequenceAmount;

    [Header("Booster Poses")]
    [SerializeField] private Vector3 hiddenLocalPosition;
    [SerializeField] private Vector3 hiddenLocalEulerAngles;
    [SerializeField] private Vector3 deployedLocalPosition;
    [SerializeField] private Vector3 deployedLocalEulerAngles;

    [Header("Sequence Timing")]
    [SerializeField, Range(0.01f, 1f)] private float coversFullyOpenAt = 0.35f;
    [SerializeField, Range(0f, 0.99f)] private float boosterMovementStartsAt;
    [SerializeField, Range(0.01f, 1f)] private float boosterMovementEndsAt = 0.8f;
    [SerializeField, Range(0f, 0.99f)] private float levelingStart;
    [SerializeField, Range(0.01f, 1f)] private float levelingEndsAt = 0.8f;

    [Header("Cover Angles")]
    [SerializeField] private float topOpenAngle = 45f;
    [SerializeField] private float middleOpenAngle = 35f;
    [SerializeField] private float bottomOpenAngle = 25f;

    [Header("Cover Offsets")]
    [SerializeField] private Vector3 topOpenOffset = new Vector3(0f, 0f, 0.05f);
    [SerializeField] private Vector3 middleLeftOpenOffset = new Vector3(0.015f, 0.08f, 0.18f);
    [SerializeField] private Vector3 middleRightOpenOffset = new Vector3(-0.015f, 0.08f, 0.18f);
    [SerializeField] private Vector3 bottomLeftOpenOffset = new Vector3(-0.025f, 0.16f, 0.30f);
    [SerializeField] private Vector3 bottomRightOpenOffset = new Vector3(0.025f, 0.16f, 0.30f);

    [Header("Back Track")]
    [SerializeField] private Vector3 backTrackOpenOffset = new Vector3(0f, 0.05f, 0f);
    [SerializeField] private Vector3 backTrackOpenEulerAngles = new Vector3(9.46f, 0f, 0f);

    [Header("Nozzle Extension")]
    [SerializeField] private Vector3 ringRetractedLocalPosition = new Vector3(0.06299999f, -0.178f, -0.033f);
    [SerializeField] private Vector3 ringExtendedLocalPosition = new Vector3(-0.057f, -0.178f, -0.033f);
    [SerializeField] private Vector3 nozzleRetractedLocalPosition = new Vector3(0.0588f, -0.148f, -0.0348f);
    [SerializeField] private Vector3 nozzleExtendedLocalPosition = new Vector3(-0.09f, -0.152f, -0.025f);
    [SerializeField] private Vector3 petalGroupRetractedLocalPosition = new Vector3(-0.0202f, 0.0053f, -0.0308f);
    [SerializeField] private Vector3 petalGroupExtendedLocalPosition = new Vector3(-0.169f, 0.0013f, -0.021f);
    [SerializeField, Range(0f, 1f)] private float nozzleExtensionStartsAt = 0.8f;
    [SerializeField, Range(0f, 1f)] private float nozzleExtensionImpactAt = 0.84f;
    [SerializeField, Range(0f, 1f)] private float nozzleExtensionSettlesAt = 0.88f;
    [SerializeField, Range(0f, 0.2f)] private float nozzleExtensionOvershoot = 0.04f;
    [SerializeField, Range(0f, 1f)] private float fanIdleSpeedMultiplier = 0.12f;
    [SerializeField, Range(0f, 1f)] private float fanAccelerationStartsAt = 0.94f;

    private readonly Transform[] coverPivots = new Transform[6];
    private readonly Vector3[] coverClosedPositions = new Vector3[6];
    private readonly Quaternion[] coverClosedRotations = new Quaternion[6];
    private Transform backTrack;
    private Vector3 backTrackClosedPosition;
    private Quaternion backTrackClosedRotation;
    private Quaternion hiddenLocalRotation;
    private Quaternion deployedLocalRotation;
    private readonly List<Transform> nozzleExtensionParts = new List<Transform>();
    private readonly List<Vector3> nozzleRetractedPositions = new List<Vector3>();
    private readonly List<Vector3> nozzleExtendedPositions = new List<Vector3>();
    private ContinuousLocalRotation[] rotatingFans = new ContinuousLocalRotation[0];
    private BoosterPetalPulse[] petalPulses = new BoosterPetalPulse[0];
    private bool initialized;

    public float DeployAmount => masterSequenceAmount;

    private void Awake()
    {
        InitializeOnce();
        ApplySequence();
    }

    private void LateUpdate()
    {
        InitializeOnce();
        ApplySequence();
    }

    private void InitializeOnce()
    {
        if (initialized)
            return;

        hiddenLocalRotation = Quaternion.Euler(hiddenLocalEulerAngles);
        deployedLocalRotation = Quaternion.Euler(deployedLocalEulerAngles);

        float sequence = Mathf.Clamp01(masterSequenceAmount);
        float coverAmount = SmoothRange(0f, coversFullyOpenAt, sequence);

        Transform searchRoot = transform.root;
        for (int i = 0; i < coverPivots.Length; i++)
        {
            coverPivots[i] = FindDescendant(searchRoot, $"CoverPivot_{i + 1:00}");
            if (coverPivots[i] == null)
                continue;

            GetCoverMotion(i, out float angle, out Vector3 offset);
            coverClosedPositions[i] = coverPivots[i].localPosition - offset * coverAmount;
            coverClosedRotations[i] = coverPivots[i].localRotation
                * Quaternion.Inverse(Quaternion.AngleAxis(angle * coverAmount, Vector3.right));
        }

        backTrack = FindDescendant(searchRoot, "backTrack");
        if (backTrack != null)
        {
            backTrackClosedPosition = backTrack.localPosition - backTrackOpenOffset * coverAmount;
            backTrackClosedRotation = backTrack.localRotation
                * Quaternion.Inverse(Quaternion.Euler(backTrackOpenEulerAngles * coverAmount));
        }

        foreach (Transform child in transform.GetComponentsInChildren<Transform>(true))
        {
            if (!TryGetNozzlePartPositions(
                    child.name,
                    out Vector3 retractedPosition,
                    out Vector3 extendedPosition))
                continue;

            nozzleExtensionParts.Add(child);
            nozzleRetractedPositions.Add(retractedPosition);
            nozzleExtendedPositions.Add(extendedPosition);
        }

        rotatingFans = transform.GetComponentsInChildren<ContinuousLocalRotation>(true);
        petalPulses = transform.GetComponentsInChildren<BoosterPetalPulse>(true);

        initialized = true;
    }

    private void ApplySequence()
    {
        float sequence = Mathf.Clamp01(masterSequenceAmount);
        float coverAmount = SmoothRange(0f, coversFullyOpenAt, sequence);
        float boosterAmount = SmoothRange(boosterMovementStartsAt, boosterMovementEndsAt, sequence);
        float levelingAmount = SmoothRange(levelingStart, levelingEndsAt, sequence);

        transform.localPosition = Vector3.LerpUnclamped(hiddenLocalPosition, deployedLocalPosition, boosterAmount);
        transform.localRotation = Quaternion.SlerpUnclamped(hiddenLocalRotation, deployedLocalRotation, levelingAmount);

        ApplyCover(0, topOpenAngle, topOpenOffset, coverAmount);
        ApplyCover(1, bottomOpenAngle, bottomLeftOpenOffset, coverAmount);
        ApplyCover(2, topOpenAngle, topOpenOffset, coverAmount);
        ApplyCover(3, middleOpenAngle, middleLeftOpenOffset, coverAmount);
        ApplyCover(4, middleOpenAngle, middleRightOpenOffset, coverAmount);
        ApplyCover(5, bottomOpenAngle, bottomRightOpenOffset, coverAmount);

        if (backTrack != null)
        {
            backTrack.localPosition = backTrackClosedPosition + backTrackOpenOffset * coverAmount;
            backTrack.localRotation = backTrackClosedRotation * Quaternion.Euler(backTrackOpenEulerAngles * coverAmount);
        }

        ApplyEngineReveal(sequence);
    }

    private void ApplyEngineReveal(float sequence)
    {
        float extension = GetNozzleExtensionAmount(sequence);

        for (int i = 0; i < nozzleExtensionParts.Count; i++)
            nozzleExtensionParts[i].localPosition = Vector3.LerpUnclamped(
                nozzleRetractedPositions[i],
                nozzleExtendedPositions[i],
                extension);

        float fanAcceleration = SmoothRange(fanAccelerationStartsAt, 1f, sequence);
        float fanSpeed = Mathf.Lerp(fanIdleSpeedMultiplier, 1f, fanAcceleration);
        for (int i = 0; i < rotatingFans.Length; i++)
            rotatingFans[i].SetSpeedMultiplier(fanSpeed);

        for (int i = 0; i < petalPulses.Length; i++)
            petalPulses[i].ApplyAmount(sequence);
    }

    private void ApplyCover(int index, float angle, Vector3 offset, float amount)
    {
        Transform pivot = coverPivots[index];
        if (pivot == null)
            return;

        pivot.localPosition = coverClosedPositions[index] + offset * amount;
        pivot.localRotation = coverClosedRotations[index] * Quaternion.AngleAxis(angle * amount, Vector3.right);
    }

    private void GetCoverMotion(int index, out float angle, out Vector3 offset)
    {
        switch (index)
        {
            case 0:
            case 2:
                angle = topOpenAngle;
                offset = topOpenOffset;
                break;
            case 1:
                angle = bottomOpenAngle;
                offset = bottomLeftOpenOffset;
                break;
            case 3:
                angle = middleOpenAngle;
                offset = middleLeftOpenOffset;
                break;
            case 4:
                angle = middleOpenAngle;
                offset = middleRightOpenOffset;
                break;
            default:
                angle = bottomOpenAngle;
                offset = bottomRightOpenOffset;
                break;
        }
    }

    private bool TryGetNozzlePartPositions(
        string objectName,
        out Vector3 retractedPosition,
        out Vector3 extendedPosition)
    {
        switch (objectName)
        {
            case "futuristic+turbine+ring+3d+model":
                retractedPosition = ringRetractedLocalPosition;
                extendedPosition = ringExtendedLocalPosition;
                return true;
            case "rocket+nozzle+3d+model":
                retractedPosition = nozzleRetractedLocalPosition;
                extendedPosition = nozzleExtendedLocalPosition;
                return true;
            case "Nozzle_PetalGroup":
                retractedPosition = petalGroupRetractedLocalPosition;
                extendedPosition = petalGroupExtendedLocalPosition;
                return true;
            default:
                retractedPosition = Vector3.zero;
                extendedPosition = Vector3.zero;
                return false;
        }
    }

    private float GetNozzleExtensionAmount(float sequence)
    {
        float extension = SmoothRange(nozzleExtensionStartsAt, nozzleExtensionImpactAt, sequence);
        float settle = Pulse(nozzleExtensionImpactAt, nozzleExtensionSettlesAt, sequence);
        return extension + settle * nozzleExtensionOvershoot;
    }

    public void SetDeployAmount(float value)
    {
        masterSequenceAmount = Mathf.Clamp01(value);
    }

    public void PreviewSequence()
    {
        InitializeOnce();
        ApplySequence();
    }

    private static float SmoothRange(float start, float end, float value)
    {
        float t = Mathf.InverseLerp(start, end, value);
        return Mathf.SmoothStep(0f, 1f, t);
    }

    private static float Pulse(float start, float end, float value)
    {
        float t = Mathf.InverseLerp(start, end, value);
        return Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == objectName)
                return child;

        return null;
    }
}
