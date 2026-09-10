using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Use this Volume's authored vignette only while the racing camera is in first person.</summary>
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
    float firstPersonIntensity;

    void OnEnable()
    {
        volume = GetComponent<Volume>();
        previousProfile = volume.HasInstantiatedProfile() ? volume.profile : null;
        var source = previousProfile != null ? previousProfile : volume.sharedProfile;
        if (source == null || !source.TryGet<Vignette>(out var authoredVignette)) return;

        firstPersonIntensity = authoredVignette.intensity.value;
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
        volume.profile = runtimeProfile;
        ApplyVignette(0f);
    }

    void LateUpdate()
    {
        if (vignette == null) return;
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
        if (vignette == null) return;
        // Keep the override at zero in TPS so the authored intensity cannot leak back in.
        vignette.intensity.Override(firstPersonIntensity * Mathf.Clamp01(amount));
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
    }

    static void Release(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
