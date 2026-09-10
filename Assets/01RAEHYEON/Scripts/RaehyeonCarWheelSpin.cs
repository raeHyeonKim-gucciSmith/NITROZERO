using UnityEngine;

public sealed class RaehyeonCarWheelSpin : MonoBehaviour
{
    [Header("Wheel Spin")]
    [SerializeField] private float degreesPerSecond = 1440f;
    [SerializeField] private float acceleration = 2880f;
    [SerializeField] private bool reverseDirection;

    [Header("Drift Brake")]
    [SerializeField] private KeyCode brakeToggleKey = KeyCode.S;
    [SerializeField] private bool allowKeyboardControl = true;

    private readonly Transform[] wheels = new Transform[4];
    private static readonly string[] WheelNames = { "FL", "FR", "RL", "RR" };
    private float currentSpeed;
    private bool wheelsLocked;

    private void Awake() => FindWheels();

    private void OnEnable()
    {
        currentSpeed = 0f;
        if (!HasAllWheels()) FindWheels();
    }

    private void Update()
    {
        if (allowKeyboardControl && IsBrakeKeyPressed())
            SetWheelLocked(!wheelsLocked);

        if (wheelsLocked) return;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            Mathf.Max(0f, degreesPerSecond),
            Mathf.Max(0f, acceleration) * Time.deltaTime);

        float angle = currentSpeed * (reverseDirection ? -1f : 1f) * Time.deltaTime;
        Vector3 worldAxle = transform.right;
        for (int i = 0; i < wheels.Length; i++)
            if (wheels[i] != null) wheels[i].Rotate(worldAxle, angle, Space.World);
    }

    public void SlamStop()
    {
        wheelsLocked = true;
        currentSpeed = 0f;
    }

    public void ResumeSpin()
    {
        wheelsLocked = false;
        currentSpeed = 0f;
    }

    public void SetWheelLocked(bool locked)
    {
        if (locked) SlamStop(); else ResumeSpin();
    }

    private void FindWheels()
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < WheelNames.Length; i++)
        {
            wheels[i] = null;
            for (int j = 0; j < children.Length; j++)
                if (children[j].name == WheelNames[i]) { wheels[i] = children[j]; break; }
        }

        if (!HasAllWheels())
            Debug.LogWarning($"[RaehyeonCarWheelSpin] {name}: FL/FR/RL/RR 바퀴를 모두 찾지 못했습니다.", this);
    }

    private bool HasAllWheels()
    {
        for (int i = 0; i < wheels.Length; i++) if (wheels[i] == null) return false;
        return true;
    }

    private bool IsBrakeKeyPressed()
    {
        try { if (Input.GetKeyDown(brakeToggleKey)) return true; } catch { }
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && brakeToggleKey == KeyCode.S) return keyboard.sKey.wasPressedThisFrame;
#endif
        return false;
    }
}
