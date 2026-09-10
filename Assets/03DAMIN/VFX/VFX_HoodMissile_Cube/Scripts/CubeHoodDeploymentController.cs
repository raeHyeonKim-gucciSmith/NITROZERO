using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Damin.VFX.HoodMissile.CubePreview
{
    /// <summary>Standalone cube preview. No references to the production vehicle/controller.</summary>
    public sealed class CubeHoodDeploymentController : MonoBehaviour
    {
        [Header("큐브 모형 연결")]
        [SerializeField] private Transform hoodHinge;
        [SerializeField] private Transform equipmentBay;
        [SerializeField] private Transform leftVentPoint;
        [SerializeField] private Transform rightVentPoint;
        [SerializeField] private Transform missileSpawnPoint;
        [SerializeField] private Transform interiorSmokeVolume;

        [Header("보넷 개방 — Amount 1 = 측정한 원본 각도")]
        [Range(0f, 1f)] [SerializeField] private float hoodOpenAmount = 0.25f;
        [SerializeField] private float referenceOpenAngle = 30f;
        [Min(0.01f)] [SerializeField] private float hoodOpenDuration = 0.8f;
        [SerializeField] private Vector3 hingeLocalAxis = Vector3.left;
        [Header("장착구 상승 — 보넷 개방 후 시작")]
        [SerializeField] private Vector3 bayDeployLocalOffset = new Vector3(0f, 0.025f, 0f);
        [Min(0f)] [SerializeField] private float delayAfterHatch = 0.1f;
        [Min(0.01f)] [SerializeField] private float bayDeployDuration = 0.7f;

        [Header("미리보기 입력 / 나중에 VFX 이벤트 연결")]
        [SerializeField] private bool useShiftInput = true;
        [SerializeField] private bool useRToReset = true;
        [SerializeField] private UnityEvent onDeploymentStarted = new UnityEvent();
        [SerializeField] private UnityEvent onBayReady = new UnityEvent();
        [SerializeField] private UnityEvent onReset = new UnityEvent();

        [SerializeField, HideInInspector] private Quaternion closedHingeRotation = Quaternion.identity;
        [SerializeField, HideInInspector] private Vector3 closedBayPosition;
        [SerializeField, HideInInspector] private bool poseCaptured;
        private float elapsed;
        private bool running;
        private bool deployed;
        public bool IsDeploying => running;
        public bool IsDeployed => deployed;
        public float TotalDuration => Mathf.Max(.01f, hoodOpenDuration) + Mathf.Max(0f, delayAfterHatch) + Mathf.Max(.01f, bayDeployDuration);
        public Transform LeftVentPoint => leftVentPoint;
        public Transform RightVentPoint => rightVentPoint;
        public Transform MissileSpawnPoint => missileSpawnPoint;
        public Transform InteriorSmokeVolume => interiorSmokeVolume;

        private void Awake()
        {
            if (!poseCaptured && hoodHinge && equipmentBay) CaptureClosedPose();
        }

        private void Update()
        {
            if (useRToReset && ResetPressed()) { ResetPreview(); return; }
            if (useShiftInput && ShiftPressed()) StartDeployment();
            AdvancePreview(Time.deltaTime);
        }

        [ContextMenu("Capture Closed Pose (only when closed)")]
        public void CaptureClosedPose()
        {
            if (!hoodHinge || !equipmentBay || running || deployed) return;
            closedHingeRotation = hoodHinge.localRotation;
            closedBayPosition = equipmentBay.localPosition;
            poseCaptured = true;
        }

        public void StartDeployment()
        {
            if (running || deployed) return;
            if (!hoodHinge || !equipmentBay)
            {
                Debug.LogError("[Hood Cube Preview] Hood Hinge / Equipment Bay reference is missing.", this);
                return;
            }
            if (!poseCaptured) CaptureClosedPose();
            elapsed = 0f;
            running = true;
            ApplyPose(0f);
            onDeploymentStarted.Invoke();
        }

        // Explicit step is also used by the isolated authoring verification.
        public void AdvancePreview(float deltaTime)
        {
            if (!running) return;
            elapsed = Mathf.Min(TotalDuration, elapsed + Mathf.Max(0f, deltaTime));
            ApplyPose(elapsed);
            if (elapsed < TotalDuration) return;
            running = false;
            deployed = true;
            onBayReady.Invoke();
        }

        private void ApplyPose(float time)
        {
            Vector3 axis = hingeLocalAxis.sqrMagnitude > .0001f ? hingeLocalAxis.normalized : Vector3.left;
            float hoodT = Smooth(Mathf.Clamp01(time / Mathf.Max(.01f, hoodOpenDuration)));
            hoodHinge.localRotation = closedHingeRotation * Quaternion.AngleAxis(referenceOpenAngle * Mathf.Clamp01(hoodOpenAmount) * hoodT, axis);
            float bayT = Smooth(Mathf.Clamp01((time - Mathf.Max(.01f, hoodOpenDuration) - Mathf.Max(0f, delayAfterHatch)) / Mathf.Max(.01f, bayDeployDuration)));
            equipmentBay.localPosition = closedBayPosition + bayDeployLocalOffset * bayT;
        }

        [ContextMenu("Reset Preview")]
        public void ResetPreview()
        {
            running = false; deployed = false; elapsed = 0f;
            if (poseCaptured)
            {
                if (hoodHinge) hoodHinge.localRotation = closedHingeRotation;
                if (equipmentBay) equipmentBay.localPosition = closedBayPosition;
            }
            onReset.Invoke();
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);
        private static bool ShiftPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null) return Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
#else
            return false;
#endif
        }
        private static bool ResetPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null) return Keyboard.current.rKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.R);
#else
            return false;
#endif
        }

        private void OnDrawGizmosSelected()
        {
            if (hoodHinge) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(hoodHinge.position, .035f); }
            DrawPoint(leftVentPoint, Color.cyan); DrawPoint(rightVentPoint, Color.cyan); DrawPoint(missileSpawnPoint, Color.red);
            if (interiorSmokeVolume)
            {
                var old = Gizmos.matrix; Gizmos.matrix = interiorSmokeVolume.localToWorldMatrix;
                Gizmos.color = new Color(.5f, 1f, .5f, .8f); Gizmos.DrawWireCube(Vector3.zero, Vector3.one); Gizmos.matrix = old;
            }
        }
        private static void DrawPoint(Transform point, Color color)
        {
            if (!point) return; Gizmos.color = color; Gizmos.DrawWireSphere(point.position, .025f);
            Gizmos.DrawLine(point.position, point.position + point.forward * .15f);
        }
    }
}
