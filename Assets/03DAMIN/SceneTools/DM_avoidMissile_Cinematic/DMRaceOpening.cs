using UnityEngine;

namespace Damin.CinematicCopy
{
    /// <summary>Scene-owned opening motion. Does not modify vehicle prefabs or their movement code.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class DMRaceOpening : MonoBehaviour
    {
        [Tooltip("DM 씬에 배치된 차량 인스턴스만 연결합니다.")]
        public Transform[] vehicles;
        [Tooltip("Play 첫 프레임부터 일정한 속도로 주행합니다. 실제 게임 이동/Timeline 사용 시 끄세요.")]
        public bool moveOnPlay = true;
        [Min(0)] public float speedKph = 90;
        public Vector3 roadForward = Vector3.left;
        [Tooltip("도로 끝을 넘어가지 않도록 제한하는 주행 거리. 끝에서는 정지하며 순간이동하지 않습니다.")]
        [Min(0)] public float maxTravelMetres = 3000;
        public float TravelledMetres { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public bool ManualSimulation { get; set; }
        Vector3[] startPositions;

        void Awake() { CaptureStart(); }
        void Update() { if (!ManualSimulation) Advance(Time.deltaTime); }
        public void CaptureStart()
        {
            startPositions = new Vector3[vehicles == null ? 0 : vehicles.Length];
            for (int i = 0; i < startPositions.Length; i++) if (vehicles[i]) startPositions[i] = vehicles[i].position;
            TravelledMetres = 0;
            ElapsedSeconds = 0;
        }
        public void Advance(float deltaTime)
        {
            if (!Application.isPlaying || !moveOnPlay || vehicles == null || deltaTime <= 0) return;
            Vector3 forward = Vector3.ProjectOnPlane(roadForward, Vector3.up).normalized;
            float step = Mathf.Min(Mathf.Max(0, speedKph) / 3.6f * deltaTime, Mathf.Max(0, maxTravelMetres - TravelledMetres));
            for (int i = 0; i < vehicles.Length; i++) if (vehicles[i]) vehicles[i].position += forward * step;
            TravelledMetres += step;
            if (speedKph > 0) ElapsedSeconds += step / (speedKph / 3.6f);
        }
        [ContextMenu("Restart opening positions (Play only)")]
        public void RestartOpening()
        {
            if (!Application.isPlaying || startPositions == null || vehicles == null || vehicles.Length != startPositions.Length) return;
            for (int i = 0; i < vehicles.Length; i++) if (vehicles[i]) vehicles[i].position = startPositions[i];
            TravelledMetres = 0;
            ElapsedSeconds = 0;
        }
    }
}
