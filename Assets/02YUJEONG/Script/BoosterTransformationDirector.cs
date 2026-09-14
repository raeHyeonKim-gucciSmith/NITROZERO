using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

namespace YUJEONG
{
    /// <summary>
    /// booston_yujeong_1 씬 전용 부스터 변형 & 카메라 분리 발진 시네마틱 디렉터
    /// 1. 스플라인 주행 시작 (크루즈 속도)
    /// 2. 1초 대기 후 두 차량 부스터 변형 착착착 진행
    /// 3. 변형 완료 직후: 두 차량 최종 부스터 동시 점화!
    /// 4. 카메라를 노면에 남겨두고(Detach) 두 차만 폭발적인 부스터 가속(부와앙-!)으로 질주!
    /// </summary>
    public class BoosterTransformationDirector : MonoBehaviour
    {
        [Header("[ ⏱️ 1단계: 스플라인 주행 및 부스터 변형 타이밍 ]")]
        [Tooltip("스플라인 애니메이트 컴포넌트 (비워둘 시 씬에서 자동 탐색)")]
        public SplineAnimate splineAnimate;

        [Tooltip("스플라인 주행 시작 후 각 차량의 부스터 변형이 시작되기까지 대기 시간 (초 단위, 기본 1.0초)")]
        [Range(0.1f, 10.0f)]
        public float transformDelay = 1.0f;

        [Tooltip("변형 시작 후 최종 부스터 점화 및 발진까지의 변형 소요 시간 (초 단위, 기본 3.2초)")]
        [Range(1.0f, 8.0f)]
        public float transformationDuration = 3.2f;

        [Tooltip("주행 시작 시 두 차량을 기본 미변형 모습(0%)으로 자동 초기화")]
        public bool resetToNormalOnStart = true;

        [Tooltip("게임 실행(Play) 시 자동으로 주행 및 시퀀스 시작")]
        public bool playOnStart = true;

        [Tooltip("시퀀스 재시작 단축키 (기본: Space)")]
        public KeyCode restartKey = KeyCode.Space;

        [Header("[ 💥 2단계: 카메라 두고 부스터 폭발 발진 (부와앙-!) ]")]
        [Tooltip("최종 부스터 점화 시 카메라를 노면에 남겨두고 차만 발진할지 여부")]
        public bool leaveCameraBehindOnBoost = true;

        [Tooltip("분리할 카메라 트랜스폼 (비워둘 시 CM_Shot01 또는 Main Camera 자동 탐색)")]
        public Transform targetCamera;

        [Tooltip("부스터 점화 전 일반 순항 속도 (Spline Speed)")]
        [Range(20f, 400f)]
        public float cruiseSpeed = 100f;

        [Tooltip("부스터 점화 후 폭발적인 최종 로켓 질주 속도 (너무 빠르지 않은 적정 속도, 기본 200 권장)")]
        [Range(50f, 600f)]
        public float boostRocketSpeed = 200f;

        [Tooltip("부스터 점화 시 최고 속도까지 치고 나가는 급가속 시간 (초 단위, 부드럽고 묵직한 가속을 위해 0.8초 권장)")]
        [Range(0.1f, 3.0f)]
        public float boostAccelerationDuration = 0.8f;

        [Header("[ 🏎️ 대상 차량 컨트롤러 (비워둘 시 자동 탐색) ]")]
        public VehicleTransformationController blueCarController;
        public BoosterDeploymentController redCarController;

        private Coroutine sequenceCoroutine;
        private Coroutine boostAccelerationRoutine;
        private bool isSequenceRunning = false;
        public bool IsSequenceRunning => isSequenceRunning;

        // 카메라 원본 계층 구조 백업
        private Transform originalCameraParent;
        private Vector3 originalCameraLocalPos;
        private Quaternion originalCameraLocalRot;
        private Vector3 detachedCameraWorldPos;
        private Quaternion detachedCameraWorldRot;
        private bool isCameraDetached = false;

        private void Awake()
        {
            AutoFindComponents();
            StoreCameraHierarchy();
        }

        private void Start()
        {
            if (resetToNormalOnStart)
            {
                ResetVehiclesToNormal();
            }

            if (playOnStart)
            {
                PlaySequence();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (IsRestartKeyPressed())
            {
                PlaySequence();
            }
        }

        private void OnDisable()
        {
            ResetSequenceState();
        }

        private void LateUpdate()
        {
            if (isCameraDetached && targetCamera != null)
            {
                // CameraWindBlastShock가 있으면 해당 컴포넌트가 좌표를 관리하므로 충돌 방지
                var shock = targetCamera.GetComponent<CameraWindBlastShock>();
                if (shock == null)
                {
                    targetCamera.position = detachedCameraWorldPos;
                    targetCamera.rotation = detachedCameraWorldRot;
                }
            }
        }

        /// <summary>
        /// 스플라인 주행을 시작하고, 1초 대기 후 변형 ➔ 변형 완료 후 카메라 분리 & 부스터 폭발 발진!
        /// </summary>
        [ContextMenu("🎬 주행 ➔ 1초 후 변형 ➔ 카메라 두고 부스터 발진 (Space)")]
        public void PlaySequence()
        {
            AutoFindComponents();
            StoreCameraHierarchy();

            if (sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine);
                sequenceCoroutine = null;
            }

            sequenceCoroutine = StartCoroutine(SequenceRoutine());
        }

        private IEnumerator SequenceRoutine()
        {
            isSequenceRunning = true;

            // 1. 카메라 복귀 및 차량 0% 미변형 상태 초기화
            ResetSequenceState();

            // 2. 스플라인 크루즈 주행 시작
            if (splineAnimate != null)
            {
                if (splineAnimate.AnimationMethod == SplineAnimate.Method.Speed)
                {
                    splineAnimate.MaxSpeed = cruiseSpeed;
                }
                splineAnimate.Restart(true);
            }

            // 3. 1초 대기
            float waitTime = Mathf.Max(0.05f, transformDelay);
            yield return new WaitForSeconds(waitTime);

            // 4. 두 차량 부스터 변형 시작!
            TriggerBothTransformations();

            // 5. 두 차량 변형이 완료되는 시간(3.2초) 대기
            yield return new WaitForSeconds(Mathf.Max(0.5f, transformationDuration));

            // 6. 💥 카메라를 두고 두 차가 진짜 부스터 킨듯 부와앙- 질주!
            if (leaveCameraBehindOnBoost)
            {
                DetachCameraAndRocketLaunch();
            }

            isSequenceRunning = false;
        }

        /// <summary>
        /// 부스터 변형 완료 직후: 카메라를 노면에 남겨두고 두 차량이 폭발적인 부스터 가속(부와앙-)으로 질주
        /// </summary>
        [ContextMenu("💥 카메라 두고 부스터 폭발 발진 (부와앙-!)")]
        public void DetachCameraAndRocketLaunch()
        {
            AutoFindComponents();

            // 1. 카메라 분리 & 현재 월드 좌표에 고정 (계층 구조 변경 없이 위치 고정)
            if (targetCamera != null && !isCameraDetached)
            {
                StoreCameraHierarchy();
                detachedCameraWorldPos = targetCamera.position;
                detachedCameraWorldRot = targetCamera.rotation;
                isCameraDetached = true;
            }

            // 2. 두 차량 최종 부스터 100% 동시 풀 점등
            if (blueCarController != null)
            {
                blueCarController.SetSubBoosterFxActive(true);
                blueCarController.SetMainBoosterFxActive(true);
            }

            if (redCarController != null)
            {
                redCarController.SnapToFullDeployment();
            }

            // 3. 폭발적인 부스터 속도 급가속 (부와앙-!)
            if (boostAccelerationRoutine != null) StopCoroutine(boostAccelerationRoutine);
            boostAccelerationRoutine = StartCoroutine(AnimateBoostRocketSpeed(boostRocketSpeed, boostAccelerationDuration));

            // 4. 카메라 후폭풍 쉐이크 트리거 (CameraWindBlastShock가 카메라에 있는 경우)
            if (targetCamera != null)
            {
                var shock = targetCamera.GetComponent<CameraWindBlastShock>();
                if (shock != null)
                {
                    shock.DetachCameraAndRocketLaunch();
                }
            }

            Debug.Log("[BoosterTransformationDirector] 💥 카메라를 두고 두 차량 부스터 동시 점화 & 폭발적 초고속 질주 발진 (부와앙-!)");
        }

        private IEnumerator AnimateBoostRocketSpeed(float targetSpeed, float duration)
        {
            if (splineAnimate == null) yield break;

            float startSpeed = splineAnimate.MaxSpeed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curve = Mathf.Sin(t * Mathf.PI * 0.5f);
                splineAnimate.MaxSpeed = Mathf.Lerp(startSpeed, targetSpeed, curve);
                yield return null;
            }

            splineAnimate.MaxSpeed = targetSpeed;
        }

        public void ResetSequenceState()
        {
            if (boostAccelerationRoutine != null)
            {
                StopCoroutine(boostAccelerationRoutine);
                boostAccelerationRoutine = null;
            }

            // 카메라 복귀 (SetParent 없이 로컬 포즈 복귀)
            if (isCameraDetached && targetCamera != null)
            {
                var shock = targetCamera.GetComponent<CameraWindBlastShock>();
                if (shock != null)
                {
                    shock.ResetBoosterLaunchState();
                }
                else
                {
                    targetCamera.localPosition = originalCameraLocalPos;
                    targetCamera.localRotation = originalCameraLocalRot;
                }
                isCameraDetached = false;
            }

            if (splineAnimate != null)
            {
                splineAnimate.MaxSpeed = cruiseSpeed;
            }

            ResetVehiclesToNormal();
        }

        /// <summary>
        /// 블루카 및 레드카 변형 시퀀스 시작
        /// </summary>
        [ContextMenu("🚀 두 차량 부스터 변형 시작")]
        public void TriggerBothTransformations()
        {
            AutoFindComponents();

            if (blueCarController != null)
            {
                blueCarController.DeployFullTransformation();
            }

            if (redCarController != null)
            {
                redCarController.DeployBoosters();
            }

            Debug.Log($"[BoosterTransformationDirector] 🚀 주행 중 {transformDelay:F1}초 대기 후 두 차량 부스터 변형 시작!");
        }

        /// <summary>
        /// 두 차량을 기본 미변형 원래 모습(0%)으로 복귀
        /// </summary>
        [ContextMenu("🔄 두 차량 기본 원래 모습 복귀 (0%)")]
        public void ResetVehiclesToNormal()
        {
            AutoFindComponents();

            if (blueCarController != null)
            {
                blueCarController.SnapToNormalState();
            }

            if (redCarController != null)
            {
                redCarController.SnapToFullRetraction();
            }
        }

        /// <summary>
        /// 두 차량을 즉시 100% 풀 부스터 상태로 변경
        /// </summary>
        [ContextMenu("⚡ 두 차량 즉시 100% 풀 부스터 완료 (Snap)")]
        public void SnapBothVehiclesFullBoosted()
        {
            AutoFindComponents();

            if (blueCarController != null)
            {
                blueCarController.SnapToFullTransformation();
            }

            if (redCarController != null)
            {
                redCarController.SnapToFullDeployment();
            }
        }

        private void StoreCameraHierarchy()
        {
            if (targetCamera != null && originalCameraParent == null && targetCamera.parent != null)
            {
                originalCameraParent = targetCamera.parent;
                originalCameraLocalPos = targetCamera.localPosition;
                originalCameraLocalRot = targetCamera.localRotation;
            }
        }

        public void AutoFindComponents()
        {
            if (splineAnimate == null)
            {
                splineAnimate = GetComponent<SplineAnimate>();
                if (splineAnimate == null)
                {
                    splineAnimate = FindFirstObjectByType<SplineAnimate>();
                }
            }

            if (blueCarController == null)
            {
                blueCarController = FindFirstObjectByType<VehicleTransformationController>();
            }

            if (redCarController == null)
            {
                redCarController = FindFirstObjectByType<BoosterDeploymentController>();
            }

            if (targetCamera == null)
            {
                var shock = FindFirstObjectByType<CameraWindBlastShock>();
                if (shock != null)
                {
                    targetCamera = shock.transform;
                }
                else if (Camera.main != null)
                {
                    targetCamera = Camera.main.transform;
                }
            }
        }

        private bool IsRestartKeyPressed()
        {
            try
            {
                if (Input.GetKeyDown(restartKey)) return true;
            }
            catch { }

#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) return true;
#endif
            return false;
        }
    }
}
