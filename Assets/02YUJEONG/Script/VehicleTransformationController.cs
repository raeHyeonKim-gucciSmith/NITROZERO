using System.Collections;
using UnityEngine;

namespace YUJEONG
{
    /// <summary>
    /// 차량 파츠 변형 시퀀스 제어 컨트롤러
    /// 숫자 키패드 및 숫자 상단키(1, 2, 3...)로 단계별 기믹을 테스트하고 제어할 수 있습니다.
    /// </summary>
    public class VehicleTransformationController : MonoBehaviour
    {
        [System.Serializable]
        public struct TransformPose
        {
            public Vector3 position;
            public Vector3 rotationEuler;

            public TransformPose(Vector3 pos, Vector3 rot)
            {
                position = pos;
                rotationEuler = rot;
            }
        }

        [Header("[ 제어 키 설정 ]")]
        [Tooltip("1단계: 보조 벤트 판 1, 2 작동 키")]
        public KeyCode triggerKey1 = KeyCode.Alpha1;
        public KeyCode triggerKeypad1 = KeyCode.Keypad1;

        [Tooltip("2단계: 3D 스캐너 전개 키")]
        public KeyCode triggerKey2 = KeyCode.Alpha2;
        public KeyCode triggerKeypad2 = KeyCode.Keypad2;

        [Tooltip("3단계: 연결부 1, 2 접기 키")]
        public KeyCode triggerKey3 = KeyCode.Alpha3;
        public KeyCode triggerKeypad3 = KeyCode.Keypad3;

        [Tooltip("4단계: 대형 단발 제트 부스터 노즐 돌출 키")]
        public KeyCode triggerKey4 = KeyCode.Alpha4;
        public KeyCode triggerKeypad4 = KeyCode.Keypad4;

        [Tooltip("5단계: 부품 메인상판덮개 개폐 키")]
        public KeyCode triggerKey5 = KeyCode.Alpha5;
        public KeyCode triggerKeypad5 = KeyCode.Keypad5;

        [Header("[ 풀 변형 시퀀스 설정 (1->2->3->4+5) ]")]
        [Tooltip("전체 순차 변형 단축키 (스페이스바 또는 숫자 0)")]
        public KeyCode triggerKeyFullSequence = KeyCode.Space;
        public KeyCode triggerKeyFullSequenceAlt = KeyCode.Alpha0;

        [Tooltip("4번 노즐 돌출 시작 후 5번 상판 수납이 시작되기까지의 딜레이 (한 박자 늦게, 초 단위)")]
        [Range(0.02f, 0.3f)]
        public float stage5DelayAfterStage4 = 0.08f;

        [Header("[ 대상 차량 루트 (기본: SportCar_4change_2) ]")]
        public Transform targetCarRoot;

        [Header("[ 1단계 대상: 보조 벤트 판 ]")]
        public Transform ventLeft;  // 부품_보조벤트판1
        public Transform ventRight; // 부품_보조벤트판2

        [Header("[ 2단계 대상: 3D 스캐너 ]")]
        public Transform scanner3D; // 3d스캐너

        [Header("[ 3단계 대상: 카울 연결부 1, 2 ]")]
        public Transform couplerLeft;  // 부품_연결부1
        public Transform couplerRight; // 부품_연결부2

        [Header("[ 3단계 대상: 기계식 상부 아머드 카울 ]")]
        public Transform armoredCowl;  // 부품_기계식상부아머드카울

        [Header("[ 4단계 대상: 대형 단발 제트 부스터 노즐 ]")]
        public Transform boosterNozzle; // 대형_단발_제트_부스터_노즐
        public GameObject boosterNeon;   // 네온

        [Header("[ 5단계 대상: 부품 메인상판덮개 ]")]
        public Transform mainCover;     // 부품_메인상판덮개

        [Header("[ 애니메이션 세부 설정 ]")]
        [Tooltip("각 단계별 기본 이동 소요 시간 (초)")]
        [Range(0.05f, 1.0f)]
        public float stepDuration = 0.2f;

        [Tooltip("메인상판덮개가 차 안으로 슬라이드 수납되는 시간 (초, 값이 클수록 천천히 부드럽게 들어감)")]
        [Range(0.1f, 1.5f)]
        public float coverRetractDuration = 0.45f;

        [Tooltip("단계 사이 기계적 딜레이 (철컥 느낌)")]
        [Range(0.0f, 0.5f)]
        public float stepPause = 0.05f;

        [Tooltip("기계적 움직임을 위한 가감속 커브")]
        public AnimationCurve motionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("[ 보조벤트판1 (좌측) 4단계 포즈 ]")]
        [Tooltip("1단계: 원래 위치")]
        public TransformPose leftStep1 = new TransformPose(new Vector3(-0.44f, 0.168f, -1.126f), new Vector3(-85.53f, 90f, -180f));
        [Tooltip("2단계: 수직 상승")]
        public TransformPose leftStep2 = new TransformPose(new Vector3(-0.443f, 0.204f, -1.126f), new Vector3(-85.53f, 90f, -180f));
        [Tooltip("3단계: 안쪽 눕기 (틸트)")]
        public TransformPose leftStep3 = new TransformPose(new Vector3(-0.481f, 0.223f, -1.126f), new Vector3(-44.74f, 90f, -180f));
        [Tooltip("4단계: 메인상판덮개 안으로 완전 수납")]
        public TransformPose leftStep4 = new TransformPose(new Vector3(-0.441f, 0.262f, -1.126f), new Vector3(-44.74f, 90f, -180f));

        [Header("[ 3D 스캐너 3단계 포즈 ]")]
        [Tooltip("1단계: 원래 위치 (지붕 뒤쪽 대기)")]
        public TransformPose scannerStep1 = new TransformPose(new Vector3(0.003f, 0.243f, -0.883f), new Vector3(-108.4f, 0f, 0f));
        [Tooltip("2단계: 대각선 슬라이드 상승")]
        public TransformPose scannerStep2 = new TransformPose(new Vector3(0.003f, 0.324f, -0.641f), new Vector3(-108.4f, 0f, 0f));
        [Tooltip("3단계: 지붕 위 수평 전개 완료")]
        public TransformPose scannerStep3 = new TransformPose(new Vector3(0.003f, 0.341f, -0.419f), new Vector3(-90.38f, 0f, 0f));

        [Header("[ 연결부1 (좌측) 3단계 포즈 ]")]
        [Tooltip("1단계: 원래 위치 (펼쳐진 상태)")]
        public TransformPose couplerLeftStep1 = new TransformPose(new Vector3(-0.441f, -0.048f, -1.481f), new Vector3(-90f, 0f, 0f));
        [Tooltip("2단계: 80도 회전 접힘")]
        public TransformPose couplerLeftStep2 = new TransformPose(new Vector3(-0.392f, -0.048f, -1.534f), new Vector3(-90f, 0f, -80.13f));
        [Tooltip("3단계: 147도 완전 접힘")]
        public TransformPose couplerLeftStep3 = new TransformPose(new Vector3(-0.227f, -0.048f, -1.589f), new Vector3(-90f, 0f, -147.2f));

        [Header("[ 기계식 상부 아머드 카울 3단계 포즈 ]")]
        [Tooltip("1단계: 원래 위치")]
        public TransformPose cowlStep1 = new TransformPose(new Vector3(0.023f, -0.013f, -1.324f), new Vector3(-89.49f, 180f, 0f));
        [Tooltip("2단계: 수직 대폭 상승")]
        public TransformPose cowlStep2 = new TransformPose(new Vector3(0.023f, 0.164f, -1.327f), new Vector3(-89.49f, 180f, 0f));
        [Tooltip("3단계: 전방 살짝 틸트 및 완전 상승")]
        public TransformPose cowlStep3 = new TransformPose(new Vector3(0.023f, 0.184f, -1.253f), new Vector3(-80.23f, 180f, 0f));

        [Header("[ 대형 단발 제트 부스터 노즐 2단계 포즈 ]")]
        [Tooltip("1단계: 원래 대기 위치")]
        public TransformPose boosterStep1 = new TransformPose(new Vector3(0.016f, -0.306f, -1.088f), new Vector3(-89.65f, 0f, 90f));
        [Tooltip("2단계: 후방 돌출 발동 위치")]
        public TransformPose boosterStep2 = new TransformPose(new Vector3(0.017f, -0.305f, -1.251f), new Vector3(-89.65f, 0f, 90f));

        [Header("[ 부품 메인상판덮개 3단계 포즈 ]")]
        [Tooltip("1단계: 원래 닫힌 위치")]
        public TransformPose coverStep1 = new TransformPose(new Vector3(0.003f, 0.1718f, -1.244f), new Vector3(-90f, 180f, 0f));
        [Tooltip("2단계: 사용자 지정 오픈 대기 위치")]
        public TransformPose coverStep2 = new TransformPose(new Vector3(0.003f, 0.442f, -1.131f), new Vector3(-142.1f, 180f, 0f));
        [Tooltip("3단계: 차체 내부 완전 슬라이드 수납")]
        public TransformPose coverStep3 = new TransformPose(new Vector3(0.003f, -0.098f, -0.565f), new Vector3(-142.1f, 180f, 0f));

        // 3D 스캐너 상태 관리
        private bool isScannerDeployed = false;
        private Coroutine scannerCoroutine = null;

        // 연결부 상태 관리
        private bool isCouplerFolded = false;
        private Coroutine couplerCoroutine = null;

        // 아머드 카울 상태 관리
        private bool isCowlLifted = false;
        private Coroutine cowlCoroutine = null;

        // 제트 부스터 노즐 상태 관리
        private bool isBoosterDeployed = false;
        private Coroutine boosterCoroutine = null;

        // 메인 상판 덮개 상태 관리
        private bool isCoverOpened = false;
        private Coroutine coverCoroutine = null;

        // 전체 시퀀스 상태 관리
        private bool isFullSequenceRunning = false;
        private bool isFullyTransformed = false;
        private Coroutine fullSequenceCoroutine = null;

        // 내부 상태 관리
        private bool isVentHidden = false; // 현재 수납되었는지 여부 (토글용)
        private Coroutine ventCoroutine = null;

        private void Reset()
        {
            AutoBindParts();
        }

        private void Awake()
        {
            if (ventLeft == null || ventRight == null)
            {
                AutoBindParts();
            }

            // 사용자 지정 정확한 2단계 오픈 대기 위치 및 3단계 수납 위치 적용
            coverStep2.position = new Vector3(0.003f, 0.442f, -1.131f);
            coverStep2.rotationEuler = new Vector3(-142.1f, 180f, 0f);
            coverStep3.position = new Vector3(0.003f, -0.098f, -0.565f);
            coverStep3.rotationEuler = new Vector3(-142.1f, 180f, 0f);
        }

        [ContextMenu("SportCar_4change_2 부품 자동 연결")]
        public void AutoBindParts()
        {
            // 1. targetCarRoot 찾기
            if (targetCarRoot == null)
            {
                // 컴포넌트 자신이 SportCar_4change_2에 붙어있는 경우
                if (gameObject.name == "SportCar_4change_2")
                {
                    targetCarRoot = transform;
                }
                else
                {
                    GameObject carObj = GameObject.Find("SportCar_4change_2");
                    if (carObj != null) targetCarRoot = carObj.transform;
                }
            }

            // 2. targetCarRoot 하위에서 부품 찾기
            if (targetCarRoot != null)
            {
                Transform foundLeft = targetCarRoot.Find("부품_보조벤트판1");
                if (foundLeft != null) ventLeft = foundLeft;

                Transform foundRight = targetCarRoot.Find("부품_보조벤트판2");
                if (foundRight != null) ventRight = foundRight;

                Transform foundScanner = targetCarRoot.Find("3d스캐너");
                if (foundScanner != null) scanner3D = foundScanner;

                Transform foundCouplerL = targetCarRoot.Find("부품_연결부1");
                if (foundCouplerL != null) couplerLeft = foundCouplerL;

                Transform foundCouplerR = targetCarRoot.Find("부품_연결부2");
                if (foundCouplerR != null) couplerRight = foundCouplerR;

                Transform foundCowl = targetCarRoot.Find("부품_기계식상부아머드카울");
                if (foundCowl != null) armoredCowl = foundCowl;

                // 대형_단발_제트_부스터_노즐 찾기
                Transform foundBooster = targetCarRoot.Find("대형_단발_제트_부스터_노즐");
                if (foundBooster == null)
                {
                    Transform parentBooster = targetCarRoot.Find("대형_단발_제트_부스터");
                    if (parentBooster != null)
                    {
                        foundBooster = parentBooster.Find("대형_단발_제트_부스터_노즐");
                        if (foundBooster == null) foundBooster = parentBooster;
                    }
                }
                if (foundBooster != null) boosterNozzle = foundBooster;

                // 네온 찾기
                Transform foundNeon = targetCarRoot.Find("네온");
                if (foundNeon == null)
                {
                    Transform parentBooster = targetCarRoot.Find("대형_단발_제트_부스터");
                    if (parentBooster != null) foundNeon = parentBooster.Find("네온");
                }
                if (foundNeon != null) boosterNeon = foundNeon.gameObject;

                // 부품_메인상판덮개 찾기
                Transform foundCover = targetCarRoot.Find("부품_메인상판덮개");
                if (foundCover != null) mainCover = foundCover;

                Debug.Log($"[VehicleTransformationController] '{targetCarRoot.name}' 하위에서 부품들을 자동으로 연결했습니다.");
            }
            else
            {
                // 전역 탐색 백업
                GameObject leftObj = GameObject.Find("부품_보조벤트판1");
                if (leftObj != null) ventLeft = leftObj.transform;

                GameObject rightObj = GameObject.Find("부품_보조벤트판2");
                if (rightObj != null) ventRight = rightObj.transform;

                GameObject scannerObj = GameObject.Find("3d스캐너");
                if (scannerObj != null) scanner3D = scannerObj.transform;

                GameObject couplerLObj = GameObject.Find("부품_연결부1");
                if (couplerLObj != null) couplerLeft = couplerLObj.transform;

                GameObject couplerRObj = GameObject.Find("부품_연결부2");
                if (couplerRObj != null) couplerRight = couplerRObj.transform;

                GameObject cowlObj = GameObject.Find("부품_기계식상부아머드카울");
                if (cowlObj != null) armoredCowl = cowlObj.transform;

                GameObject boosterObj = GameObject.Find("대형_단발_제트_부스터_노즐");
                if (boosterObj != null) boosterNozzle = boosterObj.transform;

                GameObject neonObj = GameObject.Find("네온");
                if (neonObj != null) boosterNeon = neonObj;

                GameObject coverObj = GameObject.Find("부품_메인상판덮개");
                if (coverObj != null) mainCover = coverObj.transform;
            }
        }

        [Header("[ 거울(Mirror) 대칭 모드 ]")]
        [Tooltip("체크 시 좌측 벤트판의 움직임을 실시간 거울 대칭하여 우측에 자동 적용합니다 (뒤집힘 0% 보장)")]
        public bool mirrorRightFromLeft = true;

        [Tooltip("체크 시 좌측 연결부1의 움직임을 실시간 거울 대칭하여 우측 연결부2에 자동 적용합니다")]
        public bool mirrorRightCoupler = true;

        private Vector3 initialLeftPos;
        private Quaternion initialLeftRot;
        private Vector3 initialRightPos;
        private Quaternion initialRightRot;

        private Vector3 initialCouplerLeftPos;
        private Quaternion initialCouplerLeftRot;
        private Vector3 initialCouplerRightPos;
        private Quaternion initialCouplerRightRot;

        private Vector3 initialCowlPos;
        private Quaternion initialCowlRot;

        // 카울이 올라갈 때 기준이 될 연결부 포즈
        private Vector3 couplerLeftBeforeCowlPos;
        private Quaternion couplerLeftBeforeCowlRot;

        // 메인 상판 덮개가 열릴 때 기준이 될 보조 벤트 포즈
        private Vector3 ventLeftBeforeCoverPos;
        private Quaternion ventLeftBeforeCoverRot;
        private Vector3 ventRightBeforeCoverPos;
        private Quaternion ventRightBeforeCoverRot;

        private void Start()
        {
            if (ventLeft == null || ventRight == null || scanner3D == null || couplerLeft == null || couplerRight == null || armoredCowl == null || boosterNozzle == null || mainCover == null)
            {
                AutoBindParts();
            }

            if (ventLeft != null && ventRight != null)
            {
                initialLeftPos = leftStep1.position;
                initialLeftRot = Quaternion.Euler(leftStep1.rotationEuler);

                initialRightPos = ventRight.localPosition;
                initialRightRot = ventRight.localRotation;

                ventLeftBeforeCoverPos = ventLeft.localPosition;
                ventLeftBeforeCoverRot = ventLeft.localRotation;
                ventRightBeforeCoverPos = ventRight.localPosition;
                ventRightBeforeCoverRot = ventRight.localRotation;
            }

            if (couplerLeft != null && couplerRight != null)
            {
                initialCouplerLeftPos = couplerLeftStep1.position;
                initialCouplerLeftRot = Quaternion.Euler(couplerLeftStep1.rotationEuler);

                initialCouplerRightPos = couplerRight.localPosition;
                initialCouplerRightRot = couplerRight.localRotation;
            }

            if (armoredCowl != null)
            {
                initialCowlPos = cowlStep1.position;
                initialCowlRot = Quaternion.Euler(cowlStep1.rotationEuler);
            }

            Debug.Log($"[VehicleTransformationController] ✅ 준비 완료! Vent: {(ventLeft != null)}, Scanner: {(scanner3D != null)}, Coupler: {(couplerLeft != null)}, Cowl: {(armoredCowl != null)}, Booster: {(boosterNozzle != null)}, Cover: {(mainCover != null)}");
        }

        private void Update()
        {
            // 0. 전체 변형 시퀀스 키 입력 체크 (스페이스바 / 0번)
            bool keyFullPressed = false;
            try
            {
                if (Input.GetKeyDown(triggerKeyFullSequence) || Input.GetKeyDown(triggerKeyFullSequenceAlt))
                    keyFullPressed = true;
            }
            catch { }

#if ENABLE_INPUT_SYSTEM || UNITY_INPUT_SYSTEM_EXISTS
            if (!keyFullPressed && UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb.spaceKey.wasPressedThisFrame || kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame)
                    keyFullPressed = true;
            }
#endif
            if (keyFullPressed)
            {
                Debug.Log("[VehicleTransformationController] 🔥 전체 변형 시퀀스 키 입력 감지!");
                ToggleFullTransformation();
            }

            // 1. 1번 키 입력 체크 (보조 벤트판)
            bool key1Pressed = false;
            try
            {
                if (Input.GetKeyDown(triggerKey1) || Input.GetKeyDown(triggerKeypad1))
                    key1Pressed = true;
            }
            catch { }

#if ENABLE_INPUT_SYSTEM || UNITY_INPUT_SYSTEM_EXISTS
            if (!key1Pressed && UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
                    key1Pressed = true;
            }
#endif
            if (key1Pressed)
            {
                Debug.Log("[VehicleTransformationController] 🎯 1번 키 입력 감지! 보조 벤트 변형 시작");
                ToggleVentRetraction();
            }

            // 2. 2번 키 입력 체크 (3D 스캐너 + 카울 연결부 1, 2 동시 실행)
            bool key2Pressed = false;
            try
            {
                if (Input.GetKeyDown(triggerKey2) || Input.GetKeyDown(triggerKeypad2))
                    key2Pressed = true;
            }
            catch { }

#if ENABLE_INPUT_SYSTEM || UNITY_INPUT_SYSTEM_EXISTS
            if (!key2Pressed && UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
                    key2Pressed = true;
            }
#endif
            if (key2Pressed)
            {
                Debug.Log("[VehicleTransformationController] 🎯 2번 키 입력 감지! 3D 스캐너 + 연결부 동시 변형 시작");
                ToggleStage2();
            }

            // 3. 3번 키 입력 체크 (기계식 상부 아머드 카울 + 접힌 연결부 동반 상승)
            bool key3Pressed = false;
            try
            {
                if (Input.GetKeyDown(triggerKey3) || Input.GetKeyDown(triggerKeypad3))
                    key3Pressed = true;
            }
            catch { }

#if ENABLE_INPUT_SYSTEM || UNITY_INPUT_SYSTEM_EXISTS
            if (!key3Pressed && UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
                    key3Pressed = true;
            }
#endif
            if (key3Pressed)
            {
                Debug.Log("[VehicleTransformationController] 🎯 3번 키 입력 감지! 기계식 상부 아머드 카울 상승 시작");
                ToggleCowlLift();
            }

            // 4. 4번 키 입력 체크 (대형 단발 제트 부스터 노즐 돌출)
            bool key4Pressed = false;
            try
            {
                if (Input.GetKeyDown(triggerKey4) || Input.GetKeyDown(triggerKeypad4))
                    key4Pressed = true;
            }
            catch { }

#if ENABLE_INPUT_SYSTEM || UNITY_INPUT_SYSTEM_EXISTS
            if (!key4Pressed && UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame)
                    key4Pressed = true;
            }
#endif
            if (key4Pressed)
            {
                Debug.Log("[VehicleTransformationController] 🎯 4번 키 입력 감지! 제트 부스터 노즐 돌출 시작");
                ToggleBoosterDeploy();
            }

            // 5. 5번 키 입력 체크 (부품 메인상판덮개 개폐)
            bool key5Pressed = false;
            try
            {
                if (Input.GetKeyDown(triggerKey5) || Input.GetKeyDown(triggerKeypad5))
                    key5Pressed = true;
            }
            catch { }

#if ENABLE_INPUT_SYSTEM || UNITY_INPUT_SYSTEM_EXISTS
            if (!key5Pressed && UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame)
                    key5Pressed = true;
            }
#endif
            if (key5Pressed)
            {
                Debug.Log("[VehicleTransformationController] 🎯 5번 키 입력 감지! 메인 상판 덮개 개폐 시작");
                ToggleMainCover();
            }
        }

        private void OnGUI()
        {
            // 화면 좌상단에 테스트용 UI 패널 표시
            GUI.Box(new Rect(10, 10, 260, 245), "🚗 변형 제어 패널");

            // 전체 변형 시퀀스 버튼 (1->2->3->4+5)
            GUI.color = isFullyTransformed ? new Color(1f, 0.75f, 0.75f) : new Color(0.75f, 1f, 0.75f);
            if (GUI.Button(new Rect(20, 35, 240, 30), isFullyTransformed ? "🔥 전체 원복 (Space / 0)" : "🔥 풀 변형 시퀀스 (Space / 0)"))
            {
                ToggleFullTransformation();
            }
            GUI.color = Color.white;

            // 1번 버튼: 보조 벤트
            if (GUI.Button(new Rect(20, 70, 240, 26), isVentHidden ? "1번: 보조벤트 원복 (4->1)" : "1번: 보조벤트 수납 (1->4)"))
            {
                ToggleVentRetraction();
            }

            // 2번 버튼: 3D 스캐너 + 연결부 2개 동시 전개
            if (GUI.Button(new Rect(20, 100, 240, 26), (isScannerDeployed || isCouplerFolded) ? "2번: 스캐너+연결부 복구" : "2번: 스캐너+연결부 동시 전개"))
            {
                ToggleStage2();
            }

            // 3번 버튼: 기계식 상부 아머드 카울 + 연결부 상승
            if (GUI.Button(new Rect(20, 130, 240, 26), isCowlLifted ? "3번: 아머드 카울 하강 (3->1)" : "3번: 카울+연결부 상승 (1->3)"))
            {
                ToggleCowlLift();
            }

            // 4번 버튼: 대형 단발 제트 부스터 노즐 돌출
            if (GUI.Button(new Rect(20, 160, 240, 26), isBoosterDeployed ? "4번: 부스터 노즐 수납 (2->1)" : "4번: 부스터 노즐 돌출 (1->2)"))
            {
                ToggleBoosterDeploy();
            }

            // 5번 버튼: 부품 메인상판덮개 개폐 (내부 수납)
            if (GUI.Button(new Rect(20, 190, 240, 26), isCoverOpened ? "5번: 메인상판 원복 (3->1)" : "5번: 메인상판 내부수납 (1->3)"))
            {
                ToggleMainCover();
            }
        }

        /// <summary>
        /// 2단계: 3D 스캐너 지붕 상승 + 카울 연결부 2개 접기 동시 실행
        /// </summary>
        [ContextMenu("2번 3D스캐너 + 연결부2개 동시 실행 / 토글")]
        public void ToggleStage2()
        {
            bool targetState = !(isScannerDeployed && isCouplerFolded);

            if (scannerCoroutine != null) StopCoroutine(scannerCoroutine);
            if (couplerCoroutine != null) StopCoroutine(couplerCoroutine);

            isScannerDeployed = targetState;
            isCouplerFolded = targetState;

            scannerCoroutine = StartCoroutine(AnimateScannerSequence(targetState));
            couplerCoroutine = StartCoroutine(AnimateCouplerSequence(targetState));

            Debug.Log($"[VehicleTransformationController] 🚀 2번 실행: 3D스캐너와 연결부 2개가 동시에 {(targetState ? "전개/접힘" : "복구")}됩니다!");
        }

        /// <summary>
        /// 3D 스캐너 지붕 전개 / 복구 토글
        /// </summary>
        [ContextMenu("2번 3D 스캐너 전개 / 복구 토글")]
        public void ToggleScannerDeployment()
        {
            if (scannerCoroutine != null)
            {
                StopCoroutine(scannerCoroutine);
            }

            isScannerDeployed = !isScannerDeployed;
            scannerCoroutine = StartCoroutine(AnimateScannerSequence(isScannerDeployed));
        }

        /// <summary>
        /// 3D 스캐너 3단계 전개 시퀀스 코루틴
        /// </summary>
        private IEnumerator AnimateScannerSequence(bool deploy)
        {
            if (deploy)
            {
                // 1 -> 2단계: 대각선 슬라이드 상승
                yield return AnimateScannerBetweenPoses(scannerStep1, scannerStep2, stepDuration);
                if (stepPause > 0) yield return new WaitForSeconds(stepPause);

                // 2 -> 3단계: 지붕 위 수평 전개 완료
                yield return AnimateScannerBetweenPoses(scannerStep2, scannerStep3, stepDuration);
            }
            else
            {
                // 역순 복구 (3 -> 2 -> 1)
                yield return AnimateScannerBetweenPoses(scannerStep3, scannerStep2, stepDuration);
                if (stepPause > 0) yield return new WaitForSeconds(stepPause);

                yield return AnimateScannerBetweenPoses(scannerStep2, scannerStep1, stepDuration);
            }

            scannerCoroutine = null;
        }

        /// <summary>
        /// 3D 스캐너의 두 포즈 사이 부드러운 보간
        /// </summary>
        private IEnumerator AnimateScannerBetweenPoses(TransformPose from, TransformPose to, float duration)
        {
            if (scanner3D == null) yield break;

            float elapsed = 0f;
            Quaternion rotFrom = Quaternion.Euler(from.rotationEuler);
            Quaternion rotTo = Quaternion.Euler(to.rotationEuler);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveT = motionCurve != null ? motionCurve.Evaluate(t) : t;

                scanner3D.localPosition = Vector3.Lerp(from.position, to.position, curveT);
                scanner3D.localRotation = Quaternion.Slerp(rotFrom, rotTo, curveT);

                yield return null;
            }

            scanner3D.localPosition = to.position;
            scanner3D.localRotation = rotTo;
        }

        /// <summary>
        /// 보조 벤트 판 수납 / 복구 토글
        /// </summary>
        [ContextMenu("1번 변형 실행 / 토글")]
        public void ToggleVentRetraction()
        {
            if (isCoverOpened)
            {
                Debug.LogWarning("[VehicleTransformationController] ⚠️ 메인 상판 덮개가 열려 있는 상태에서는 보조 벤트를 단독으로 조작할 수 없습니다. 덮개를 먼저 닫아주세요.");
                return;
            }

            if (ventCoroutine != null)
            {
                StopCoroutine(ventCoroutine);
            }

            isVentHidden = !isVentHidden;
            ventCoroutine = StartCoroutine(AnimateVentSequence(isVentHidden));
        }

        /// <summary>
        /// 4단계 변형 시퀀스 애니메이션 코루틴
        /// </summary>
        private IEnumerator AnimateVentSequence(bool hide)
        {
            if (hide)
            {
                // 1 -> 2단계: 수직 상승
                yield return AnimateBetweenPoses(leftStep1, leftStep2, stepDuration);
                if (stepPause > 0) yield return new WaitForSeconds(stepPause);

                // 2 -> 3단계: 안쪽으로 눕기 (틸트)
                yield return AnimateBetweenPoses(leftStep2, leftStep3, stepDuration);
                if (stepPause > 0) yield return new WaitForSeconds(stepPause);

                // 3 -> 4단계: 상판 덮개 안으로 완전 수납
                yield return AnimateBetweenPoses(leftStep3, leftStep4, stepDuration);
            }
            else
            {
                // 역순 복구 (4 -> 3 -> 2 -> 1)
                yield return AnimateBetweenPoses(leftStep4, leftStep3, stepDuration);
                if (stepPause > 0) yield return new WaitForSeconds(stepPause);

                yield return AnimateBetweenPoses(leftStep3, leftStep2, stepDuration);
                if (stepPause > 0) yield return new WaitForSeconds(stepPause);

                yield return AnimateBetweenPoses(leftStep2, leftStep1, stepDuration);
            }

            ventCoroutine = null;
        }

        /// <summary>
        /// 좌측 포즈를 보간 이동하면서, 우측은 수학적 거울 대칭으로 완벽 연동
        /// </summary>
        private IEnumerator AnimateBetweenPoses(TransformPose leftFrom, TransformPose leftTo, float duration)
        {
            float elapsed = 0f;

            Quaternion leftRotFrom = Quaternion.Euler(leftFrom.rotationEuler);
            Quaternion leftRotTo = Quaternion.Euler(leftTo.rotationEuler);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveT = motionCurve != null ? motionCurve.Evaluate(t) : t;

                // 1. 좌측 벤트판 보간
                if (ventLeft != null)
                {
                    ventLeft.localPosition = Vector3.Lerp(leftFrom.position, leftTo.position, curveT);
                    ventLeft.localRotation = Quaternion.Slerp(leftRotFrom, leftRotTo, curveT);
                }

                // 2. 우측 벤트판은 좌측의 움직임을 거울(Mirror) 반사
                if (ventRight != null && mirrorRightFromLeft)
                {
                    ApplyMirrorTransformToRight();
                }

                yield return null;
            }

            // 최종 위치 정확히 고정
            if (ventLeft != null)
            {
                ventLeft.localPosition = leftTo.position;
                ventLeft.localRotation = leftRotTo;
            }

            if (ventRight != null && mirrorRightFromLeft)
            {
                ApplyMirrorTransformToRight();
            }
        }

        /// <summary>
        /// 좌측 벤트판의 현재 상대 변화량을 기반으로 우측 벤트판에 데칼코마니 거울 대칭 적용
        /// </summary>
        private void ApplyMirrorTransformToRight()
        {
            if (ventLeft == null || ventRight == null) return;

            // 1. 위치 미러링: X축 이동량만 반대 부호, Y/Z는 동일
            Vector3 deltaPosLeft = ventLeft.localPosition - initialLeftPos;
            Vector3 deltaPosRight = new Vector3(-deltaPosLeft.x, deltaPosLeft.y, deltaPosLeft.z);
            ventRight.localPosition = initialRightPos + deltaPosRight;

            // 2. 회전 미러링: 시작 각도에서 벗어난 회전량(Delta)을 거울 반사
            Quaternion deltaRotLeft = ventLeft.localRotation * Quaternion.Inverse(initialLeftRot);
            deltaRotLeft.ToAngleAxis(out float angle, out Vector3 axis);

            if (angle > 180f) angle -= 360f;

            // 거울 대칭 반사 공식 (X축 반전 시 회전축과 각도 반사)
            Vector3 mirrorAxis = new Vector3(-axis.x, axis.y, axis.z);
            float mirrorAngle = -angle;

            Quaternion deltaRotRight = Quaternion.AngleAxis(mirrorAngle, mirrorAxis);
            ventRight.localRotation = deltaRotRight * initialRightRot;
        }

        /// <summary>
        /// 카울 연결부 1, 2 접기 / 복구 토글
        /// </summary>
        [ContextMenu("3번 카울 연결부 접기 / 복구 토글")]
        public void ToggleCouplerFolding()
        {
            if (couplerCoroutine != null)
            {
                StopCoroutine(couplerCoroutine);
            }

            isCouplerFolded = !isCouplerFolded;
            couplerCoroutine = StartCoroutine(AnimateCouplerSequence(isCouplerFolded));
        }

        /// <summary>
        /// 카울 연결부 3단계 접힘 시퀀스 코루틴
        /// </summary>
        private IEnumerator AnimateCouplerSequence(bool fold)
        {
            if (fold)
            {
                // 1 -> 2단계: 80도 회전 접힘
                yield return AnimateCouplerBetweenPoses(couplerLeftStep1, couplerLeftStep2, stepDuration);
                if (stepPause > 0) yield return new WaitForSeconds(stepPause);

                // 2 -> 3단계: 147도 완전 접힘
                yield return AnimateCouplerBetweenPoses(couplerLeftStep2, couplerLeftStep3, stepDuration);
            }
            else
            {
                // 역순 복구 (3 -> 2 -> 1)
                yield return AnimateCouplerBetweenPoses(couplerLeftStep3, couplerLeftStep2, stepDuration);
                if (stepPause > 0) yield return new WaitForSeconds(stepPause);

                yield return AnimateCouplerBetweenPoses(couplerLeftStep2, couplerLeftStep1, stepDuration);
            }

            couplerCoroutine = null;
        }

        /// <summary>
        /// 연결부1 좌측 포즈를 보간 이동하면서, 연결부2 우측은 수학적 거울 대칭으로 완벽 연동
        /// </summary>
        private IEnumerator AnimateCouplerBetweenPoses(TransformPose leftFrom, TransformPose leftTo, float duration)
        {
            float elapsed = 0f;

            Quaternion leftRotFrom = Quaternion.Euler(leftFrom.rotationEuler);
            Quaternion leftRotTo = Quaternion.Euler(leftTo.rotationEuler);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveT = motionCurve != null ? motionCurve.Evaluate(t) : t;

                // 1. 좌측 연결부1 보간
                if (couplerLeft != null)
                {
                    couplerLeft.localPosition = Vector3.Lerp(leftFrom.position, leftTo.position, curveT);
                    couplerLeft.localRotation = Quaternion.Slerp(leftRotFrom, leftRotTo, curveT);
                }

                // 2. 우측 연결부2 실시간 거울 대칭
                if (couplerRight != null && mirrorRightCoupler)
                {
                    ApplyMirrorTransformToRightCoupler();
                }

                yield return null;
            }

            if (couplerLeft != null)
            {
                couplerLeft.localPosition = leftTo.position;
                couplerLeft.localRotation = leftRotTo;
            }

            if (couplerRight != null && mirrorRightCoupler)
            {
                ApplyMirrorTransformToRightCoupler();
            }
        }

        /// <summary>
        /// 좌측 연결부1의 상대 변화량을 기반으로 우측 연결부2에 완벽한 거울 대칭 적용
        /// </summary>
        private void ApplyMirrorTransformToRightCoupler()
        {
            if (couplerLeft == null || couplerRight == null) return;

            // 1. 위치 미러링: X축 이동량만 반대 부호, Y/Z는 동일
            Vector3 deltaPosLeft = couplerLeft.localPosition - initialCouplerLeftPos;
            Vector3 deltaPosRight = new Vector3(-deltaPosLeft.x, deltaPosLeft.y, deltaPosLeft.z);
            couplerRight.localPosition = initialCouplerRightPos + deltaPosRight;

            // 2. 회전 미러링: 시작 각도에서 벗어난 회전량(Delta)을 거울 반사
            Quaternion deltaRotLeft = couplerLeft.localRotation * Quaternion.Inverse(initialCouplerLeftRot);
            deltaRotLeft.ToAngleAxis(out float angle, out Vector3 axis);

            if (angle > 180f) angle -= 360f;

            Vector3 mirrorAxis = new Vector3(-axis.x, axis.y, axis.z);
            float mirrorAngle = -angle;

            Quaternion deltaRotRight = Quaternion.AngleAxis(mirrorAngle, mirrorAxis);
            couplerRight.localRotation = deltaRotRight * initialCouplerRightRot;
        }

        /// <summary>
        /// 3단계: 기계식 상부 아머드 카울 상승 / 하강 (메인 상판 덮개도 카울 위에 얹혀져 있으므로 함께 상승/하강)
        /// </summary>
        [ContextMenu("3번 아머드 카울 상승 / 하강 토글")]
        public void ToggleCowlLift()
        {
            if (cowlCoroutine != null)
            {
                StopCoroutine(cowlCoroutine);
            }

            isCowlLifted = !isCowlLifted;

            // 카울이 상승을 시작할 때, 현재 접혀있는 연결부 및 상판 덮개 안의 보조 벤트 기준점 스냅샷
            if (isCowlLifted)
            {
                if (couplerLeft != null)
                {
                    couplerLeftBeforeCowlPos = couplerLeft.localPosition;
                    couplerLeftBeforeCowlRot = couplerLeft.localRotation;
                }
                if (ventLeft != null)
                {
                    ventLeftBeforeCoverPos = ventLeft.localPosition;
                    ventLeftBeforeCoverRot = ventLeft.localRotation;
                }
                if (ventRight != null)
                {
                    ventRightBeforeCoverPos = ventRight.localPosition;
                    ventRightBeforeCoverRot = ventRight.localRotation;
                }
            }

            cowlCoroutine = StartCoroutine(AnimateCowlWithCoverSequence(isCowlLifted));
        }

        /// <summary>
        /// 카울 상승 시 메인 상판 덮개(1->2)도 카울 위에 얹힌 채 함께 동반 상승/하강
        /// </summary>
        private IEnumerator AnimateCowlWithCoverSequence(bool lift)
        {
            float totalCowlDuration = stepDuration * 2f + (stepPause > 0 ? stepPause : 0f);

            Coroutine cowlCor = StartCoroutine(AnimateCowlSequence(lift));
            Coroutine coverCor = null;

            if (lift)
            {
                // 상판 덮개가 아직 차체에 닫혀 있다면 카울과 완벽히 동기화되어 1 -> 2로 함께 상승
                coverCor = StartCoroutine(AnimateCoverBetweenPoses(coverStep1, coverStep2, totalCowlDuration));
            }
            else
            {
                // 상판 덮개가 2단계(카울 상단)에 있다면 카울과 함께 2 -> 1로 원래 위치 하강
                coverCor = StartCoroutine(AnimateCoverBetweenPoses(coverStep2, coverStep1, totalCowlDuration));
            }

            yield return cowlCor;
            if (coverCor != null) yield return coverCor;
            cowlCoroutine = null;
        }

        /// <summary>
        /// 아머드 카울 3단계 상승 시퀀스 코루틴
        /// </summary>
        private IEnumerator AnimateCowlSequence(bool lift)
        {
            if (lift)
            {
                // 1 -> 2단계: 수직 대폭 상승
                yield return AnimateCowlBetweenPoses(cowlStep1, cowlStep2, stepDuration);
                if (stepPause > 0) yield return new WaitForSeconds(stepPause);

                // 2 -> 3단계: 전방 틸트 및 완전 상승
                yield return AnimateCowlBetweenPoses(cowlStep2, cowlStep3, stepDuration);
            }
            else
            {
                // 역순 하강 (3 -> 2 -> 1)
                yield return AnimateCowlBetweenPoses(cowlStep3, cowlStep2, stepDuration);
                if (stepPause > 0) yield return new WaitForSeconds(stepPause);

                yield return AnimateCowlBetweenPoses(cowlStep2, cowlStep1, stepDuration);
            }

            cowlCoroutine = null;
        }

        /// <summary>
        /// 카울을 보간 이동/회전시키면서, 연결부 2개도 카울의 움직임만큼 함께 동반 이동
        /// </summary>
        private IEnumerator AnimateCowlBetweenPoses(TransformPose from, TransformPose to, float duration)
        {
            if (armoredCowl == null) yield break;

            float elapsed = 0f;
            Quaternion rotFrom = Quaternion.Euler(from.rotationEuler);
            Quaternion rotTo = Quaternion.Euler(to.rotationEuler);

            Quaternion initialCowlRotInv = Quaternion.Inverse(Quaternion.Euler(cowlStep1.rotationEuler));

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveT = motionCurve != null ? motionCurve.Evaluate(t) : t;

                // 1. 카울 본체 보간
                armoredCowl.localPosition = Vector3.Lerp(from.position, to.position, curveT);
                armoredCowl.localRotation = Quaternion.Slerp(rotFrom, rotTo, curveT);

                // 2. 카울의 시작점 대비 이동량 & 회전량 계산
                Vector3 deltaPosCowl = armoredCowl.localPosition - cowlStep1.position;
                Quaternion deltaRotCowl = armoredCowl.localRotation * initialCowlRotInv;

                // 3. 접힌 연결부 1 (좌측)을 카울을 따라 동반 이동/회전
                if (couplerLeft != null)
                {
                    couplerLeft.localPosition = couplerLeftBeforeCowlPos + deltaPosCowl;
                    couplerLeft.localRotation = deltaRotCowl * couplerLeftBeforeCowlRot;
                }

                // 4. 우측 연결부 2는 좌측 연결부의 움직임을 실시간 거울 대칭
                if (couplerRight != null && mirrorRightCoupler)
                {
                    ApplyMirrorTransformToRightCoupler();
                }

                yield return null;
            }

            // 최종 포즈 고정
            armoredCowl.localPosition = to.position;
            armoredCowl.localRotation = rotTo;

            Vector3 finalDeltaPosCowl = armoredCowl.localPosition - cowlStep1.position;
            Quaternion finalDeltaRotCowl = armoredCowl.localRotation * initialCowlRotInv;

            if (couplerLeft != null)
            {
                couplerLeft.localPosition = couplerLeftBeforeCowlPos + finalDeltaPosCowl;
                couplerLeft.localRotation = finalDeltaRotCowl * couplerLeftBeforeCowlRot;
            }

            if (couplerRight != null && mirrorRightCoupler)
            {
                ApplyMirrorTransformToRightCoupler();
            }
        }

        /// <summary>
        /// 4단계: 대형 단발 제트 부스터 노즐 돌출 / 수납 토글
        /// </summary>
        [ContextMenu("4번 제트 부스터 노즐 돌출 / 수납 토글")]
        public void ToggleBoosterDeploy()
        {
            if (boosterCoroutine != null)
            {
                StopCoroutine(boosterCoroutine);
            }

            isBoosterDeployed = !isBoosterDeployed;
            boosterCoroutine = StartCoroutine(AnimateBoosterSequence(isBoosterDeployed));
        }

        /// <summary>
        /// 부스터 노즐 돌출 시퀀스 코루틴 (후방 돌출 - 네온은 모든 기믹 완료 후 순서대로 진행 예정)
        /// </summary>
        private IEnumerator AnimateBoosterSequence(bool deploy)
        {
            if (deploy)
            {
                // 1 -> 2단계: 후방으로 노즐 슬라이드 돌출
                yield return AnimateBoosterBetweenPoses(boosterStep1, boosterStep2, stepDuration);
                // 네온 연출은 모든 변형 완료 후 순서에 맞춰 점등할 예정 (사용자 요청으로 보류)
            }
            else
            {
                // 2 -> 1단계: 원래 대기 위치로 수납
                yield return AnimateBoosterBetweenPoses(boosterStep2, boosterStep1, stepDuration);
            }

            boosterCoroutine = null;
        }

        /// <summary>
        /// 부스터 노즐의 두 포즈 사이 부드러운 보간
        /// </summary>
        private IEnumerator AnimateBoosterBetweenPoses(TransformPose from, TransformPose to, float duration)
        {
            if (boosterNozzle == null) yield break;

            float elapsed = 0f;
            Quaternion rotFrom = Quaternion.Euler(from.rotationEuler);
            Quaternion rotTo = Quaternion.Euler(to.rotationEuler);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveT = motionCurve != null ? motionCurve.Evaluate(t) : t;

                boosterNozzle.localPosition = Vector3.Lerp(from.position, to.position, curveT);
                boosterNozzle.localRotation = Quaternion.Slerp(rotFrom, rotTo, curveT);

                yield return null;
            }

            boosterNozzle.localPosition = to.position;
            boosterNozzle.localRotation = rotTo;
        }

        /// <summary>
        /// 5단계: 부품 메인상판덮개 개폐 토글
        /// </summary>
        [ContextMenu("5번 메인상판덮개 개폐 토글")]
        public void ToggleMainCover()
        {
            if (coverCoroutine != null)
            {
                StopCoroutine(coverCoroutine);
            }

            isCoverOpened = !isCoverOpened;

            // 덮개가 열리기 시작할 때, 현재 수납된 보조 벤트의 위치와 회전을 기준점으로 기억
            if (isCoverOpened)
            {
                if (ventLeft != null)
                {
                    ventLeftBeforeCoverPos = ventLeft.localPosition;
                    ventLeftBeforeCoverRot = ventLeft.localRotation;
                }
                if (ventRight != null)
                {
                    ventRightBeforeCoverPos = ventRight.localPosition;
                    ventRightBeforeCoverRot = ventRight.localRotation;
                }
            }

            coverCoroutine = StartCoroutine(AnimateMainCoverSequence(isCoverOpened));
        }

        [ContextMenu("5번 사진 위치/각도 적용 (대기: -142.1도, Y:0.442, Z:-1.131)")]
        public void OptimizeCoverAngles()
        {
            coverStep2 = new TransformPose(new Vector3(0.003f, 0.442f, -1.131f), new Vector3(-142.1f, 180f, 0f));
            coverStep3 = new TransformPose(new Vector3(0.003f, -0.098f, -0.565f), new Vector3(-142.1f, 180f, 0f));
            Debug.Log("[VehicleTransformationController] 5번 상판 덮개 2단계 대기 위치(Y: 0.442, Z: -1.131, Rot: -142.1) 및 3단계 수납 설정을 완벽히 적용했습니다.");
        }

        /// <summary>
        /// 메인상판덮개 개폐 시퀀스 코루틴 (1단계 닫힘 <-> 2단계 틸트 오픈 <-> 3단계 차체 내부 수납)
        /// </summary>
        private IEnumerator AnimateMainCoverSequence(bool open)
        {
            if (open)
            {
                // 만약 카울이 아직 안 올라와서 덮개가 1단계(닫힘)에 있다면 1->2 틸트 오픈 먼저 진행
                if (!isCowlLifted)
                {
                    yield return AnimateCoverBetweenPoses(coverStep1, coverStep2, stepDuration);
                    if (stepPause > 0) yield return new WaitForSeconds(stepPause);
                }

                // 2 -> 3단계: 기울어진 각도를 유지하며 차체 내부로 슬라이드 수납 (조절된 부드러운 속도)
                yield return AnimateCoverBetweenPoses(coverStep2, coverStep3, coverRetractDuration);
            }
            else
            {
                // 3 -> 2단계: 차체 내부에서 위로 슬라이드 상승 (조절된 부드러운 속도)
                yield return AnimateCoverBetweenPoses(coverStep3, coverStep2, coverRetractDuration);

                // 카울이 내려가 있는 상태였다면 2 -> 1로 닫힘 복구
                if (!isCowlLifted)
                {
                    if (stepPause > 0) yield return new WaitForSeconds(stepPause);
                    yield return AnimateCoverBetweenPoses(coverStep2, coverStep1, stepDuration);
                }
            }

            coverCoroutine = null;
        }

        /// <summary>
        /// 메인상판덮개의 두 포즈 사이 부드러운 보간 이동 및 회전 (수납된 보조 벤트 2개 동반 이동)
        /// </summary>
        private IEnumerator AnimateCoverBetweenPoses(TransformPose from, TransformPose to, float duration)
        {
            if (mainCover == null) yield break;

            float elapsed = 0f;
            Quaternion rotFrom = Quaternion.Euler(from.rotationEuler);
            Quaternion rotTo = Quaternion.Euler(to.rotationEuler);

            Quaternion initialCoverRotInv = Quaternion.Inverse(Quaternion.Euler(coverStep1.rotationEuler));
            Vector3 coverPivot = coverStep1.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveT = motionCurve != null ? motionCurve.Evaluate(t) : t;

                // 1. 메인상판덮개 보간
                mainCover.localPosition = Vector3.Lerp(from.position, to.position, curveT);
                mainCover.localRotation = Quaternion.Slerp(rotFrom, rotTo, curveT);

                // 2. 덮개의 회전량 계산 (coverStep1 기준)
                Quaternion deltaRotCover = mainCover.localRotation * initialCoverRotInv;

                // 3. 수납된 보조 벤트판 1 (좌측) 동반 이동 및 회전
                if (ventLeft != null)
                {
                    ventLeft.localPosition = mainCover.localPosition + deltaRotCover * (ventLeftBeforeCoverPos - coverPivot);
                    ventLeft.localRotation = deltaRotCover * ventLeftBeforeCoverRot;
                }

                // 4. 수납된 보조 벤트판 2 (우측) 동반 이동 및 회전
                if (ventRight != null)
                {
                    ventRight.localPosition = mainCover.localPosition + deltaRotCover * (ventRightBeforeCoverPos - coverPivot);
                    ventRight.localRotation = deltaRotCover * ventRightBeforeCoverRot;
                }

                yield return null;
            }

            // 최종 포즈 고정
            mainCover.localPosition = to.position;
            mainCover.localRotation = rotTo;

            Quaternion finalDeltaRotCover = mainCover.localRotation * initialCoverRotInv;

            if (ventLeft != null)
            {
                ventLeft.localPosition = mainCover.localPosition + finalDeltaRotCover * (ventLeftBeforeCoverPos - coverPivot);
                ventLeft.localRotation = finalDeltaRotCover * ventLeftBeforeCoverRot;
            }

            if (ventRight != null)
            {
                ventRight.localPosition = mainCover.localPosition + finalDeltaRotCover * (ventRightBeforeCoverPos - coverPivot);
                ventRight.localRotation = finalDeltaRotCover * ventRightBeforeCoverRot;
            }
        }

        /// <summary>
        /// 1, 2, 3, 4, 5단계 풀 변형 시퀀스 토글 (전체 순차 전개 / 전체 복구)
        /// 1, 2, 3, 4번이 차례대로 착착착 진행되고, 5번은 4번 시작 후 한 박자 늦게 시작됩니다.
        /// </summary>
        [ContextMenu("🔥 전체 풀 변형 시퀀스 실행 / 토글")]
        public void ToggleFullTransformation()
        {
            if (isFullSequenceRunning) return;

            isFullyTransformed = !isFullyTransformed;
            fullSequenceCoroutine = StartCoroutine(AnimateFullSequence(isFullyTransformed));
        }

        private IEnumerator AnimateFullSequence(bool deploy)
        {
            isFullSequenceRunning = true;

            if (deploy)
            {
                // [1단계] 보조 벤트 수납
                if (!isVentHidden)
                {
                    isVentHidden = true;
                    yield return AnimateVentSequence(true);
                    if (stepPause > 0) yield return new WaitForSeconds(stepPause);
                }

                // [2단계] 3D 스캐너 전개 + 카울 연결부 2개 접기 동시 진행
                if (!isScannerDeployed || !isCouplerFolded)
                {
                    isScannerDeployed = true;
                    isCouplerFolded = true;
                    Coroutine sCor = StartCoroutine(AnimateScannerSequence(true));
                    Coroutine cCor = StartCoroutine(AnimateCouplerSequence(true));
                    yield return sCor;
                    yield return cCor;
                    if (stepPause > 0) yield return new WaitForSeconds(stepPause);
                }

                // [3단계] 기계식 상부 아머드 카울 + 접힌 연결부 + 메인 상판 덮개(1->2) 동반 상승!
                if (!isCowlLifted)
                {
                    isCowlLifted = true;
                    if (couplerLeft != null)
                    {
                        couplerLeftBeforeCowlPos = couplerLeft.localPosition;
                        couplerLeftBeforeCowlRot = couplerLeft.localRotation;
                    }
                    if (ventLeft != null)
                    {
                        ventLeftBeforeCoverPos = ventLeft.localPosition;
                        ventLeftBeforeCoverRot = ventLeft.localRotation;
                    }
                    if (ventRight != null)
                    {
                        ventRightBeforeCoverPos = ventRight.localPosition;
                        ventRightBeforeCoverRot = ventRight.localRotation;
                    }

                    yield return AnimateCowlWithCoverSequence(true);
                    if (stepPause > 0) yield return new WaitForSeconds(stepPause);
                }

                // [4단계 & 5단계] 4번 부스터 노즐 돌출 시작 + 한 박자 늦게 5번 메인상판덮개 수납 (2->3) 시작!
                if (!isBoosterDeployed || !isCoverOpened)
                {
                    isBoosterDeployed = true;
                    isCoverOpened = true;

                    // 4번 부스터 노즐 먼저 출발
                    Coroutine boosterCor = StartCoroutine(AnimateBoosterSequence(true));

                    // 한 박자 늦게 대기 (기본 약 0.08초)
                    if (stage5DelayAfterStage4 > 0) yield return new WaitForSeconds(stage5DelayAfterStage4);

                    // 5번 메인 상판 덮개: 이미 카울과 함께 2단계로 올라와 있으므로, 차체 내부 수납(2->3)만 부드러운 속도로 실행!
                    Coroutine coverCor = StartCoroutine(AnimateCoverBetweenPoses(coverStep2, coverStep3, coverRetractDuration));

                    yield return boosterCor;
                    yield return coverCor;
                }
            }
            else
            {
                // [역순 원복] 5 + 4 -> 3 -> 2 -> 1
                if (isBoosterDeployed || isCoverOpened)
                {
                    isBoosterDeployed = false;
                    isCoverOpened = false;

                    // 상판 덮개 차체 내부에서 2단계로 솟아나옴 + 부스터 수납
                    Coroutine coverCor = StartCoroutine(AnimateCoverBetweenPoses(coverStep3, coverStep2, coverRetractDuration));
                    Coroutine boosterCor = StartCoroutine(AnimateBoosterSequence(false));

                    yield return coverCor;
                    yield return boosterCor;
                    if (stepPause > 0) yield return new WaitForSeconds(stepPause);
                }

                // 3번 원복: 카울 하강과 함께 상판 덮개도 2->1로 원래 자리 하강
                if (isCowlLifted)
                {
                    isCowlLifted = false;
                    yield return AnimateCowlWithCoverSequence(false);
                    if (stepPause > 0) yield return new WaitForSeconds(stepPause);
                }

                if (isScannerDeployed || isCouplerFolded)
                {
                    isScannerDeployed = false;
                    isCouplerFolded = false;
                    Coroutine sCor = StartCoroutine(AnimateScannerSequence(false));
                    Coroutine cCor = StartCoroutine(AnimateCouplerSequence(false));
                    yield return sCor;
                    yield return cCor;
                    if (stepPause > 0) yield return new WaitForSeconds(stepPause);
                }

                if (isVentHidden)
                {
                    isVentHidden = false;
                    yield return AnimateVentSequence(false);
                }
            }

            isFullSequenceRunning = false;
            fullSequenceCoroutine = null;
        }
    }
}
