using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(UIDocument))]
public class RacingHudController : MonoBehaviour
{
    public ArcadeCarController car;
    [Header("UI Documents / T View Toggle")]
    [Tooltip("3인칭에서 표시할 TPS UI Document입니다. 비워두면 UIDocument의 Source Asset을 사용합니다.")]
    public VisualTreeAsset tpsDocument;
    [Tooltip("1인칭에서 표시할 FPS UI Document입니다. UI Builder에서 독립적으로 편집할 수 있습니다.")]
    public VisualTreeAsset fpsDocument;
    VisualTreeAsset originalDocument;
    VisualTreeAsset runtimeTpsDocument;
    [Min(1f)] public float speedometerMaximum = 300f;
    [Min(1000f)] public float tachometerMaximum = 8000f;
    public float needleStartAngle = 150f;
    public float needleSweep = 240f;
    [Min(0f)] public float needleResponse = 12f;
    [Header("Fuel Consumption")]
    [Min(1f)] public float fuelCapacityLitres = 60f;
    [HideInInspector] public float displayedFuelLitres = 60f;
    [Tooltip("차량 최고속도에서 연료 1%가 소모되는 시간(초)")]
    [Min(0.01f)] public float secondsPerFuelPercentAtMaxSpeed = 5f;
    double remainingFuelPercent = 100.0;
    public float FuelPercent => (float)remainingFuelPercent;

    public static double ConsumeFuelPercent(double remaining, double speedKmh,
        double maximumSpeedKmh, double secondsPerPercent, double deltaSeconds)
    {
        double ratio = Math.Max(0.0, Math.Min(1.0, speedKmh / Math.Max(1.0, maximumSpeedKmh)));
        double used = ratio * Math.Max(0.0, deltaSeconds) / Math.Max(0.01, secondsPerPercent);
        return Math.Max(0.0, Math.Min(100.0, remaining - used));
    }
    [Header("Helmet Startup")]
    public bool playHelmetStartup = true;
    [Min(0f)] public float startupClearTime = 0.3f;
    [Min(0.01f)] public float shieldCloseTime = 0.65f;
    [Min(0f)] public float startupBlackHold = 0.15f;
    [Min(0.01f)] public float hudPowerFadeTime = 0.8f;
    public AudioClip powerOnSound;
    public AudioClip shieldCloseSound;
    [Range(0f,1f)] public float startupVolume = 0.4f;
    public bool StartupComplete { get; private set; }
    public float StartupOpacity { get; private set; }
    public float StartupShieldCoverage { get; private set; }
    public float StartupShieldOpacity => StartupComplete ? 0f : 1f - StartupOpacity;
    public bool IsFirstPersonHud => hud != null && hud.ClassListContains("fps-document");
    VisualElement startupShield;
    AudioSource startupAudio;
    float startupClock;
    bool shieldSoundPlayed, powerSoundPlayed;
    RacingUI.DigitalReadout gearText;
    Label gearLimitText;
    RacingUI.DigitalReadout fuelText, fuelPercentText, fuelCapacityText;
    VisualElement fuelFill;
    [Header("Speed UI Shake")]
    public bool enableSpeedShake = true;
    [Tooltip("흔들림이 시작되는 속도(km/h)")]
    [Min(0f)] public float shakeStartSpeed = 30f;
    [Tooltip("최대 흔들림에 도달하는 속도(km/h)")]
    [Min(1f)] public float shakeFullSpeed = 300f;
    [Tooltip("최대 이동 폭(UI 픽셀). 0이면 흔들리지 않습니다.")]
    [Range(0f, 12f)] public float shakeMaxPixels = 2.5f;
    [Range(1f, 30f)] public float shakeFrequency = 14f;
    [Min(0.01f)] public float shakeSmoothTime = 0.2f;
    float shakeAmplitude, shakeVelocity, shakeClock;
    public Vector2 CurrentShakeOffset { get; private set; }
    [Header("Live Minimap")]
    [Tooltip("미니맵에 표시할 직선 도로입니다. 비워두면 Road를 찾습니다.")]
    public Transform minimapRoad;
    [Tooltip("도로의 미니맵 시작점과 끝점(UI 좌표)입니다.")]
    public Vector2 mapStart = new Vector2(126f, 198f);
    public Vector2 mapEnd = new Vector2(126f, 52f);
    [Min(1f)] public float mapRoadWidth = 20f;
    public float ElapsedSeconds { get; private set; }
    // The controller has no combustion-engine simulation: this is an indicated RPM.
    public float IndicatedRpm { get; private set; }
    public float DisplayedSpeed { get; private set; }
    public float LapProgressPercent { get; private set; }
    [Tooltip("Optional lap start. Defaults to SpawnPoint, then the starting vehicle position.")]
    public Transform lapStartPoint;
    float lapStartRoadZ;
    bool lapStartResolved;
    public static float CalculateLapProgressPercent(float progress) => 100f * Mathf.Clamp01(progress);
    [Header("Finish Ranking")]
    public string playerName = "PLAYER";
    [Tooltip("현재 직선 Road의 끝을 통과하면 플레이어 완주 기록을 저장합니다.")]
    public bool finishAtRoadEnd = true;
    readonly RaceFinishResults results = new RaceFinishResults();
    bool hasPreviousRoadPosition;
    Vector3 previousRoadPosition;
    VisualElement speedNeedle, rpmNeedle;
    CarCinemachineSetup viewCamera;
    VisualElement hud;
    readonly VisualElement[] rpmBars = new VisualElement[40];
    RacingUI.DigitalReadout speedText, timeText;
    UIDocument document;
    VisualElement boundRoot;
    VisualElement playerMarker;
    Bounds roadBounds;
    bool hasRoadBounds;

    void OnEnable()
    {
        remainingFuelPercent = 100.0;
        displayedFuelLitres = Mathf.Max(1f, fuelCapacityLitres);
        shakeAmplitude = shakeVelocity = shakeClock = 0f;
        CurrentShakeOffset = Vector2.zero;
        document = GetComponent<UIDocument>();
        originalDocument = document.visualTreeAsset;
        runtimeTpsDocument = tpsDocument != null ? tpsDocument : originalDocument;
        ElapsedSeconds = 0f;
        results.Clear();
        LapProgressPercent = 0f;
        lapStartResolved = false;
        hasPreviousRoadPosition = false;
        startupClock = 0f;
        StartupShieldCoverage = 0f;
        shieldSoundPlayed = powerSoundPlayed = false;
        StartupComplete = !playHelmetStartup;
        StartupOpacity = StartupComplete ? 1f : 0f;
        if (startupAudio == null)
        {
            startupAudio = gameObject.AddComponent<AudioSource>();
            startupAudio.playOnAwake = false;
            startupAudio.spatialBlend = 0f;
        }
        Bind();
    }

    void Start()
    {
        if (car == null) car = FindFirstObjectByType<ArcadeCarController>();
        viewCamera = FindFirstObjectByType<CarCinemachineSetup>();
        SetStartupLocks(!StartupComplete);
    }

    void Bind()
    {
        boundRoot = document.rootVisualElement;
        hud = boundRoot.Q("racing-hud");
        AttachHelmetMask();
        if (hud != null) { RacingHudArtwork.Attach(hud); hud.style.opacity = StartupOpacity; }
        startupShield = boundRoot.Q("startup-shield");
        if (startupShield != null) startupShield.style.display = StartupComplete ? DisplayStyle.None : DisplayStyle.Flex;
        gearText = boundRoot.Q<RacingUI.DigitalReadout>("gear-value");
        fuelText = boundRoot.Q<RacingUI.DigitalReadout>("fuel-value");
        fuelPercentText = boundRoot.Q<RacingUI.DigitalReadout>("fuel-percent");
        gearLimitText = boundRoot.Q<Label>("gear-limit");
        fuelFill = boundRoot.Q("fuel-fill");
        fuelCapacityText = boundRoot.Q<RacingUI.DigitalReadout>("fuel-capacity");
        for (int i = 0; i < rpmBars.Length; i++) rpmBars[i] = boundRoot.Q("rpm-bar-" + i);
        speedNeedle = boundRoot.Q("speed-needle");
        rpmNeedle = boundRoot.Q("rpm-needle");
        speedText = boundRoot.Q<RacingUI.DigitalReadout>("speed-value");
        timeText = boundRoot.Q<RacingUI.DigitalReadout>("time-value");
        RefreshRanking();
        playerMarker = boundRoot.Q(className: "player-marker");
        boundRoot.pickingMode = PickingMode.Ignore;
        boundRoot.Query<VisualElement>().ForEach(element => element.pickingMode = PickingMode.Ignore);
    }

    void Update()
    {
        if (viewCamera == null) viewCamera = FindFirstObjectByType<CarCinemachineSetup>();
        SelectViewDocument();
        if (document.rootVisualElement != boundRoot || speedText == null || speedText.panel == null) Bind();
        if (car == null) car = FindFirstObjectByType<ArcadeCarController>();
        DisplayedSpeed = car != null ? car.SpeedKmh : 0f;
        UpdateStartup();
        if (StartupComplete) { UpdateSpeedShake(); ElapsedSeconds += Time.deltaTime; UpdateMinimap(); }
        if (gearText != null) gearText.text = (car != null ? car.CurrentGear : 1).ToString();
        if (gearLimitText != null) gearLimitText.text = "LIMIT " + (car != null ? car.CurrentGearSpeedLimit : 50f).ToString("000");
        if (StartupComplete && car != null)
            remainingFuelPercent = ConsumeFuelPercent(remainingFuelPercent, DisplayedSpeed,
                car.maxForwardSpeed, secondsPerFuelPercentAtMaxSpeed, Time.deltaTime);
        displayedFuelLitres = (float)(remainingFuelPercent * 0.01) * Mathf.Max(1f, fuelCapacityLitres);
        float fuel = displayedFuelLitres;
        if (fuelText != null) fuelText.text = fuel.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        if (fuelCapacityText != null) fuelCapacityText.text = Mathf.Max(1f,fuelCapacityLitres).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        if (fuelPercentText != null) fuelPercentText.text = Math.Max(0, (int)Math.Ceiling(remainingFuelPercent - 0.000001)).ToString();
        if (fuelFill != null) fuelFill.style.width = Length.Percent(100f * fuel / Mathf.Max(1f, fuelCapacityLitres));
        float topSpeed = car != null ? car.CurrentGearSpeedLimit : 50f;
        float targetRpm = Mathf.Lerp(850f, 7500f, Mathf.Clamp01(DisplayedSpeed / Mathf.Max(1f, topSpeed)));
        IndicatedRpm = Mathf.Lerp(IndicatedRpm, targetRpm, 1f - Mathf.Exp(-needleResponse * Time.deltaTime));
        if (speedText != null) speedText.text = Mathf.RoundToInt(DisplayedSpeed).ToString("000");
        int milliseconds = Mathf.FloorToInt(ElapsedSeconds * 1000f);
        if (timeText != null) timeText.text = $"{milliseconds / 60000:00}:{milliseconds / 1000 % 60:00}.{milliseconds % 1000:000}";
        speedText?.MarkDirtyRepaint(); timeText?.MarkDirtyRepaint();
        for (int i = 0; i < rpmBars.Length; i++) if (rpmBars[i] != null)
            rpmBars[i].style.opacity = IndicatedRpm / tachometerMaximum * rpmBars.Length > i ? 1f : 0.12f;
        if (hud != null && viewCamera != null)
        {
            bool first = viewCamera.ViewBlend >= 0.5f;
            hud.EnableInClassList("first-person", first);

            // Hide the layout swap in a brief dip while the camera travels between views.
            hud.style.opacity = StartupOpacity * (1f - 0.45f * Mathf.Sin(viewCamera.ViewBlend * Mathf.PI));
        }
        var status = boundRoot.Q<Label>("drive-status");
        if (status != null) status.text = car != null && car.IsBraking ? "BRAKING" : DisplayedSpeed > 1f ? "DRIVE" : "READY";
        if (speedNeedle != null) speedNeedle.style.rotate = new Rotate(new Angle(
            needleStartAngle + needleSweep * Mathf.Clamp01(DisplayedSpeed / speedometerMaximum)));
        if (rpmNeedle != null) rpmNeedle.style.rotate = new Rotate(new Angle(
            needleStartAngle + needleSweep * Mathf.Clamp01(IndicatedRpm / tachometerMaximum)));
    }

    void AttachHelmetMask()
    {
        var frame = hud?.Q("fps-frame");
        if (frame == null || hud.Q("helmet-outside-mask") != null) return;
        var mask = new VisualElement { name = "helmet-outside-mask", pickingMode = PickingMode.Ignore };
        mask.style.position = Position.Absolute;
        mask.style.left = mask.style.right = mask.style.top = mask.style.bottom = 0;
        // Share the authored frame transform, including its 1.01 x 1.05 FHD overscan.
        mask.style.scale = frame.style.scale;
        mask.style.overflow = Overflow.Visible;
        mask.generateVisualContent += context => DrawHelmetMask(context.painter2D, mask.contentRect);
        mask.RegisterCallback<GeometryChangedEvent>(_ => mask.MarkDirtyRepaint());
        hud.Insert(hud.IndexOf(frame), mask);
    }

    // Outer helmet silhouette in the source artwork's 1920 x 1080 coordinates.
    // The frame is drawn above the mask, preserving its red rim and live readouts.
    static readonly Vector2[] HelmetOpening = {
        new Vector2(105, 38), new Vector2(490, 55), new Vector2(510, 35),
        new Vector2(820, 51), new Vector2(1100, 51), new Vector2(1400, 35),
        new Vector2(1430, 54), new Vector2(1810, 38), new Vector2(1880, 100),
        new Vector2(1890, 230), new Vector2(1820, 300), new Vector2(1850, 420),
        new Vector2(1860, 540), new Vector2(1840, 675), new Vector2(1790, 795),
        new Vector2(1840, 870), new Vector2(1790, 955), new Vector2(1420, 985),
        new Vector2(1370, 1030), new Vector2(550, 1030), new Vector2(495, 985),
        new Vector2(130, 955), new Vector2(80, 870), new Vector2(130, 795),
        new Vector2(80, 675), new Vector2(60, 540), new Vector2(70, 420),
        new Vector2(100, 300), new Vector2(30, 230), new Vector2(40, 100)
    };

    static void DrawHelmetMask(Painter2D painter, Rect rect)
    {
        if (rect.width <= 0f || rect.height <= 0f) return;
        painter.fillColor = Color.black;
        painter.BeginPath();
        // Extend past the viewport so HUD shake and curvature never expose a seam.
        painter.MoveTo(new Vector2(-128, -128));
        painter.LineTo(new Vector2(rect.width + 128, -128));
        painter.LineTo(new Vector2(rect.width + 128, rect.height + 128));
        painter.LineTo(new Vector2(-128, rect.height + 128));
        painter.ClosePath();
        for (int i = 0; i < HelmetOpening.Length; i++)
        {
            Vector2 point = new Vector2(HelmetOpening[i].x * rect.width / 1920f,
                HelmetOpening[i].y * rect.height / 1080f);
            if (i == 0) painter.MoveTo(point); else painter.LineTo(point);
        }
        painter.ClosePath();
        painter.Fill(FillRule.OddEven);
    }

    void SelectViewDocument()
    {
        // Swap only the visual tree: race time, fuel, startup and alarms stay on this controller.
        if (fpsDocument == null || viewCamera == null) return;
        var next = viewCamera.ViewBlend >= 0.5f ? fpsDocument : runtimeTpsDocument;
        if (next == null || document.visualTreeAsset == next) return;
        document.visualTreeAsset = next;
        Bind();
    }

    void UpdateSpeedShake()
    {
        if (hud == null) return;
        if (!enableSpeedShake || car == null || shakeMaxPixels <= 0f)
        {
            ResetShake();
            return;
        }
        // Scaled time freezes the effect when the game is paused.
        if (Time.deltaTime <= 0f) return;
        float ratio = Mathf.Clamp01((DisplayedSpeed - shakeStartSpeed) /
            Mathf.Max(1f, shakeFullSpeed - shakeStartSpeed));
        float target = ratio * ratio * Mathf.Clamp(shakeMaxPixels, 0f, 12f);
        shakeAmplitude = Mathf.SmoothDamp(shakeAmplitude, target, ref shakeVelocity,
            Mathf.Max(0.01f, shakeSmoothTime), Mathf.Infinity, Time.deltaTime);
        shakeClock = Mathf.Repeat(shakeClock + Time.deltaTime * Mathf.Clamp(shakeFrequency, 1f, 30f), 10000f);
        if (target == 0f && shakeAmplitude < 0.01f) { ResetShake(); return; }
        CurrentShakeOffset = new Vector2(
            Mathf.Clamp((Mathf.PerlinNoise(shakeClock, 7.31f) - 0.5f) * 2f, -1f, 1f),
            Mathf.Clamp((Mathf.PerlinNoise(19.77f, shakeClock) - 0.5f) * 2f, -1f, 1f)) * shakeAmplitude;
        // Move the whole HUD so text, needles, and backplates stay aligned.
        hud.style.translate = new Translate(CurrentShakeOffset.x, CurrentShakeOffset.y);
    }

    void ResetShake()
    {
        shakeAmplitude = shakeVelocity = 0f;
        CurrentShakeOffset = Vector2.zero;
        if (hud != null) hud.style.translate = StyleKeyword.Null;
    }

    void OnDisable()
    {
        ResetShake();
        SetStartupLocks(false);
        if (startupAudio != null) startupAudio.Stop();
        if (startupShield != null) startupShield.style.display = DisplayStyle.None;
        if (document != null && originalDocument != null && document.visualTreeAsset != originalDocument)
            document.visualTreeAsset = originalDocument;
    }

    void SetStartupLocks(bool locked)
    {
        if (car != null) car.ControlsLocked = locked;
        if (viewCamera != null) viewCamera.ViewInputLocked = locked;
    }

    void UpdateStartup()
    {
        if (StartupComplete) return;
        SetStartupLocks(true);
        startupClock += Time.deltaTime;
        float closeStart = Mathf.Max(0f, startupClearTime);
        float closedAt = closeStart + Mathf.Max(0.01f, shieldCloseTime);
        float powerAt = closedAt + Mathf.Max(0f, startupBlackHold);
        float progress = Mathf.Clamp01((startupClock - closeStart) / Mathf.Max(0.01f, shieldCloseTime));
        StartupShieldCoverage = Mathf.SmoothStep(0f, 1f, progress);
        if (startupClock >= closeStart && !shieldSoundPlayed)
        {
            shieldSoundPlayed = true;
            if (shieldCloseSound != null) startupAudio.PlayOneShot(shieldCloseSound, startupVolume);
        }
        if (startupClock >= powerAt && !powerSoundPlayed)
        {
            powerSoundPlayed = true;
            if (powerOnSound != null) startupAudio.PlayOneShot(powerOnSound, startupVolume);
        }
        float fade = Mathf.Clamp01((startupClock - powerAt) / Mathf.Max(0.01f, hudPowerFadeTime));
        StartupOpacity = Mathf.SmoothStep(0f, 1f, fade);
        if (startupShield != null)
        {
            startupShield.style.display = DisplayStyle.Flex;
            startupShield.style.translate = new Translate(0f, Length.Percent(-100f * (1f - Mathf.SmoothStep(0f,1f,progress))));
            startupShield.style.opacity = 1f - StartupOpacity;
        }
        if (hud != null) hud.style.opacity = StartupOpacity;
        if (fade >= 1f)
        {
            StartupComplete = true;
            if (startupShield != null) startupShield.style.display = DisplayStyle.None;
            SetStartupLocks(false);
        }
    }

    void UpdateMinimap()
    {
        if (car == null) return;
        if (minimapRoad == null)
        {
            var road = GameObject.Find("Road");
            if (road == null) return;
            minimapRoad = road.transform;
            hasRoadBounds = false;
        }
        if (!hasRoadBounds)
        {
            foreach (var mesh in minimapRoad.GetComponentsInChildren<MeshFilter>())
            {
                if (mesh.sharedMesh == null) continue;
                Bounds bounds = mesh.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 point = minimapRoad.InverseTransformPoint(mesh.transform.TransformPoint(corner));
                    if (!hasRoadBounds) { roadBounds = new Bounds(point, Vector3.zero); hasRoadBounds = true; }
                    else roadBounds.Encapsulate(point);
                }
            }
        }
        if (!hasRoadBounds) return;

        Vector3 local = minimapRoad.InverseTransformPoint(car.transform.position);
        if (finishAtRoadEnd && hasPreviousRoadPosition &&
            previousRoadPosition.z < roadBounds.max.z && local.z >= roadBounds.max.z &&
            Mathf.Abs(local.x - roadBounds.center.x) <= roadBounds.extents.x && car.IsGrounded)
        {
            float fraction = Mathf.InverseLerp(previousRoadPosition.z, local.z, roadBounds.max.z);
            RecordFinish(playerName, ElapsedSeconds - Time.deltaTime * (1f - fraction));
        }
        previousRoadPosition = local;
        hasPreviousRoadPosition = true;
        if (!lapStartResolved)
        {
            if (lapStartPoint == null) { var spawn = GameObject.Find("SpawnPoint"); if (spawn != null) lapStartPoint = spawn.transform; }
            lapStartRoadZ = Mathf.Clamp(lapStartPoint != null ? minimapRoad.InverseTransformPoint(lapStartPoint.position).z : local.z, roadBounds.min.z, roadBounds.max.z - 0.001f);
            lapStartResolved = true;
        }
        float progress = Mathf.InverseLerp(lapStartRoadZ, roadBounds.max.z, local.z);
        LapProgressPercent = CalculateLapProgressPercent(progress);
        RefreshRanking();
        var navigation = boundRoot.Q<RacingUI.NavigationMap>("navigation-road");
        if (navigation != null)
        {
            Vector3 roadForward = minimapRoad.InverseTransformDirection(car.transform.forward);
            navigation.SetVehicle((local.x - roadBounds.center.x) / Mathf.Max(0.01f, roadBounds.size.x),
                Mathf.Atan2(roadForward.x, roadForward.z) * Mathf.Rad2Deg,
                local.z * Mathf.Abs(minimapRoad.lossyScale.z));
            var remainingLabel = boundRoot.Q<Label>("route-distance");
            float remaining = Mathf.Max(0f, roadBounds.max.z - local.z) * Mathf.Abs(minimapRoad.lossyScale.z);
            if (remainingLabel != null) remainingLabel.text = remaining >= 1000f ? $"STRAIGHT  {remaining / 1000f:0.0} KM" : $"STRAIGHT  {remaining:0} M";
            return;
        }
        if (playerMarker == null) return;
        var viewport = boundRoot.Q("map-viewport");
        Vector2 start = mapStart, end = mapEnd;
        if (viewport != null && viewport.contentRect.width > 0)
        {
            start = new Vector2(viewport.contentRect.width * 0.5f, viewport.contentRect.height - 20f);
            end = new Vector2(start.x, 24f);
        }
        var distance = boundRoot.Q<Label>("route-distance");
        if (distance != null) distance.text = $"STRAIGHT  {roadBounds.size.z * minimapRoad.lossyScale.z / 1000f:0.00} KM";
        Vector2 position = Vector2.Lerp(start, end, progress);
        position.x += Mathf.Clamp((local.x - roadBounds.center.x) / Mathf.Max(0.01f, roadBounds.size.x), -2f, 2f) * mapRoadWidth;
        playerMarker.style.left = position.x - 8f;
        playerMarker.style.top = position.y - 8f;
        Vector3 forward = minimapRoad.InverseTransformDirection(car.transform.forward);
        playerMarker.style.rotate = new Rotate(new Angle(Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg));
    }

    // Pass elapsed race time, not Time.time. Repeated finish notifications are ignored.
    public void RecordFinish(string driverName, float elapsedSeconds)
    {
        if (results.Record(driverName, elapsedSeconds)) RefreshRanking();
    }

    public void FinishPlayer() => RecordFinish(playerName, ElapsedSeconds);

    public void ResetRace()
    {
        ElapsedSeconds = 0f;
        results.Clear();
        LapProgressPercent = 0f;
        lapStartResolved = false;
        hasPreviousRoadPosition = false;
        RefreshRanking();
    }

    void RefreshRanking()
    {
        if (boundRoot == null) return;
        var waiting = new List<string>();
        if (!results.Contains(playerName)) waiting.Add(playerName);
        for (int i = 0; i < 6; i++)
        {
            var row = boundRoot.Q("ranking-row-" + (i + 1));
            if (row == null) continue;
            bool finished = i < results.Count;
            int waitingIndex = i - results.Count;
            string name = finished ? results.NameAt(i) : waitingIndex < waiting.Count ? waiting[waitingIndex] : "---";
            row.Q<Label>(className: "rank-pos").text = (i + 1).ToString("00");
            row.Q<Label>(className: "rank-driver").text = name;
            row.Q<Label>(className: "rank-time").text = finished ? "100%" : name == playerName ? Mathf.FloorToInt(LapProgressPercent).ToString() + "%" : "--%";
            row.EnableInClassList("ranking-player", i == 0);
        }
    }
}

// Millisecond precision is shared by sorting, winner time and gaps.
public sealed class RaceFinishResults
{
    readonly List<KeyValuePair<string, long>> entries = new List<KeyValuePair<string, long>>();
    public int Count => entries.Count;
    public void Clear() => entries.Clear();
    public bool Contains(string name) => entries.Exists(entry => entry.Key == name);
    public bool Record(string name, double seconds)
    {
        if (string.IsNullOrWhiteSpace(name) || double.IsNaN(seconds) || double.IsInfinity(seconds) ||
            seconds < 0 || seconds > 315360000 || Contains(name)) return false;
        long milliseconds = (long)Math.Round(seconds * 1000, MidpointRounding.AwayFromZero);
        int index = entries.FindIndex(entry => entry.Value > milliseconds);
        entries.Insert(index < 0 ? entries.Count : index, new KeyValuePair<string, long>(name, milliseconds));
        return true;
    }
    public string NameAt(int index) => entries[index].Key;
    public string TimeAt(int index) => (index == 0 ? "" : "+") + Format(
        entries[index].Value - (index == 0 ? 0 : entries[0].Value));
    static string Format(long ms) => $"{ms / 60000:00}:{ms / 1000 % 60:00}.{ms % 1000:000}";
}

// Geometry scales with the named UI Builder elements; no screenshot backgrounds.
public static class RacingHudArtwork
{
    static readonly CustomStyleProperty<Color> Accent = new CustomStyleProperty<Color>("--hud-accent");
    static readonly CustomStyleProperty<Color> Secondary = new CustomStyleProperty<Color>("--hud-secondary");
    public static void Attach(VisualElement root)
    {
        if (root.ClassListContains("photo-hud") || root.ClassListContains("artwork-bound")) return;
        root.AddToClassList("artwork-bound");
        Color a = new Color(0.95f,0.81f,0.45f), b = a;
        root.RegisterCallback<CustomStyleResolvedEvent>(evt => {
            root.customStyle.TryGetValue(Accent, out a); root.customStyle.TryGetValue(Secondary, out b);
            root.Query<VisualElement>().ForEach(e => e.MarkDirtyRepaint());
        });
        root.Query<VisualElement>(className:"panel").ForEach(e => e.generateVisualContent += ctx => Panel(ctx.painter2D,e.contentRect,a,b));
        var visor = root.Q("visor-frame");
        if (visor != null) visor.generateVisualContent += ctx => Visor(ctx.painter2D,visor.contentRect,a,b);
        var map = root.Q("map-viewport");
        if (map != null) map.generateVisualContent += ctx => {
            var p=ctx.painter2D; p.strokeColor=new Color(a.r,a.g,a.b,0.15f);p.lineWidth=0.6f;
            for(float x=8;x<map.contentRect.width;x+=22) Line(p,new Vector2(x,0),new Vector2(x,map.contentRect.height));
            for(float y=8;y<map.contentRect.height;y+=22) Line(p,new Vector2(0,y),new Vector2(map.contentRect.width,y));
        };
    }
    static void Line(Painter2D p,Vector2 a,Vector2 b) { p.BeginPath();p.MoveTo(a);p.LineTo(b);p.Stroke(); }
    static void Shape(Painter2D p,Vector2[] points,bool fill)
    { p.BeginPath();p.MoveTo(points[0]);for(int i=1;i<points.Length;i++)p.LineTo(points[i]);p.ClosePath();if(fill)p.Fill();else p.Stroke(); }
    static Vector2[] Bevel(Rect r,float inset,float cut) => new[]{new Vector2(r.xMin+inset+cut,r.yMin+inset),new Vector2(r.xMax-inset-cut,r.yMin+inset),new Vector2(r.xMax-inset,r.yMin+inset+cut),new Vector2(r.xMax-inset,r.yMax-inset-cut),new Vector2(r.xMax-inset-cut,r.yMax-inset),new Vector2(r.xMin+inset+cut,r.yMax-inset),new Vector2(r.xMin+inset,r.yMax-inset-cut),new Vector2(r.xMin+inset,r.yMin+inset+cut)};
    static void Panel(Painter2D p,Rect r,Color a,Color b)
    {
        // Expand into padding so children remain inset from the decorative bevel.
        r=new Rect(r.x-12,r.y-12,r.width+24,r.height+24);
        // The editable UXML backplate supplies the fill; avoid stacking two 70% layers.
        p.strokeColor=new Color(a.r,a.g,a.b,0.22f);p.lineWidth=5;Shape(p,Bevel(r,2,18),false);
        p.strokeColor=a;p.lineWidth=1.2f;Shape(p,Bevel(r,4,18),false);
        p.strokeColor=new Color(b.r,b.g,b.b,0.5f);p.lineWidth=0.7f;Shape(p,Bevel(r,9,15),false);
        p.strokeColor=b;p.lineWidth=3;Line(p,new Vector2(r.xMax-75,r.yMax-4),new Vector2(r.xMax-25,r.yMax-4));
        p.strokeColor=new Color(a.r,a.g,a.b,0.06f);p.lineWidth=1;
        for(float x=r.xMin+25;x<r.xMax-25;x+=12)Line(p,new Vector2(x,r.yMin+13),new Vector2(Mathf.Min(x+30,r.xMax-15),r.yMin+33));
        p.strokeColor=a;p.lineWidth=2;
        for(int i=0;i<4;i++)Line(p,new Vector2(r.xMin+25+i*9,r.yMin+4),new Vector2(r.xMin+30+i*9,r.yMin+4));
    }
    static void Visor(Painter2D p,Rect r,Color a,Color b)
    {
        // Separate open side rails keep the center of the road unobstructed.
        for(int side=0;side<2;side++)
        {
            Color c=side==0?a:b; float w=r.width,h=r.height;
            Func<float,float,float,Vector2> pt=(x,y,z)=>new Vector2(side==0?x:w-x,y);
            var points=new[]{pt(0,0,0),pt(0,h,0),pt(w*.23f,h,0),pt(w*.27f,h*.86f,0),pt(90,h*.88f,0),pt(42,h*.72f,0),pt(64,h*.30f,0),pt(24,h*.22f,0),pt(36,70,0),pt(95,28,0),pt(w*.35f,35,0),pt(w*.35f,0,0)};
            p.fillColor=new Color(0,0.008f,0.016f,.3f);Shape(p,points,true);
            p.strokeColor=new Color(c.r,c.g,c.b,.2f);p.lineWidth=7;Shape(p,points,false);
            p.strokeColor=c;p.lineWidth=1.3f;Shape(p,points,false);
            for(int i=0;i<42;i++) { float y=h*.33f+i*h*.009f; float x=48+Mathf.Sin(i/41f*Mathf.PI)*10;
                p.strokeColor=new Color(c.r,c.g,c.b,i%5==0?.95f:.4f);p.lineWidth=i%5==0?2:1;Line(p,pt(x,y,0),pt(x+(i%5==0?15:7),y,0)); }
        }
    }
    static readonly int[] Masks={63,6,91,79,102,109,125,7,127,111};
    public static void Digits(Painter2D p,Rect r,string text,Color c, float offOpacity, float thickness, float spacing)
    {
        if(string.IsNullOrEmpty(text)||r.height<=0)return;
        float units=0;foreach(char ch in text)units+=(ch==':'||ch=='.')?.35f:1f;
        float cell=Mathf.Min(r.width/units,r.height*.62f), h=Mathf.Min(r.height,cell*1.7f),x=r.x+(r.width-cell*units)/2,y=r.y+(r.height-h)/2;
        foreach(char ch in text)
        {
            if(ch==':'||ch=='.') {p.fillColor=c;float cy=ch=='.'?h*.9f:h*.35f;Box(p,x+cell*.12f,y+cy,cell*.08f,cell*.08f);if(ch==':')Box(p,x+cell*.12f,y+h*.7f,cell*.08f,cell*.08f);x+=cell*.35f;continue;}
            int mask=ch>='0'&&ch<='9'?Masks[ch-'0']:ch=='-'?64:0;
            float t=cell*thickness,w=cell*(1f-spacing);
            for(int i=0;i<7;i++)
            {
                bool on=(mask&(1<<i))!=0;p.fillColor=on?c:new Color(c.r,c.g,c.b,c.a*offOpacity);
                if(i==0||i==3||i==6){float yy=y+(i==0?0:i==3?h-t:h*.5f-t*.5f);Shape(p,new[]{new Vector2(x+t,yy),new Vector2(x+w-t,yy),new Vector2(x+w,yy+t*.5f),new Vector2(x+w-t,yy+t),new Vector2(x+t,yy+t),new Vector2(x,yy+t*.5f)},true);}
                else {float xx=x+((i==1||i==2)?w-t:0),yy=y+((i==1||i==5)?t:h*.5f+t*.3f);Box(p,xx,yy,t,h*.5f-t*1.3f);}
            }
            x+=cell;
        }
    }
    static void Box(Painter2D p,float x,float y,float w,float h) {Shape(p,new[]{new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h)},true);}
}


namespace RacingUI
{
    /// <summary>Seven-segment UXML element rendered in UI Builder, Edit mode and Play mode.</summary>
    [UxmlElement]
    public partial class DigitalReadout : VisualElement
    {
        Label glyphs;
        string displayText = "000";
        float offOpacity = 0.055f, thickness = 0.11f, spacing = 0.17f;
        bool fitDigits, fitDirty = true;
        Vector2 fittedSize;
        float fittedFontSize;
        IVisualElementScheduledItem glyphFitTask;

        [UxmlAttribute]
        public bool fitToBounds
        {
            get => fitDigits;
            set
            {
                if (fitDigits == value) return;
                fitDigits = value;
                fitDirty = true;
                if (value)
                {
                    style.overflow = Overflow.Hidden;
                    glyphFitTask?.Resume();
                    FitGlyphs();
                }
                else
                {
                    glyphFitTask?.Pause();
                    style.overflow = StyleKeyword.Null;
                    if (glyphs != null) glyphs.style.scale = StyleKeyword.Null;
                }
            }
        }

        [UxmlAttribute]
        public string text
        {
            get => displayText;
            set
            {
                if (displayText == value) return;
                displayText = value ?? "";
                if (glyphs != null) glyphs.text = displayText;
                fitDirty = true;
                FitGlyphs();
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute]
        public float offSegmentOpacity
        {
            get => offOpacity;
            set { offOpacity = Mathf.Clamp01(value); MarkDirtyRepaint(); }
        }

        [UxmlAttribute]
        public float segmentThickness
        {
            get => thickness;
            set { thickness = Mathf.Clamp(value, 0.04f, 0.18f); MarkDirtyRepaint(); }
        }

        [UxmlAttribute]
        public float digitSpacing
        {
            get => spacing;
            set { spacing = Mathf.Clamp(value, 0.04f, 0.3f); MarkDirtyRepaint(); }
        }

        public DigitalReadout()
        {
            pickingMode = PickingMode.Position;
            var font = Resources.Load<Font>("RacingHud/DS-DIGI");
            if (font != null)
            {
                glyphs = new Label(displayText) { pickingMode = PickingMode.Ignore };
                glyphs.AddToClassList("digital-glyphs");
                glyphs.style.position = Position.Absolute;
                glyphs.style.left = glyphs.style.right = glyphs.style.top = glyphs.style.bottom = 0;
                glyphs.style.marginLeft = glyphs.style.marginRight = glyphs.style.marginTop = glyphs.style.marginBottom = 0;
                glyphs.style.paddingLeft = glyphs.style.paddingRight = glyphs.style.paddingTop = glyphs.style.paddingBottom = 0;
                glyphs.style.unityTextAlign = TextAnchor.MiddleCenter;
                glyphs.style.whiteSpace = WhiteSpace.NoWrap;
                glyphs.style.overflow = Overflow.Visible;
                glyphs.style.unityFont = font;
                glyphs.style.unityFontDefinition = FontDefinition.FromFont(font);
                Add(glyphs);
                // The scheduler also catches font-size edits in UI Builder; detached trees pause it.
                glyphFitTask = schedule.Execute(FitGlyphs).Every(100);
                glyphFitTask.Pause();
            }
            else generateVisualContent += Draw;
            RegisterCallback<GeometryChangedEvent>(_ => { FitGlyphs(); MarkDirtyRepaint(); });
            RegisterCallback<CustomStyleResolvedEvent>(_ => { fitDirty = true; FitGlyphs(); MarkDirtyRepaint(); });
        }

        void FitGlyphs()
        {
            if (!fitDigits || glyphs == null || panel == null) return;
            Vector2 size = contentRect.size;
            float fontSize = glyphs.resolvedStyle.fontSize;
            if (!(size.x > 4f) || !(size.y > 4f) || !(fontSize > 0f)) return;
            if (!fitDirty && fittedSize == size && Mathf.Approximately(fittedFontSize, fontSize)) return;
            Vector2 measured = glyphs.MeasureTextSize(displayText,
                0f, MeasureMode.Undefined, 0f, MeasureMode.Undefined);
            if (!(measured.x > 0f) || !(measured.y > 0f)) return;
            float fit = Mathf.Min(1f, Mathf.Min((size.x - 4f) / measured.x, (size.y - 4f) / measured.y));
            // Scale about the centre, keeping the entire number readable and inside its own box.
            glyphs.style.scale = new Scale(new Vector3(fit, fit, 1f));
            fittedSize = size;
            fittedFontSize = fontSize;
            fitDirty = false;
        }

        void Draw(MeshGenerationContext context)
        {
            Rect area = contentRect;
            // Font Size in UI Builder controls glyph height, bounded by the element's size.
            float height = Mathf.Min(area.height, Mathf.Max(1f, resolvedStyle.fontSize));
            area.y += (area.height - height) * 0.5f;
            area.height = height;
            RacingHudArtwork.Digits(context.painter2D, area, text, resolvedStyle.color,
                offOpacity, thickness, spacing);
        }
    }
}
