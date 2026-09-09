using UnityEngine;

namespace YUJEONG
{
    /// <summary>
    /// 차량 4개 바퀴 (FL, FR, RL, RR) 주행 롤링 모션 제어 컨트롤러
    /// 
    /// [단축키 조작 (완전 독립 분리 동작)]
    /// - T 키: 바퀴 일반 주행 속도 (1080 deg/s) 회전 켜기 / 끄기 토글
    /// - B 키: 바퀴 초고속 (2520 deg/s) 회전 켜기 / 끄기 토글
    /// - Space 키: 차체 제트 부스터 변신 시퀀스 (VehicleTransformationController에서 독립 동작)
    /// </summary>
    public class VehicleWheelSpinController : MonoBehaviour
    {
        [Header("[ 대상 차량 루트 (기본: SportCar_4change_2) ]")]
        public Transform targetCarRoot;

        [Header("[ 4개 바퀴 트랜스폼 ]")]
        [Tooltip("전방 좌측 바퀴 (Front Left)")]
        public Transform wheelFL;
        [Tooltip("전방 우측 바퀴 (Front Right)")]
        public Transform wheelFR;
        [Tooltip("후방 좌측 바퀴 (Rear Left)")]
        public Transform wheelRL;
        [Tooltip("후방 우측 바퀴 (Rear Right)")]
        public Transform wheelRR;

        public enum WheelState
        {
            [InspectorName("정지 (Stopped)")]
            Stopped,
            [InspectorName("일반 주행 속도 (Normal, T)")]
            Normal,
            [InspectorName("초고속 바퀴 회전 (SuperFast, B)")]
            SuperFast
        }

        [Header("[ 현재 바퀴 주행 상태 ]")]
        [Tooltip("현재 바퀴 상태: 정지(Stopped) / 일반 주행(Normal) / 초고속 회전(SuperFast)")]
        public WheelState currentState = WheelState.Stopped;

        public enum SpinDirection
        {
            [InspectorName("전진 롤링 (Forward)")]
            Forward,
            [InspectorName("후진 롤링 (Reverse)")]
            Reverse
        }

        [Tooltip("바퀴 회전 방향 (전진 / 후진)")]
        public SpinDirection spinDirection = SpinDirection.Forward;

        [Header("[ 주행 속도 설정 (독립 조작) ]")]
        [Tooltip("일반 주행 시 바퀴 회전 속도 (T 키) (도/초, 1080도 = 초당 3회전, 약 90~110km/h 주행감)")]
        [Range(0f, 3600f)]
        public float normalWheelSpeed = 1080f;

        [Tooltip("초고속 회전 시 바퀴 속도 (B 키) (도/초, 2520도 = 초당 7회전, 초고속 질주감)")]
        [Range(1080f, 7200f)]
        public float boostWheelSpeed = 2520f;

        [Header("[ 가속 및 감속 설정 ]")]
        [Tooltip("체크 시 부드럽게 가속하며 회전 시작")]
        public bool smoothAcceleration = true;

        [Tooltip("일반 주행 도달 및 정지까지의 가속 소요 시간 (초)")]
        [Range(0.1f, 5.0f)]
        public float accelerationTime = 1.0f;

        [Tooltip("초고속 회전 도달 및 전환 소요 시간 (초)")]
        [Range(0.1f, 3.0f)]
        public float boostAccelerationTime = 0.6f;

        #region [ 하위 호환성 프로퍼티 ]
        public bool isSpinning
        {
            get => currentState != WheelState.Stopped;
            set => currentState = value ? (isBoostMode ? WheelState.SuperFast : WheelState.Normal) : WheelState.Stopped;
        }

        public bool isBoostMode
        {
            get => currentState == WheelState.SuperFast;
            set => currentState = value ? WheelState.SuperFast : (currentState == WheelState.SuperFast ? WheelState.Normal : currentState);
        }

        public float wheelSpeed
        {
            get => currentState == WheelState.SuperFast ? boostWheelSpeed : normalWheelSpeed;
            set => normalWheelSpeed = value;
        }
        #endregion

        [Header("[ 개별 바퀴 방향 반전 옵션 (필요 시 체크) ]")]
        public bool invertFL = false;
        public bool invertFR = false;
        public bool invertRL = false;
        public bool invertRR = false;

        [Header("[ 조작 단축키 (독립 작동) ]")]
        [Tooltip("일반 주행 속도 토글 키 (기본: T 키)")]
        public KeyCode toggleKey = KeyCode.T;

        [Tooltip("초고속 바퀴 회전 토글 키 (기본: B 키)")]
        public KeyCode boostToggleKey = KeyCode.B;

        // 현재 회전 속도 (도/초)
        private float currentSpeed = 0f;

        // B키 토글 시 이전 상태 복귀용 기억 변수
        private WheelState previousState = WheelState.Stopped;

        private void Reset()
        {
            AutoBindWheels();
        }

        private void Awake()
        {
            if (wheelFL == null || wheelFR == null || wheelRL == null || wheelRR == null)
            {
                AutoBindWheels();
            }

            currentSpeed = (!smoothAcceleration && currentState != WheelState.Stopped) ? wheelSpeed : 0f;
        }

        [ContextMenu("🚗 바퀴 4개 (FL, FR, RL, RR) 자동 연결")]
        public void AutoBindWheels()
        {
            if (targetCarRoot == null)
            {
                if (gameObject.name == "SportCar_4change_2")
                {
                    targetCarRoot = transform;
                }
                else
                {
                    GameObject car = GameObject.Find("SportCar_4change_2");
                    if (car != null) targetCarRoot = car.transform;
                    else targetCarRoot = transform;
                }
            }

            if (targetCarRoot != null)
            {
                if (wheelFL == null) wheelFL = FindDeepChild(targetCarRoot, "FL");
                if (wheelFR == null) wheelFR = FindDeepChild(targetCarRoot, "FR");
                if (wheelRL == null) wheelRL = FindDeepChild(targetCarRoot, "RL");
                if (wheelRR == null) wheelRR = FindDeepChild(targetCarRoot, "RR");

                Debug.Log($"[VehicleWheelSpinController] '{targetCarRoot.name}'에서 바퀴들을 성공적으로 연결했습니다: " +
                          $"FL({(wheelFL != null ? "O" : "X")}), " +
                          $"FR({(wheelFR != null ? "O" : "X")}), " +
                          $"RL({(wheelRL != null ? "O" : "X")}), " +
                          $"RR({(wheelRR != null ? "O" : "X")})");
            }
        }

        private Transform FindDeepChild(Transform parent, string childName)
        {
            Transform direct = parent.Find(childName);
            if (direct != null) return direct;

            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName) return child;
            }
            return null;
        }

        private void Update()
        {
            // 1. T 키: 일반 주행 속도로 달리기 토글
            if (IsNormalKeyPressed())
            {
                ToggleNormalMode();
            }

            // 2. B 키: 초고속으로 바퀴 회전 토글
            if (IsSuperFastKeyPressed())
            {
                ToggleSuperFastMode();
            }

            // 3. 목표 속도 결정
            float targetSpeedVal = 0f;
            switch (currentState)
            {
                case WheelState.Stopped:
                    targetSpeedVal = 0f;
                    break;
                case WheelState.Normal:
                    targetSpeedVal = normalWheelSpeed;
                    break;
                case WheelState.SuperFast:
                    targetSpeedVal = boostWheelSpeed;
                    break;
            }

            // 4. 부드러운 가속 및 감속 속도 보간
            float duration = (currentState == WheelState.SuperFast) ? boostAccelerationTime : accelerationTime;
            if (smoothAcceleration && duration > 0.01f)
            {
                float maxSpeedRef = Mathf.Max(normalWheelSpeed, boostWheelSpeed);
                float rate = (maxSpeedRef / duration) * Time.deltaTime;
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeedVal, rate);
            }
            else
            {
                currentSpeed = targetSpeedVal;
            }

            if (currentSpeed <= 0.001f) return;

            // 5. 4개 바퀴 회전 실행
            RotateAllWheels(currentSpeed * Time.deltaTime);
        }

        private void RotateAllWheels(float deltaAngle)
        {
            if (targetCarRoot == null) targetCarRoot = transform;

            float baseDir = (spinDirection == SpinDirection.Forward) ? 1f : -1f;

            // 차량의 측면(가로 가상 차축) 축을 기준으로 회전하여 좌우 4개 바퀴가 모두 정확하게 전진 롤링
            Vector3 axleAxis = targetCarRoot.right;

            RotateWheel(wheelFL, axleAxis, deltaAngle * baseDir * (invertFL ? -1f : 1f));
            RotateWheel(wheelFR, axleAxis, deltaAngle * baseDir * (invertFR ? -1f : 1f));
            RotateWheel(wheelRL, axleAxis, deltaAngle * baseDir * (invertRL ? -1f : 1f));
            RotateWheel(wheelRR, axleAxis, deltaAngle * baseDir * (invertRR ? -1f : 1f));
        }

        private void RotateWheel(Transform wheel, Vector3 axis, float angle)
        {
            if (wheel == null) return;
            wheel.Rotate(axis, angle, Space.World);
        }

        /// <summary>
        /// [T 키] 일반 주행 속도로 달리기 토글
        /// - 이미 일반 주행 중이면 -> 정지(Stopped)
        /// - 멈춰있거나 초고속 회전 중이면 -> 일반 주행 속도(Normal)로 전환
        /// </summary>
        [ContextMenu("🛞 [T] 일반 주행 속도로 달리기 토글")]
        public void ToggleNormalMode()
        {
            if (currentState == WheelState.Normal)
            {
                currentState = WheelState.Stopped;
                previousState = WheelState.Stopped;
                Debug.Log("[VehicleWheelSpinController] ⏹ 바퀴 회전 정지 (T)");
            }
            else
            {
                currentState = WheelState.Normal;
                previousState = WheelState.Normal;
                Debug.Log($"[VehicleWheelSpinController] ▶ [T] 일반 주행 속도로 달리기 시작! ({normalWheelSpeed} deg/s)");
            }
        }

        /// <summary>
        /// [B 키] 초고속 바퀴 회전 토글
        /// - 이미 초고속 회전 중이면 -> 이전 상태(일반 주행 또는 정지)로 복귀
        /// - 멈춰있거나 일반 주행 중이면 -> 초고속 회전(SuperFast)으로 전환
        /// </summary>
        [ContextMenu("⚡ [B] 초고속 바퀴 회전 토글")]
        public void ToggleSuperFastMode()
        {
            if (currentState == WheelState.SuperFast)
            {
                currentState = (previousState == WheelState.Normal) ? WheelState.Normal : WheelState.Stopped;
                Debug.Log($"[VehicleWheelSpinController] ⚡ [B] 초고속 모드 종료 -> {(currentState == WheelState.Normal ? "일반 주행 복귀" : "정지")}");
            }
            else
            {
                previousState = currentState;
                currentState = WheelState.SuperFast;
                Debug.Log($"[VehicleWheelSpinController] ⚡ [B] 초고속 바퀴 회전 시작! ({boostWheelSpeed} deg/s)");
            }
        }

        /// <summary>
        /// 외부 스크립트 연동용 (필요 시)
        /// </summary>
        public void SetBoostMode(bool boostActive)
        {
            if (boostActive)
            {
                if (currentState != WheelState.SuperFast)
                {
                    previousState = currentState;
                    currentState = WheelState.SuperFast;
                }
            }
            else
            {
                if (currentState == WheelState.SuperFast)
                {
                    currentState = (previousState == WheelState.Normal) ? WheelState.Normal : WheelState.Stopped;
                }
            }
        }

        public void SetSpeed(float newSpeed)
        {
            normalWheelSpeed = newSpeed;
        }

        [ContextMenu("▶ 바퀴 일반 주행 시작")]
        public void StartSpinning() => ToggleNormalMode();

        [ContextMenu("⏹ 바퀴 회전 완전 정지")]
        public void StopSpinning()
        {
            currentState = WheelState.Stopped;
            previousState = WheelState.Stopped;
        }

        private bool IsNormalKeyPressed()
        {
            // 1. 구형 Input Manager 호환
            try
            {
                if (Input.GetKeyDown(toggleKey))
                    return true;
            }
            catch { }

            // 2. 신규 Input System 패키지 호환
#if ENABLE_INPUT_SYSTEM || UNITY_INPUT_SYSTEM_EXISTS
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb.tKey.wasPressedThisFrame)
                    return true;
            }
            catch { }
#endif
            return false;
        }

        private bool IsSuperFastKeyPressed()
        {
            // 1. 구형 Input Manager 호환
            try
            {
                if (Input.GetKeyDown(boostToggleKey))
                    return true;
            }
            catch { }

            // 2. 신규 Input System 패키지 호환
#if ENABLE_INPUT_SYSTEM || UNITY_INPUT_SYSTEM_EXISTS
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb.bKey.wasPressedThisFrame)
                    return true;
            }
            catch { }
#endif
            return false;
        }

        private void OnGUI()
        {
            // 1. [T] 일반 주행 버튼
            bool isNormal = (currentState == WheelState.Normal);
            GUI.color = isNormal ? new Color(0.7f, 1f, 0.7f) : new Color(0.92f, 0.92f, 0.92f);
            string normalLabel = isNormal 
                ? $"🛞 [T] 일반 주행 중 ({(int)currentSpeed} deg/s)" 
                : $"🛞 [T] 일반 주행 속도로 달리기 ({normalWheelSpeed} deg/s)";

            if (GUI.Button(new Rect(10, 295, 260, 26), normalLabel))
            {
                ToggleNormalMode();
            }

            // 2. [B] 초고속 바퀴 회전 버튼
            bool isSuper = (currentState == WheelState.SuperFast);
            GUI.color = isSuper ? new Color(1f, 0.85f, 0.25f) : new Color(0.92f, 0.92f, 0.92f);
            string superLabel = isSuper 
                ? $"⚡ [B] 초고속 회전 중! ({(int)currentSpeed} deg/s)" 
                : $"⚡ [B] 초고속으로 바퀴 회전 ({boostWheelSpeed} deg/s)";

            if (GUI.Button(new Rect(10, 324, 260, 26), superLabel))
            {
                ToggleSuperFastMode();
            }

            GUI.color = Color.white;
        }
    }
}
