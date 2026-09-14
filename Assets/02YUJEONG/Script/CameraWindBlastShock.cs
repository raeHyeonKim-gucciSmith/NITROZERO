using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

namespace YUJEONG
{
    /// <summary>
    /// 시네마틱 카메라 풍압 전복 및 지면 진동 연출 컨트롤러 (Fly-by Wind Blast Shock Cam)
    /// 
    /// [정밀 연출 타임라인]
    /// 1. 조용함 (0초 ~ 1.0초): 카메라는 평온한 상태 유지 (뒤에 대기 중인 두 차량)
    /// 2. 진동 고조 (1.0초 ~ 3.0초, 약 2초간): 엔진 기동 및 지면 진동이 점점 거세짐
    /// 3. 스플라인 발진 (3.0초 시점): 두 차량이 스플라인을 타고 굉음과 함께 카메라를 추월
    /// 4. 위태로운 추월 샷 (약 0.4초간): 카메라 앞을 쏜살같이 스쳐 지나가는 자동차들의 뒷모습을 격렬한 진동과 함께 포착
    /// 5. 풍압 전복 (0.4초 직후): 엄청난 후폭풍에 카메라가 뒤로 덜컹! 벌러덩 넘어가 밤하늘을 바라봄
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(200)]
    public class CameraWindBlastShock : MonoBehaviour
    {
        public enum CinematicPhase
        {
            [InspectorName("0. 정적 (Quiet)")]
            Quiet,
            [InspectorName("1. 진동 고조 (Rumble Building)")]
            RumbleBuilding,
            [InspectorName("2. 차량 스쳐지나감 (Pass-by Rush)")]
            PassByRush,
            [InspectorName("3. 풍압 전복 중 (Knocking Down)")]
            KnockedDown,
            [InspectorName("4. 하늘 바라보기 (Sky Gazing, 3초)")]
            SkyGazing,
            [InspectorName("5. 시퀀스 완료 (Completed)")]
            Completed
        }

        [Header("[ 🚗 스플라인 및 차량 제어 ]")]
        [Tooltip("스플라인 주행 컴포넌트 (미지정 시 '중심점' 오브젝트에서 자동 탐색)")]
        public SplineAnimate splineAnimate;

        [Tooltip("추적 기준 오브젝트 (중심점 또는 차량 루트)")]
        public Transform targetVehicle;

        [Tooltip("게임 실행 시 시네마틱 연출 시퀀스 자동 시작")]
        public bool playOnStart = true;

        [Tooltip("시퀀스 수동 재시작 키 (기본: Space)")]
        public KeyCode restartKey = KeyCode.Space;

        [Header("[ 🚀 차량 부스터 변형 연출 설정 ]")]
        [Tooltip("카메라 뒤에서 대기할 때부터 두 차의 변형과 모든 부스터 이펙트를 100% 켠 상태로 시작")]
        public bool startCarsWithFullBoosters = true;

        [Tooltip("스플라인 출발 시 차량 부스터 변형도 자동 동시 트리거 (풀 부스터 대기가 아닐 때 사용)")]
        public bool triggerBoosterOnLaunch = false;

        [Tooltip("스플라인 주행 시작 후 부스터 변형이 시작되기까지 대기할 시간 (초 단위, 요청: 1초 정도 대기 후 변형)")]
        [Range(0f, 10.0f)]
        public float boosterTransformDelay = 1.0f;

        [Header("[ 💥 부스터 점화 & 카메라 분리 발진 연출 (부와앙-!) ]")]
        [Tooltip("두 차량의 부스터 변형이 끝난 직후, 카메라를 노면에 남겨두고 두 차가 폭발적인 부스터 가속으로 질주")]
        public bool leaveCameraBehindOnBoost = true;

        [Tooltip("변형 시작 후 최종 부스터가 켜지기까지 걸리는 변형 진행 시간 (초 단위, 기본 3.2초)")]
        [Range(1.0f, 8.0f)]
        public float transformationDuration = 3.2f;

        [Tooltip("부스터 점화 전 일반 주행 순항 속도 (SplineAnimate Speed)")]
        [Range(20f, 400f)]
        public float cruiseSpeed = 100f;

        [Tooltip("부스터 점화 후 폭발적인 최종 로켓 질주 속도 (너무 빠르지 않은 적정 속도, 기본 200 권장)")]
        [Range(50f, 600f)]
        public float boostRocketSpeed = 200f;

        [Tooltip("부스터 점화 시 최고 속도까지 치고 나가는 급가속 시간 (초 단위, 부드럽고 묵직한 가속을 위해 0.8초 권장)")]
        [Range(0.1f, 3.0f)]
        public float boostAccelerationDuration = 0.8f;

        [Header("[ ⏱️ 타임라인 시간 설정 ]")]
        [Tooltip("1단계: 카메라가 정면을 응시하는 초기 대기 시간 (초, 권장 0.5초)")]
        [Range(0f, 3.0f)]
        public float quietDuration = 0.5f;

        [Tooltip("2단계: 뒤에서 엔진 굉음 및 지면 진동 고조 시간 (초, 권장 0.5초)")]
        [Range(0f, 3.0f)]
        public float rumbleBuildUpDuration = 0.5f;

        [Tooltip("3단계: 스플라인 출발 후 차량이 카메라 옆을 스쳐 지나갈 때까지의 시간 (위치 자동 감지 미사용 시 사용, 기본 0.4초)")]
        [Range(0.1f, 15.0f)]
        public float passByViewDuration = 0.4f;

        [Tooltip("차량이 카메라 옆을 스쳐 지나간 직후 진동이 급격하게 가라앉는 시간 (초, 요청: 1.0초 내외로 급격히 감쇠)")]
        [Range(0.2f, 5.0f)]
        public float passByCalmDownDuration = 1.0f;

        [Tooltip("차량이 카메라 평면을 통과하는 순간을 실시간 좌표로 감지하여 정확히 스쳐지나가는 찰나에 전복 트리거")]
        public bool autoDetectPassByPosition = true;

        [Tooltip("스플라인 주행 완주 시간 (초, 8초 권장)")]
        public float splineDriveDuration = 8.0f;

        [Header("[ 🌌 하늘 바라보기 지속 시간 (여운 연출) ]")]
        [Tooltip("카메라가 뒤로 넘어진 후 밤하늘을 가만히 응시하며 머무는 시간 (초, 요청: 3.0초)")]
        [Range(0.5f, 10.0f)]
        public float skyHoldDuration = 3.0f;

        [Header("[ 🌪️ 진동 및 충격 세기 설정 (0으로 설정 시 완전 무진동) ]")]
        [Tooltip("지면/차체 진동 최대 위치 흔들림 세기 (미터 단위, 0 설정 시 완전 고정)")]
        [Range(0f, 0.5f)]
        public float maxRumbleShake = 0f;

        [Tooltip("지면/차체 진동 최대 각도 흔들림 세기 (도 단위, 0 설정 시 완전 고정)")]
        [Range(0f, 20.0f)]
        public float maxRumbleAngleShake = 0f;

        [Tooltip("차량이 고속 주행 시 발생하는 난기류/풍압 배율 (0 설정 시 완전 고정)")]
        [Range(0f, 10.0f)]
        public float passByTurbulenceMultiplier = 0f;

        [Tooltip("진동 주파수 (Hz, 높을수록 빠르고 앙칼진 고속 엔진 진동, 25~45Hz 권장)")]
        [Range(0f, 120f)]
        public float shakeFrequency = 32f;

        [Tooltip("체크 시 주행 내내 진동이 줄어들지 않고 강렬하게 100% 유지 (차량 탑승 온보드 액션캠 필수)")]
        public bool continuousOnboardShake = true;

        [Header("[ 💥 풍압 전복 옵션 (카메라 넘어짐 제어) ]")]
        [Tooltip("체크 시 차가 지나갈 때 카메라가 뒤로 벌러덩 넘어가 하늘을 응시함 (창문 탑승 캠 사용 시 반드시 체크 해제)")]
        public bool enableKnockdown = false;

        [Tooltip("뒤로 넘어질 때 렌즈가 하늘을 향하는 X축 회전 각도 (음수 각도가 하늘 방향, 기본 약 -82도)")]
        [Range(-95f, -60f)]
        public float knockdownPitch = -82f;

        [Tooltip("넘어질 때 옆으로 기우는 롤(Roll) 각도 (Z축)")]
        [Range(-30f, 30f)]
        public float knockdownRoll = 14f;

        [Tooltip("풍압에 뒤로 벌러덩 넘어가는 데 걸리는 시간 (초, 작을수록 순간적인 충격감)")]
        [Range(0.15f, 0.6f)]
        public float knockdownDuration = 0.30f;

        [Tooltip("바닥에 부딪힐 때 덜컹거리는 충격 반동 (Bounce) 세기")]
        [Range(0f, 0.5f)]
        public float groundImpactBounce = 0.22f;

        [Tooltip("카메라 지지대가 꺾이며 지면으로 주저앉는 높이 하강 오프셋 (미터)")]
        [Range(-1.5f, 0f)]
        public float groundDropHeight = -0.35f;

        [Header("[ 📊 현재 진행 상태 모니터링 ]")]
        [SerializeField] private CinematicPhase currentPhase = CinematicPhase.Quiet;
        [SerializeField] private float sequenceTimer = 0f;
        [SerializeField] private float skyGazeTimer = 0f;
        [SerializeField] private bool isSequenceRunning = false;

        // 원본 트랜스폼 보존
        private Vector3 initialLocalPos;
        private Quaternion initialLocalRot;
        private bool isInitialized = false;

        // 카메라 분리 및 부스터 발진 상태
        private bool isCameraDetached = false;
        private Vector3 detachedWorldPos;
        private Quaternion detachedWorldRot;
        private Coroutine boostAccelerationRoutine;
        private bool isBoostBlastActive = false;
        private float boostBlastTimer = 0f;

        // 전복 애니메이션 변수
        private bool isKnockedDown = false;
        private float knockdownProgress = 0f;
        private float bounceDecay = 0f;
        private bool splineLaunched = false;
        private float initialDotProduct = -1f;

        private void Awake()
        {
            // 사용자가 인스펙터에서 조절한 수치를 절대 덮어쓰지 않고 100% 존중하여 유지
            StoreInitialPose();
            AutoFindComponents();
        }

        private void OnEnable()
        {
            StoreInitialPose();
            AutoFindComponents();
        }

        private void OnDisable()
        {
            RestoreInitialPose();
        }

        private IEnumerator Start()
        {
            if (Application.isPlaying)
            {
                AutoFindComponents();

                // 스플라인이 시작 시 자동으로 달리지 않도록 일시 대기
                if (splineAnimate != null)
                {
                    splineAnimate.NormalizedTime = 0f;
                    splineAnimate.Pause();
                }

                if (startCarsWithFullBoosters)
                {
                    ApplyVehiclesFullBoosted();
                }

                // 1프레임 대기 (타 스크립트 Start() 이후 확실하게 적용)
                yield return null;

                if (startCarsWithFullBoosters)
                {
                    ApplyVehiclesFullBoosted();
                }

                if (playOnStart)
                {
                    StartCinematicSequence();
                }
            }
        }

        public void StoreInitialPose()
        {
            if (isKnockedDown) return;
            if (!isCameraDetached)
            {
                initialLocalPos = transform.localPosition;
                initialLocalRot = transform.localRotation;
            }
            isInitialized = true;
        }

        public void RestoreInitialPose()
        {
            if (!isInitialized) return;
            ResetBoosterLaunchState();
            transform.localPosition = initialLocalPos;
            transform.localRotation = initialLocalRot;
            isKnockedDown = false;
            knockdownProgress = 0f;
            currentPhase = CinematicPhase.Quiet;
            sequenceTimer = 0f;
            skyGazeTimer = 0f;
            isSequenceRunning = false;
            splineLaunched = false;
            initialDotProduct = -1f;

            if (splineAnimate != null && Application.isPlaying)
            {
                splineAnimate.NormalizedTime = 0f;
                splineAnimate.Pause();
            }
        }

        public void AutoFindComponents()
        {
            if (targetVehicle == null)
            {
                GameObject centerObj = GameObject.Find("중심점");
                if (centerObj != null) targetVehicle = centerObj.transform;
                else
                {
                    GameObject blueCar = GameObject.Find("Blue_Car_Final");
                    if (blueCar != null)
                    {
                        targetVehicle = (blueCar.transform.parent != null && blueCar.transform.parent != blueCar.transform.root)
                            ? blueCar.transform.parent
                            : blueCar.transform;
                    }
                }
            }

            if (splineAnimate == null && targetVehicle != null)
            {
                splineAnimate = targetVehicle.GetComponent<SplineAnimate>();
                if (splineAnimate == null)
                    splineAnimate = targetVehicle.GetComponentInChildren<SplineAnimate>();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            // 재시작 키 체크 (New Input System 및 Legacy Input 호환)
            if (IsRestartKeyPressed())
            {
                StartCinematicSequence();
            }

            if (!isSequenceRunning) return;

            sequenceTimer += Time.deltaTime;
            float time = Time.time;

            float launchTime = quietDuration + rumbleBuildUpDuration;
            float knockdownTime = launchTime + passByViewDuration;

            // 1단계: 조용함 (카메라 정면 응시)
            if (sequenceTimer < quietDuration)
            {
                currentPhase = CinematicPhase.Quiet;
            }
            // 2단계: 진동 고조 (엔진 굉음 빌드업)
            else if (sequenceTimer < launchTime)
            {
                currentPhase = CinematicPhase.RumbleBuilding;
            }
            // 3단계: 스플라인 작동 & 차량 질주 / 풍압 전복
            else
            {
                if (!splineLaunched)
                {
                    LaunchCarsOnSpline();
                    if (targetVehicle != null)
                    {
                        Vector3 rel = targetVehicle.position - transform.position;
                        initialDotProduct = Vector3.Dot(rel, transform.forward);
                    }
                }

                if (enableKnockdown)
                {
                    if (!isKnockedDown)
                    {
                        currentPhase = CinematicPhase.PassByRush;

                        bool shouldKnockdown = false;

                        // 차량 위치 기반 자동 감지 (카메라 옆을 지나치는 찰나 감지)
                        if (autoDetectPassByPosition && targetVehicle != null)
                        {
                            Vector3 rel = targetVehicle.position - transform.position;
                            float currentDot = Vector3.Dot(rel, transform.forward);
                            float dist = rel.magnitude;

                            // 뒤에서 출발하여 카메라 앞을 지나칠 때 (Dot: 음수 -> 양수 전환)
                            if (initialDotProduct < 0f && currentDot >= 0f)
                            {
                                shouldKnockdown = true;
                            }
                            // 앞에서 다가와 카메라 뒤로 지나칠 때 (Dot: 양수 -> 음수 전환)
                            else if (initialDotProduct > 0f && currentDot <= 0f)
                            {
                                shouldKnockdown = true;
                            }
                            // 또는 카메라와 최근접(5m 이내) 통과 시
                            else if (dist < 5.0f)
                            {
                                shouldKnockdown = true;
                            }
                        }

                        // 타이머 기반 감지 (fallback)
                        if (sequenceTimer >= knockdownTime)
                        {
                            shouldKnockdown = true;
                        }

                        if (shouldKnockdown)
                        {
                            TriggerKnockdownInternal();
                        }
                    }
                    else
                    {
                        // 4단계: 풍압 전복 진행 중 -> 5단계: 하늘 바라보기 (skyHoldDuration) -> 완료
                        if (knockdownProgress < 1f)
                        {
                            currentPhase = CinematicPhase.KnockedDown;
                        }
                        else
                        {
                            skyGazeTimer += Time.deltaTime;
                            if (skyGazeTimer < skyHoldDuration)
                            {
                                currentPhase = CinematicPhase.SkyGazing;
                            }
                            else
                            {
                                currentPhase = CinematicPhase.Completed;
                            }
                        }
                    }
                }
                else
                {
                    currentPhase = CinematicPhase.PassByRush;
                }
            }
        }

        private void TriggerKnockdownInternal()
        {
            if (isKnockedDown) return;
            isKnockedDown = true;
            knockdownProgress = 0f;
            bounceDecay = 1f;
            skyGazeTimer = 0f;
            currentPhase = CinematicPhase.KnockedDown;
        }

        private void LaunchCarsOnSpline()
        {
            splineLaunched = true;

            if (splineAnimate != null)
            {
                // Speed 모드인 경우 사용자가 지정한 크루즈 스피드 적용
                if (splineAnimate.AnimationMethod == SplineAnimate.Method.Speed)
                {
                    splineAnimate.MaxSpeed = cruiseSpeed;
                }
                else if (splineDriveDuration > 0f)
                {
                    splineAnimate.Duration = splineDriveDuration;
                }
                splineAnimate.Restart(true);
            }

            if (startCarsWithFullBoosters)
            {
                ApplyVehiclesFullBoosted();
            }
            else if (triggerBoosterOnLaunch)
            {
                ApplyVehiclesNormalState();

                if (boosterTransformDelay <= 0f)
                {
                    TriggerAllVehiclesBoosterTransformation();
                    if (leaveCameraBehindOnBoost)
                    {
                        StartCoroutine(DelayedCameraDetachRoutine(transformationDuration));
                    }
                }
                else
                {
                    StartCoroutine(DelayedBoosterTriggerRoutine(boosterTransformDelay));
                }
            }
        }

        private IEnumerator DelayedBoosterTriggerRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            TriggerAllVehiclesBoosterTransformation();

            if (leaveCameraBehindOnBoost)
            {
                yield return new WaitForSeconds(Mathf.Max(0.5f, transformationDuration));
                DetachCameraAndRocketLaunch();
            }
        }

        private IEnumerator DelayedCameraDetachRoutine(float duration)
        {
            yield return new WaitForSeconds(Mathf.Max(0.5f, duration));
            DetachCameraAndRocketLaunch();
        }

        /// <summary>
        /// 부스터 변형 완료 직후: 카메라를 노면에 남겨두고 두 차량이 폭발적인 부스터 가속(부와앙-)으로 질주하는 연출
        /// </summary>
        [ContextMenu("💥 카메라 두고 부스터 폭발 발진 (부와앙-!)")]
        public void DetachCameraAndRocketLaunch()
        {
            if (isCameraDetached) return;

            // 1. 카메라 월드 좌표 스냅샷 기록 & 분리 플래그 활성화 (계층 구조 변경 없이 LateUpdate에서 월드 좌표 고정)
            detachedWorldPos = transform.position;
            detachedWorldRot = transform.rotation;
            isCameraDetached = true;

            // 2. 두 차량 최종 부스터 100% 동시 풀 점등
            var blueCtrl = FindFirstObjectByType<VehicleTransformationController>();
            var redCtrl = FindFirstObjectByType<BoosterDeploymentController>();

            if (blueCtrl != null)
            {
                blueCtrl.SetSubBoosterFxActive(true);
                blueCtrl.SetMainBoosterFxActive(true);
            }

            if (redCtrl != null)
            {
                redCtrl.SnapToFullDeployment();
            }

            // 3. 폭발적인 부스터 속도 급가속 (부와앙-!)
            if (boostAccelerationRoutine != null) StopCoroutine(boostAccelerationRoutine);
            boostAccelerationRoutine = StartCoroutine(AnimateBoostRocketSpeed(boostRocketSpeed, boostAccelerationDuration));

            // 4. 카메라 부스터 후폭풍 진동 트리거
            boostBlastTimer = 0f;
            isBoostBlastActive = true;

            Debug.Log("[CameraWindBlastShock] 💥 카메라를 두고 두 차량 부스터 동시 점화 & 폭발적 초고속 질주 발진 (부와앙-!)");
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

        public void ResetBoosterLaunchState()
        {
            if (boostAccelerationRoutine != null)
            {
                StopCoroutine(boostAccelerationRoutine);
                boostAccelerationRoutine = null;
            }

            isBoostBlastActive = false;
            boostBlastTimer = 0f;

            if (isCameraDetached)
            {
                isCameraDetached = false;
                transform.localPosition = initialLocalPos;
                transform.localRotation = initialLocalRot;
            }

            if (splineAnimate != null)
            {
                splineAnimate.MaxSpeed = cruiseSpeed;
            }

            ApplyVehiclesNormalState();
        }

        /// <summary>
        /// 블루카 및 레드카의 부스터 변형 시퀀스를 동시에 시작
        /// </summary>
        [ContextMenu("🚀 두 차량 부스터 변형 시퀀스 동시 시작")]
        public void TriggerAllVehiclesBoosterTransformation()
        {
            // 블루카 풀 변형 시퀀스 트리거
            var blueCtrl = FindFirstObjectByType<VehicleTransformationController>();
            if (blueCtrl != null)
            {
                blueCtrl.DeployFullTransformation();
            }

            // 레드카 부스터 트리거
            var redCtrl = FindFirstObjectByType<BoosterDeploymentController>();
            if (redCtrl != null)
            {
                redCtrl.DeployBoosters();
            }

            Debug.Log("[CameraWindBlastShock] 🚀 두 차량 주행 중 1초 대기 후 부스터 변형 시퀀스 시작!");
        }

        /// <summary>
        /// 블루카 및 레드카를 원래 기본 미변형 상태(0%)로 즉시 복귀
        /// </summary>
        [ContextMenu("🔄 두 차량 기본 미변형 모습 복귀 (0%)")]
        public void ApplyVehiclesNormalState()
        {
            var blueCtrl = FindFirstObjectByType<VehicleTransformationController>();
            if (blueCtrl != null)
            {
                blueCtrl.SnapToNormalState();
            }

            var redCtrl = FindFirstObjectByType<BoosterDeploymentController>();
            if (redCtrl != null)
            {
                redCtrl.SnapToFullRetraction();
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;
            if (!isInitialized) StoreInitialPose();

            float time = Time.time;
            Vector3 shakePos = Vector3.zero;
            Vector3 shakeRotEuler = Vector3.zero;

            float launchTime = quietDuration + rumbleBuildUpDuration;

            bool hasNoShake = maxRumbleShake <= 0.00001f && maxRumbleAngleShake <= 0.00001f;

            if (!isKnockedDown)
            {
                if (hasNoShake || currentPhase == CinematicPhase.Quiet)
                {
                    // 완전 정적 (사용자가 인스펙터에서 흔들림 0으로 조절했거나 Quiet 구간)
                    shakePos = Vector3.zero;
                    shakeRotEuler = Vector3.zero;
                }
                else if (currentPhase == CinematicPhase.RumbleBuilding)
                {
                    // 진동이 점점 거세짐 (1초~3초 구간)
                    float t = Mathf.Clamp01((sequenceTimer - quietDuration) / Mathf.Max(0.01f, rumbleBuildUpDuration));
                    float curve = Mathf.Pow(t, 2.2f); // 서서히 거세지다가 출발 직전에 폭발적으로 진동

                    float noiseTime = time * shakeFrequency;
                    float nX = (Mathf.PerlinNoise(noiseTime, 0f) - 0.5f) * 2f;
                    float nY = (Mathf.PerlinNoise(0f, noiseTime) - 0.5f) * 2f;
                    float nRot = (Mathf.PerlinNoise(noiseTime * 0.7f, noiseTime * 0.7f) - 0.5f) * 2f;

                    shakePos = new Vector3(nX, nY, 0f) * (maxRumbleShake * curve);
                    shakeRotEuler = new Vector3(nY * 0.6f, nX * 0.6f, nRot) * (maxRumbleAngleShake * curve);
                }
                else if (currentPhase == CinematicPhase.PassByRush)
                {
                    if (hasNoShake)
                    {
                        shakePos = Vector3.zero;
                        shakeRotEuler = Vector3.zero;
                    }
                    else
                    {
                        float currentTurbulence = passByTurbulenceMultiplier;

                        if (!continuousOnboardShake)
                        {
                            // 카메라 옆으로 차들이 지나간 이후에는 급격하게 진동 감쇠
                            float passMoment = launchTime + Mathf.Min(passByViewDuration, 0.4f);
                            if (sequenceTimer < passMoment)
                            {
                                currentTurbulence = passByTurbulenceMultiplier;
                            }
                            else
                            {
                                float elapsedAfterPass = sequenceTimer - passMoment;
                                float t = Mathf.Clamp01(elapsedAfterPass / Mathf.Max(0.1f, passByCalmDownDuration));
                                float decay = Mathf.Pow(1f - t, 3.0f);
                                currentTurbulence = passByTurbulenceMultiplier * decay;
                            }
                        }

                        if (isBoostBlastActive)
                        {
                            boostBlastTimer += Time.deltaTime;
                            float blastT = Mathf.Clamp01(boostBlastTimer / Mathf.Max(0.1f, passByCalmDownDuration));
                            float decay = Mathf.Pow(1f - blastT, 3.0f);
                            currentTurbulence = Mathf.Max(currentTurbulence, passByTurbulenceMultiplier * 2.5f * decay);
                            if (blastT >= 1f) isBoostBlastActive = false;
                        }

                        float noiseTime = time * (shakeFrequency * 1.1f);
                        float nX = (Mathf.PerlinNoise(noiseTime, 15.3f) - 0.5f) * 2f;
                        float nY = (Mathf.PerlinNoise(32.1f, noiseTime) - 0.5f) * 2f;
                        float nRot = (Mathf.PerlinNoise(noiseTime, noiseTime) - 0.5f) * 2f;

                        shakePos = new Vector3(nX, nY, 0f) * (maxRumbleShake * currentTurbulence);
                        shakeRotEuler = new Vector3(nY, nX, nRot) * (maxRumbleAngleShake * currentTurbulence);
                    }
                }

                if (isCameraDetached)
                {
                    transform.position = detachedWorldPos + shakePos;
                    transform.rotation = detachedWorldRot * Quaternion.Euler(shakeRotEuler);
                }
                else
                {
                    transform.localPosition = initialLocalPos + shakePos;
                    transform.localRotation = initialLocalRot * Quaternion.Euler(shakeRotEuler);
                }
            }
            else
            {
                // 뒤로 덜컹! 벌러덩 넘어가는 전복 물리 애니메이션
                if (knockdownProgress < 1f)
                {
                    knockdownProgress += Time.deltaTime / Mathf.Max(0.01f, knockdownDuration);
                    if (knockdownProgress > 1f) knockdownProgress = 1f;
                }

                float smoothT = Mathf.SmoothStep(0f, 1f, knockdownProgress);

                // 바닥 충돌 바운스 반동 (Damped Sine Bounce)
                bounceDecay = Mathf.MoveTowards(bounceDecay, 0f, Time.deltaTime * 2.8f);
                float bounceAngle = Mathf.Sin(knockdownProgress * Mathf.PI * 4f) * (groundImpactBounce * 16f) * bounceDecay;
                float bounceY = Mathf.Abs(Mathf.Sin(knockdownProgress * Mathf.PI * 3f)) * (groundImpactBounce * 0.35f) * bounceDecay;

                // 회전: 하늘 보기(knockdownPitch) + 약간의 롤 기울기(knockdownRoll)
                float currentPitch = Mathf.Lerp(0f, knockdownPitch, smoothT) + bounceAngle;
                float currentRoll = Mathf.Lerp(0f, knockdownRoll, smoothT);
                Quaternion tumbleRot = Quaternion.Euler(currentPitch, 0f, currentRoll);

                // 위치: 바닥으로 주저앉음
                float currentDrop = Mathf.Lerp(0f, groundDropHeight, smoothT) + bounceY;
                Vector3 tumblePos = new Vector3(0f, currentDrop, 0f);

                // 바닥에 넘어진 후의 미세 여진
                float afterNoise = (Mathf.PerlinNoise(time * 20f, 0f) - 0.5f) * 0.012f * bounceDecay;

                transform.localPosition = initialLocalPos + tumblePos + new Vector3(0f, afterNoise, 0f);
                transform.localRotation = initialLocalRot * tumbleRot;
            }
        }

        private bool IsRestartKeyPressed()
        {
            if (restartKey == KeyCode.None) return false;

            try
            {
                if (Input.GetKeyDown(restartKey)) return true;
            }
            catch { }

#if ENABLE_INPUT_SYSTEM || UNITY_INPUT_SYSTEM_EXISTS
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (restartKey == KeyCode.Space && kb.spaceKey.wasPressedThisFrame) return true;
                    if (restartKey == KeyCode.Return && kb.enterKey.wasPressedThisFrame) return true;
                    if (restartKey == KeyCode.R && kb.rKey.wasPressedThisFrame) return true;
                    if (restartKey == KeyCode.T && kb.tKey.wasPressedThisFrame) return true;
                    if (restartKey == KeyCode.Alpha1 && kb.digit1Key.wasPressedThisFrame) return true;
                    if (restartKey == KeyCode.Alpha2 && kb.digit2Key.wasPressedThisFrame) return true;
                    if (restartKey == KeyCode.Escape && kb.escapeKey.wasPressedThisFrame) return true;
                }
            }
            catch { }
#endif
            return false;
        }

        private void Reset()
        {
            ApplyOnboardWindowCamPreset();
        }

        /// <summary>
        /// 카메라 전복 & 밤하늘 3초 응시 시네마틱 프리셋 적용 (요청 모드)
        /// </summary>
        [ContextMenu("🎬 카메라 전복 & 밤하늘 3초 응시 프리셋 적용")]
        public void ApplyKnockdownSky3sPreset()
        {
            enableKnockdown = true;
            startCarsWithFullBoosters = true;
            quietDuration = 0.5f;
            rumbleBuildUpDuration = 0.5f;
            passByViewDuration = 0.4f;
            autoDetectPassByPosition = true;
            skyHoldDuration = 3.0f;
            splineDriveDuration = 8.0f;
            maxRumbleShake = 0.025f;
            maxRumbleAngleShake = 0.6f;
            passByTurbulenceMultiplier = 1.4f;
            shakeFrequency = 20f;
            knockdownDuration = 0.32f;
            knockdownPitch = -82f;
            knockdownRoll = 14f;
            groundImpactBounce = 0.22f;
            groundDropHeight = -0.35f;
        }

        /// <summary>
        /// 차량 창문 온보드 액션캠 프리셋 (속도감 & 노면/엔진 고주파 떨림)
        /// </summary>
        [ContextMenu("🏎️ 차량 창문 온보드 액션캠 프리셋 적용")]
        public void ApplyOnboardWindowCamPreset()
        {
            enableKnockdown = false;
            startCarsWithFullBoosters = true;
            quietDuration = 0.0f;
            rumbleBuildUpDuration = 0.2f;
            passByViewDuration = 999f;
            autoDetectPassByPosition = false;
            splineDriveDuration = 8.0f;
            maxRumbleShake = 0.012f;
            maxRumbleAngleShake = 0.45f;
            passByTurbulenceMultiplier = 1.2f;
            shakeFrequency = 28f;
            continuousOnboardShake = true;
        }

        /// <summary>
        /// booston_yujeong_1 씬 전용: 스플라인 속도 주행 ➔ 1초 대기 ➔ 부스터 변형 ➔ 카메라 두고 두 차량 부와앙- 발진 연출 프리셋 (요청 모드)
        /// </summary>
        [ContextMenu("🎬 booston_yujeong_1 1초 변형 후 카메라 두고 부스터 발진 프리셋 적용")]
        public void ApplyBoosterTransformationShotPreset()
        {
            enableKnockdown = false;
            startCarsWithFullBoosters = false;
            triggerBoosterOnLaunch = true;
            boosterTransformDelay = 1.0f;
            leaveCameraBehindOnBoost = true;
            transformationDuration = 3.2f;
            cruiseSpeed = 150f;
            boostRocketSpeed = 500f;
            boostAccelerationDuration = 0.35f;
            continuousOnboardShake = true;
            quietDuration = 0f;
            rumbleBuildUpDuration = 0.1f;
            maxRumbleShake = 0.008f;
            maxRumbleAngleShake = 0.8f;
            passByTurbulenceMultiplier = 1.8f;
            passByCalmDownDuration = 1.0f;
            ResetBoosterLaunchState();
        }

        /// <summary>
        /// 도로 정적 ➔ 진동 고조 ➔ 질주 추월 ➔ 멀어지며 고요해짐 연출 프리셋 (요청 모드)
        /// </summary>
        [ContextMenu("🎬 도로 정적 ➔ 진동 고조 ➔ 멀어지며 조용해짐 프리셋 적용")]
        public void ApplyRoadPassByFadePreset()
        {
            enableKnockdown = false;
            continuousOnboardShake = false;
            startCarsWithFullBoosters = true;
            quietDuration = 1.2f;
            rumbleBuildUpDuration = 1.8f;
            passByViewDuration = 0.4f;
            passByCalmDownDuration = 1.0f;
            autoDetectPassByPosition = false;
            splineDriveDuration = 8.0f;
            maxRumbleShake = 0.035f;
            maxRumbleAngleShake = 1.6f;
            passByTurbulenceMultiplier = 1.8f;
            shakeFrequency = 24f;
        }

        /// <summary>
        /// 8초 도로 질주 연출 프리셋 (카메라 넘어짐 없음, 풀 부스터 질주)
        /// </summary>
        [ContextMenu("🎬 8초 도로 질주 프리셋 적용 (넘어짐 제거)")]
        public void Apply8SecondDrivePreset()
        {
            enableKnockdown = false;
            startCarsWithFullBoosters = true;
            quietDuration = 0.5f;
            rumbleBuildUpDuration = 0.5f;
            passByViewDuration = 7.0f;
            autoDetectPassByPosition = false;
            splineDriveDuration = 8.0f;
            maxRumbleShake = 0.025f;
            maxRumbleAngleShake = 0.6f;
            passByTurbulenceMultiplier = 1.4f;
            shakeFrequency = 20f;
        }

        /// <summary>
        /// 6초 완성 시네마틱 연출 프리셋 일괄 적용
        /// </summary>
        [ContextMenu("🎬 6초 시네마틱 프리셋 적용")]
        public void Apply6SecondPreset()
        {
            enableKnockdown = true;
            startCarsWithFullBoosters = true;
            quietDuration = 1.0f;
            rumbleBuildUpDuration = 1.5f;
            passByViewDuration = 0.4f;
            skyHoldDuration = 3.0f;
            splineDriveDuration = 5.0f;
            maxRumbleShake = 0.025f;
            maxRumbleAngleShake = 0.6f;
            passByTurbulenceMultiplier = 1.4f;
            shakeFrequency = 20f;
            knockdownDuration = 0.35f;
            knockdownPitch = -82f;
            knockdownRoll = 14f;
            groundImpactBounce = 0.22f;
            groundDropHeight = -0.35f;
        }

        /// <summary>
        /// 블루카 및 레드카의 부스터 변형 및 모든 이펙트를 대기 상태부터 100% 완료 상태로 즉시 설정
        /// </summary>
        [ContextMenu("🚀 두 차량 부스터 풀 전개 & 점등 즉시 적용")]
        public void ApplyVehiclesFullBoosted()
        {
            // 블루카 풀 변형 & 부스터 VFX 점등
            var blueCtrl = FindFirstObjectByType<VehicleTransformationController>();
            if (blueCtrl != null)
            {
                blueCtrl.SnapToFullTransformation();
            }

            // 레드카 풀 변형 & 부스터 VFX 점등
            var redCtrl = FindFirstObjectByType<BoosterDeploymentController>();
            if (redCtrl != null)
            {
                redCtrl.SnapToFullDeployment();
            }
        }

        /// <summary>
        /// 시네마틱 시퀀스를 처음부터 재실행
        /// </summary>
        [ContextMenu("🎬 시네마틱 시퀀스 처음부터 실행")]
        public void StartCinematicSequence()
        {
            RestoreInitialPose();
            AutoFindComponents();

            if (splineAnimate != null && Application.isPlaying)
            {
                splineAnimate.NormalizedTime = 0f;
                splineAnimate.Pause();
            }

            if (startCarsWithFullBoosters)
            {
                ApplyVehiclesFullBoosted();
            }
            else
            {
                ApplyVehiclesNormalState();
            }

            sequenceTimer = 0f;
            isSequenceRunning = true;
            splineLaunched = false;
            isKnockedDown = false;
        }

        /// <summary>
        /// 에디터 또는 런타임에서 즉시 풍압 전복 효과만 테스트
        /// </summary>
        [ContextMenu("💥 테스트: 풍압 전복 실행")]
        public void TriggerKnockdownTest()
        {
            StoreInitialPose();
            currentPhase = CinematicPhase.KnockedDown;
            isKnockedDown = true;
            knockdownProgress = 0f;
            bounceDecay = 1f;
        }

        /// <summary>
        /// 카메라를 원래 정면 상태로 리셋
        /// </summary>
        [ContextMenu("🔄 카메라 원래대로 리셋")]
        public void ResetCameraTest()
        {
            RestoreInitialPose();
        }
    }
}
