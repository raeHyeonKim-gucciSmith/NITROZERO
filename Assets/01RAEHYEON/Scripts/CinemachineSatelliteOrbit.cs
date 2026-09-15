using UnityEngine;

/// <summary>
/// 재생 시작 시 카메라가 바라보는 정면 방향으로 천천히 직선 이동합니다.
/// Cinemachine Camera의 부모 Rig에 붙여 사용합니다.
/// </summary>
public class CinemachineSatelliteOrbit : MonoBehaviour
{
    [Header("Forward Movement")]
    [Tooltip("정면으로 이동할 전체 거리")]
    [Min(0f)]
    [SerializeField] private float travelDistance = 30f;

    [Tooltip("전체 거리를 이동하는 시간. 값이 클수록 느립니다.")]
    [Min(0.01f)]
    [SerializeField] private float travelDuration = 40f;

    [Tooltip("처음과 끝에서 부드럽게 가속·감속")]
    [SerializeField] private AnimationCurve movementCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Space Floating")]
    [Tooltip("이동 중 천천히 기우는 최대 각도")]
    [Range(0f, 20f)]
    [SerializeField] private float rollAmount = 3f;

    [Tooltip("기울어지는 속도")]
    [Min(0f)]
    [SerializeField] private float rollSpeed = 0.08f;

    [Header("Playback")]
    [SerializeField] private bool playOnStart = true;

    private Vector3 startPosition;
    private Vector3 forwardDirection;
    private Quaternion startRotation;
    private float elapsed;
    private bool playing;
    private bool captured;

    private void Awake()
    {
        CaptureStart();
        playing = playOnStart;
    }

    private void LateUpdate()
    {
        if (!playing)
            return;

        if (!captured)
            CaptureStart();

        elapsed += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(elapsed / travelDuration);
        float distanceT = movementCurve.Evaluate(normalizedTime);

        // 시작할 때 바라보던 정면 방향으로만 이동합니다.
        transform.position =
            startPosition + forwardDirection * (travelDistance * distanceT);

        // 진행 방향은 바꾸지 않고 카메라의 앞축을 중심으로만 천천히 기울입니다.
        float roll = Mathf.Sin(elapsed * rollSpeed * Mathf.PI * 2f) * rollAmount;
        transform.rotation =
            startRotation * Quaternion.AngleAxis(roll, Vector3.forward);

        if (normalizedTime >= 1f)
            playing = false;
    }

    private void CaptureStart()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        forwardDirection = transform.forward.normalized;
        elapsed = 0f;
        captured = true;
    }

    public void Play()
    {
        if (!captured)
            CaptureStart();
        playing = true;
    }

    public void Pause() => playing = false;

    public void Restart()
    {
        CaptureStart();
        playing = true;
    }
}
