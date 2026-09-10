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

    Volume volume;
    VolumeProfile previousProfile, runtimeProfile;
    Vignette vignette;
    LensDistortion helmetLens;
    float firstPersonIntensity;
    float helmetLensIntensity, helmetLensScale;

#if UNITY_EDITOR
    // Preview uses the same temporary profile as play mode and never edits the authored asset.
    public void PreviewHelmetEffects(bool firstPerson)
    {
        if (Application.isPlaying) return;
        if (runtimeProfile == null) OnEnable();
        ApplyVignette(firstPerson ? 1f : 0f);
    }

    public void EndHelmetPreview()
    {
        if (!Application.isPlaying) OnDisable();
    }
#endif

    void OnEnable()
    {
        volume = GetComponent<Volume>();
        previousProfile = volume.HasInstantiatedProfile() ? volume.profile : null;
        var source = previousProfile != null ? previousProfile : volume.sharedProfile;
        if (source == null) return;
        firstPersonIntensity = source.TryGet<Vignette>(out var authoredVignette) ? authoredVignette.intensity.value : 0f;
        // Deep-copy all components: Vignette changes must never modify the shared project asset.
        runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        runtimeProfile.name = source.name + " (first-person runtime)";
        runtimeProfile.hideFlags = HideFlags.HideAndDontSave;
        foreach (var component in source.components)
        {
            if (component == null) continue;
            var copy = Instantiate(component);
            copy.hideFlags = HideFlags.HideAndDontSave;
            runtimeProfile.components.Add(copy);
        }
        runtimeProfile.TryGet(out vignette);
        // Use the user's Volume settings; do not create or tune an additional lens effect.
        if (runtimeProfile.TryGet(out helmetLens))
        {
            helmetLensIntensity = helmetLens.intensity.overrideState ? helmetLens.intensity.value : 0f;
            helmetLensScale = helmetLens.scale.overrideState ? helmetLens.scale.value : 1f;
        }
        volume.profile = runtimeProfile;
        ApplyVignette(0f);
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
        // Wait for the shield to close, then fade in with HUD power. T still blends both ways.
        float amount = viewCamera != null && viewCamera.isActiveAndEnabled ? viewCamera.ViewBlend : 0f;
        amount *= helmetHud != null && helmetHud.isActiveAndEnabled ? helmetHud.StartupOpacity : 0f;
        ApplyVignette(amount);
    }

    void ApplyVignette(float amount)
    {
        if (runtimeProfile == null) return;
        // Keep the override at zero in TPS so the authored intensity cannot leak back in.
        float blend = Mathf.Clamp01(amount);
        if (vignette != null) vignette.intensity.Override(firstPersonIntensity * blend);
        if (helmetLens != null)
        {
            // An active zero override suppresses the authored distortion in third person.
            // Reset zoom as well, so TPS keeps its normal framing.
            helmetLens.intensity.Override(helmetLensIntensity * blend);
            helmetLens.scale.Override(Mathf.Lerp(1f, helmetLensScale, blend));
        }
    }

    void OnDisable()
    {
        if (runtimeProfile == null) return;
        if (volume != null && volume.HasInstantiatedProfile() && volume.profile == runtimeProfile)
            volume.profile = previousProfile;

        foreach (var component in runtimeProfile.components) Release(component);
        Release(runtimeProfile);
        runtimeProfile = null;
        previousProfile = null;
        vignette = null;
        helmetLens = null;
    }

    static void Release(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
