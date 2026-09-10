using UnityEngine;

namespace YUJEONG
{
    /// <summary>
    /// 차량 드리프트 모션 전용 컨트롤러 (비주얼/애니메이션 쇼케이스용)
    /// 
    /// [핵심 연출]
    /// 1. 왼쪽 드리프트 (Q 키 토글):
    ///    - 뒷바퀴 2개(RL, RR) 완전 락(정지)
    ///    - 앞바퀴 2개(FL, FR) 오른쪽 바깥 카운터 조향(+각도)으로 부드럽게 꺾이면서 주행 회전 롤링
    /// 2. 오른쪽 드리프트 (E 키 토글):
    ///    - 뒷바퀴 2개(RL, RR) 완전 락(정지)
    ///    - 앞바퀴 2개(FL, FR) 왼쪽 바깥 카운터 조향(-각도)으로 부드럽게 꺾이면서 주행 회전 롤링
    /// 3. 토글 해제 시:
    ///    - 앞바퀴가 원래 정면(0도)으로 부드럽게 복귀 및 정지
    /// </summary>
    public class VehicleDriftMotionController : MonoBehaviour
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

        public enum DriftMode
        {
            [InspectorName("정지 / 중립 (None)")]
            None,
            [InspectorName("왼쪽 드리프트 (Left Drift, Q)")]
            DriftLeft,
            [InspectorName("오른쪽 드리프트 (Right Drift, E)")]
            DriftRight
        }

        [Header("[ 현재 드리프트 상태 (토글 방식) ]")]
        [Tooltip("현재 드리프트 상태: 정지(None) / 왼쪽 드리프트(DriftLeft) / 오른쪽 드리프트(DriftRight)")]
        public DriftMode currentDriftMode = DriftMode.None;

        [Header("[ 조작 단축키 (토글 방식) ]")]
        [Tooltip("왼쪽 드리프트 토글 키 (기본: Q) - 앞바퀴가 오른쪽 바깥으로 꺾이며 회전, 뒷바퀴 정지")]
        public KeyCode leftDriftKey = KeyCode.Q;

        [Tooltip("오른쪽 드리프트 토글 키 (기본: E) - 앞바퀴가 왼쪽 바깥으로 꺾이며 회전, 뒷바퀴 정지")]
        public KeyCode rightDriftKey = KeyCode.E;

        [Tooltip("모든 드리프트 즉시 해제 및 정지 키 (기본: Escape)")]
        public KeyCode cancelKey = KeyCode.Escape;

        [Header("[ 앞바퀴 카운터 조향(Steering) 설정 ]")]
        [Tooltip("드리프트 시 앞바퀴가 바깥쪽으로 꺾이는 최대 카운터 각도 (기본: 30도)")]
        [Range(5f, 50f)]
        public float counterSteerAngle = 30f;

        [Tooltip("앞바퀴 꺾임 및 복귀 전환 소요 시간 (초, 작을수록 민첩하게 꺾임)")]
        [Range(0.05f, 1.0f)]
        public float steerTransitionDuration = 0.2f;

        [Tooltip("체크 시 조향 좌우 방향 반전")]
        public bool invertSteerDirection = false;

        [Header("[ 바퀴 회전(Spin / Roll) 속도 설정 ]")]
        [Tooltip("드리프트 중 앞바퀴 회전 속도 (도/초, 1260도 = 약 100km/h 주행감)")]
        [Range(360f, 3600f)]
        public float frontWheelDriftSpeed = 1260f;

        [Tooltip("체크 시 드리프트 중 뒷바퀴 회전 완전 락(정지)")]
        public bool lockRearWheels = true;

        [Tooltip("뒷바퀴 급제동 락 감속 소요 시간 (초, 0이면 칼같이 즉시 정지)")]
        [Range(0f, 0.5f)]
        public float rearBrakeDecelTime = 0.12f;

        [Header("[ 선택: 직진 주행 모드 (T 키) ]")]
        [Tooltip("드리프트 전/후에 4바퀴 모두 굴러가는 직진 주행 모드 지원 여부")]
        public bool allowStraightDrive = true;
        [Tooltip("직진 주행 토글 키 (기본: T 키)")]
        public KeyCode straightDriveKey = KeyCode.T;
        [Tooltip("직진 주행 시 바퀴 회전 속도 (도/초)")]
        public float straightWheelSpeed = 1080f;
        [Tooltip("현재 직진 주행 중 여부")]
        public bool isStraightDriving = false;

        [Header("[ 개별 바퀴 회전 방향 반전 옵션 ]")]
        public bool invertFL = false;
        public bool invertFR = false;
        public bool invertRL = false;
        public bool invertRR = false;

        [Header("[ 기존 바퀴 스크립트 자동 충돌 방지 ]")]
        [Tooltip("체크 시 드리프트 작동 중 기존 VehicleWheelSpinController를 자동으로 일시 중지하여 회전 충돌을 방지합니다.")]
        public bool autoManageOtherWheelSpin = true;

        [Header("[ 온스크린 GUI 표시 ]")]
        [Tooltip("화면에 클릭 가능한 드리프트 조작 버튼 표시 여부")]
        public bool showOnGUI = true;

        // --- 내부 제어 변수 ---
        private float currentSteerAngle = 0f;
        private float accumulatedRollFL = 0f;
        private float accumulatedRollFR = 0f;
        private float accumulatedRollRL = 0f;
        private float accumulatedRollRR = 0f;

        private float currentFrontSpeed = 0f;
        private float currentRearSpeed = 0f;

        private Quaternion initialRelRotFL = Quaternion.identity;
        private Quaternion initialRelRotFR = Quaternion.identity;
        private Quaternion initialRelRotRL = Quaternion.identity;
        private Quaternion initialRelRotRR = Quaternion.identity;
        private bool hasInitializedRotations = false;

        private VehicleWheelSpinController cachedWheelSpinController;

        private void Reset()
        {
            AutoBindWheels();
        }

        private void Awake()
        {
            if (targetCarRoot == null) targetCarRoot = transform;

            if (wheelFL == null || wheelFR == null || wheelRL == null || wheelRR == null)
            {
                AutoBindWheels();
            }

            SaveInitialRotations();

            if (autoManageOtherWheelSpin && targetCarRoot != null)
            {
                cachedWheelSpinController = targetCarRoot.GetComponent<VehicleWheelSpinController>();
                if (cachedWheelSpinController == null)
                {
                    cachedWheelSpinController = GetComponent<VehicleWheelSpinController>();
                }
            }
        }

        private void OnEnable()
        {
            if (!hasInitializedRotations)
            {
                SaveInitialRotations();
            }
        }

        /// <summary>
        /// 바퀴들의 차량 기준 초기 상대 각도를 저장합니다.
        /// </summary>
        [ContextMenu("🔄 바퀴 초기 각도 재기록 (Recalibrate)")]
        public void SaveInitialRotations()
        {
            if (targetCarRoot == null) targetCarRoot = transform;

            Quaternion invRoot = Quaternion.Inverse(targetCarRoot.rotation);

            if (wheelFL != null) initialRelRotFL = invRoot * wheelFL.rotation;
            if (wheelFR != null) initialRelRotFR = invRoot * wheelFR.rotation;
            if (wheelRL != null) initialRelRotRL = invRoot * wheelRL.rotation;
            if (wheelRR != null) initialRelRotRR = invRoot * wheelRR.rotation;

            hasInitializedRotations = true;
        }

        /// <summary>
        /// 차량 루트 및 4개 바퀴 자동 검색 및 할당
        /// </summary>
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

                Debug.Log($"[VehicleDriftMotionController] '{targetCarRoot.name}' 바퀴 검색 결과: " +
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
            HandleInputs();
            UpdateDriftMotion(Time.deltaTime);
        }

        private void HandleInputs()
        {
            // 1. [Q] 왼쪽 드리프트 토글
            if (IsKeyPressed(leftDriftKey, "q"))
            {
                ToggleLeftDrift();
            }

            // 2. [E] 오른쪽 드리프트 토글
            if (IsKeyPressed(rightDriftKey, "e"))
            {
                ToggleRightDrift();
            }

            // 3. [T] 직진 주행 토글 (선택적)
            if (allowStraightDrive && IsKeyPressed(straightDriveKey, "t"))
            {
                ToggleStraightDrive();
            }

            // 4. [ESC] 즉시 정지 및 해제
            if (IsKeyPressed(cancelKey, "escape"))
            {
                ResetDrift();
            }
        }

        /// <summary>
        /// [Q 키] 왼쪽 드리프트 토글
        /// - 앞바퀴: 오른쪽 바깥 카운터 조향 (+각도) + 고속 롤링
        /// - 뒷바퀴: 락 (회전 정지)
        /// </summary>
        [ContextMenu("◀ [Q] 왼쪽 드리프트 토글")]
        public void ToggleLeftDrift()
        {
            if (currentDriftMode == DriftMode.DriftLeft)
            {
                currentDriftMode = DriftMode.None;
                Debug.Log("[VehicleDriftMotionController] ⏹ 왼쪽 드리프트 해제 (정면 복귀)");
            }
            else
            {
                currentDriftMode = DriftMode.DriftLeft;
                isStraightDriving = false;
                SyncOtherWheelSpin(false);
                Debug.Log($"[VehicleDriftMotionController] ◀ [Q] 왼쪽 드리프트 시작! (앞바퀴 오른쪽 카운터 조향 + 뒷바퀴 락)");
            }
        }

        /// <summary>
        /// [E 키] 오른쪽 드리프트 토글
        /// - 앞바퀴: 왼쪽 바깥 카운터 조향 (-각도) + 고속 롤링
        /// - 뒷바퀴: 락 (회전 정지)
        /// </summary>
        [ContextMenu("▶ [E] 오른쪽 드리프트 토글")]
        public void ToggleRightDrift()
        {
            if (currentDriftMode == DriftMode.DriftRight)
            {
                currentDriftMode = DriftMode.None;
                Debug.Log("[VehicleDriftMotionController] ⏹ 오른쪽 드리프트 해제 (정면 복귀)");
            }
            else
            {
                currentDriftMode = DriftMode.DriftRight;
                isStraightDriving = false;
                SyncOtherWheelSpin(false);
                Debug.Log($"[VehicleDriftMotionController] ▶ [E] 오른쪽 드리프트 시작! (앞바퀴 왼쪽 카운터 조향 + 뒷바퀴 락)");
            }
        }

        /// <summary>
        /// [T 키] 직진 주행 모드 토글
        /// </summary>
        [ContextMenu("🛞 [T] 직진 주행 토글")]
        public void ToggleStraightDrive()
        {
            if (currentDriftMode != DriftMode.None)
            {
                currentDriftMode = DriftMode.None;
            }

            isStraightDriving = !isStraightDriving;
            Debug.Log($"[VehicleDriftMotionController] 🛞 직진 주행 모드: {(isStraightDriving ? "ON" : "OFF")}");
        }

        /// <summary>
        /// 드리프트 및 주행 완전 정지 및 중립 정렬
        /// </summary>
        [ContextMenu("⏹ 전체 정지 및 중립 복귀")]
        public void ResetDrift()
        {
            currentDriftMode = DriftMode.None;
            isStraightDriving = false;
            SyncOtherWheelSpin(true);
            Debug.Log("[VehicleDriftMotionController] ⏹ 전체 정지 및 앞바퀴 정면 정렬");
        }

        private void SyncOtherWheelSpin(bool allowOther)
        {
            if (autoManageOtherWheelSpin && cachedWheelSpinController != null)
            {
                cachedWheelSpinController.enabled = allowOther;
            }
        }

        private void UpdateDriftMotion(float dt)
        {
            if (targetCarRoot == null) targetCarRoot = transform;
            if (!hasInitializedRotations) SaveInitialRotations();

            // 1. 목표 조향 각도 및 목표 회전 속도 결정
            float targetSteerAngle = 0f;
            float targetFrontSpeed = 0f;
            float targetRearSpeed = 0f;

            float steerSign = invertSteerDirection ? -1f : 1f;

            switch (currentDriftMode)
            {
                case DriftMode.DriftLeft:
                    // 왼쪽 드리프트: 차체 뒤가 오른쪽으로 미끄러지므로 앞바퀴는 오른쪽 바깥(+각도)으로 카운터 조향
                    targetSteerAngle = counterSteerAngle * steerSign;
                    targetFrontSpeed = frontWheelDriftSpeed;
                    targetRearSpeed = lockRearWheels ? 0f : frontWheelDriftSpeed;
                    break;

                case DriftMode.DriftRight:
                    // 오른쪽 드리프트: 차체 뒤가 왼쪽으로 미끄러지므로 앞바퀴는 왼쪽 바깥(-각도)으로 카운터 조향
                    targetSteerAngle = -counterSteerAngle * steerSign;
                    targetFrontSpeed = frontWheelDriftSpeed;
                    targetRearSpeed = lockRearWheels ? 0f : frontWheelDriftSpeed;
                    break;

                case DriftMode.None:
                    targetSteerAngle = 0f;
                    if (isStraightDriving)
                    {
                        targetFrontSpeed = straightWheelSpeed;
                        targetRearSpeed = straightWheelSpeed;
                    }
                    else
                    {
                        targetFrontSpeed = 0f;
                        targetRearSpeed = 0f;
                    }
                    break;
            }

            // 2. 조향 각도 부드러운 보간 (스티어링 애니메이션)
            float steerDuration = Mathf.Max(0.01f, steerTransitionDuration);
            float steerRate = (counterSteerAngle / steerDuration) * dt;
            currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetSteerAngle, steerRate);

            // 3. 앞바퀴 속도 보간 (자연스러운 롤링 가속)
            currentFrontSpeed = Mathf.MoveTowards(currentFrontSpeed, targetFrontSpeed, 3600f * dt);

            // 4. 뒷바퀴 속도 보간 (급제동 브레이크 락 연출)
            if (rearBrakeDecelTime <= 0.01f || currentDriftMode != DriftMode.None)
            {
                if (lockRearWheels && currentDriftMode != DriftMode.None)
                {
                    float rearDecelRate = (frontWheelDriftSpeed / Mathf.Max(0.01f, rearBrakeDecelTime)) * dt;
                    currentRearSpeed = Mathf.MoveTowards(currentRearSpeed, 0f, rearDecelRate);
                }
                else
                {
                    currentRearSpeed = Mathf.MoveTowards(currentRearSpeed, targetRearSpeed, 3600f * dt);
                }
            }
            else
            {
                currentRearSpeed = Mathf.MoveTowards(currentRearSpeed, targetRearSpeed, 2000f * dt);
            }

            // 5. 바퀴별 롤링 각도 누적 (360도 모듈로 정규화)
            accumulatedRollFL = (accumulatedRollFL + currentFrontSpeed * dt * (invertFL ? -1f : 1f)) % 360f;
            accumulatedRollFR = (accumulatedRollFR + currentFrontSpeed * dt * (invertFR ? -1f : 1f)) % 360f;
            accumulatedRollRL = (accumulatedRollRL + currentRearSpeed * dt * (invertRL ? -1f : 1f)) % 360f;
            accumulatedRollRR = (accumulatedRollRR + currentRearSpeed * dt * (invertRR ? -1f : 1f)) % 360f;

            // 6. 3D 트랜스폼 최종 회전 적용 (바퀴 축 뒤틀림 방지 정밀 연산)
            Quaternion carRot = targetCarRoot.rotation;

            // 조향 쿼터니언 (차량 기준 Y축/상단 기준 회전)
            Quaternion steerRot = Quaternion.AngleAxis(currentSteerAngle, Vector3.up);

            // [앞바퀴] 조향(Yaw) + 회전(Roll) 결합 적용
            if (wheelFL != null)
            {
                Quaternion rollRotFL = Quaternion.AngleAxis(accumulatedRollFL, Vector3.right);
                wheelFL.rotation = carRot * (steerRot * rollRotFL * initialRelRotFL);
            }

            if (wheelFR != null)
            {
                Quaternion rollRotFR = Quaternion.AngleAxis(accumulatedRollFR, Vector3.right);
                wheelFR.rotation = carRot * (steerRot * rollRotFR * initialRelRotFR);
            }

            // [뒷바퀴] 조향 없이 롤링만 적용 (드리프트 시 0회전 락 고정)
            if (wheelRL != null)
            {
                Quaternion rollRotRL = Quaternion.AngleAxis(accumulatedRollRL, Vector3.right);
                wheelRL.rotation = carRot * (rollRotRL * initialRelRotRL);
            }

            if (wheelRR != null)
            {
                Quaternion rollRotRR = Quaternion.AngleAxis(accumulatedRollRR, Vector3.right);
                wheelRR.rotation = carRot * (rollRotRR * initialRelRotRR);
            }
        }

        private bool IsKeyPressed(KeyCode code, string keyName)
        {
            try
            {
                if (Input.GetKeyDown(code)) return true;
            }
            catch { }

#if ENABLE_INPUT_SYSTEM || UNITY_INPUT_SYSTEM_EXISTS
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (keyName == "q" && kb.qKey.wasPressedThisFrame) return true;
                    if (keyName == "e" && kb.eKey.wasPressedThisFrame) return true;
                    if (keyName == "t" && kb.tKey.wasPressedThisFrame) return true;
                    if (keyName == "escape" && kb.escapeKey.wasPressedThisFrame) return true;
                }
            }
            catch { }
#endif
            return false;
        }

        private void OnGUI()
        {
            if (!showOnGUI) return;

            // 드리프트 조작 안내 및 토글 버튼 패널
            GUILayout.BeginArea(new Rect(10, 360, 270, 150), GUI.skin.box);
            GUILayout.Label("<b>🏎️ [드리프트 쇼케이스 컨트롤]</b>");

            // 1. 왼쪽 드리프트 버튼 [Q]
            bool isLeft = (currentDriftMode == DriftMode.DriftLeft);
            GUI.color = isLeft ? new Color(0.3f, 1f, 0.4f) : Color.white;
            string leftLabel = isLeft 
                ? $"◀ [Q] 왼쪽 드리프트 중! (+{(int)counterSteerAngle}°)" 
                : "◀ [Q] 왼쪽 드리프트 (우측 조향 꺾임)";
            if (GUILayout.Button(leftLabel, GUILayout.Height(26)))
            {
                ToggleLeftDrift();
            }

            // 2. 오른쪽 드리프트 버튼 [E]
            bool isRight = (currentDriftMode == DriftMode.DriftRight);
            GUI.color = isRight ? new Color(0.3f, 1f, 0.4f) : Color.white;
            string rightLabel = isRight 
                ? $"▶ [E] 오른쪽 드리프트 중! (-{(int)counterSteerAngle}°)" 
                : "▶ [E] 오른쪽 드리프트 (좌측 조향 꺾임)";
            if (GUILayout.Button(rightLabel, GUILayout.Height(26)))
            {
                ToggleRightDrift();
            }

            // 3. 해제 / 정지 버튼 [ESC]
            GUI.color = (currentDriftMode == DriftMode.None && !isStraightDriving) ? Color.gray : new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("⏹ [ESC] 드리프트 해제 (중립 정렬)", GUILayout.Height(24)))
            {
                ResetDrift();
            }

            GUI.color = Color.white;
            GUILayout.EndArea();
        }
    }
}
