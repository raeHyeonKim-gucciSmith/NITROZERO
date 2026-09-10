using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TAURUS 차량의 바퀴 시각 회전 테스트용.
/// 이 컴포넌트를 차량 최상위 Root(예: Interferer_Car_ROOT)에 붙이면
/// 하위에서 tire_001_R17* / rim_1421* 오브젝트를 자동으로 찾아
/// Play가 시작되는 즉시 빠르게 회전시키며 계속 유지합니다.
/// </summary>
public class TaurusWheelSpinTest : MonoBehaviour
{
    [Header("Auto Find")]
    [Tooltip("Scene 파일에서 확인된 타이어 이름 접두사입니다.")]
    [SerializeField] private string tireNamePrefix = "tire_001_R17";

    [Tooltip("Scene 파일에서 확인된 림 이름 접두사입니다.")]
    [SerializeField] private string rimNamePrefix = "rim_1421";

    [Header("Spin")]
    [Tooltip("Play 시작 후 계속 유지할 목표 회전 속도 (degree/sec).")]
    [SerializeField] private float spinSpeed = 3600f;

    [Tooltip("키를 눌렀다/뗄 때 속도가 부드럽게 변하는 정도.")]
    [SerializeField] private float acceleration = 18000f;

    [Tooltip("현재 모델의 바퀴 축. 첨부 Scene의 배치 기준 기본값은 Local X입니다.")]
    [SerializeField] private Vector3 localSpinAxis = Vector3.right;

    [Tooltip("회전 방향을 반대로 바꾸려면 체크.")]
    [SerializeField] private bool reverseDirection = false;

    [Header("Debug")]
    [SerializeField] private bool logFoundObjects = true;

    private readonly List<Transform> wheelVisuals = new List<Transform>();
    private float currentSpeed;

    private void Awake()
    {
        FindWheelVisuals();
    }

    private void OnEnable()
    {
        // Prefab 재연결/Hierarchy 변경 후에도 다시 잡히도록 보강.
        if (wheelVisuals.Count == 0)
            FindWheelVisuals();
    }

    private void Update()
    {
        // Play가 시작되면 입력 없이 항상 회전한다.
        float targetSpeed = spinSpeed;
        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            acceleration * Time.deltaTime
        );

        if (Mathf.Approximately(currentSpeed, 0f) || wheelVisuals.Count == 0)
            return;

        float direction = reverseDirection ? -1f : 1f;
        float angle = currentSpeed * direction * Time.deltaTime;

        Vector3 axis = localSpinAxis.sqrMagnitude > 0.0001f
            ? localSpinAxis.normalized
            : Vector3.right;

        for (int i = 0; i < wheelVisuals.Count; i++)
        {
            Transform wheel = wheelVisuals[i];
            if (wheel != null)
                wheel.Rotate(axis, angle, Space.Self);
        }
    }

    [ContextMenu("Re-Find Wheel Visuals")]
    public void FindWheelVisuals()
    {
        wheelVisuals.Clear();

        Transform[] all = GetComponentsInChildren<Transform>(true);

        int tireCount = 0;
        int rimCount = 0;

        foreach (Transform t in all)
        {
            if (t == transform)
                continue;

            if (t.name.StartsWith(tireNamePrefix, System.StringComparison.Ordinal))
            {
                AddUnique(t);
                tireCount++;
            }
            else if (t.name.StartsWith(rimNamePrefix, System.StringComparison.Ordinal))
            {
                AddUnique(t);
                rimCount++;
            }
        }

        if (logFoundObjects)
        {
            Debug.Log(
                $"[TaurusWheelSpinTest] Found {tireCount} tire object(s), " +
                $"{rimCount} rim object(s), total rotating visuals: {wheelVisuals.Count}.",
                this
            );

            foreach (Transform t in wheelVisuals)
                Debug.Log($"[TaurusWheelSpinTest] Wheel visual: {GetPath(t)}", t);
        }

        if (tireCount < 4)
        {
            Debug.LogWarning(
                $"[TaurusWheelSpinTest] 타이어를 {tireCount}개만 찾았습니다. " +
                $"정상 차량이라면 '{tireNamePrefix}'로 시작하는 타이어가 4개 있어야 합니다. " +
                "현재 Prefab이 Missing 상태라면 먼저 차량 Prefab을 복구하세요.",
                this
            );
        }
    }

    private void AddUnique(Transform t)
    {
        if (!wheelVisuals.Contains(t))
            wheelVisuals.Add(t);
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        Transform p = t.parent;

        while (p != null)
        {
            path = p.name + "/" + path;
            p = p.parent;
        }

        return path;
    }
}
