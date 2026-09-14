using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Camera-owned override also supplies zero in edit mode so shared volume
// defaults cannot darken the helmet preview before gameplay starts.
[ExecuteAlways, DisallowMultipleComponent]
public sealed class RacingHelmetVignette : MonoBehaviour
{
    CarCinemachineSetup settings;
    RacingHudController hud;
    Volume overlay;
    VolumeProfile profile;
    Vignette vignette;
    LensDistortion lens;

    void OnEnable() { settings=GetComponent<CarCinemachineSetup>(); UpdateEffect(); }
    void Update() => UpdateEffect();
    void LateUpdate() => UpdateEffect();
    void UpdateEffect()
    {
        if(!gameObject.scene.IsValid() || !gameObject.scene.isLoaded || !settings)return;
        if(!overlay)
        {
            var owner=new GameObject("RacingCamera Helmet Vignette (temporary)");
            owner.hideFlags=HideFlags.HideAndDontSave;
            owner.SetActive(false);owner.transform.SetParent(transform,false);
            overlay=owner.AddComponent<Volume>();
            overlay.isGlobal=true;overlay.priority=10000;overlay.weight=1;
            profile=ScriptableObject.CreateInstance<VolumeProfile>();
            profile.hideFlags=HideFlags.HideAndDontSave;
            vignette=profile.Add<Vignette>();vignette.hideFlags=HideFlags.HideAndDontSave;
            vignette.intensity.Override(0);
            lens=profile.Add<LensDistortion>();lens.hideFlags=HideFlags.HideAndDontSave;
            lens.intensity.Override(0);lens.scale.Override(1);
            overlay.sharedProfile=profile;owner.SetActive(true);
        }
        overlay.gameObject.layer=gameObject.layer;
        if(hud && hud.ViewCamera!=settings)hud=null;
        if(Application.isPlaying && (!hud || !hud.isActiveAndEnabled))
            foreach(var candidate in FindObjectsByType<RacingHudController>(FindObjectsSortMode.None))
                if(candidate.isActiveAndEnabled && candidate.gameObject.scene==gameObject.scene
                    && candidate.ViewCamera==settings){hud=candidate;break;}
        bool worn=Application.isPlaying && settings.isActiveAndEnabled
            && settings.IsFirstPerson && hud && hud.isActiveAndEnabled && hud.StartupShieldClosed;
        bool show=worn && settings.enableHelmetVignette;
        overlay.priority=worn?10001:10000;
        bool distort=worn && settings.enableHelmetLensDistortion;
        lens.intensity.Override(distort?settings.helmetLensDistortionIntensity:0);
        lens.scale.Override(distort?settings.helmetLensDistortionScale:1);
        vignette.intensity.Override(show?Mathf.Clamp01(settings.helmetVignetteIntensity)*hud.StartupOpacity:0);
        vignette.smoothness.Override(Mathf.Clamp(settings.helmetVignetteSmoothness,.01f,1));
        vignette.color.Override(Color.black);
        vignette.center.Override(new Vector2(.5f,.5f));
        vignette.rounded.Override(false);
    }
    void OnDisable()
    {
        if(overlay){overlay.enabled=false;overlay.sharedProfile=null;Release(overlay.gameObject);}
        if(profile){foreach(var component in profile.components)Release(component);Release(profile);}
        overlay=null;profile=null;vignette=null;lens=null;hud=null;
    }
    static void Release(Object value)
    {
        if(!value)return;
        if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);
    }
}
