using UnityEngine;
using UnityEngine.Splines;

public class SplineCarController : MonoBehaviour
{
    [Header("Spline")]
    [SerializeField] private SplineContainer splineContainer;

    [Header("Speed")]
    [Tooltip("목표 이동 속도 (m/s)")]
    [SerializeField] private float targetSpeed = 20f;

    [Tooltip("가속도 (m/s²)")]
    [SerializeField] private float acceleration = 10f;

    [Tooltip("감속도 (m/s²)")]
    [SerializeField] private float deceleration = 15f;

    [Header("Movement")]
    [Tooltip("시작 시 자동으로 이동")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("Spline 끝에 도착하면 처음부터 반복")]
    [SerializeField] private bool loop = false;

    [Tooltip("Spline 진행 방향으로 차량 회전")]
    [SerializeField] private bool followRotation = true;

    [Tooltip("대형의 실제 전방축이 Unity +Z와 다를 때 사용하는 로컬 회전 보정값")]
    [SerializeField] private Vector3 rotationOffsetEuler;

    private float currentSpeed;
    private float distanceTravelled;
    private float splineLength;

    private bool isPlaying;

    private void Start()
    {
        if (splineContainer == null)
        {
            Debug.LogError("SplineContainer가 지정되지 않았습니다.");
            enabled = false;
            return;
        }

        splineLength = splineContainer.CalculateLength();
        isPlaying = playOnStart;
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        UpdateSpeed();
        MoveAlongSpline();
    }

    private void UpdateSpeed()
    {
        // 목표 속도까지 부드럽게 가속 / 감속
        float changeRate =
            currentSpeed < targetSpeed
            ? acceleration
            : deceleration;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            changeRate * Time.deltaTime
        );
    }

    private void MoveAlongSpline()
    {
        if (splineLength <= 0f)
            return;

        // 실제 이동 거리
        distanceTravelled += currentSpeed * Time.deltaTime;

        if (distanceTravelled >= splineLength)
        {
            if (loop)
            {
                distanceTravelled %= splineLength;
            }
            else
            {
                distanceTravelled = splineLength;
                currentSpeed = 0f;
                isPlaying = false;
            }
        }

        float t = Mathf.Clamp01(
            distanceTravelled / splineLength
        );

        Vector3 position =
            splineContainer.EvaluatePosition(t);

        Vector3 tangent =
            splineContainer.EvaluateTangent(t);

        transform.position = position;

        if (followRotation && tangent.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    tangent.normalized,
                    Vector3.up
                );

            transform.rotation = targetRotation * Quaternion.Euler(rotationOffsetEuler);
        }
    }

    // ============================
    // 외부 제어용
    // ============================

    public void SetSpeed(float speed)
    {
        targetSpeed = Mathf.Max(0f, speed);
    }

    public void Play()
    {
        isPlaying = true;
    }

    public void Pause()
    {
        isPlaying = false;
    }

    public void Stop()
    {
        isPlaying = false;
        currentSpeed = 0f;
    }

    public void Restart()
    {
        distanceTravelled = 0f;
        currentSpeed = 0f;
        isPlaying = true;
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    public float GetProgress()
    {
        if (splineLength <= 0f)
            return 0f;

        return distanceTravelled / splineLength;
    }
}
