using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace YUJEONG
{
    /// <summary>
    /// CCTV / 과속 단속 카메라 헤드 상승(엘리베이터) 및 회전 제어 스크립트
    /// 
    /// 단축키:
    /// - O 키: 첫 번째 사진(Y: 0) 상태에서 두 번째 사진(Y: 0.045) 상태로 부드럽게 '지이잉~' 쭉 올라옴 (다시 누르면 내려감)
    /// - [ 키: 오른쪽으로 고개 회전 후 유지
    /// - ] 키: 왼쪽으로 고개 회전 후 유지
    /// - P 키: 원래 정면 각도로 리셋
    /// </summary>
    public class CCTVHeadRotationController : MonoBehaviour
    {
        public enum ControlMode
        {
            [Tooltip("키를 한 번 누르면 목표 각도(오른쪽/왼쪽)까지 회전하고 정지")]
            FixedTargetAngle,

            [Tooltip("키를 누르고 있는 동안 해당 방향으로 계속 회전")]
            HoldToRotate,

            [Tooltip("키를 누를 때마다 일정 각도(stepAngle)씩 추가 회전")]
            StepByStep
        }

        [Header("[ 상승 / 하강 엘리베이터 모션 (O 키) ]")]
        [Tooltip("목 올라오기/내려가기 토글 단축키 (기본: O 키)")]
        public KeyCode riseKey = KeyCode.O;

        [Tooltip("평소 들어가 있는 기본 Y 위치 (첫 번째 사진: 0)")]
        public float downY = 0f;

        [Tooltip("쭉 올라왔을 때의 목표 Y 위치 (두 번째 사진: 0.045)")]
        public float upY = 0.045f;

        [Tooltip("올라오고 내려가는 속도 (초당 이동 거리, 숫자가 클수록 빠름)")]
        [Range(0.01f, 1f)]
        public float riseSpeed = 0.06f;

        [Tooltip("기계식 등속 대신 부드럽게 감속하며 멈출지 여부")]
        public bool smoothRise = false;

        [Tooltip("P(리셋) 키를 누를 때 높이도 원래대로(downY) 내려보낼지 여부")]
        public bool resetElevationOnResetKey = false;

        [Header("[ 회전 중심축 오브젝트 (Pivot Transform) ]")]
        [Tooltip("회전축이 될 오브젝트 (cameraheadermove). 비워두면 자동으로 씬에서 cameraheadermove를 찾습니다.")]
        public Transform pivotTransform;

        [Tooltip("회전시킬 카메라 얼굴 오브젝트. 비워두면 이 스크립트가 붙은 오브젝트 자신을 회전시킵니다.")]
        public Transform targetHead;

        [Tooltip("회전 축으로 pivotTransform의 Up(위쪽) 방향 사용 (체크 해제 시 World Y축)")]
        public bool usePivotUpAxis = true;

        [Header("[ 회전 조작 단축키 설정 ]")]
        [Tooltip("좌우 회전 방향 반전 (체크 시 [ / ] 방향 반대)")]
        public bool invertDirection = false;

        [Header("[ 회전 모드 설정 ]")]
        [Tooltip("회전 동작 방식 선택")]
        public ControlMode controlMode = ControlMode.FixedTargetAngle;

        [Header("[ 회전 속도 및 각도 조절 ]")]
        [Tooltip("회전 속도 (초당 회전 각도, 숫자가 클수록 빠름)")]
        [Range(10f, 720f)]
        public float rotationSpeed = 90f;

        [Tooltip("고정 목표 각도 모드에서 돌아갈 각도 (도 단위)")]
        [Range(10f, 180f)]
        public float targetAngle = 60f;

        [Tooltip("StepByStep 모드에서 한 번 누를 때 돌아가는 각도")]
        [Range(5f, 90f)]
        public float stepAngle = 15f;

        [Tooltip("회전 시 부드러운 감속 여부")]
        public bool smoothDamping = false;

        // 내부 상태 변수
        private Transform _effectiveHead;
        private Vector3 _initialLocalPosition;
        private Quaternion _initialHeadRotation;
        private Vector3 _initialPivotPosition;

        private float _currentY = 0f;
        private float _targetY = 0f;
        private bool _isElevated = false;

        private float _currentAngleOffset = 0f;
        private float _targetAngleOffset = 0f;

        private void Awake()
        {
            InitializePivotAndTarget();
        }

        private void Reset()
        {
            AutoFindReferences();
        }

        private void Start()
        {
            CaptureCurrentPoseAsInitial();
        }

        private void AutoFindReferences()
        {
            if (pivotTransform == null)
            {
                if (transform.parent != null && transform.parent.name.ToLower().Contains("cameraheadermove"))
                {
                    pivotTransform = transform.parent;
                }
                if (pivotTransform == null)
                {
                    var child = transform.Find("cameraheadermove");
                    if (child != null) pivotTransform = child;
                }
                if (pivotTransform == null)
                {
                    var go = GameObject.Find("cameraheadermove");
                    if (go != null) pivotTransform = go.transform;
                }
            }

            if (targetHead == null)
            {
                if (transform.name.ToLower().Contains("cameraheadermove"))
                {
                    var childFace = transform.Find("카메라얼굴") ?? transform.Find("카메라얼굴_1") ?? transform.Find("카메라얼굴_2");
                    if (childFace != null)
                    {
                        targetHead = childFace;
                    }
                }
            }
        }

        public void InitializePivotAndTarget()
        {
            AutoFindReferences();
            _effectiveHead = (targetHead != null) ? targetHead : transform;
            CaptureCurrentPoseAsInitial();
        }

        public void CaptureCurrentPoseAsInitial()
        {
            if (_effectiveHead == null)
            {
                _effectiveHead = (targetHead != null) ? targetHead : transform;
            }

            _initialLocalPosition = _effectiveHead.localPosition;
            _initialHeadRotation = _effectiveHead.rotation;

            _currentY = downY;
            _targetY = downY;
            _isElevated = false;

            if (pivotTransform != null)
            {
                _initialPivotPosition = pivotTransform.position;
            }
            else
            {
                _initialPivotPosition = _effectiveHead.position;
            }

            _currentAngleOffset = 0f;
            _targetAngleOffset = 0f;
        }

        private void Update()
        {
            HandleInput();
            UpdateElevation();
            UpdateRotation();
        }

        private void HandleInput()
        {
            bool riseDown = false;
            bool rightDown = false;
            bool rightHold = false;
            bool leftDown = false;
            bool leftHold = false;
            bool resetDown = false;

            // 1. New Input System 패키지 지원
            #if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                riseDown = kb.oKey.wasPressedThisFrame;

                rightDown = kb.leftBracketKey.wasPressedThisFrame;
                rightHold = kb.leftBracketKey.isPressed;

                leftDown = kb.rightBracketKey.wasPressedThisFrame;
                leftHold = kb.rightBracketKey.isPressed;

                resetDown = kb.pKey.wasPressedThisFrame;
            }
            #endif

            // 2. Legacy Input Fallback
            #if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (!riseDown) riseDown = Input.GetKeyDown(KeyCode.O);

                if (!rightDown) rightDown = Input.GetKeyDown(KeyCode.LeftBracket);
                if (!rightHold) rightHold = Input.GetKey(KeyCode.LeftBracket);

                if (!leftDown) leftDown = Input.GetKeyDown(KeyCode.RightBracket);
                if (!leftHold) leftHold = Input.GetKey(KeyCode.RightBracket);

                if (!resetDown) resetDown = Input.GetKeyDown(KeyCode.P);
            }
            catch { }
            #endif

            // O 키로 상승/하강 토글
            if (riseDown)
            {
                ToggleElevation();
            }

            // 회전 입력 처리
            switch (controlMode)
            {
                case ControlMode.FixedTargetAngle:
                    if (rightDown)
                    {
                        TurnRight();
                    }
                    else if (leftDown)
                    {
                        TurnLeft();
                    }
                    else if (resetDown)
                    {
                        ResetAll();
                    }
                    break;

                case ControlMode.HoldToRotate:
                    float dir = invertDirection ? -1f : 1f;
                    if (rightHold)
                    {
                        _targetAngleOffset += dir * rotationSpeed * Time.deltaTime;
                    }
                    else if (leftHold)
                    {
                        _targetAngleOffset -= dir * rotationSpeed * Time.deltaTime;
                    }

                    if (resetDown)
                    {
                        ResetAll();
                    }
                    break;

                case ControlMode.StepByStep:
                    if (rightDown)
                    {
                        TurnRightStep();
                    }
                    else if (leftDown)
                    {
                        TurnLeftStep();
                    }
                    else if (resetDown)
                    {
                        ResetAll();
                    }
                    break;
            }
        }

        private void UpdateElevation()
        {
            if (smoothRise)
            {
                _currentY = Mathf.Lerp(_currentY, _targetY, Time.deltaTime * (riseSpeed * 25f));
            }
            else
            {
                _currentY = Mathf.MoveTowards(_currentY, _targetY, riseSpeed * Time.deltaTime);
            }
        }

        private void UpdateRotation()
        {
            if (_effectiveHead == null) return;

            // 회전 각도 보간
            if (smoothDamping)
            {
                _currentAngleOffset = Mathf.Lerp(_currentAngleOffset, _targetAngleOffset, Time.deltaTime * (rotationSpeed / 15f));
            }
            else
            {
                _currentAngleOffset = Mathf.MoveTowards(_currentAngleOffset, _targetAngleOffset, rotationSpeed * Time.deltaTime);
            }

            // 1. 현재 높이(Y)가 반영된 로컬 기본 위치 계산
            Vector3 currentLocalBase = new Vector3(_initialLocalPosition.x, _currentY, _initialLocalPosition.z);
            Vector3 currentWorldBase = (_effectiveHead.parent != null) 
                ? _effectiveHead.parent.TransformPoint(currentLocalBase) 
                : currentLocalBase;

            // 2. 회전 축 및 피벗 위치 결정 (카메라헤더무브 축)
            Vector3 axis = Vector3.up;
            Vector3 pivot = _initialPivotPosition;

            if (pivotTransform != null)
            {
                pivot = pivotTransform.position;
                if (usePivotUpAxis)
                {
                    axis = pivotTransform.up;
                }
            }

            // 피벗의 수평(X, Z)을 유지하면서 현재 상승 높이(Y)에 맞춤
            Vector3 elevatedPivot = new Vector3(pivot.x, currentWorldBase.y, pivot.z);

            // 3. 회전 적용
            Quaternion rotOffset = Quaternion.AngleAxis(_currentAngleOffset, axis.normalized);
            Quaternion newRot = rotOffset * _initialHeadRotation;

            // 피벗 기준 위치 회전: newPos = elevatedPivot + rotOffset * (currentWorldBase - elevatedPivot)
            Vector3 offsetFromPivot = currentWorldBase - elevatedPivot;
            Vector3 newPos = elevatedPivot + (rotOffset * offsetFromPivot);

            _effectiveHead.rotation = newRot;
            _effectiveHead.position = newPos;
        }

        /// <summary>
        /// O 키 토글 (올라오기 <-> 내려가기)
        /// </summary>
        [ContextMenu("Toggle Elevation (O)")]
        public void ToggleElevation()
        {
            if (_isElevated)
            {
                LowerDown();
            }
            else
            {
                RiseUp();
            }
        }

        /// <summary>
        /// 위로 쭉 올라오기 (두 번째 사진 상태: Y: 0.045)
        /// </summary>
        [ContextMenu("Rise Up")]
        public void RiseUp()
        {
            _targetY = upY;
            _isElevated = true;
        }

        /// <summary>
        /// 아래로 쏙 내려가기 (첫 번째 사진 상태: Y: 0)
        /// </summary>
        [ContextMenu("Lower Down")]
        public void LowerDown()
        {
            _targetY = downY;
            _isElevated = false;
        }

        /// <summary>
        /// 오른쪽으로 회전 (단축키: [)
        /// </summary>
        [ContextMenu("Turn Right ([)")]
        public void TurnRight()
        {
            float dir = invertDirection ? -1f : 1f;
            _targetAngleOffset = dir * -targetAngle;
        }

        /// <summary>
        /// 왼쪽으로 회전 (단축키: ])
        /// </summary>
        [ContextMenu("Turn Left (])")]
        public void TurnLeft()
        {
            float dir = invertDirection ? -1f : 1f;
            _targetAngleOffset = dir * targetAngle;
        }

        private void TurnRightStep()
        {
            float dir = invertDirection ? -1f : 1f;
            _targetAngleOffset += dir * -stepAngle;
        }

        private void TurnLeftStep()
        {
            float dir = invertDirection ? -1f : 1f;
            _targetAngleOffset += dir * stepAngle;
        }

        /// <summary>
        /// 원래 정면 각도로 리셋 (단축키: P)
        /// </summary>
        [ContextMenu("Reset Rotation (P)")]
        public void ResetRotation()
        {
            _targetAngleOffset = 0f;
        }

        /// <summary>
        /// P 키를 눌렀을 때 전체 리셋
        /// </summary>
        public void ResetAll()
        {
            ResetRotation();
            if (resetElevationOnResetKey)
            {
                LowerDown();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 pivot = (pivotTransform != null) ? pivotTransform.position : transform.position;
            Vector3 axis = (pivotTransform != null && usePivotUpAxis) ? pivotTransform.up : Vector3.up;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pivot, 0.025f);
            Gizmos.DrawLine(pivot - axis * 0.1f, pivot + axis * 0.2f);
        }
    }
}
