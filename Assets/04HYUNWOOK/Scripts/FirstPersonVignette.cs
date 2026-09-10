using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Blend this Volume's authored helmet effects in only for first-person driving.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Volume))]
public sealed class FirstPersonVignette : MonoBehaviour
{
    [Tooltip("T 시점 전환을 담당하는 카메라입니다. 비워두면 같은 씬에서 자동으로 찾습니다.")]
    public CarCinemachineSetup viewCamera;

    [Tooltip("헬멧 착용과 UI 전원 연출을 담당합니다. 비워두면 같은 씬에서 자동으로 찾습니다.")]
    public RacingHudController helmetHud;

    Volume volume, viewOverrides;
    VolumeProfile previousProfile, runtimeProfile, overrideProfile;
    LensDistortion lensBlocker;
    Vignette vignetteOverride;

#if UNITY_EDITOR
    // Preview uses the same temporary profile as play mode and never edits the authored asset.
    public void PreviewHelmetEffects(bool firstPerson)
    {
        if (Application.isPlaying) return;
        if (runtimeProfile == null) OnEnable();
        ApplyVignette(firstPerson ? 1f : 0f, firstPerson);
    }

    public void EndHelmetPreview()
    {
        if (!Application.isPlaying) OnDisable();
    }
#endif

    void OnEnable()
    {
        if (runtimeProfile != null) OnDisable();
        volume = GetComponent<Volume>();
        previousProfile = volume.HasInstantiatedProfile() ? volume.profile : null;
        var source = previousProfile != null ? previousProfile : volume.sharedProfile;
        if (source == null) return;

        // Inspector edits this private copy. Never write animation values into it.
        runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        runtimeProfile.name = source.name + " (Play Mode Settings)";
        runtimeProfile.hideFlags = HideFlags.DontSave;
        foreach (var component in source.components)
        {
            if (component == null) continue;
            var copy = Instantiate(component);
            copy.hideFlags = HideFlags.DontSave;
            runtimeProfile.components.Add(copy);
        }
        volume.profile = runtimeProfile;

        // A separate higher-priority volume suppresses effects in TPS. It is never
        // exposed as the editable Global Volume profile and is destroyed on exit.
        var gate = new GameObject("Helmet View Overrides") { hideFlags = HideFlags.HideAndDontSave };
        gate.SetActive(false);
        gate.transform.SetParent(transform, false);
        gate.layer = gameObject.layer;
        viewOverrides = gate.AddComponent<Volume>();
        viewOverrides.isGlobal = true;
        viewOverrides.weight = 1f;
        viewOverrides.priority = volume.priority + 1f;
        overrideProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        overrideProfile.hideFlags = HideFlags.HideAndDontSave;
        lensBlocker = overrideProfile.Add<LensDistortion>();
        lensBlocker.intensity.Override(0f);
        lensBlocker.scale.Override(1f);
        vignetteOverride = overrideProfile.Add<Vignette>();
        viewOverrides.sharedProfile = overrideProfile;
        ApplyVignette(0f, false);
        gate.SetActive(true);
    }

    void LateUpdate()
    {
        if (runtimeProfile == null) return;
        if (viewCamera == null)
        {
            foreach (var candidate in FindObjectsByType<CarCinemachineSetup>(FindObjectsSortMode.None))
            {
                if (candidate.isActiveAndEnabled && candidate.gameObject.scene == gameObject.scene)
                {
                    viewCamera = candidate;
                    break;
                }
            }
        }
        if (helmetHud == null)
        {
            foreach (var candidate in FindObjectsByType<RacingHudController>(FindObjectsSortMode.None))
            {
                if (candidate.isActiveAndEnabled && candidate.gameObject.scene == gameObject.scene)
                {
                    helmetHud = candidate;
                    break;
                }
            }
        }
        // Keep distortion off until the shield is fully closed, during the black hold.
        // Once dressed, T switches distortion immediately with the selected view.
        float amount = viewCamera != null && viewCamera.isActiveAndEnabled ? viewCamera.ViewBlend : 0f;
        amount *= helmetHud != null && helmetHud.isActiveAndEnabled ? helmetHud.StartupOpacity : 0f;
        bool firstPerson = viewCamera != null && viewCamera.isActiveAndEnabled && viewCamera.IsFirstPerson;
        float lensCoverage = helmetHud != null && helmetHud.isActiveAndEnabled && !helmetHud.StartupComplete
            ? (helmetHud.StartupShieldClosed ? 1f : 0f) : 1f;
        ApplyVignette(amount, firstPerson, lensCoverage);
    }

    void ApplyVignette(float amount, bool firstPerson, float lensCoverage = 1f)
    {
        if (runtimeProfile == null || viewOverrides == null) return;
        // Only the hidden blocker is animated. Global Volume's editable values stay
        // exactly as entered, including edits to zero and edits made while in TPS.
        viewOverrides.enabled = volume.isActiveAndEnabled;
        viewOverrides.gameObject.layer = gameObject.layer;
        viewOverrides.priority = volume.priority + 1f;
        var settings = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
        lensBlocker.active = !firstPerson || lensCoverage < 1f;
        if (lensBlocker.active)
        {
            lensBlocker.intensity.Override(0f);
            lensBlocker.scale.Override(1f);
        }
        if (settings != null && settings.TryGet<Vignette>(out var vignette) && vignette.active)
        {
            vignetteOverride.active = true;
            float intensity = vignette.intensity.overrideState ? vignette.intensity.value : 0f;
            vignetteOverride.intensity.Override(intensity * Mathf.Clamp01(amount) * volume.weight);
        }
        else vignetteOverride.active = false;
    }

    void OnDisable()
    {
        if (viewOverrides != null)
        {
            viewOverrides.enabled = false;
            viewOverrides.sharedProfile = null;
            Release(viewOverrides.gameObject);
        }
        viewOverrides = null;
        if (volume != null && volume.HasInstantiatedProfile() && volume.profile == runtimeProfile)
            volume.profile = previousProfile;
        ReleaseProfile(overrideProfile);
        ReleaseProfile(runtimeProfile);
        overrideProfile = runtimeProfile = previousProfile = null;
        lensBlocker = null;
        vignetteOverride = null;
    }

    static void ReleaseProfile(VolumeProfile profile)
    {
        if (profile == null) return;
        foreach (var component in profile.components) Release(component);
        Release(profile);
    }

    static void Release(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
