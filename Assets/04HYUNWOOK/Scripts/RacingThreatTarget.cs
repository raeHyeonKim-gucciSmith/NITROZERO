using System.Collections.Generic;
using UnityEngine;

/// <summary>Attach to an enemy vehicle or an active incoming missile. No tag/layer setup is required.</summary>
[DisallowMultipleComponent]
public sealed class RacingThreatTarget : MonoBehaviour
{
    public enum ThreatKind { EnemyVehicle, IncomingMissile }
    public ThreatKind kind;
    [Tooltip("Missile target. If empty, the HUD also checks that the missile is approaching.")]
    public Transform intendedTarget;
    public bool activeThreat = true;
    static readonly HashSet<RacingThreatTarget> active = new HashSet<RacingThreatTarget>();
    public static IEnumerable<RacingThreatTarget> Active => active;
    void OnEnable() => active.Add(this);
    void OnDisable() => active.Remove(this);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRegistry() => active.Clear();
}
