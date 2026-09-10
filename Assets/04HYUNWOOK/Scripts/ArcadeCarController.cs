using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class ArcadeCarController : MonoBehaviour
{
    [Header("Speed (km/h)")]
    [Min(1f)] public float maxForwardSpeed = 160f;
    [Min(1f)] public float maxReverseSpeed = 30f;

    [Header("Manual Gearbox - Q / E")]
    [Tooltip("1~6단의 최고 속도(km/h). 전체 최고 속도 이하로 적용됩니다.")]
    public float[] gearSpeedLimits = { 50f, 100f, 150f, 200f, 250f, 300f };
    [Range(1, 6)] public int startingGear = 1;
    [Min(0.01f)] public float shiftCooldown = 0.15f;
    [Min(1f)] public float gearLimitDeceleration = 20f;
    public AudioClip gearShiftSound;
    [Range(0f,1f)] public float gearShiftVolume = 0.45f;
    public int CurrentGear { get; private set; } = 1;
    public bool ControlsLocked { get; set; }
    public float CurrentGearSpeedLimit => GetGearSpeedLimit(CurrentGear);
    public float EngineSpeedRatio => Mathf.Clamp01(SpeedKmh / Mathf.Max(1f, CurrentGearSpeedLimit));
    float nextShiftTime;
    AudioSource shiftAudio;

    public float GetGearSpeedLimit(int gear)
    {
        float limit = 1f;
        float maximum = Mathf.Max(1f, maxForwardSpeed);
        for (int i = 0; i < Mathf.Clamp(gear,1,6); i++)
        {
            float configured = gearSpeedLimits != null && i < gearSpeedLimits.Length ? gearSpeedLimits[i] : (i+1)*50f;
            if (float.IsNaN(configured) || float.IsInfinity(configured)) configured = (i+1)*50f;
            limit = Mathf.Clamp(configured, limit, maximum);
        }
        return limit;
    }

    public bool ShiftGear(int direction)
    {
        if (ControlsLocked || Time.timeScale <= 0f || direction == 0 || Time.time < nextShiftTime) return false;
        int gear = Mathf.Clamp(CurrentGear + (direction > 0 ? 1 : -1), 1, 6);
        if (gear == CurrentGear) return false;
        CurrentGear = gear;
        nextShiftTime = Time.time + Mathf.Max(0.01f, shiftCooldown);
        if (shiftAudio != null && gearShiftSound != null) shiftAudio.PlayOneShot(gearShiftSound, gearShiftVolume);
        return true;
    }

    public static Vector3 LimitForwardVelocity(Vector3 velocity, Vector3 forward, float maximumKmh, float deceleration, float dt)
    {
        Vector3 planar = Vector3.ProjectOnPlane(velocity, Vector3.up);
        float maximum = Mathf.Max(1f, maximumKmh) / 3.6f;
        if (Vector3.Dot(planar, forward) <= 0f || planar.magnitude <= maximum) return velocity;
        float capped = Mathf.MoveTowards(planar.magnitude, maximum, Mathf.Max(1f, deceleration) * Mathf.Max(0f,dt));
        return velocity + planar.normalized * (capped - planar.magnitude);
    }

    [Header("Acceleration / Brake")]
    [Min(0f)] public float acceleration = 12f;
    [Min(0f)] public float reverseAcceleration = 6f;
    [Min(0f)] public float brakeDeceleration = 25f;
    [Min(0f)] public float coastDeceleration = 1.5f;

    [Header("Steering / Grip")]
    [Min(0f)] public float steeringSpeed = 75f;
    [Range(0.1f, 1f)] public float highSpeedSteering = 0.35f;
    [Min(0f)] public float lateralGrip = 8f;
    [Range(0f, 45f)] public float maxWheelSteerAngle = 30f;
    [Tooltip("구동/제동 방향 타이어 접지력입니다. 가속 시 과도한 휠스핀을 줄입니다.")]
    [Min(0.1f)] public float forwardTireGrip = 3f;

    [Header("Straight-line stability")]
    [Tooltip("조향하지 않을 때 남은 좌우 회전과 미끄러짐을 줄입니다. 도로 방향으로 강제 회전하지 않습니다.")]
    [Min(0f)] public float straightLineStability = 5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundMask;
    [Min(0.01f)] public float groundCheckDistance = 0.6f;

    [Header("Respawn")]
    [Tooltip("낙하 시 복귀할 위치와 방향입니다.")]
    public Transform spawnPoint;
    [Tooltip("월드 Y 좌표가 이 값 이하이면 복귀합니다.")]
    public float respawnBelowY = -15f;

    private Rigidbody body;
    private float throttleInput;
    private float steeringInput;
    private bool brakeInput;
    private WheelCollider[] driveWheels;

    public bool IsGrounded { get; private set; }
    // Explicit brake or opposing throttle, excluding passive coasting resistance.
    public bool IsBraking => body != null && (brakeInput ||
        (throttleInput * Vector3.Dot(body.linearVelocity, transform.forward) < 0f
         && Mathf.Abs(Vector3.Dot(body.linearVelocity, transform.forward)) > 0.5f));

    // 1 Unity unit = 1 m 기준. 수직 낙하 속도를 제외한 지면 방향 속도입니다.
    public float SpeedKmh => body == null ? 0f :
        Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude * 3.6f;

    // Keep valid Inspector assignments; restore only missing migration settings.
    private void ResolveGroundSettings()
    {
        if (groundCheck == null)
            groundCheck = transform.Find("GroundCheck");
        if (groundMask.value == 0)
            groundMask = LayerMask.GetMask("Ground");
    }

    private void Reset()
    {
        ResolveGroundSettings();
    }

    private void OnValidate()
    {
        ResolveGroundSettings();
    }

    private void Awake()
    {
        ResolveGroundSettings();
        body = GetComponent<Rigidbody>();
        CurrentGear = Mathf.Clamp(startingGear, 1, 6);
        shiftAudio = gameObject.AddComponent<AudioSource>();
        shiftAudio.playOnAwake = false;
        shiftAudio.spatialBlend = 0f;
        shiftAudio.dopplerLevel = 0f;
        driveWheels = GetComponentsInChildren<WheelCollider>();
        foreach (WheelCollider wheel in driveWheels)
        {
            if (wheel.attachedRigidbody != body) continue;
            // More solver steps avoid abrupt tire-force changes at speed and on mesh edges.
            wheel.ConfigureVehicleSubsteps(5f, 5, 8);
            var friction = wheel.forwardFriction;
            friction.stiffness = forwardTireGrip;
            wheel.forwardFriction = friction;
        }

        if (groundCheck == null || groundMask.value == 0)
        {
            Debug.LogError("Ground Check와 Ground Mask를 지정해 주세요.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        throttleInput = 0f;
        steeringInput = 0f;
        brakeInput = false;
        if (ControlsLocked) { brakeInput = true; return; }

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.qKey.wasPressedThisFrame) ShiftGear(-1);
        else if (keyboard.eKey.wasPressedThisFrame) ShiftGear(1);

        throttleInput = (keyboard.wKey.isPressed ? 1f : 0f)
                      - (keyboard.sKey.isPressed ? 1f : 0f);
        steeringInput = (keyboard.dKey.isPressed ? 1f : 0f)
                      - (keyboard.aKey.isPressed ? 1f : 0f);
        brakeInput = keyboard.spaceKey.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Q)) ShiftGear(-1);
        else if (Input.GetKeyDown(KeyCode.E)) ShiftGear(1);
        throttleInput = (Input.GetKey(KeyCode.W) ? 1f : 0f)
                      - (Input.GetKey(KeyCode.S) ? 1f : 0f);
        steeringInput = (Input.GetKey(KeyCode.D) ? 1f : 0f)
                      - (Input.GetKey(KeyCode.A) ? 1f : 0f);
        brakeInput = Input.GetKey(KeyCode.Space);
#endif
    }

    private void FixedUpdate()
    {
        if (body.position.y <= respawnBelowY && spawnPoint != null)
        {
            Respawn();
            return;
        }

        if (ControlsLocked) { throttleInput = steeringInput = 0f; brakeInput = true; }
        // Smooth engine limiting also handles downhill overspeed and downshifts.
        body.linearVelocity = LimitForwardVelocity(body.linearVelocity, transform.forward,
            CurrentGearSpeedLimit, gearLimitDeceleration, Time.fixedDeltaTime);

        // WheelCollider 차량은 바퀴의 토크/조향으로 구동합니다.
        // AddForce만 적용하면 구동 토크가 없는 바퀴의 정지 마찰에 막힐 수 있습니다.
        if (driveWheels != null && driveWheels.Length > 0)
        {
            DriveWithWheels();
            return;
        }

        // 공중에서는 엔진 가속, 제동, 조향, 가로 미끄러짐 보정을 하지 않습니다.
        IsGrounded = Physics.Raycast(
            groundCheck.position, Vector3.down,
            groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);

        if (!IsGrounded) return;

        float dt = Time.fixedDeltaTime;
        Vector3 forward = body.rotation * Vector3.forward;
        Vector3 right = body.rotation * Vector3.right;
        float forwardSpeed = Vector3.Dot(body.linearVelocity, forward);
        float sidewaysSpeed = Vector3.Dot(body.linearVelocity, right);

        float driveAcceleration = CalculateDriveAcceleration(forwardSpeed, dt);

        // 차체 옆으로 미끄러지는 속도를 줄입니다.
        float sidewaysAcceleration = -sidewaysSpeed * Mathf.Min(lateralGrip, 1f / dt);
        body.AddForce(
            forward * driveAcceleration + right * sidewaysAcceleration,
            ForceMode.Acceleration);

        // 정지 상태의 제자리 회전을 막고, 고속에서는 조향을 약하게 합니다.
        if (Mathf.Abs(forwardSpeed) > 0.1f)
        {
            float lowSpeedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 3f);
            float speedRatio = Mathf.Clamp01(
                Mathf.Abs(forwardSpeed) / (maxForwardSpeed / 3.6f));
            float steeringFactor = Mathf.Lerp(1f, highSpeedSteering, speedRatio);

            // 후진할 때는 차체의 회전 방향도 반대로 적용됩니다.
            float turn = steeringInput * steeringSpeed * lowSpeedFactor
                       * steeringFactor * Mathf.Sign(forwardSpeed) * dt;

            body.MoveRotation(body.rotation * Quaternion.Euler(0f, turn, 0f));
        }
    }

    private void DriveWithWheels()
    {
        int activeWheels = 0;
        int groundedWheels = 0;
        IsGrounded = false;
        foreach (WheelCollider wheel in driveWheels)
        {
            if (wheel == null || !wheel.enabled || !wheel.gameObject.activeInHierarchy
                || wheel.attachedRigidbody != body) continue;
            activeWheels++;
            if (wheel.GetGroundHit(out WheelHit hit)
                && (groundMask.value & (1 << hit.collider.gameObject.layer)) != 0)
            {
                groundedWheels++;
                IsGrounded = true;
            }
        }
        if (activeWheels == 0 || !IsGrounded)
        {
            ClearWheelForces();
            return;
        }

        // Only stabilize with solid contact and no steering command. Preserve the driver's heading.
        if (groundedWheels >= 3 && Mathf.Abs(steeringInput) < 0.01f && straightLineStability > 0f)
        {
            float keep = Mathf.Exp(-straightLineStability * Time.fixedDeltaTime);
            Vector3 angular = body.angularVelocity;
            body.angularVelocity = angular - Vector3.up * Vector3.Dot(angular, Vector3.up) * (1f - keep);
            Vector3 lateral = transform.right * Vector3.Dot(body.linearVelocity, transform.right);
            body.linearVelocity -= lateral * (1f - keep);
        }
        float speed = Vector3.Dot(body.linearVelocity, transform.forward);
        bool oppositeInput = throttleInput * speed < 0f && Mathf.Abs(speed) > 0.5f;
        bool braking = brakeInput || oppositeInput || Mathf.Approximately(throttleInput, 0f);
        float deceleration = brakeInput || oppositeInput ? brakeDeceleration : coastDeceleration;
        float driveAcceleration = braking ? 0f : CalculateDriveAcceleration(speed, Time.fixedDeltaTime);
        float speedRatio = Mathf.Clamp01(Mathf.Abs(speed) / (maxForwardSpeed / 3.6f));
        float steerAngle = steeringInput * maxWheelSteerAngle
                         * Mathf.Lerp(1f, highSpeedSteering, speedRatio);

        foreach (WheelCollider wheel in driveWheels)
        {
            if (wheel == null || wheel.attachedRigidbody != body) continue;
            bool active = wheel.enabled && wheel.gameObject.activeInHierarchy;
            float torquePerAcceleration = body.mass * wheel.radius / activeWheels;
            wheel.motorTorque = active ? driveAcceleration * torquePerAcceleration : 0f;
            wheel.brakeTorque = active && braking ? deceleration * torquePerAcceleration : 0f;
            // 이 차량은 로컬 +Z 쪽 두 바퀴가 앞바퀴입니다.
            wheel.steerAngle = active && transform.InverseTransformPoint(wheel.transform.position).z > 0f
                ? steerAngle : 0f;
        }
    }

    private void ClearWheelForces()
    {
        if (driveWheels == null) return;
        foreach (WheelCollider wheel in driveWheels)
        {
            if (wheel == null || wheel.attachedRigidbody != body) continue;
            wheel.motorTorque = 0f;
            wheel.brakeTorque = 0f;
            wheel.steerAngle = 0f;
        }
    }

    public void Respawn()
    {
        if (body == null || spawnPoint == null) return;

        CurrentGear = Mathf.Clamp(startingGear, 1, 6);
        nextShiftTime = 0f;
        Vector3 positionDelta = spawnPoint.position - body.position;
        ClearWheelForces();
        throttleInput = steeringInput = 0f;
        brakeInput = false;
        IsGrounded = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = spawnPoint.position;
        body.rotation = spawnPoint.rotation;
        // Teleport the interpolated transform too, so the old falling pose is not displayed.
        transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        body.WakeUp();
        Unity.Cinemachine.CinemachineCore.OnTargetObjectWarped(transform, positionDelta);
    }

    private float CalculateDriveAcceleration(float speed, float dt)
    {
        // 진행 방향과 반대 키를 누르면 먼저 제동하고, 이후 반대 방향으로 출발합니다.
        bool oppositeInput = throttleInput * speed < 0f && Mathf.Abs(speed) > 0.5f;

        if (brakeInput || oppositeInput)
        {
            return -Mathf.Sign(speed)
                 * Mathf.Min(brakeDeceleration, Mathf.Abs(speed) / dt);
        }

        if (throttleInput > 0f)
        {
            float remainingSpeed = Mathf.Max(0f, CurrentGearSpeedLimit / 3.6f - speed);
            return Mathf.Min(acceleration, remainingSpeed / dt);
        }

        if (throttleInput < 0f)
        {
            float remainingSpeed = Mathf.Max(0f, maxReverseSpeed / 3.6f + speed);
            return -Mathf.Min(reverseAcceleration, remainingSpeed / dt);
        }

        // 가속 키를 놓으면 서서히 감속합니다.
        return -Mathf.Sign(speed)
             * Mathf.Min(coastDeceleration, Mathf.Abs(speed) / dt);
    }

    private void OnDisable()
    {
        ClearWheelForces();

        throttleInput = 0f;
        steeringInput = 0f;
        brakeInput = false;
        IsGrounded = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(groundCheck.position,
            groundCheck.position + Vector3.down * groundCheckDistance);
    }
}
