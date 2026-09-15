using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// 기존 SplineCarController와 독립된 대형 이동용 컨트롤러입니다.
/// 에디터에서 배치한 위치·높이·회전 차이를 스플라인 기준으로 보존합니다.
/// </summary>
public sealed class SplineFormationController : MonoBehaviour
{
    [Header("Spline")]
    [SerializeField] private SplineContainer splineContainer;

    [Header("Speed")]
    [Min(0f)]
    [SerializeField] private float targetSpeed = 20f;
    [Min(0f)]
    [SerializeField] private float acceleration = 10f;
    [Min(0f)]
    [SerializeField] private float deceleration = 15f;

    [Header("Movement")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop;
    [SerializeField] private bool followRotation = true;

    [Header("Preserve Editor Placement")]
    [Tooltip("현재 배치와 스플라인 시작점 사이의 위치 차이를 자동 보존합니다.")]
    [SerializeField] private bool preserveInitialPositionOffset = true;

    [Tooltip("현재 회전과 스플라인 진행 방향 사이의 회전 차이를 자동 보존합니다.")]
    [SerializeField] private bool preserveInitialRotationOffset = true;

    private Vector3 positionOffsetInSplineSpace;
    private Quaternion rotationOffset = Quaternion.identity;
    private float currentSpeed;
    private float distanceTravelled;
    private float splineLength;
    private bool isPlaying;

    private void Start()
    {
        if (splineContainer == null)
        {
            Debug.LogError("Spline Container가 지정되지 않았습니다.", this);
            enabled = false;
            return;
        }

        splineLength = splineContainer.CalculateLength();
        if (splineLength <= 0f)
        {
            Debug.LogError("Spline 길이가 0입니다.", this);
            enabled = false;
            return;
        }

        Vector3 startPosition = splineContainer.EvaluatePosition(0f);
        Vector3 startTangent = splineContainer.EvaluateTangent(0f);
        Quaternion startRotation = GetPathRotation(startTangent, transform.rotation);

        positionOffsetInSplineSpace = preserveInitialPositionOffset
            ? Quaternion.Inverse(startRotation) * (transform.position - startPosition)
            : Vector3.zero;

        rotationOffset = preserveInitialRotationOffset
            ? Quaternion.Inverse(startRotation) * transform.rotation
            : Quaternion.identity;

        isPlaying = playOnStart;
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        float rate = currentSpeed < targetSpeed ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);
        distanceTravelled += currentSpeed * Time.deltaTime;

        if (distanceTravelled >= splineLength)
        {
            if (loop)
                distanceTravelled %= splineLength;
            else
            {
                distanceTravelled = splineLength;
                currentSpeed = 0f;
                isPlaying = false;
            }
        }

        ApplyPose(distanceTravelled / splineLength);
    }

    private void ApplyPose(float t)
    {
        t = Mathf.Clamp01(t);
        Vector3 pathPosition = splineContainer.EvaluatePosition(t);
        Vector3 tangent = splineContainer.EvaluateTangent(t);
        Quaternion pathRotation = GetPathRotation(tangent, transform.rotation);

        Vector3 finalPosition =
            pathPosition + pathRotation * positionOffsetInSplineSpace;
        Quaternion finalRotation = followRotation
            ? pathRotation * rotationOffset
            : transform.rotation;

        transform.SetPositionAndRotation(finalPosition, finalRotation);
    }

    private static Quaternion GetPathRotation(
        Vector3 tangent,
        Quaternion fallback)
    {
        return tangent.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(tangent.normalized, Vector3.up)
            : fallback;
    }

    public void Play() => isPlaying = true;
    public void Pause() => isPlaying = false;

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
        ApplyPose(0f);
    }

    public void SetSpeed(float speed)
    {
        targetSpeed = Mathf.Max(0f, speed);
    }
}
