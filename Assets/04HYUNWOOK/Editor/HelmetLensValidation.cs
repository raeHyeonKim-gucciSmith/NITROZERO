using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class HelmetLensValidation
{
    const string Request = "Library/HelmetLensValidation.request";
    static HelmetLensValidation() => EditorApplication.update += CheckRequest;
    static void CheckRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        Run();
    }
    [MenuItem("Tools/HYUNWOOK/Validate Helmet Lens Switching")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        GameObject go = null;
        VolumeProfile profile = null;
        VolumeStack stack = null;
        try
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var source = profile.Add<LensDistortion>(true);
            source.intensity.Override(-0.35f);
            source.scale.Override(1.05f);
            go = new GameObject("Isolated helmet test") { hideFlags = HideFlags.HideAndDontSave, layer = 31 };
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10000f;
            volume.sharedProfile = profile;
            var effect = go.AddComponent<FirstPersonVignette>();
            effect.PreviewHelmetEffects(true);
            stack = VolumeManager.instance.CreateStack();
            var editableProfile = volume.profile;
            editableProfile.TryGet<LensDistortion>(out var editable);
            if ((editableProfile.hideFlags & HideFlags.NotEditable) != 0 || (editable.hideFlags & HideFlags.NotEditable) != 0)
                throw new Exception("Inspector NotEditable flag remains.");
            if (editableProfile == profile || editable == source) throw new Exception("Source is not isolated.");
            Action<float, float> check = (intensity, scale) =>
            {
                VolumeManager.instance.Update(stack, go.transform, 1 << 31);
                var actual = stack.GetComponent<LensDistortion>();
                if (Mathf.Abs(actual.intensity.value - intensity) > 0.0001f || Mathf.Abs(actual.scale.value - scale) > 0.0001f)
                    throw new Exception($"Rendered lens {actual.intensity.value}/{actual.scale.value}, expected {intensity}/{scale}.");
            };
            check(-0.35f, 1.05f);
            var apply = typeof(FirstPersonVignette).GetMethod("ApplyVignette",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            foreach (float coverage in new[] { 0f, 0.25f, 0.5f, 0.9999f, 1f })
            {
                apply.Invoke(effect, new object[] { 0f, true, coverage });
                check(coverage >= 1f ? -0.35f : 0f, coverage >= 1f ? 1.05f : 1f);
            }
            apply.Invoke(effect, new object[] { 1f, false, 0.5f });
            check(0f, 1f);
            effect.PreviewHelmetEffects(true);
            editable.intensity.value = -0.65f;
            editable.scale.value = 1.15f;
            for (int i = 0; i < 10; i++)
            {
                effect.PreviewHelmetEffects(false);
                check(0f, 1f);
                if (editable.intensity.value != -0.65f || editable.scale.value != 1.15f) throw new Exception("TPS overwrote Inspector.");
                effect.PreviewHelmetEffects(true);
                check(-0.65f, 1.15f);
            }
            effect.PreviewHelmetEffects(false);
            editable.intensity.value = 0f;
            effect.PreviewHelmetEffects(true);
            check(0f, 1.15f);
            editable.intensity.value = -0.2f;
            effect.PreviewHelmetEffects(true);
            check(-0.2f, 1.15f);
            effect.EndHelmetPreview();
            if (volume.HasInstantiatedProfile() || volume.sharedProfile != profile || source.intensity.value != -0.35f || source.scale.value != 1.05f)
                throw new Exception("Pre-play settings not restored.");
            effect.PreviewHelmetEffects(true);
            check(-0.35f, 1.05f);
            effect.EndHelmetPreview();
            File.WriteAllText("Library/HelmetLensValidation-result.txt", "PASS v4: distortion zero until shield fully closed; full restoration at black hold; TPS suppression; editable Inspector; actual VolumeStack evaluation; 10 FPS/TPS round trips; pre-play values restored.");
        }
        catch (Exception e)
        {
            File.WriteAllText("Library/HelmetLensValidation-result.txt", "FAIL v2: " + e);
            Debug.LogException(e);
        }
        finally
        {
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
            if (stack != null) VolumeManager.instance.DestroyStack(stack);
            if (profile != null)
            {
                foreach (var component in profile.components) UnityEngine.Object.DestroyImmediate(component);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }
    }
}
