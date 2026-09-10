using UnityEngine;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
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
    [Min(0.01f)] public float transitionDuration = 0.3f;
    [Range(0f, 10f)] public float transitionFovKick = 4f;
    public bool startInFirstPerson = true;
    public bool ViewInputLocked { get; set; }
    public bool IsFirstPerson { get; private set; }
    public float ViewBlend { get; private set; }
    float transitionStart, transitionElapsed = 1f, baseFov;
    CinemachineFollow viewFollow;
    CinemachineRotationComposer viewAim;
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
        ViewBlend = transitionStart = IsFirstPerson ? 1f : 0f;
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
        transitionElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(transitionElapsed / Mathf.Max(0.01f, transitionDuration));
        ViewBlend = Mathf.Lerp(transitionStart, IsFirstPerson ? 1f : 0f, t * t * (3f - 2f * t));
        if (viewFollow != null) viewFollow.FollowOffset = Vector3.Lerp(followOffset, firstPersonOffset, ViewBlend);
        if (viewAim != null) viewAim.TargetOffset = Vector3.Lerp(lookAtOffset, firstPersonOffset + Vector3.forward * firstPersonLookDistance, ViewBlend);
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
        viewFollow = follow;
        viewAim = aim;

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
        if (carTarget == null || ViewInputLocked) return;
        transitionStart = ViewBlend;
        transitionElapsed = 0f;
        IsFirstPerson = !IsFirstPerson;
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
        ViewBlend = transitionStart = 0f;
        transitionElapsed = Mathf.Max(0.01f, transitionDuration);
        if (carTarget != null) ApplyCameraSettings();
    }
}
