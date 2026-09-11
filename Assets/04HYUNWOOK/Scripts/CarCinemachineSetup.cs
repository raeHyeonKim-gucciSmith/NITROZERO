using UnityEngine;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(CinemachineCamera))]
[RequireComponent(typeof(CinemachineFollow))]
[RequireComponent(typeof(CinemachineRotationComposer))]
public class CarCinemachineSetup : MonoBehaviour
{
    [Header("Target")]
    public Transform carTarget;

    [Header("View Toggle (T)")]
    [Tooltip("차량 기준 1인칭 눈 위치입니다. X=좌우, Y=높이, Z=전후.")]
    public Vector3 firstPersonOffset = new Vector3(0f, 1.5f, 1.0f);
    [Min(1f)] public float firstPersonLookDistance = 30f;
    [Tooltip("실내 모델이 없는 차량의 차체가 1인칭 화면을 가리지 않게 숨깁니다.")]
    public bool hideVehicleInFirstPerson = false;
    [Tooltip("Fade to black, switch the view, then fade in.")]
    [Min(0.01f)] public float transitionDuration = 0.45f;
    [Tooltip("전환 중 추가 확대 효과입니다. 0이면 시야각 출렁임 없이 전환합니다.")]
    [Range(0f, 10f)] public float transitionFovKick = 0f;
    public bool startInFirstPerson = true;
    public bool ViewInputLocked { get; set; }
    public bool IsFirstPerson { get; private set; }
    public float ViewBlend { get; private set; }
    // ViewBlend changes only while the screen is fully black.
    public float ViewOpacity { get; private set; } = 1f;
    float transitionElapsed = 1f, baseFov;
    bool transitioning, viewSwitched;
    DrivingViewTransition transitionEffect;
    readonly Dictionary<Renderer, bool> hiddenRenderers = new Dictionary<Renderer, bool>();

    [Header("Camera Position")]
    public Vector3 followOffset = new Vector3(0f, 2.4f, -7f);
    public Vector3 lookAtOffset = new Vector3(0f, 1.2f, 6f);
    [Tooltip("차량 기준 높이/거리를 유지합니다. 속도감은 FOV만 변경합니다.")]
    public bool lockFollowPosition = true;
    [Range(20f, 100f)] public float fieldOfView = 55f;

    [Header("Speed FOV")]
    [Tooltip("최고 속도에서 사용할 최대 시야각입니다.")]
    [Range(20f, 100f)] public float maximumFieldOfView = 80f;
    [Tooltip("최대 시야각에 도달하는 속도(km/h)입니다.")]
    [Min(1f)] public float speedForMaximumFov = 300f;
    [Tooltip("시야각이 부드럽게 변하는 시간(초)입니다. 작을수록 빠르게 반응합니다.")]
    [Min(0.01f)] public float fovSmoothTime = 0.5f;

    private CinemachineCamera speedCamera;
    private ArcadeCarController speedSource;
    private Transform speedSourceTarget;
    private float fovVelocity;

    [Header("Damping")]
    public Vector3 positionDamping = new Vector3(0.2f, 0.5f, 0.2f);
    [Min(0f)] public float yawDamping = 0.5f;
    public Vector2 aimDamping = new Vector2(0.3f, 0.3f);

    private void Start()
    {
        IsFirstPerson = startInFirstPerson;
        ViewBlend = IsFirstPerson ? 1f : 0f;
        transitionElapsed = Mathf.Max(0.01f, transitionDuration);
        ApplyCameraSettings();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame) ToggleView();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.T)) ToggleView();
#endif
        if (Time.deltaTime <= 0f) return;
        if (speedCamera == null) speedCamera = GetComponent<CinemachineCamera>();
        if (speedSourceTarget != carTarget || speedSource == null)
        {
            speedSourceTarget = carTarget;
            speedSource = carTarget != null ? carTarget.GetComponentInParent<ArcadeCarController>() : null;
        }

        float speed = speedSource != null ? speedSource.SpeedKmh : 0f;
        float ratio = Mathf.Clamp01(speed / Mathf.Max(1f, speedForMaximumFov));
        float minimum = Mathf.Clamp(fieldOfView, 20f, 100f);
        float maximum = Mathf.Clamp(maximumFieldOfView, minimum, 100f);
        float target = Mathf.Lerp(minimum, maximum, ratio);
        baseFov = Mathf.Clamp(Mathf.SmoothDamp(
            baseFov, target, ref fovVelocity,
            Mathf.Max(0.01f, fovSmoothTime), Mathf.Infinity, Time.deltaTime), minimum, maximum);
        UpdateViewTransition(Time.unscaledDeltaTime);
        float t = transitioning ? Mathf.Clamp01(transitionElapsed / Mathf.Max(0.01f, transitionDuration)) : 1f;
        speedCamera.Lens.FieldOfView = Mathf.Clamp(baseFov + Mathf.Sin(t * Mathf.PI) * transitionFovKick, minimum, maximum);
        bool shouldHide = hideVehicleInFirstPerson && ViewBlend > 0.55f;
        if (shouldHide && hiddenRenderers.Count == 0)
        {
            var controller = carTarget != null ? carTarget.GetComponentInParent<ArcadeCarController>() : null;
            if (controller != null) foreach (var renderer in controller.GetComponentsInChildren<Renderer>())
            {
                hiddenRenderers[renderer] = renderer.forceRenderingOff;
                renderer.forceRenderingOff = true;
            }
        }
        else if (!shouldHide && hiddenRenderers.Count > 0) RestoreVehicleVisibility();
    }

    // Inspector에서 이 컴포넌트의 메뉴로 실행하면 Play 전에도 설정을 적용합니다.
    [ContextMenu("Apply Camera Settings")]
    public void ApplyCameraSettings()
    {
        if (carTarget == null)
        {
            Debug.LogError("Car Target에 차량 오브젝트를 넣어 주세요.", this);
            return;
        }

        CinemachineCamera cam = GetComponent<CinemachineCamera>();
        speedCamera = cam;
        fovVelocity = 0f;
        CinemachineFollow follow = GetComponent<CinemachineFollow>();
        CinemachineRotationComposer aim =
            GetComponent<CinemachineRotationComposer>();

        cam.Follow = carTarget;
        cam.LookAt = carTarget;
        cam.Lens.FieldOfView = fieldOfView;
        baseFov = fieldOfView;

        follow.FollowOffset = IsFirstPerson ? firstPersonOffset : followOffset;

        var tracking = follow.TrackerSettings;
        tracking.BindingMode = BindingMode.LockToTargetWithWorldUp;
        tracking.AngularDampingMode = AngularDampingMode.Euler;
        tracking.PositionDamping = IsFirstPerson || lockFollowPosition ? Vector3.zero : positionDamping;
        tracking.RotationDamping = IsFirstPerson || lockFollowPosition ? Vector3.zero : new Vector3(0f, yawDamping, 0f);
        follow.TrackerSettings = tracking;

        aim.TargetOffset = IsFirstPerson ? firstPersonOffset + Vector3.forward * firstPersonLookDistance : lookAtOffset;
        aim.Damping = IsFirstPerson || lockFollowPosition ? Vector2.zero : aimDamping;

        var composition = aim.Composition;
        composition.ScreenPosition = Vector2.zero;
        aim.Composition = composition;
    }

    public void ToggleView()
    {
        if (carTarget == null || ViewInputLocked || transitioning) return;
        if (transitionEffect == null) transitionEffect = gameObject.AddComponent<DrivingViewTransition>();
        transitionEffect.Prepare();
        transitionElapsed = 0f;
        transitioning = true;
        viewSwitched = false;
        IsFirstPerson = !IsFirstPerson;
    }

    void UpdateViewTransition(float deltaTime)
    {
        if (!transitioning || transitionEffect == null || !transitionEffect.IsReady) return;
        transitionElapsed += deltaTime;
        float t = Mathf.Clamp01(transitionElapsed / Mathf.Max(.01f, transitionDuration));
        if (!viewSwitched && t >= .5f) {
            // Guarantee a fully black frame even when a slow frame skips the hold.
            if (t > .6f) { t = .6f; transitionElapsed = transitionDuration * t; }
            transitionEffect.Apply(.5f, 1f);
            ViewBlend = IsFirstPerson ? 1f : 0f;
            ViewOpacity = 1f;
            float currentFov = baseFov, currentFovVelocity = fovVelocity;
            ApplyCameraSettings();
            baseFov = currentFov; fovVelocity = currentFovVelocity;
            speedCamera.PreviousStateIsValid = false;
            viewSwitched = true;
            return;
        }
        transitionEffect.Apply(t, 1f);
        if (t >= 1f) {
            transitioning = false;
            transitionEffect.Cancel();
        }
    }

    void RestoreVehicleVisibility()
    {
        foreach (var entry in hiddenRenderers)
            if (entry.Key != null) entry.Key.forceRenderingOff = entry.Value;
        hiddenRenderers.Clear();
    }

    void OnDisable()
    {
        RestoreVehicleVisibility();
        IsFirstPerson = false;
        ViewBlend = 0f;
        ViewOpacity = 1f;
        transitioning = viewSwitched = false;
        if (transitionEffect != null) transitionEffect.Cancel();
        transitionElapsed = Mathf.Max(0.01f, transitionDuration);
        if (carTarget != null) ApplyCameraSettings();
    }
}
