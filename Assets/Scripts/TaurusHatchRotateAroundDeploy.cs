using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// TAURUS 해치 전개용.
/// Pivot 재부모화 없이, 지정한 Hinge Point를 중심으로 Hatch Target 전체를 회전시킵니다.
/// Shift 1회:
/// 1) Hatch Target이 Hinge Point를 중심으로 약 30도 회전
/// 2) Equipment Bay가 아주 살짝 상승
/// </summary>
public class TaurusHatchRotateAroundDeploy : MonoBehaviour
{
    [Header("References")]
    [Tooltip("실제로 열릴 해치 전체 Root")]
    [SerializeField] private Transform hatchTarget;

    [Tooltip("해치 뒤쪽 힌지 위치에 둔 Empty")]
    [SerializeField] private Transform hingePoint;

    [Tooltip("미사일 장착구")]
    [SerializeField] private Transform equipmentBayModule;

    [Header("Hatch")]
    [SerializeField] private float hatchOpenAngle = 30f;
    [SerializeField] private float hatchOpenDuration = 0.8f;

    [Tooltip("월드 기준 회전축. 보통 차량 좌우 방향이면 X축.")]
    [SerializeField] private Vector3 hingeWorldAxis = Vector3.right;

    [SerializeField] private bool reverseDirection = false;

    [Header("Equipment Bay")]
    [Tooltip("아주 살짝만 올라오도록 기본 0.025")]
    [SerializeField] private Vector3 bayDeployLocalOffset = new Vector3(0f, 0.025f, 0f);

    [SerializeField] private float bayDeployDuration = 0.7f;
    [SerializeField] private float delayAfterHatch = 0.1f;

    [Header("Input")]
    [SerializeField] private bool useShiftInput = true;

    private Vector3 hatchClosedPosition;
    private Quaternion hatchClosedRotation;
    private Vector3 bayClosedLocalPosition;

    private bool busy;
    private bool deployed;

    private void Awake()
    {
        if (hatchTarget != null)
        {
            hatchClosedPosition = hatchTarget.position;
            hatchClosedRotation = hatchTarget.rotation;
        }

        if (equipmentBayModule != null)
            bayClosedLocalPosition = equipmentBayModule.localPosition;
    }

    private void Update()
    {
        if (!useShiftInput || busy || deployed)
            return;

        if (ShiftPressedThisFrame())
            StartCoroutine(DeploySequence());
    }

    private IEnumerator DeploySequence()
    {
        if (hatchTarget == null || hingePoint == null || equipmentBayModule == null)
        {
            Debug.LogError("[TAURUS] Hatch Target / Hinge Point / Equipment Bay Module 중 비어 있는 항목이 있습니다.", this);
            yield break;
        }

        busy = true;

        float direction = reverseDirection ? -1f : 1f;
        Vector3 axis = hingeWorldAxis.sqrMagnitude > 0.0001f
            ? hingeWorldAxis.normalized
            : Vector3.right;

        float elapsed = 0f;
        float lastAngle = 0f;
        float duration = Mathf.Max(0.01f, hatchOpenDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);

            float currentAngle = hatchOpenAngle * direction * eased;
            float deltaAngle = currentAngle - lastAngle;

            hatchTarget.RotateAround(hingePoint.position, axis, deltaAngle);

            lastAngle = currentAngle;
            yield return null;
        }

        if (delayAfterHatch > 0f)
            yield return new WaitForSeconds(delayAfterHatch);

        Vector3 bayStart = equipmentBayModule.localPosition;
        Vector3 bayEnd = bayClosedLocalPosition + bayDeployLocalOffset;

        elapsed = 0f;
        duration = Mathf.Max(0.01f, bayDeployDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);

            equipmentBayModule.localPosition = Vector3.Lerp(bayStart, bayEnd, eased);
            yield return null;
        }

        equipmentBayModule.localPosition = bayEnd;

        deployed = true;
        busy = false;
    }

    [ContextMenu("Reset Deployment")]
    public void ResetDeployment()
    {
        StopAllCoroutines();

        if (hatchTarget != null)
        {
            hatchTarget.position = hatchClosedPosition;
            hatchTarget.rotation = hatchClosedRotation;
        }

        if (equipmentBayModule != null)
            equipmentBayModule.localPosition = bayClosedLocalPosition;

        busy = false;
        deployed = false;
    }

    private bool ShiftPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.leftShiftKey.wasPressedThisFrame ||
                   Keyboard.current.rightShiftKey.wasPressedThisFrame;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.LeftShift) ||
               Input.GetKeyDown(KeyCode.RightShift);
#else
        return false;
#endif
    }
}
