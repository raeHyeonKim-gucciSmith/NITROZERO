using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

namespace Damin.VFX.TireSmoke
{
    /// <summary>Smoke-only follower. Does not drive or rotate the vehicle/wheels.</summary>
    [DisallowMultipleComponent]
    public sealed class TireSmokeWheelController : MonoBehaviour
    {
        [Serializable]
        public sealed class WheelSlot
        {
            public Transform wheel;
            [Tooltip("Optional non-spinning center marker. Overrides renderer bounds and wheel pivot.")]
            public Transform centerPoint;
            [Tooltip("Use a renderer on the assigned tire to support imported meshes with an offset pivot.")]
            public bool useRendererCenter = true;
            [Tooltip("Optional non-spinning steering/knuckle reference. Do not assign the spinning tire here.")]
            public Transform directionReference;
            [Min(0.01f)] public float radius = 0.35f;
            [Tooltip("Meters, in the vehicle-aligned smoke frame: X right, Y up, Z forward.")]
            public Vector3 positionOffset;
            [Range(0f, 2f)] public float intensity = 1f;
            public bool emit = true;
        }

        [Header("References / 연결")]
        [Tooltip("Assign the target vehicle root here. This controller belongs on the smoke parent, NOT on the car.")]
        [SerializeField] private Transform vehicleRoot;
        [SerializeField] private GameObject smokePrefab;
        [Tooltip("While wheels are connected in Play mode, hide only the source smoke renderers on this object. Restore them when disconnected or disabled.")]
        [SerializeField] private bool hideSourceSmokeWhenBound = true;
        [Header("Wheels / 타이어를 드래그")]
        [SerializeField] private WheelSlot frontLeft = new WheelSlot();
        [SerializeField] private WheelSlot frontRight = new WheelSlot();
        [SerializeField] private WheelSlot rearLeft = new WheelSlot();
        [SerializeField] private WheelSlot rearRight = new WheelSlot();
        [Header("Direction / 차량 기준 방향")]
        [SerializeField] private Vector3 vehicleForwardAxis = Vector3.forward;
        [SerializeField] private Vector3 vehicleUpAxis = Vector3.up;
        [SerializeField] private bool worldUp = true;
        [Header("Smoke / 연출")]
        [SerializeField] private bool emitSmoke = true;
        [Range(0f, 2f)] [SerializeField] private float intensity = 1f;
        [Min(0f)] [SerializeField] private float fadeTime = 0.3f;
        [Tooltip("Radius represented by the authored Final smoke, before scaling. Do not change to fit each wheel; use its Radius instead.")]
        [Min(0.01f)] [SerializeField] private float referenceWheelRadius = 0.65f;
        [Min(0.01f)] [SerializeField] private float sizeMultiplier = 1f;
        [Tooltip("Use the controller object's layer for spawned smoke (avoids test-only Layer 30).")]
        [SerializeField] private bool useControllerLayer = true;

        private sealed class Follower
        {
            public WheelSlot slot;
            public Transform wheel;
            public Renderer renderer;
            public GameObject root;
            public Vector3 authoredScale;
            public VisualEffect[] effects;
            public float[] rates;
            public float strength;
        }
        private readonly List<Follower> followers = new List<Follower>();
        private static readonly int EmissionRate = Shader.PropertyToID("EmissionRate");
        private GameObject builtPrefab;
        private Transform builtVehicleRoot;
        private readonly Dictionary<Renderer, bool> sourceRenderers = new Dictionary<Renderer, bool>();
        public int SpawnedWheelCount => followers.Count;
        private WheelSlot[] Slots => new[] { frontLeft, frontRight, rearLeft, rearRight };
        private Transform Frame => vehicleRoot;

        private void OnEnable() { if (Application.isPlaying) RebuildSmoke(); }
        private void OnDisable() { ClearSmoke(); }
        private void OnDestroy() { ClearSmoke(); }

        /// <summary>Future driving code can call this using slip/brake/burnout intensity.</summary>
        public void SetIntensity(float value) { intensity = Mathf.Clamp(value, 0f, 2f); }
        public void SetEmission(bool value) { emitSmoke = value; }

        [ContextMenu("Rebuild Wheel Smoke (Play Mode)")]
        public void RebuildSmoke()
        {
            if (!Application.isPlaying) return;
            ClearSmoke();
            builtPrefab = smokePrefab;
            builtVehicleRoot = vehicleRoot;
            if (!smokePrefab || !vehicleRoot) return;
            var used = new HashSet<Transform>();
            string[] labels = { "FrontLeft", "FrontRight", "RearLeft", "RearRight" };
            var slots = Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || !slot.wheel || !used.Add(slot.wheel)) continue;
                // Deliberately unparented: vehicle scale and wheel spin cannot rotate/scale the smoke frame.
                var root = Instantiate(smokePrefab);
                root.name = "TireSmoke_" + labels[i] + " (Runtime)";
                SceneManager.MoveGameObjectToScene(root, gameObject.scene);
                if (useControllerLayer)
                    foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
                var fx = root.GetComponentsInChildren<VisualEffect>(true);
                var f = new Follower { slot = slot, wheel = slot.wheel, root = root,
                    renderer = slot.wheel.GetComponent<Renderer>() ?? slot.wheel.GetComponentInChildren<Renderer>(),
                    authoredScale = root.transform.localScale, effects = fx, rates = new float[fx.Length] };
                for (int n = 0; n < fx.Length; n++)
                {
                    f.rates[n] = fx[n].HasFloat(EmissionRate) ? fx[n].GetFloat(EmissionRate) : -1f;
                    if (f.rates[n] >= 0f) fx[n].SetFloat(EmissionRate, 0f);
                }
                followers.Add(f);
                UpdatePose(f);
                foreach (var effect in fx) effect.Reinit();
            }
            UpdateSourceVisibility();
        }

        private void LateUpdate()
        {
            if (builtPrefab != smokePrefab || builtVehicleRoot != vehicleRoot || (smokePrefab && vehicleRoot && ReferencesChanged())) RebuildSmoke();
            UpdateSourceVisibility();
            foreach (var f in followers)
            {
                if (!f.root || !f.wheel) continue;
                UpdatePose(f);
                float target = emitSmoke && f.slot.emit && f.wheel.gameObject.activeInHierarchy
                    ? intensity * f.slot.intensity : 0f;
                f.strength = fadeTime <= 0f ? target : Mathf.MoveTowards(f.strength, target, Time.deltaTime * 4f / fadeTime);
                for (int i = 0; i < f.effects.Length; i++)
                    if (f.effects[i] && f.rates[i] >= 0f)
                        f.effects[i].SetFloat(EmissionRate, f.rates[i] * f.strength);
            }
        }

        private bool ReferencesChanged()
        {
            var used = new HashSet<Transform>();
            int n = 0;
            foreach (var slot in Slots)
            {
                if (slot == null || !slot.wheel || !used.Add(slot.wheel)) continue;
                if (n >= followers.Count || followers[n].wheel != slot.wheel || followers[n].slot != slot || !followers[n].root) return true;
                n++;
            }
            return n != followers.Count;
        }

        private void UpdatePose(Follower f)
        {
            var frame = Frame;
            Vector3 up = worldUp ? Vector3.up : frame.TransformDirection(SafeAxis(vehicleUpAxis, Vector3.up)).normalized;
            var direction = f.slot.directionReference ? f.slot.directionReference : frame;
            Vector3 forward = Vector3.ProjectOnPlane(direction.TransformDirection(SafeAxis(vehicleForwardAxis, Vector3.forward)), up);
            if (forward.sqrMagnitude < 0.00001f) forward = Vector3.Cross(up, Mathf.Abs(up.x) < 0.9f ? Vector3.right : Vector3.forward);
            Quaternion rotation = Quaternion.LookRotation(forward.normalized, up);
            Vector3 center = f.slot.centerPoint ? f.slot.centerPoint.position :
                (f.slot.useRendererCenter && f.renderer ? f.renderer.bounds.center : f.wheel.position);
            float radius = Mathf.Max(0.01f, f.slot.radius);
            f.root.transform.SetPositionAndRotation(center - up * radius + rotation * f.slot.positionOffset, rotation);
            f.root.transform.localScale = f.authoredScale * (radius / Mathf.Max(0.01f, referenceWheelRadius)) * Mathf.Max(0.01f, sizeMultiplier);
        }

        private static Vector3 SafeAxis(Vector3 axis, Vector3 fallback) => axis.sqrMagnitude > 0.00001f ? axis.normalized : fallback;

        private void ClearSmoke()
        {
            foreach (var f in followers)
                if (f.root) { f.root.SetActive(false); if (Application.isPlaying) Destroy(f.root); else DestroyImmediate(f.root); }
            followers.Clear();
            RestoreSourceVisibility();
        }

        private void UpdateSourceVisibility()
        {
            if (!hideSourceSmokeWhenBound || followers.Count == 0) { RestoreSourceVisibility(); return; }
            foreach (var effect in GetComponentsInChildren<VisualEffect>(true))
            {
                var renderer = effect.GetComponent<Renderer>();
                if (!renderer || sourceRenderers.ContainsKey(renderer)) continue;
                sourceRenderers.Add(renderer, renderer.enabled);
                renderer.enabled = false;
            }
        }

        private void RestoreSourceVisibility()
        {
            foreach (var entry in sourceRenderers) if (entry.Key) entry.Key.enabled = entry.Value;
            sourceRenderers.Clear();
        }
    }
}
