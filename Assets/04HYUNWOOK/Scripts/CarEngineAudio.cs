using UnityEngine;

[RequireComponent(typeof(ArcadeCarController))]
public class CarEngineAudio : MonoBehaviour
{
    public AudioClip engineLoop;
    [Range(0f, 1f)] public float idleVolume = 0.12f;
    [Range(0f, 1f)] public float drivingVolume = 0.5f;
    [Range(0.1f, 3f)] public float idlePitch = 0.8f;
    [Range(0.1f, 3f)] public float topSpeedPitch = 2.2f;
    [Header("Tire Skid")]
    public AudioClip skidLoop;
    [Range(0f, 1f)] public float skidVolume = 0.4f;
    [Min(0f)] public float minimumSkidSpeed = 12f;
    [Tooltip("제동 중 전후 미끄러짐 또는 실제 옆 미끄러짐이 이 값 이상일 때 소리가 시작됩니다.")]
    [Min(0.01f)] public float skidStartSlip = 0.25f;
    [Min(0.02f)] public float skidFullSlip = 0.8f;
    [Min(0.01f)] public float skidFadeInTime = 0.1f;
    [Min(0.01f)] public float skidFadeOutTime = 0.2f;
    [Tooltip("코너링 스키드음을 허용하는 최소 차체 옆 이동 속도(m/s)입니다. 출발 시 타이어 떨림을 제외합니다.")]
    [Min(0.1f)] public float minimumSidewaysSkidSpeed = 1f;
    ArcadeCarController car;
    AudioSource engine;
    AudioSource skid;
    WheelCollider[] wheels;
    Rigidbody body;
    public float CurrentPitch => engine != null ? engine.pitch : 0f;

    void Awake()
    {
        car = GetComponent<ArcadeCarController>();
        engine = gameObject.AddComponent<AudioSource>();
        engine.playOnAwake = false;
        engine.loop = true;
        engine.spatialBlend = 0f;
        engine.dopplerLevel = 0f;
        engine.clip = engineLoop;
        engine.pitch = idlePitch;
        engine.volume = idleVolume;
        body = GetComponent<Rigidbody>();
        wheels = GetComponentsInChildren<WheelCollider>();
        skid = gameObject.AddComponent<AudioSource>();
        skid.playOnAwake = false;
        skid.loop = true;
        skid.spatialBlend = 0f;
        skid.dopplerLevel = 0f;
        skid.clip = skidLoop;
        skid.volume = 0f;
    }

    void OnEnable()
    {
        if (engine != null && engine.clip != null) engine.Play();
    }

    void Update()
    {
        if (engine == null) return;
        if (Time.timeScale == 0f)
        {
            engine.Pause();
            if (skid != null) skid.Pause();
            return;
        }
        float finishRatio = car.FinishBraking ? Mathf.Clamp01(car.FinishSpeedRatio) : 1f;
        float speedRatio = Mathf.Clamp01(car.EngineSpeedRatio) * finishRatio;
        float blend = 1f - Mathf.Exp(-5f * Time.deltaTime);
        engine.pitch = Mathf.Lerp(engine.pitch, Mathf.Lerp(idlePitch, topSpeedPitch, speedRatio), blend);
        engine.volume = Mathf.Lerp(engine.volume, Mathf.Lerp(idleVolume, drivingVolume, Mathf.Sqrt(speedRatio)) * finishRatio, blend);
        // Pause with Unity's time scale, while retaining the correct loop position.
        if (!engine.isPlaying && engine.clip != null) engine.UnPause();
        UpdateSkid();
    }

    void UpdateSkid()
    {
        if (skid == null || skid.clip == null) return;
        float slip = 0f;
        if (car.SpeedKmh >= minimumSkidSpeed && wheels != null)
        {
            bool braking = car.IsBraking;
            bool slidingSideways = Mathf.Abs(Vector3.Dot(body.linearVelocity, transform.right))
                >= minimumSidewaysSkidSpeed;
            foreach (var wheel in wheels)
            {
                if (wheel == null || !wheel.enabled || !wheel.gameObject.activeInHierarchy
                    || wheel.attachedRigidbody != body || !wheel.GetGroundHit(out WheelHit hit)) continue;
                if ((car.groundMask.value & (1 << hit.collider.gameObject.layer)) == 0) continue;
                // Launch wheelspin is not braking: don't turn normal acceleration into a skid sound.
                float brakeSlip = braking ? Mathf.Abs(hit.forwardSlip) : 0f;
                float cornerSlip = slidingSideways ? Mathf.Abs(hit.sidewaysSlip) : 0f;
                slip = Mathf.Max(slip, brakeSlip, cornerSlip);
            }
        }
        float intensity = Mathf.InverseLerp(skidStartSlip, Mathf.Max(skidStartSlip + 0.01f, skidFullSlip), slip);
        float targetVolume = intensity * skidVolume;
        float fadeTime = targetVolume > skid.volume ? skidFadeInTime : skidFadeOutTime;
        skid.volume = Mathf.MoveTowards(skid.volume, targetVolume,
            Mathf.Max(0.01f, skidVolume) * Time.deltaTime / Mathf.Max(0.01f, fadeTime));
        skid.pitch = Mathf.Lerp(0.95f, 1.12f, intensity);
        if (skid.volume > 0.001f)
        {
            if (!skid.isPlaying) { skid.UnPause(); if (!skid.isPlaying) skid.Play(); }
        }
        else if (skid.isPlaying) skid.Stop();
    }

    void OnDisable()
    {
        if (engine != null) engine.Stop();
        if (skid != null) { skid.Stop(); skid.volume = 0f; }
    }

    void OnDestroy()
    {
        if (engine != null) Destroy(engine);
        if (skid != null) Destroy(skid);
    }
}
