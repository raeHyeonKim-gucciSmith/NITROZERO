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
    [SerializeField, Range(0f, 0.99f)] private float boosterMovementStartsAt = 0.2f;
    [SerializeField, Range(0f, 1f)] private float levelingStart = 0.65f;

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

    [Header("Engine Reveal")]
    [SerializeField] private Vector3 ringSlideAxis = Vector3.left;
    [SerializeField] private float ringSlideDistance = 0.12f;
    [SerializeField, Range(0f, 1f)] private float ringUnlockAt = 0.73f;
    [SerializeField, Range(0f, 1f)] private float ringSlideStartsAt = 0.76f;
    [SerializeField, Range(0f, 1f)] private float ringSlideEndsAt = 0.84f;
    [SerializeField, Range(0f, 1f)] private float ringSettlesAt = 0.88f;
    [SerializeField, Range(0f, 1f)] private float fanAccelerationStartsAt = 0.84f;

    private readonly Transform[] coverPivots = new Transform[6];
    private readonly Vector3[] coverClosedPositions = new Vector3[6];
    private readonly Quaternion[] coverClosedRotations = new Quaternion[6];
    private Transform backTrack;
    private Vector3 backTrackClosedPosition;
    private Quaternion backTrackClosedRotation;
    private Quaternion hiddenLocalRotation;
    private Quaternion deployedLocalRotation;
    private readonly List<Transform> slidingRings = new List<Transform>();
    private readonly List<Vector3> ringClosedPositions = new List<Vector3>();
    private ContinuousLocalRotation[] rotatingFans = new ContinuousLocalRotation[0];
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

        Transform searchRoot = transform.root;
        for (int i = 0; i < coverPivots.Length; i++)
        {
            coverPivots[i] = FindDescendant(searchRoot, $"CoverPivot_{i + 1:00}");
            if (coverPivots[i] == null)
                continue;

            coverClosedPositions[i] = coverPivots[i].localPosition;
            coverClosedRotations[i] = coverPivots[i].localRotation;
        }

        backTrack = FindDescendant(searchRoot, "backTrack");
        if (backTrack != null)
        {
            backTrackClosedPosition = backTrack.localPosition;
            backTrackClosedRotation = backTrack.localRotation;
        }

        foreach (Transform child in transform.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != "futuristic+turbine+ring+3d+model")
                continue;

            slidingRings.Add(child);
            ringClosedPositions.Add(child.localPosition);
        }

        rotatingFans = transform.GetComponentsInChildren<ContinuousLocalRotation>(true);

        initialized = true;
    }

    private void ApplySequence()
    {
        float sequence = Mathf.Clamp01(masterSequenceAmount);
        float coverAmount = SmoothRange(0f, coversFullyOpenAt, sequence);
        float boosterAmount = SmoothRange(boosterMovementStartsAt, 1f, sequence);
        float levelingAmount = SmoothRange(levelingStart, 1f, boosterAmount);

        transform.localPosition = Vector3.LerpUnclamped(deployedLocalPosition, hiddenLocalPosition, boosterAmount);
        transform.localRotation = Quaternion.SlerpUnclamped(deployedLocalRotation, hiddenLocalRotation, levelingAmount);

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
        float unlock = Pulse(ringUnlockAt, ringSlideStartsAt, sequence);
        float slide = SmoothRange(ringSlideStartsAt, ringSlideEndsAt, sequence);
        float settle = Pulse(ringSlideEndsAt, ringSettlesAt, sequence);
        float travel = slide + unlock * 0.035f + settle * 0.08f;
        Vector3 direction = ringSlideAxis.sqrMagnitude > 0.000001f
            ? ringSlideAxis.normalized
            : Vector3.left;

        for (int i = 0; i < slidingRings.Count; i++)
            slidingRings[i].localPosition = ringClosedPositions[i] + direction * ringSlideDistance * travel;

        float fanSpeed = SmoothRange(fanAccelerationStartsAt, 1f, sequence);
        for (int i = 0; i < rotatingFans.Length; i++)
            rotatingFans[i].SetSpeedMultiplier(fanSpeed);
    }

    private void ApplyCover(int index, float angle, Vector3 offset, float amount)
    {
        Transform pivot = coverPivots[index];
        if (pivot == null)
            return;

        pivot.localPosition = coverClosedPositions[index] + offset * amount;
        pivot.localRotation = coverClosedRotations[index] * Quaternion.AngleAxis(angle * amount, Vector3.right);
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
