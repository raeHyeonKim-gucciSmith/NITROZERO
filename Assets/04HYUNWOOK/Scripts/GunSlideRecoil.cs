using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Call PlayRecoil from the successful shot event.</summary>
public sealed class GunSlideRecoil : MonoBehaviour
{
    public Transform slide;
    public Transform trigger;
    public Transform muzzle;
    [Tooltip("Use the muzzle position to determine the rear direction after FBX axis conversion.")]
    public bool useMuzzleDirection = true;
    [Min(0f)] public float recoilDistance = 0.055f;
    public Vector3 localRecoilOffset = new Vector3(-0.055f, 0f, 0f);
    [Range(0f, 35f)] public float triggerPullDegrees = 16f;
    [Min(0.001f)] public float retractSeconds = 0.035f;
    [Min(0.001f)] public float returnSeconds = 0.085f;
    [Tooltip("Visual preview only; does not fire a projectile.")]
    public bool previewWithLeftMouse;
    Vector3 restPosition;
    Quaternion triggerRestRotation;
    Vector3 recoilOffset, triggerAxis;
    float elapsed;
    bool playing;
    void Awake()
    {
        if (slide == null) slide = FindPart("Slide");
        if (trigger == null) trigger = FindPart("Trigger");
        if (muzzle == null) muzzle = FindPart("Muzzle");
        if (slide != null) restPosition = slide.localPosition;
        if (trigger != null) triggerRestRotation = trigger.localRotation;
    }
    Transform FindPart(string partName)
    {
        foreach (var part in GetComponentsInChildren<Transform>())
            if (part.name == partName) return part;
        return null;
    }
    [ContextMenu("Preview Slide Recoil (Play Mode)")]
    public void PlayRecoil()
    {
        if (!Application.isPlaying || slide == null) return;
        // FBX handedness can reverse X. Determine rear from the actual muzzle instead.
        Vector3 rear = muzzle != null
            ? Vector3.ProjectOnPlane(transform.position - muzzle.position, transform.up).normalized
            : -transform.right;
        if (rear.sqrMagnitude < 0.001f) rear = -transform.right;
        recoilOffset = useMuzzleDirection
            ? (slide.parent != null ? slide.parent.InverseTransformVector(rear * recoilDistance * transform.lossyScale.x) : rear * recoilDistance)
            : localRecoilOffset;
        Vector3 worldAxis = Vector3.Cross(rear, transform.up).normalized;
        if (trigger != null) triggerAxis = trigger.InverseTransformDirection(worldAxis);
        elapsed = 0f;
        playing = true;
    }
    void Update()
    {
        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        pressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        pressed = Input.GetMouseButtonDown(0);
#endif
        if (previewWithLeftMouse && pressed) PlayRecoil();
        if (!playing || slide == null) return;
        elapsed += Time.deltaTime;
        float back = Mathf.Max(0.001f, retractSeconds);
        float forward = Mathf.Max(0.001f, returnSeconds);
        float amount = elapsed < back ? Mathf.SmoothStep(0f, 1f, elapsed / back)
            : Mathf.SmoothStep(1f, 0f, (elapsed - back) / forward);
        slide.localPosition = restPosition + recoilOffset * amount;
        if (trigger != null) trigger.localRotation = triggerRestRotation * Quaternion.AngleAxis(triggerPullDegrees * amount, triggerAxis);
        if (elapsed >= back + forward) Restore();
    }
    void Restore()
    {
        if (slide != null) slide.localPosition = restPosition;
        if (trigger != null) trigger.localRotation = triggerRestRotation;
        playing = false;
    }
    void OnDisable() { if (playing) Restore(); }
}

