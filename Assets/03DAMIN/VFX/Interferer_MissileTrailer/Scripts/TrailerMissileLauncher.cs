using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Damin.Trailer.MissileCar
{
    [DisallowMultipleComponent]
    public sealed class TrailerMissileLauncher : MonoBehaviour
    {
        [Serializable] public sealed class Slot
        {
            public string label;
            public Transform mount;
            public TrailerMissileFlight loadedMissile;
            [HideInInspector] public Vector3 localPosition, localScale = Vector3.one;
            [HideInInspector] public Quaternion localRotation = Quaternion.identity;
        }

        [Header("연결")]
        public TrailerHoodDeployment deployment;
        public Transform vehicleFrame, flightDirection;
        public TrailerMissileFlight missilePrefab;
        public Slot[] slots = new Slot[4];
        [Header("입력 / 트레일러 자동 재생")]
        public bool useShiftInput = true;
        public bool playOnStart;
        [Min(0)] public float sequenceStartDelay;
        [Header("발사 타이밍 — 전개가 끝난 후")]
        [Min(0)] public float launchDelayAfterDeployment = .25f;
        [Tooltip("보넷이 열리기 시작한 뒤 첫 발사까지 확보할 최소 시간. 연기가 쌓일 시간을 확보합니다. 0이면 기존 전개·상승·대기 시간만 사용합니다.")]
        [Min(0)] public float minimumFirstLaunchTime;
        [Min(.01f)] public float intervalBetweenMissiles = .5f;
        [Range(1, 4)] public int missilesPerSequence = 1;
        [Tooltip("장착 위치 번호는 1부터 시작합니다. 기본 첫 발은 4번입니다.")]
        public int[] launchOrder = { 4, 1, 2, 3 };
        [Header("발사 전 — 선택한 미사일만 상승하고 대기")]
        public bool enablePreLaunchLift = true;
        [Range(1, 4)] public int preLaunchMissileNumber = 4;
        [Tooltip("차량 위쪽으로 추가 상승하는 높이. Unity 1 단위 = 1m 기준이며 차량 스케일과 무관합니다.")]
        [Min(0)] public float preLaunchLiftHeightCm = 3f;
        [Min(0)] public float preLaunchLiftDuration = .35f;
        [Min(0)] public float preLaunchHoldDuration = .4f;
        [Header("미사일 비행 — 트레일러용, 유도/피격 판정 없음")]
        [Min(0)] public float clearanceHeight = .10f;
        [Min(0)] public float clearanceForwardDistance = .65f;
        [Min(.01f)] public float clearanceTime = .3f;
        [Min(0)] public float missileSpeed = 15;
        [Min(0)] public float missileAcceleration = 20;
        [Min(.1f)] public float missileLifetime = 6;
        [Header("VFX / Timeline 연결 이벤트")]
        public UnityEvent onSequenceStarted = new UnityEvent(), onBayReady = new UnityEvent(),
            onMissileLaunched = new UnityEvent(), onSequenceFinished = new UnityEvent(), onReset = new UnityEvent();

        public bool ManualSimulation { get; set; }
        public bool ExternallyControlled { get; set; }
        public bool IsRunning { get; private set; }
        public bool IsDeployed { get; private set; }
        public float Elapsed { get; private set; }
        public TrailerMissileFlight LastLaunched { get; private set; }
        public bool IsPreparingMissile => pendingSlot >= 0;
        public int LoadedCount
        {
            get { int n = 0; if (slots != null) foreach (var s in slots) if (s != null && s.loadedMissile) n++; return n; }
        }
        public float FirstLaunchTime => Mathf.Max(0, sequenceStartDelay) + Mathf.Max(Mathf.Max(0, minimumFirstLaunchTime),
            (deployment ? deployment.Duration : 0) + Mathf.Max(0, launchDelayAfterDeployment) + PreparationDuration(FindNextSlot()));

        readonly List<TrailerMissileFlight> launched = new List<TrailerMissileFlight>();
        int fired, targetCount, pendingSlot = -1;
        float nextShot, cooldown, preparationStarted, pendingLiftTime, pendingHoldTime, pendingHeight;
        Vector3 pendingBasePosition;

        void Start() { if (playOnStart && !ExternallyControlled) PlaySequence(); }
        void Update()
        {
            if (ManualSimulation || ExternallyControlled) return;
            if (useShiftInput && ShiftPressed()) PlaySequence();
            Advance(Time.deltaTime);
        }
        public void CaptureMountPoses()
        {
            if (slots == null) return;
            foreach (var s in slots) if (s != null && s.loadedMissile)
            {
                var t = s.loadedMissile.transform;
                s.localPosition = t.localPosition; s.localRotation = t.localRotation; s.localScale = t.localScale;
            }
        }
        bool Valid() => deployment && deployment.Ready && vehicleFrame && missilePrefab && slots != null && slots.Length > 0;
        bool UsesPreparation(int index) => enablePreLaunchLift && index >= 0 && index + 1 == preLaunchMissileNumber;
        float PreparationDuration(int index) => UsesPreparation(index)
            ? Mathf.Max(0, preLaunchLiftDuration) + Mathf.Max(0, preLaunchHoldDuration) : 0;

        [ContextMenu("Play Sequence (Play 모드)")]
        public void PlaySequence()
        {
            if (ExternallyControlled) return;
            if (!Application.isPlaying && !ManualSimulation) { Debug.LogWarning("Play 모드에서 실행하세요.", this); return; }
            if (IsRunning || cooldown > 0 || LoadedCount == 0) return;
            if (!Valid()) { Debug.LogError("[Trailer Missile] 필수 연결이 비어 있습니다.", this); return; }
            // Repeated Shift preserves the original one-next-missile behavior once deployed.
            if (IsDeployed) { FireNext(); return; }
            Elapsed = 0; fired = 0; targetCount = Mathf.Clamp(missilesPerSequence, 1, slots.Length);
            nextShot = FirstLaunchTime - PreparationDuration(FindNextSlot());
            IsRunning = true;
            onSequenceStarted.Invoke();
        }

        public void Advance(float dt)
        {
            dt = Mathf.Max(0, dt);
            cooldown = Mathf.Max(0, cooldown - dt);
            if (!IsRunning) return;
            Elapsed += dt;
            if (!IsDeployed)
            {
                float deployTime = Elapsed - Mathf.Max(0, sequenceStartDelay);
                deployment.Apply(Mathf.Max(0, deployTime));
                if (deployTime >= deployment.Duration)
                {
                    IsDeployed = true;
                    onBayReady.Invoke();
                    if (!IsRunning) return;
                }
            }
            if (pendingSlot >= 0) { AdvancePreparation(); return; }
            if (!IsDeployed || Elapsed < nextShot || cooldown > 0) return;
            int index = FindNextSlot();
            if (index < 0) { FinishSequence(); return; }
            BeginShot(index, nextShot);
        }

        // Returns true when the request is accepted; the launch event fires after lift/hold.
        public bool FireNext()
        {
            if (ExternallyControlled) return false;
            if ((!Application.isPlaying && !ManualSimulation) || !Valid() || !IsDeployed || IsRunning || cooldown > 0) return false;
            int index = FindNextSlot();
            if (index < 0) return false;
            Elapsed = 0; fired = 0; targetCount = 1; nextShot = 0; IsRunning = true;
            BeginShot(index, 0);
            return true;
        }
        int FindNextSlot()
        {
            if (slots == null) return -1;
            var checkedSlots = new HashSet<int>();
            if (launchOrder != null) foreach (int number in launchOrder)
            {
                int i = number - 1;
                if (i >= 0 && i < slots.Length && checkedSlots.Add(i) && HasMissile(i)) return i;
            }
            for (int i = 0; i < slots.Length; i++) if (!checkedSlots.Contains(i) && HasMissile(i)) return i;
            return -1;
        }
        bool HasMissile(int index) => slots[index] != null && slots[index].mount && slots[index].loadedMissile;
        void BeginShot(int index, float scheduledTime)
        {
            if (!UsesPreparation(index)) { LaunchSlot(index); return; }
            pendingSlot = index;
            preparationStarted = scheduledTime;
            pendingBasePosition = slots[index].loadedMissile.transform.localPosition;
            // Snapshot this shot's controls so editing Inspector cannot jump it mid-lift.
            pendingLiftTime = Mathf.Max(0, preLaunchLiftDuration);
            pendingHoldTime = Mathf.Max(0, preLaunchHoldDuration);
            pendingHeight = Mathf.Max(0, preLaunchLiftHeightCm) * .01f;
            AdvancePreparation();
        }
        void AdvancePreparation()
        {
            int index = pendingSlot;
            if (index < 0) return;
            if (!HasMissile(index)) { pendingSlot = -1; FinishSequence(); return; }
            var t = slots[index].loadedMissile.transform;
            float time = Mathf.Max(0, Elapsed - preparationStarted);
            float amount = pendingLiftTime > 0 ? Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / pendingLiftTime)) : 1;
            // Still parented to the bay: follows the moving car until the actual launch.
            Vector3 offset = vehicleFrame.up * (pendingHeight * amount);
            t.localPosition = pendingBasePosition + (t.parent ? t.parent.InverseTransformVector(offset) : offset);
            if (time >= pendingLiftTime + pendingHoldTime)
            {
                pendingSlot = -1;
                LaunchSlot(index);
            }
        }
        void LaunchSlot(int index)
        {
            var slot = slots[index];
            var missile = slot.loadedMissile;
            slot.loadedMissile = null; LastLaunched = missile; launched.Add(missile);
            missile.ManualSimulation = ManualSimulation;
            missile.Launch(flightDirection ? flightDirection.forward : vehicleFrame.forward, vehicleFrame.up,
                clearanceHeight, clearanceForwardDistance, clearanceTime, missileSpeed, missileAcceleration, missileLifetime);
            // A flight event may synchronously reset the system.
            if (!IsRunning || LastLaunched != missile) return;
            cooldown = Mathf.Max(.01f, intervalBetweenMissiles);
            nextShot = Elapsed + cooldown; fired++;
            onMissileLaunched.Invoke();
            if (IsRunning && (fired >= targetCount || LoadedCount == 0)) FinishSequence();
        }
        void FinishSequence() { IsRunning = false; onSequenceFinished.Invoke(); }

        // The unified director owns the clock; reuse the same launch/reset events and missile list.
        public bool BeginExternalSequence()
        {
            if (!ExternallyControlled || !Valid() || IsRunning) return false;
            fired=0; targetCount=1; pendingSlot=-1; Elapsed=0; IsDeployed=false; IsRunning=true;
            onSequenceStarted.Invoke(); return true;
        }
        public void ExternalBayReady(float time)
        {
            if (!ExternallyControlled || IsDeployed) return;
            Elapsed=time; IsDeployed=true; onBayReady.Invoke();
        }
        public bool ExternalLaunch(int index,float time)
        {
            if (!ExternallyControlled || !IsRunning || !IsDeployed || index<0 || index>=slots.Length || !HasMissile(index)) return false;
            Elapsed=time; LaunchSlot(index); return true;
        }
        public void StopExternalSequence(){if(ExternallyControlled){IsRunning=false;pendingSlot=-1;}}

        [ContextMenu("Reset And Reload (Play 모드)")]
        public void ResetAndReload()
        {
            if (!Application.isPlaying && !ManualSimulation) return;
            // Restore a partly lifted missile before resetting the bay.
            if (pendingSlot >= 0 && HasMissile(pendingSlot)) slots[pendingSlot].loadedMissile.transform.localPosition = pendingBasePosition;
            pendingSlot = -1; IsRunning = IsDeployed = false; Elapsed = cooldown = 0; LastLaunched = null;
            foreach (var m in launched) if (m) { if (Application.isPlaying) Destroy(m.gameObject); else DestroyImmediate(m.gameObject); }
            launched.Clear();
            if (slots != null) foreach (var s in slots) if (s != null && s.mount && !s.loadedMissile && missilePrefab)
            {
                s.loadedMissile = Instantiate(missilePrefab, s.mount);
                var t = s.loadedMissile.transform;
                t.localPosition = s.localPosition; t.localRotation = s.localRotation; t.localScale = s.localScale;
            }
            if (deployment) deployment.ResetPose();
            onReset.Invoke();
        }
        void OnDestroy() { foreach (var m in launched) if (m) { if (Application.isPlaying) Destroy(m.gameObject); else DestroyImmediate(m.gameObject); } }
        static bool ShiftPressed()
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
    }
}
