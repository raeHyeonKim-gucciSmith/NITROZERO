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
    [SerializeField, Min(0.01f)] private float transformationDuration = 3.2f;
    [SerializeField] private bool startDeployed;

    [Header("Booster Poses")]
    [SerializeField] private Vector3 hiddenLocalPosition;
    [SerializeField] private Vector3 hiddenLocalEulerAngles;
    [SerializeField] private Vector3 deployedLocalPosition;
    [SerializeField] private Vector3 deployedLocalEulerAngles;

    [Header("Sequence Timing")]
    [SerializeField, Range(0f, 0.99f)] private float boosterMovementStartsAt = 0.171875f;
    [SerializeField, Range(0.01f, 1f)] private float boosterMovementEndsAt = 0.515625f;
    [SerializeField, Range(0f, 0.99f)] private float levelingStart = 0.171875f;
    [SerializeField, Range(0.01f, 1f)] private float levelingEndsAt = 0.515625f;
    [SerializeField, Range(0f, 0.12f)] private float engineClunkOvershoot = 0.045f;

    [Header("Panel Cascade Timing")]
    [SerializeField] private Vector2 topPanelTiming = new Vector2(0.03f, 0.25f);
    [SerializeField] private Vector2 middlePanelSlideTiming = new Vector2(0.03f, 0.20f);
    [SerializeField] private Vector2 middlePanelTiming = new Vector2(0.20f, 0.40f);
    [SerializeField] private Vector2 bottomPanelSlideTiming = new Vector2(0.07f, 0.43f);
    [SerializeField] private Vector2 bottomPanelTiming = new Vector2(0.43f, 0.60f);
    [SerializeField, Range(0f, 0.2f)] private float pairedPanelDelay = 0.035f;
    [SerializeField, Range(0f, 0.15f)] private float panelClunkOvershoot = 0.06f;

    [Header("Underbody Wing Deployment")]
    [SerializeField] private Vector2 longWingTiming = new Vector2(0.17f, 0.36f);
    [SerializeField] private Vector2 shortWingTiming = new Vector2(0.20f, 0.42f);
    [Tooltip("현재 활성 차량에서 저장한 전개 전 힌지 회전값")]
    [SerializeField] private Vector3 longLeftWingClosedEuler = new Vector3(0f, -23.121f, 0f);
    [SerializeField] private Vector3 longRightWingClosedEuler = new Vector3(0f, 22.053f, 0f);
    [SerializeField] private Vector3 shortLeftWingClosedEuler = new Vector3(0f, -45.838f, 0f);
    [SerializeField] private Vector3 shortRightWingClosedEuler = new Vector3(0f, 47.317f, 0f);

    [Header("Rear Wing Deployment")]
    [Tooltip("현재 활성 차량에서 저장한 뒤쪽 날개 전개 완료 Transform")]
    [SerializeField] private Vector3 rearLeftWingDeployedPosition = Vector3.zero;
    [SerializeField] private Vector3 rearLeftWingDeployedEuler = new Vector3(-141.245f, -58.40799f, -30.950012f);
    [SerializeField] private Vector3 rearRightWingDeployedPosition = new Vector3(1.093895f, 0.0032573342f, -0.002507329f);
    [SerializeField] private Vector3 rearRightWingDeployedEuler = new Vector3(-139.45999f, 56.357002f, 34.035995f);
    [Tooltip("현재 활성 차량에서 저장한 뒤쪽 날개 수납 Transform")]
    [SerializeField] private Vector3 rearLeftWingClosedPosition = new Vector3(-0.0394f, -0.0651f, -0.003f);
    [SerializeField] private Vector3 rearLeftWingClosedEuler = new Vector3(-114.358f, -6.3099976f, -91.231995f);
    [SerializeField] private Vector3 rearRightWingClosedPosition = new Vector3(1.111f, -0.0679f, -0.002507329f);
    [SerializeField] private Vector3 rearRightWingClosedEuler = new Vector3(-115.333f, 7.949997f, 89.313f);
    [Tooltip("사용자가 직접 배치한 상승 최고점 Transform")]
    [SerializeField] private Vector3 rearLeftWingRaisedPosition = new Vector3(-0.002f, 0.094f, -0.003f);
    [SerializeField] private Vector3 rearLeftWingRaisedEuler = new Vector3(-117.162f, -26.102997f, -69.31299f);
    [SerializeField] private Vector3 rearRightWingRaisedPosition = new Vector3(1.074f, 0.09120001f, -0.002507329f);
    [SerializeField] private Vector3 rearRightWingRaisedEuler = new Vector3(-117.585f, 23.779007f, 71.663f);
    [Tooltip("backTrack과 함께 시작해 날개가 차체 밖으로 완전히 상승하는 구간")]
    [SerializeField] private Vector2 rearWingRiseTiming = new Vector2(0.20f, 0.36f);
    [Tooltip("상승 완료 후 차량 외부면을 따라 이동하는 구간")]
    [SerializeField] private Vector2 rearWingSlideTiming = new Vector2(0.36f, 0.56f);
    [Tooltip("최종 위치에서 묵직하게 잠기는 구간")]
    [SerializeField] private Vector2 rearWingLockTiming = new Vector2(0.56f, 0.61f);
    [SerializeField] private Vector3 rearWingLiftControlOffset = new Vector3(0f, 0.08f, -0.035f);
    [SerializeField] private Vector3 rearWingSurfaceCurveOffset = new Vector3(0f, 0.012f, 0f);
    [SerializeField] private Vector3 rearWingLockOffset = new Vector3(0f, 0.006f, 0f);

    [Header("Cover Angles")]
    [SerializeField] private float topOpenAngle = 45f;
    [SerializeField] private float middleOpenAngle = 35f;
    [SerializeField] private float bottomOpenAngle = 25f;

    [Header("Cover Offsets")]
    [SerializeField] private Vector3 topOpenOffset = new Vector3(0f, 0f, 0.05f);
    [SerializeField] private Vector3 middleLeftOpenOffset = new Vector3(0.015f, 0.08f, 0.18f);
    [SerializeField] private Vector3 middleRightOpenOffset = new Vector3(-0.015f, 0.08f, 0.18f);
    [SerializeField] private Vector3 bottomLeftOpenOffset = new Vector3(-0.025f, 0.16f, 0.22f);
    [SerializeField] private Vector3 bottomRightOpenOffset = new Vector3(0.025f, 0.16f, 0.22f);

    [Header("Back Track")]
    [SerializeField] private Vector3 backTrackOpenOffset = new Vector3(0f, 0.05f, 0f);
    [SerializeField] private Vector3 backTrackOpenEulerAngles = new Vector3(9.46f, 0f, 0f);
    [SerializeField] private Vector2 backTrackTiming = new Vector2(0.20f, 0.30f);
    [SerializeField, Range(0f, 0.2f)] private float backTrackClunkOvershoot = 0.10f;

    [Header("Nozzle Extension")]
    [SerializeField] private Vector3 ringRetractedLocalPosition = new Vector3(0.06299999f, -0.178f, -0.033f);
    [SerializeField] private Vector3 ringExtendedLocalPosition = new Vector3(-0.057f, -0.178f, -0.033f);
    [SerializeField] private Vector3 nozzleRetractedLocalPosition = new Vector3(0.0588f, -0.148f, -0.0348f);
    [SerializeField] private Vector3 nozzleExtendedLocalPosition = new Vector3(-0.09f, -0.152f, -0.025f);
    [SerializeField] private Vector3 petalGroupRetractedLocalPosition = new Vector3(-0.0202f, 0.0053f, -0.0308f);
    [SerializeField] private Vector3 petalGroupExtendedLocalPosition = new Vector3(-0.169f, 0.0013f, -0.021f);
    [SerializeField, Range(0f, 1f)] private float nozzleExtensionStartsAt = 0.515625f;
    [SerializeField, Range(0f, 1f)] private float nozzleExtensionImpactAt = 0.625f;
    [SerializeField, Range(0f, 1f)] private float nozzleExtensionSettlesAt = 0.65625f;
    [SerializeField, Range(0f, 0.2f)] private float nozzleExtensionOvershoot = 0.04f;
    [SerializeField, Range(0f, 1f)] private float fanIdleSpeedMultiplier = 0.12f;
    [SerializeField, Range(0f, 1f)] private float fanAccelerationStartsAt = 0.67f;

    [Header("Booster Internal Heat")]
    [SerializeField] private bool enableInternalHeat = true;
    [SerializeField, Range(0f, 1f)] private float internalHeatStartsAt = 0.171875f;
    [SerializeField, Range(0f, 1f)] private float internalHeatFullyLitAt = 0.515625f;
    [SerializeField, Range(0f, 0.5f)] private float internalHeatPulseAmount = 0.10f;
    [SerializeField, Min(0f)] private float internalHeatPulseSpeed = 2.2f;
    [SerializeField] private bool createInternalHeatLights = true;
    [SerializeField] private Color internalHeatStartColor = new Color(1f, 0.18f, 0.025f, 1f);
    [SerializeField] private Color internalHeatFinalColor = new Color(0.08f, 0.45f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float internalHeatColorChangeStartsAt = 0.6875f;
    [SerializeField, Range(0f, 1f)] private float internalHeatColorChangeEndsAt = 1f;
    [SerializeField, Min(0f)] private float internalHeatLightIntensity = 0.85f;
    [SerializeField, Min(0.01f)] private float internalHeatLightRange = 0.24f;
    [Tooltip("turbine_wheel 중심을 기준으로 광원 위치를 미세 조정하는 값")]
    [SerializeField] private Vector3 internalHeatLightLocalOffset = Vector3.zero;

    private readonly Transform[] coverPivots = new Transform[6];
    private readonly Vector3[] coverClosedPositions = new Vector3[6];
    private readonly Quaternion[] coverClosedRotations = new Quaternion[6];
    private readonly Transform[] underbodyWingHinges = new Transform[4];
    private readonly Quaternion[] wingClosedRotations = new Quaternion[4];
    private readonly Quaternion[] wingDeployedRotations =
    {
        Quaternion.identity,
        Quaternion.identity,
        Quaternion.identity,
        Quaternion.identity
    };
    private readonly Transform[] rearWings = new Transform[2];
    private readonly Vector3[] rearWingClosedPositions = new Vector3[2];
    private readonly Vector3[] rearWingRaisedPositions = new Vector3[2];
    private readonly Vector3[] rearWingDeployedPositions = new Vector3[2];
    private readonly Quaternion[] rearWingClosedRotations = new Quaternion[2];
    private readonly Quaternion[] rearWingRaisedRotations = new Quaternion[2];
    private readonly Quaternion[] rearWingDeployedRotations = new Quaternion[2];
    private static readonly string[] WingHingeNames =
    {
        "UnderWingLongL_Hinge",
        "UnderWingLongR_Hinge",
        "UnderWingShortL_Hinge",
        "UnderWingShortR_Hinge"
    };
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
    private readonly List<Light> internalHeatLights = new List<Light>();
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
        Transform activeWingRoot = FindActiveDescendant(searchRoot, "WingDeploymentRoot");
        wingClosedRotations[0] = Quaternion.Euler(longLeftWingClosedEuler);
        wingClosedRotations[1] = Quaternion.Euler(longRightWingClosedEuler);
        wingClosedRotations[2] = Quaternion.Euler(shortLeftWingClosedEuler);
        wingClosedRotations[3] = Quaternion.Euler(shortRightWingClosedEuler);

        for (int i = 0; i < underbodyWingHinges.Length; i++)
        {
            if (activeWingRoot != null)
                underbodyWingHinges[i] = FindDescendant(activeWingRoot, WingHingeNames[i]);
        }

        if (activeWingRoot != null)
        {
            rearWings[0] = FindDescendant(activeWingRoot, "backwingL (1)");
            rearWings[1] = FindDescendant(activeWingRoot, "backwingR (1)");
        }

        rearWingClosedPositions[0] = rearLeftWingClosedPosition;
        rearWingClosedPositions[1] = rearRightWingClosedPosition;
        rearWingRaisedPositions[0] = rearLeftWingRaisedPosition;
        rearWingRaisedPositions[1] = rearRightWingRaisedPosition;
        rearWingDeployedPositions[0] = rearLeftWingDeployedPosition;
        rearWingDeployedPositions[1] = rearRightWingDeployedPosition;
        rearWingClosedRotations[0] = Quaternion.Euler(rearLeftWingClosedEuler);
        rearWingClosedRotations[1] = Quaternion.Euler(rearRightWingClosedEuler);
        rearWingRaisedRotations[0] = Quaternion.Euler(rearLeftWingRaisedEuler);
        rearWingRaisedRotations[1] = Quaternion.Euler(rearRightWingRaisedEuler);
        rearWingDeployedRotations[0] = Quaternion.Euler(rearLeftWingDeployedEuler);
        rearWingDeployedRotations[1] = Quaternion.Euler(rearRightWingDeployedEuler);

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
        SetupInternalHeat();

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
        ApplyUnderbodyWings(sequence);
        ApplyRearWings(sequence);

        if (backTrack != null)
        {
            float backTrackAmount = GetBackTrackSequenceAmount(sequence);
            backTrack.localPosition = backTrackClosedPosition + backTrackOpenOffset * backTrackAmount;
            backTrack.localRotation = backTrackClosedRotation
                * Quaternion.Euler(backTrackOpenEulerAngles * backTrackAmount);
        }

        ApplyEngineReveal(sequence);
        ApplyInternalHeat(sequence);
    }

    private void SetupInternalHeat()
    {
        if (!enableInternalHeat)
            return;

        foreach (Renderer renderer in transform.GetComponentsInChildren<Renderer>(true))
        {
            string lowerName = renderer.gameObject.name.ToLowerInvariant();
            bool isRing = lowerName.Contains("ring");
            bool isTurbine = lowerName.Contains("turbine") || lowerName.Contains("blade");
            if (!isRing && !isTurbine)
                continue;

            DisableRendererEmission(renderer);

        }

        if (!createInternalHeatLights)
            return;

        foreach (ContinuousLocalRotation rotatingFan in rotatingFans)
        {
            if (rotatingFan == null
                || !rotatingFan.gameObject.name.ToLowerInvariant().Contains("turbine_wheel"))
                continue;

            internalHeatLights.Add(CreateInternalHeatLight(rotatingFan.transform));
        }
    }

    private static void DisableRendererEmission(Renderer renderer)
    {
        foreach (Material material in renderer.materials)
        {
            if (material == null || !material.HasProperty("_EmissionColor"))
                continue;

            material.SetColor("_EmissionColor", Color.black);
            material.DisableKeyword("_EMISSION");
        }
    }

    private Light CreateInternalHeatLight(Transform parent)
    {
        Transform existing = parent.Find("InternalHeatLight_Runtime");
        GameObject lightObject = existing != null
            ? existing.gameObject
            : new GameObject("InternalHeatLight_Runtime");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = internalHeatLightLocalOffset;

        Light heatLight = lightObject.GetComponent<Light>();
        if (heatLight == null)
            heatLight = lightObject.AddComponent<Light>();
        heatLight.type = LightType.Point;
        heatLight.color = internalHeatStartColor;
        heatLight.range = internalHeatLightRange;
        heatLight.intensity = 0f;
        heatLight.shadows = LightShadows.None;
        return heatLight;
    }

    private void ApplyInternalHeat(float sequence)
    {
        if (!enableInternalHeat)
            return;

        float heat = SmoothRange(internalHeatStartsAt, internalHeatFullyLitAt, sequence);
        if (heat > 0.8f)
        {
            float pulse = Mathf.Sin(Time.time * internalHeatPulseSpeed * Mathf.PI * 2f);
            heat *= 1f + pulse * internalHeatPulseAmount;
        }

        foreach (Light heatLight in internalHeatLights)
        {
            if (heatLight == null)
                continue;

            float colorAmount = SmoothRange(
                internalHeatColorChangeStartsAt,
                internalHeatColorChangeEndsAt,
                sequence);
            heatLight.color = Color.Lerp(internalHeatStartColor, internalHeatFinalColor, colorAmount);
            heatLight.range = internalHeatLightRange;
            heatLight.transform.localPosition = internalHeatLightLocalOffset;
            heatLight.intensity = internalHeatLightIntensity * Mathf.Max(0f, heat);
        }
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

    private void ApplyUnderbodyWings(float sequence)
    {
        for (int i = 0; i < underbodyWingHinges.Length; i++)
        {
            Transform hinge = underbodyWingHinges[i];
            if (hinge == null)
                continue;

            Vector2 timing = i < 2 ? longWingTiming : shortWingTiming;
            float amount = SmoothRange(timing.x, timing.y, sequence);
            hinge.localRotation = Quaternion.Slerp(
                wingClosedRotations[i],
                wingDeployedRotations[i],
                amount);
        }
    }

    private void ApplyRearWings(float sequence)
    {
        float riseAmount = SmoothRange(rearWingRiseTiming.x, rearWingRiseTiming.y, sequence);
        float slideAmount = SmoothRange(rearWingSlideTiming.x, rearWingSlideTiming.y, sequence);
        float lockAmount = SmoothRange(rearWingLockTiming.x, rearWingLockTiming.y, sequence);

        for (int i = 0; i < rearWings.Length; i++)
        {
            Transform wing = rearWings[i];
            if (wing == null)
                continue;

            Vector3 closed = rearWingClosedPositions[i];
            Vector3 fullyRaised = rearWingRaisedPositions[i];
            Vector3 deployed = rearWingDeployedPositions[i];
            Vector3 lockPoint = deployed + rearWingLockOffset;

            if (sequence < rearWingSlideTiming.x)
            {
                Vector3 control = closed + rearWingLiftControlOffset;
                wing.localPosition = QuadraticBezier(closed, control, fullyRaised, riseAmount);
            }
            else if (sequence < rearWingLockTiming.x)
            {
                Vector3 surfaceControl = Vector3.Lerp(fullyRaised, lockPoint, 0.5f)
                    + rearWingSurfaceCurveOffset;
                wing.localPosition = QuadraticBezier(
                    fullyRaised,
                    surfaceControl,
                    lockPoint,
                    slideAmount);
            }
            else
            {
                wing.localPosition = Vector3.LerpUnclamped(lockPoint, deployed, lockAmount);
            }

            wing.localRotation = sequence < rearWingSlideTiming.x
                ? Quaternion.Slerp(rearWingClosedRotations[i], rearWingRaisedRotations[i], riseAmount)
                : Quaternion.Slerp(rearWingRaisedRotations[i], rearWingDeployedRotations[i], slideAmount);
        }
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

#if UNITY_EDITOR
    public void PreviewRetractedInEditor()
    {
        if (Application.isPlaying)
            return;

        InitializeOnce();
        masterSequenceAmount = 0f;
        targetSequenceAmount = 0f;
        startDeployed = false;
        ApplySequence();
        MarkPreviewDirty();
    }

    public void PreviewDeployedInEditor()
    {
        if (Application.isPlaying)
            return;

        InitializeOnce();
        masterSequenceAmount = 1f;
        targetSequenceAmount = 1f;
        ApplySequence();
        MarkPreviewDirty();
    }

    private void MarkPreviewDirty()
    {
        UnityEditor.EditorUtility.SetDirty(this);

        foreach (Transform child in transform.root.GetComponentsInChildren<Transform>(true))
            UnityEditor.EditorUtility.SetDirty(child);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

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

    private static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float amount)
    {
        float inverse = 1f - amount;
        return inverse * inverse * start
            + 2f * inverse * amount * control
            + amount * amount * end;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == objectName)
                return child;

        return null;
    }

    private static Transform FindActiveDescendant(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == objectName && child.gameObject.activeInHierarchy)
                return child;

        return null;
    }
}
