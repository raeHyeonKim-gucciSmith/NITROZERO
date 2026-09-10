using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class BoosterDeploymentController : MonoBehaviour
{
    [FormerlySerializedAs("deployAmount")]
    [SerializeField, Range(0f, 1f)] private float masterSequenceAmount;

    [Header("Keyboard Control")]
    [SerializeField] private KeyCode toggleKey = KeyCode.B;
    [SerializeField] private bool allowKeyboardControl = true;
    [SerializeField, Min(0.01f)] private float transformationDuration = 2.2f;
    [SerializeField] private bool startDeployed;

    [Header("Booster Poses")]
    [SerializeField] private Vector3 hiddenLocalPosition;
    [SerializeField] private Vector3 hiddenLocalEulerAngles;
    [SerializeField] private Vector3 deployedLocalPosition;
    [SerializeField] private Vector3 deployedLocalEulerAngles;

    [Header("Sequence Timing")]
    [SerializeField, Range(0f, 0.99f)] private float boosterMovementStartsAt = 0.227f;
    [SerializeField, Range(0.01f, 1f)] private float boosterMovementEndsAt = 0.627f;
    [SerializeField, Range(0f, 0.99f)] private float levelingStart = 0.227f;
    [SerializeField, Range(0.01f, 1f)] private float levelingEndsAt = 0.627f;
    [SerializeField, Range(0f, 0.12f)] private float engineClunkOvershoot = 0.045f;

    [Header("Panel Cascade Timing")]
    [SerializeField] private Vector2 topPanelTiming = new Vector2(0.04f, 0.24f);
    [SerializeField] private Vector2 middlePanelSlideTiming = new Vector2(0.04f, 0.18f);
    [SerializeField] private Vector2 middlePanelTiming = new Vector2(0.18f, 0.42f);
    [SerializeField] private Vector2 bottomPanelSlideTiming = new Vector2(0.08f, 0.48f);
    [SerializeField] private Vector2 bottomPanelTiming = new Vector2(0.50f, 0.68f);
    [SerializeField, Range(0f, 0.2f)] private float pairedPanelDelay = 0.05f;
    [SerializeField, Range(0f, 0.15f)] private float panelClunkOvershoot = 0.06f;

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
    [SerializeField] private Vector2 backTrackTiming = new Vector2(0.255f, 0.355f);
    [SerializeField, Range(0f, 0.2f)] private float backTrackClunkOvershoot = 0.10f;

    [Header("Nozzle Extension")]
    [SerializeField] private Vector3 ringRetractedLocalPosition = new Vector3(0.06299999f, -0.178f, -0.033f);
    [SerializeField] private Vector3 ringExtendedLocalPosition = new Vector3(-0.057f, -0.178f, -0.033f);
    [SerializeField] private Vector3 nozzleRetractedLocalPosition = new Vector3(0.0588f, -0.148f, -0.0348f);
    [SerializeField] private Vector3 nozzleExtendedLocalPosition = new Vector3(-0.09f, -0.152f, -0.025f);
    [SerializeField] private Vector3 petalGroupRetractedLocalPosition = new Vector3(-0.0202f, 0.0053f, -0.0308f);
    [SerializeField] private Vector3 petalGroupExtendedLocalPosition = new Vector3(-0.169f, 0.0013f, -0.021f);
    [SerializeField, Range(0f, 1f)] private float nozzleExtensionStartsAt = 0.645f;
    [SerializeField, Range(0f, 1f)] private float nozzleExtensionImpactAt = 0.72f;
    [SerializeField, Range(0f, 1f)] private float nozzleExtensionSettlesAt = 0.75f;
    [SerializeField, Range(0f, 0.2f)] private float nozzleExtensionOvershoot = 0.04f;
    [SerializeField, Range(0f, 1f)] private float fanIdleSpeedMultiplier = 0.12f;
    [SerializeField, Range(0f, 1f)] private float fanAccelerationStartsAt = 0.76f;

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
    private float targetSequenceAmount;

    public float DeployAmount => masterSequenceAmount;

    private void Awake()
    {
        InitializeOnce();
        targetSequenceAmount = startDeployed ? 1f : 0f;
        masterSequenceAmount = targetSequenceAmount;
        ApplySequence();
    }

    private void Update()
    {
        if (allowKeyboardControl && IsToggleKeyPressed())
            ToggleBoostMode();

        masterSequenceAmount = Mathf.MoveTowards(
            masterSequenceAmount,
            targetSequenceAmount,
            Time.deltaTime / Mathf.Max(0.01f, transformationDuration));
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

        Transform searchRoot = transform.root;
        for (int i = 0; i < coverPivots.Length; i++)
        {
            coverPivots[i] = FindDescendant(searchRoot, $"CoverPivot_{i + 1:00}");
            if (coverPivots[i] == null)
                continue;

            GetCoverMotion(i, out float angle, out Vector3 offset);
            GetCoverSequenceAmounts(i, sequence, out float slideAmount, out float rotationAmount);
            coverClosedPositions[i] = coverPivots[i].localPosition - offset * slideAmount;
            coverClosedRotations[i] = coverPivots[i].localRotation
                * Quaternion.Inverse(Quaternion.AngleAxis(angle * rotationAmount, Vector3.right));
        }

        backTrack = FindDescendant(searchRoot, "backTrack");
        if (backTrack != null)
        {
            float backTrackAmount = GetBackTrackSequenceAmount(sequence);
            backTrackClosedPosition = backTrack.localPosition - backTrackOpenOffset * backTrackAmount;
            backTrackClosedRotation = backTrack.localRotation
                * Quaternion.Inverse(Quaternion.Euler(backTrackOpenEulerAngles * backTrackAmount));
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
        float boosterAmount = HeavyEngineRange(boosterMovementStartsAt, boosterMovementEndsAt, sequence);
        float levelingAmount = SmoothRange(levelingStart, levelingEndsAt, sequence);

        transform.localPosition = Vector3.LerpUnclamped(hiddenLocalPosition, deployedLocalPosition, boosterAmount);
        transform.localRotation = Quaternion.SlerpUnclamped(hiddenLocalRotation, deployedLocalRotation, levelingAmount);

        ApplySequencedCover(0, topOpenAngle, topOpenOffset, sequence);
        ApplySequencedCover(1, bottomOpenAngle, bottomLeftOpenOffset, sequence);
        ApplySequencedCover(2, topOpenAngle, topOpenOffset, sequence);
        ApplySequencedCover(3, middleOpenAngle, middleLeftOpenOffset, sequence);
        ApplySequencedCover(4, middleOpenAngle, middleRightOpenOffset, sequence);
        ApplySequencedCover(5, bottomOpenAngle, bottomRightOpenOffset, sequence);

        if (backTrack != null)
        {
            float backTrackAmount = GetBackTrackSequenceAmount(sequence);
            backTrack.localPosition = backTrackClosedPosition + backTrackOpenOffset * backTrackAmount;
            backTrack.localRotation = backTrackClosedRotation
                * Quaternion.Euler(backTrackOpenEulerAngles * backTrackAmount);
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

    private void ApplySequencedCover(int index, float angle, Vector3 offset, float sequence)
    {
        Transform pivot = coverPivots[index];
        if (pivot == null)
            return;

        GetCoverSequenceAmounts(index, sequence, out float slideAmount, out float rotationAmount);
        pivot.localPosition = coverClosedPositions[index] + offset * slideAmount;
        pivot.localRotation = coverClosedRotations[index]
            * Quaternion.AngleAxis(angle * rotationAmount, Vector3.right);
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

    private void GetCoverSequenceAmounts(int index, float sequence, out float slideAmount, out float rotationAmount)
    {
        Vector2 slideTiming;
        Vector2 rotationTiming;
        bool delayedPartner;

        switch (index)
        {
            case 0:
                slideTiming = rotationTiming = topPanelTiming;
                delayedPartner = false;
                break;
            case 2:
                slideTiming = rotationTiming = topPanelTiming;
                delayedPartner = true;
                break;
            case 3:
                slideTiming = middlePanelSlideTiming;
                rotationTiming = middlePanelTiming;
                delayedPartner = false;
                break;
            case 4:
                slideTiming = middlePanelSlideTiming;
                rotationTiming = middlePanelTiming;
                delayedPartner = true;
                break;
            case 1:
                slideTiming = bottomPanelSlideTiming;
                rotationTiming = bottomPanelTiming;
                delayedPartner = false;
                break;
            default:
                slideTiming = bottomPanelSlideTiming;
                rotationTiming = bottomPanelTiming;
                delayedPartner = true;
                break;
        }

        float delay = delayedPartner ? pairedPanelDelay : 0f;
        slideAmount = HeavyMechanicalRange(slideTiming.x + delay, slideTiming.y + delay, sequence);
        rotationAmount = HeavyMechanicalRange(rotationTiming.x + delay, rotationTiming.y + delay, sequence);
    }

    private float GetBackTrackSequenceAmount(float sequence)
    {
        float amount = SmoothRange(backTrackTiming.x, backTrackTiming.y, sequence);
        float clunkWindow = Mathf.Max(0.001f, (backTrackTiming.y - backTrackTiming.x) * 0.32f);
        float clunk = Pulse(backTrackTiming.y - clunkWindow, backTrackTiming.y, sequence);
        return amount + clunk * backTrackClunkOvershoot;
    }

    private float HeavyMechanicalRange(float start, float end, float sequence)
    {
        float amount = SmoothRange(start, end, sequence);
        float clunkWindow = Mathf.Max(0.001f, (end - start) * 0.22f);
        float clunk = Pulse(end - clunkWindow, end, sequence);
        return amount + clunk * panelClunkOvershoot;
    }

    private float HeavyEngineRange(float start, float end, float sequence)
    {
        float amount = SmoothRange(start, end, sequence);
        float clunkWindow = Mathf.Max(0.001f, (end - start) * 0.18f);
        float clunk = Pulse(end - clunkWindow, end, sequence);
        return amount + clunk * engineClunkOvershoot;
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
        targetSequenceAmount = masterSequenceAmount;
    }

    public void ToggleBoostMode() => targetSequenceAmount = targetSequenceAmount >= 0.5f ? 0f : 1f;
    public void DeployBoosters() => targetSequenceAmount = 1f;
    public void RetractBoosters() => targetSequenceAmount = 0f;

    private bool IsToggleKeyPressed()
    {
        try
        {
            if (Input.GetKeyDown(toggleKey))
                return true;
        }
        catch { }

#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && toggleKey == KeyCode.B)
            return keyboard.bKey.wasPressedThisFrame;
#endif
        return false;
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
