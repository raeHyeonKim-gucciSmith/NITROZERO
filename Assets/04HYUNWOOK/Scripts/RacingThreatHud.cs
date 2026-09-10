using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent, RequireComponent(typeof(RacingHudController))]
public sealed class RacingThreatHud : MonoBehaviour
{
    [Flags] public enum MissileDirection { None=0, Left=1, Right=2, Both=3 }
    [Tooltip("차량 중심선 부근의 적/미사일은 양쪽에 표시합니다.")]
    [Min(0)] public float centerDirectionWidth = .75f;
    [Header("Incoming missile / synchronized 3 second warning")]
    [Min(1)] public float missileWarningDistance = 120f;
    [Tooltip("느낌표 효과음: 미사일 경고 시작 시 1초 간격으로 3번 재생합니다.")]
    public AudioClip proximityAlarm;
    public AudioClip missileAlarm;
    [Range(0,1)] public float warningVolume = .65f;
    [Header("Keys: 1 Left missile / 2 Right missile")]
    public bool enableWarningShortcuts = true;
    public const float WarningDuration = 3f;
    public float LeftTimeRemaining { get; private set; }
    public float RightTimeRemaining { get; private set; }
    public float ProximityTimeRemaining => Mathf.Max(LeftTimeRemaining,RightTimeRemaining);
    public float MissileTimeRemaining { get; private set; }
    public int ScheduledAlarmCount { get; private set; }
    public int MissileSoundCount { get; private set; }
    public MissileDirection IncomingDirection { get; private set; }

    RacingHudController hud;
    UIDocument document;
    VisualElement root, left, right, missile;
    Label missileDistance;
    readonly AudioSource[] pulses = new AudioSource[3];
    AudioSource missileSource;
    readonly Dictionary<int,float> lastMissileDistances = new Dictionary<int,float>();
    readonly HashSet<int> currentMissiles = new HashSet<int>();
    readonly HashSet<int> warnedMissiles = new HashSet<int>();
    readonly HashSet<int> trackedMissiles = new HashSet<int>();
    readonly List<int> staleMissiles = new List<int>();
    float currentMissileDistance;
    bool scalarMissileLatched;

    void Awake() { hud=GetComponent<RacingHudController>(); document=GetComponent<UIDocument>(); }
    void OnEnable() { EnsureAudio(); Bind(); }
    AudioSource MakeAudio()
    {
        var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.loop=false;
        s.spatialBlend=0;s.volume=warningVolume;return s;
    }
    void EnsureAudio()
    {
        if(!Application.isPlaying)return;
        for(int i=0;i<pulses.Length;i++)if(pulses[i]==null)pulses[i]=MakeAudio();
        if(missileSource==null)missileSource=MakeAudio();
    }
    void Bind()
    {
        if(document==null)document=GetComponent<UIDocument>();
        root=document.rootVisualElement;
        left=root.Q("enemy-warning-left");right=root.Q("enemy-warning-right");
        missile=root.Q("missile-warning");missileDistance=root.Q<Label>("missile-distance");
        ApplyVisuals();
    }
    void Update()
    {
        if(document==null)return;
        if(root!=document.rootVisualElement || left==null || left.panel==null)Bind();
        if(hud==null)hud=GetComponent<RacingHudController>();
        if(!hud.StartupComplete || hud.car==null)
        {
            ApplyVisuals();return;
        }
        if(Time.deltaTime<=0)return;
        ReadWarningShortcuts();EvaluateThreats(Time.deltaTime);
    }

    // World-space directions are independent of the selected first/third-person view.
    public void EvaluateThreats(float deltaSeconds)
    {
        if(hud==null)hud=GetComponent<RacingHudController>();
        if(hud.car==null)return;
        TickWarning(deltaSeconds);
        float nearestMissile=float.PositiveInfinity;
        MissileDirection directions=MissileDirection.None;
        currentMissiles.Clear();trackedMissiles.Clear();
        var player=hud.car.transform;
        foreach(var target in RacingThreatTarget.Active)
        {
            if(target==null || !target.activeThreat || target.transform.IsChildOf(player) || player.IsChildOf(target.transform))continue;
            if(target.kind==RacingThreatTarget.ThreatKind.EnemyVehicle)continue;
            Vector3 offset=target.transform.position-player.position;
            float distance=offset.magnitude;
            Vector3 local=player.InverseTransformDirection(offset);
            var side=DirectionForLocalX(local.x,centerDirectionWidth);
            int id=target.GetInstanceID();trackedMissiles.Add(id);
            bool approaching=lastMissileDistances.TryGetValue(id,out float prior)&&distance<prior-.001f;
            lastMissileDistances[id]=distance;
            bool targeted=target.intendedTarget!=null && (target.intendedTarget==player || target.intendedTarget.IsChildOf(player) || player.IsChildOf(target.intendedTarget));
            bool eligible=target.intendedTarget==null ? approaching : targeted;
            if(!eligible || distance>missileWarningDistance)continue;
            currentMissiles.Add(id);directions|=side;nearestMissile=Mathf.Min(nearestMissile,distance);
        }
        staleMissiles.Clear();
        foreach(int id in lastMissileDistances.Keys)if(!trackedMissiles.Contains(id))staleMissiles.Add(id);
        foreach(int id in staleMissiles)lastMissileDistances.Remove(id);
        warnedMissiles.RemoveWhere(id=>!currentMissiles.Contains(id));
        bool newMissile=false;
        foreach(int id in currentMissiles)if(warnedMissiles.Add(id))newMissile=true;
        if(newMissile && MissileTimeRemaining<=0)StartMissileWarning(directions,nearestMissile);
        else if(MissileTimeRemaining>0 && directions!=MissileDirection.None)
        {
            IncomingDirection=directions;currentMissileDistance=nearestMissile;
        }
        ApplyVisuals();
    }
    public static MissileDirection DirectionForLocalX(float x,float centerWidth)
        => Mathf.Abs(x)<=Mathf.Max(0,centerWidth) ? MissileDirection.Both : x<0 ? MissileDirection.Left : MissileDirection.Right;

    // Retain the preview-tool signature; enemy distance no longer controls any HUD element.
    public void Advance(float deltaSeconds,float enemyMetres,float missileMetres)
    {
        TickWarning(deltaSeconds);
        if(missileMetres>missileWarningDistance)scalarMissileLatched=false;
        if(missileMetres<=missileWarningDistance && !scalarMissileLatched)
        { scalarMissileLatched=true;StartMissileWarning(MissileDirection.Both,missileMetres); }
        ApplyVisuals();
    }
    void TickWarning(float seconds) => MissileTimeRemaining=Mathf.Max(0,MissileTimeRemaining-Mathf.Max(0,seconds));
    void ReadWarningShortcuts()
    {
        if(!enableWarningShortcuts)return;
#if ENABLE_INPUT_SYSTEM
        var keys=Keyboard.current;if(keys==null)return;
        if(keys.digit1Key.wasPressedThisFrame || keys.numpad1Key.wasPressedThisFrame)TriggerShortcut(1);
        if(keys.digit2Key.wasPressedThisFrame || keys.numpad2Key.wasPressedThisFrame)TriggerShortcut(2);
#elif ENABLE_LEGACY_INPUT_MANAGER
        if(Input.GetKeyDown(KeyCode.Alpha1)||Input.GetKeyDown(KeyCode.Keypad1))TriggerShortcut(1);
        if(Input.GetKeyDown(KeyCode.Alpha2)||Input.GetKeyDown(KeyCode.Keypad2))TriggerShortcut(2);
#endif
    }
    public void TriggerShortcut(int number)
    {
        if(!Application.isPlaying)return;
        if(number==1)ShowLeftWarning();else if(number==2)ShowRightWarning();
    }
    [ContextMenu("Preview Left Missile (Play Mode)")]
    public void ShowLeftWarning()=>StartMissileWarning(MissileDirection.Left,0);
    [ContextMenu("Preview Right Missile (Play Mode)")]
    public void ShowRightWarning()=>StartMissileWarning(MissileDirection.Right,0);
    [ContextMenu("Preview Center / Both Missiles (Play Mode)")]
    public void ShowMissileWarning()=>StartMissileWarning(MissileDirection.Both,0);
    [Obsolete("Use ShowMissileWarning to preview an alert.")]
    public void ShowProximityWarning()=>ShowMissileWarning();
    void StartMissileWarning(MissileDirection direction,float distance)
    {
        if(!Application.isPlaying || direction==MissileDirection.None)return;
        EnsureAudio();IncomingDirection=direction;currentMissileDistance=distance;MissileTimeRemaining=WarningDuration;
        ScheduledAlarmCount=0;double at=AudioSettings.dspTime+.025;
        for(int i=0;i<pulses.Length;i++)
        {
            var s=pulses[i];s.Stop();s.volume=warningVolume;s.clip=proximityAlarm;
            if(proximityAlarm!=null){s.PlayScheduled(at+i);ScheduledAlarmCount++;}
        }
        missileSource.Stop();missileSource.volume=warningVolume;
        if(missileAlarm!=null){missileSource.PlayOneShot(missileAlarm);MissileSoundCount++;}
        ApplyVisuals();
    }
    public static float PulseScale(float elapsed)=>.775f+.225f*Mathf.Cos(Mathf.PI*2f*elapsed);
    void ApplyVisuals()
    {
        LeftTimeRemaining=(IncomingDirection&MissileDirection.Left)!=0?MissileTimeRemaining:0;
        RightTimeRemaining=(IncomingDirection&MissileDirection.Right)!=0?MissileTimeRemaining:0;
        ApplySide(left,LeftTimeRemaining);ApplySide(right,RightTimeRemaining);
        if(missile!=null)missile.style.display=MissileTimeRemaining>0?DisplayStyle.Flex:DisplayStyle.None;
        string side=IncomingDirection==MissileDirection.Left?"LEFT":IncomingDirection==MissileDirection.Right?"RIGHT":"CENTER / BOTH SIDES";
        if(missileDistance!=null)missileDistance.text=currentMissileDistance>0?$"{side}  •  {currentMissileDistance:0} m":$"{side}  •  TAKE EVASIVE ACTION";
    }
    static void ApplySide(VisualElement element,float remaining)
    {
        if(element==null)return;
        element.style.display=remaining>0?DisplayStyle.Flex:DisplayStyle.None;
        float scale=PulseScale(WarningDuration-remaining);
        element.style.scale=new Scale(Vector3.one*scale);element.style.opacity=.35f+.65f*(scale-.55f)/.45f;
    }
    void OnDisable()
    {
        foreach(var s in pulses)if(s!=null)s.Stop();if(missileSource!=null)missileSource.Stop();
        MissileTimeRemaining=0;IncomingDirection=MissileDirection.None;scalarMissileLatched=false;
        lastMissileDistances.Clear();warnedMissiles.Clear();currentMissiles.Clear();
        ApplyVisuals();
    }
}
