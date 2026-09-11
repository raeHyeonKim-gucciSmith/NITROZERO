using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Render the actual imported UXML in an isolated preview scene, without changing
// the open game scene, the user's camera, or their Play mode state.
[InitializeOnLoad]
public static class CleanHudValidation
{
    const string Folder = "Library/CleanHUDValidation/InGame";
    const string Request = "Library/CleanHUDValidation.request";
    const string HudFolder = "Assets/04HYUNWOOK/UI Toolkit/RacingHUD/";
    static UIDocument document;
    static GameObject holder;
    static PanelSettings settings;
    static RenderTexture target;
    static Scene scene;
    static int frame, mode;
    static bool IsTps => mode == 0 || mode == 3;
    static readonly List<string> details = new List<string>();

    static CleanHudValidation()
    {
        EditorApplication.update += Tick;
        AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
    }

    [MenuItem("Tools/HYUNWOOK/Validate Clean HUD Rendering")]
    public static void Run()
    {
        Cleanup();
        Directory.CreateDirectory(Folder);
        details.Clear();
        details.Add("Play mode at validation: " + EditorApplication.isPlaying);
        try
        {
            RacingHudDocumentTools.ValidateTps(); RacingHudDocumentTools.ValidateFps();
            scene = EditorSceneManager.NewPreviewScene();
            holder = new GameObject("Isolated clean HUD validation") { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(holder, scene);
            holder.SetActive(false);
            settings = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(HudFolder + "RacingHUDPanel.asset"));
            settings.hideFlags = HideFlags.HideAndDontSave;
            settings.clearColor = true; settings.colorClearValue = new Color(.18f, .24f, .28f, 1f);
            target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32) { hideFlags = HideFlags.HideAndDontSave };
            target.Create(); settings.targetTexture = target;
            document = holder.AddComponent<UIDocument>(); document.panelSettings = settings;
            mode = 0; holder.SetActive(true); LoadMode();
            File.WriteAllText(Folder + "/status.txt", "Rendering imported TPS and FPS UXML.");
        }
        catch (Exception e) { Fail(e); }
    }

    static void LoadMode()
    {
        document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudFolder + (IsTps ? "TPS" : "FPS") + ".uxml");
        frame = 0;
        var root = document.rootVisualElement;
        // A preview scene has no Game view to provide the document's screen size.
        root.style.width = 1920; root.style.height = 1080;
        var shield = root.Q("startup-shield"); if (shield != null) shield.style.display = DisplayStyle.None;
        root.Q<Label>("route-distance").text = "STRAIGHT  2.4 KM";
        if (mode >= 2)
        {
            foreach (string id in new[] { "missile-warning", "enemy-warning-left", "enemy-warning-right" })
                root.Q(id).style.display = DisplayStyle.Flex;
            root.Q<Label>("missile-distance").text = "REAR  85 M";
        }
    }

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (holder == null)
        {
            if (File.Exists(Request)) { File.Delete(Request); Run(); }
            return;
        }
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            var utility = typeof(UIDocument).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility");
            utility?.GetMethod("UpdateRuntimePanels", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(null, null);
            if (++frame < 25) return;
            var panel = document.rootVisualElement.panel;
            if (panel == null) throw new Exception("Offscreen UIDocument has no panel.");
            var repaint = panel.GetType().GetMethod("Repaint", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (repaint == null) throw new Exception("Runtime panel repaint is unavailable.");
            repaint.Invoke(panel, new object[] { new Event { type = EventType.Repaint } });
            Inspect(); SaveImage(mode == 0 ? "TPS" : mode == 1 ? "FPS" : mode == 2 ? "FPS-alert" : "TPS-alert");
            if (++mode < 2) { LoadMode(); return; }
            File.WriteAllLines(Folder + "/report.txt", details);
            File.WriteAllText(Folder + "/status.txt", "PASS: imported UXML, vector registration, full-HD layout and native rendering.");
            Cleanup();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception e) { Fail(e); }
    }

    static void Inspect()
    {
        var root = document.rootVisualElement;
        string name = IsTps ? "TPS" : "FPS";
        if (Mathf.Abs(root.layout.width - 1920) > 1 || Mathf.Abs(root.layout.height - 1080) > 1)
            throw new Exception("Expected Full HD panel: " + root.layout);
        int parts = 0;
        root.Query<RacingUI.HudVectorPart>().ForEach(p => {
            if (!p.IsPathValid || p.worldBound.width <= 0 || p.worldBound.height <= 0)
                throw new Exception("Invalid/empty vector: " + p.name + " " + p.worldBound);
            parts++;
        });
        foreach (string id in new[] { "speed-value", "gear-value", "unit", "rpm-title", "route-distance" })
        {
            var e = root.Q(id);
            if (e == null || e.worldBound.width <= 0 || e.worldBound.height <= 0) throw new Exception("Missing layout: " + id);
            details.Add(name + " " + id + ": " + e.worldBound + " font=" + e.resolvedStyle.fontSize + " color=" + e.resolvedStyle.color);
        }
        details.Add(name + " vector parts: " + parts);
        var navigation = root.Q<RacingUI.NavigationMap>("navigation-road");
        var projection = typeof(RacingUI.NavigationMap).GetMethod("Project", BindingFlags.Instance | BindingFlags.NonPublic);
        Vector2 RoadPoint(float side, float depth) => (Vector2)projection.Invoke(navigation, new object[] { side, depth });
        if (Vector2.Distance(RoadPoint(-.5f, 0), RoadPoint(.5f, 0)) > .001f)
            throw new Exception("Road edges do not meet at one vanishing point.");
        foreach (float side in new[] { -.5f, .5f })
            if (Vector2.Distance(RoadPoint(side, .5f), Vector2.Lerp(RoadPoint(side, 0), RoadPoint(side, 1), .5f)) > .01f)
                throw new Exception("Road boundary must be a straight perspective line.");
        details.Add(name + " road: single vanishing point and straight converging edges verified.");
        if (!IsTps) InspectFirstPerson();
        if (mode == 3)
        {
            foreach (var pair in new[] { ("enemy-warning-left", 432f), ("enemy-warning-right", 1488f), ("missile-warning", 960f) })
            {
                var bounds = root.Q(pair.Item1).worldBound;
                if (Vector2.Distance(bounds.center, new Vector2(pair.Item2, 540f)) > 2f)
                    throw new Exception("TPS alert does not match FPS placement: " + pair.Item1 + " " + bounds);
                details.Add("TPS alert matches FPS placement: " + pair.Item1 + " " + bounds);
            }
        }
    }

    static void InspectFirstPerson()
    {
        var root = document.rootVisualElement;
        var hud = root.Q("racing-hud");
        foreach (string id in new[] { "warning-overlay", "missile-warning", "enemy-warning-left", "enemy-warning-right" })
            if (root.Q(id) != null) throw new Exception("Removed missile UI remains: " + id);
        foreach (string removed in new[] { "art-cooling-frame", "helmet-cooling-vent-leaders", "left-scale", "right-scale", "helmet-temple-vents", "helmet-top-machining" })
            if (root.Q(removed) != null) throw new Exception("Obsolete decoration/scale remains: " + removed);
        foreach (string id in new[] { "coolant-panel", "vehicle-indicators" })
        {
            var e = root.Q(id);
            // Leave room for the user's authored outward translation of the coolant group.
            var safeArea = new Rect(50, 320, 1820, 380);
            if (e == null || !safeArea.Contains(e.worldBound.min) || !safeArea.Contains(e.worldBound.max))
                throw new Exception("Side instrument extends outside the helmet interior: " + id);
            details.Add("FPS safe side instrument " + id + ": " + e.worldBound);
        }
        var temperature = root.Q<RacingUI.DigitalReadout>("coolant-value");
        var track = root.Q("coolant-gauge-track");
        var needle = root.Q("coolant-needle");
        var needleTip = needle.LocalToWorld(new Vector2(1f, 7f));
        float normalAngle = Mathf.Atan2(57.6f, 222f);
        float expectedTipY = track.worldBound.y + track.worldBound.height * .2f + 5f * Mathf.Sin(normalAngle);
        if (temperature == null || temperature.text != "80" || Mathf.Abs(needleTip.y - expectedTipY) > 1f ||
            root.Q<RacingUI.DigitalReadout>("coolant-hot-label").text != "100" ||
            root.Q<RacingUI.DigitalReadout>("coolant-cold-label").text != "0")
            throw new Exception("80 C must indicate 80% of the 0-100 C range.");
        details.Add("FPS coolant: 0-100 C, 80 C needle at 80% of range.");
        var coolantModule = root.Q("side-coolant-panel");
        var fuelModule = root.Q("side-fuel-panel");
        if (coolantModule == null || temperature.parent?.parent != coolantModule ||
            Mathf.Abs(coolantModule.worldBound.center.x + fuelModule.worldBound.center.x - 1920f) > 1f ||
            Mathf.Abs(coolantModule.worldBound.y - fuelModule.worldBound.y) > 1f)
            throw new Exception("Coolant readout must be in the lower-left mirror of the fuel module.");
        foreach (string id in new[] { "side-coolant-title", "coolant-value", "coolant-unit", "side-coolant-reference" })
        {
            var element = root.Q(id);
            if (!coolantModule.worldBound.Contains(element.worldBound.min) || !coolantModule.worldBound.Contains(element.worldBound.max))
                throw new Exception("Coolant text spills outside its module: " + id);
        }
        var oil = root.Q("indicator-oil-pressure").worldBound.center;
        // The coolant window is slanted: a rectangular parent alone cannot detect overflow.
        var coolantArt = root.Q("art-coolant-frame").worldBound;
        var unitRect = root.Q("coolant-unit").worldBound;
        foreach (var corner in new[] { unitRect.min, unitRect.max, new Vector2(unitRect.xMax, unitRect.yMin), new Vector2(unitRect.xMin, unitRect.yMax) })
        {
            float x = (corner.x - coolantArt.x) * 210f / coolantArt.width;
            float y = (corner.y - coolantArt.y) * 215f / coolantArt.height;
            float t = (y - 83f) / 54f;
            if (t < 0 || t > 1 || x < 48f + 25f * t + 2f || x > 128f + 24f * t - 2f)
                throw new Exception("Coolant degree unit extends beyond the slanted display recess: " + unitRect);
        }
        details.Add("FPS coolant unit fits inside the slanted display recess: " + unitRect);
        var engine = root.Q("indicator-check-engine").worldBound.center;
        var abs = root.Q("indicator-abs").worldBound.center;
        if (engine.x < oil.x + 20f || engine.x > oil.x + 28f || Mathf.Abs(oil.x - abs.x) > 1f)
            throw new Exception("Dormant warning lights must form a symmetric ')' arc.");
        details.Add("FPS mirrored coolant module: " + coolantModule.worldBound + "; warning lights form ')' arc.");
        var bracket = root.Q("helmet-instrument-rpm-endcaps").worldBound;
        var rpmInterior = new Rect(bracket.x + 143f * bracket.width / 915f,
            bracket.y + 171f * bracket.height / 255f, 611f * bracket.width / 915f, 21f * bracket.height / 255f);
        Rect previous = default;
        for (int i = 0; i < 40; i++)
        {
            var bar = root.Q("rpm-bar-" + i).worldBound;
            if (!rpmInterior.Contains(bar.min) || !rpmInterior.Contains(bar.max) ||
                Mathf.Abs(bar.width - 14f) > .1f || Mathf.Abs(bar.height - 15f) > .1f ||
                (i > 0 && Mathf.Abs(bar.xMin - previous.xMax - 1f) > .1f))
                throw new Exception("RPM segments must have equal size and gaps inside the brackets: " + i + " " + bar);
            previous = bar;
        }
        details.Add("FPS RPM: 40 equal 14x15px segments, 1px gaps, all inside [ ] brackets.");
        root.Q("vehicle-indicators").Query<RacingUI.HudVectorPart>().ForEach(p => {
            if (p.name == "vehicle-indicator-background")
            {
                if (p.fillColor != Color.black) throw new Exception("Shared indicator background must be black.");
                return;
            }
            if (p.fillColor.a > 0 || p.strokeColor.a > .5f)
                throw new Exception("Dormant warning indicator must be an unlit outline: " + p.name);
        });
        if (mode == 2)
        {
            var alert = root.Q("missile-warning");
            if (Vector2.Distance(alert.worldBound.center, new Vector2(960, 540)) > 1f)
                throw new Exception("Visible missile warning is not centered: " + alert.worldBound);
            details.Add("FPS visible missile warning centered: " + alert.worldBound);
            var leftMark = root.Q("enemy-warning-left").worldBound;
            var rightMark = root.Q("enemy-warning-right").worldBound;
            // Users can reposition the side instruments in UI Builder; verify clear gaps rather than a fixed midpoint.
            if (leftMark.xMin <= root.Q("coolant-panel").worldBound.xMax || leftMark.xMax >= alert.worldBound.xMin ||
                rightMark.xMin <= alert.worldBound.xMax || rightMark.xMax >= root.Q("vehicle-indicators").worldBound.xMin ||
                Mathf.Abs(leftMark.center.y - alert.worldBound.center.y) > 1f || Mathf.Abs(rightMark.center.y - alert.worldBound.center.y) > 1f)
                throw new Exception("Missile marks must fit between side instruments and the message without overlap.");
            details.Add("FPS missile marks between instruments and message: " + leftMark + " / " + rightMark);
            foreach (string id in new[] { "missile-title", "missile-distance" })
            {
                var text = root.Q(id);
                if (!alert.worldBound.Contains(text.worldBound.min) || !alert.worldBound.Contains(text.worldBound.max))
                    throw new Exception("Missile text spills outside its card: " + id);
            }
        }
    }

    static void SaveImage(string name)
    {
        var prior = RenderTexture.active;
        var pixels = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
        try
        {
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); pixels.Apply();
            File.WriteAllBytes(Folder + "/" + name + "-native.png", pixels.EncodeToPNG());
            foreach (string id in new[] { "speed-value", "gear-value" })
            {
                var rect = document.rootVisualElement.Q(id).worldBound;
                int colored = 0;
                for (int y = Mathf.CeilToInt(rect.yMin + 2); y < Mathf.FloorToInt(rect.yMax - 2); y++)
                    for (int x = Mathf.CeilToInt(rect.xMin + 2); x < Mathf.FloorToInt(rect.xMax - 2); x++)
                    {
                        var c = pixels.GetPixel(x, 1079 - y);
                        if (c.r > .4f && c.g < c.r * .5f) colored++;
                    }
                if (colored < 10) throw new Exception("Digital strokes are missing or dark: " + id);
                details.Add(name + " visible red pixels in " + id + ": " + colored);
            }
            var shader = Shader.Find("UI/Racing HUD Curved");
            if (shader == null) throw new Exception("HUD curvature shader is missing.");
            var curved = RenderTexture.GetTemporary(1920, 1080, 0, RenderTextureFormat.ARGB32);
            var material = new Material(shader);
            material.SetFloat("_Curvature", IsTps ? 0f : .095f);
            material.SetFloat("_OutsideOpacity", IsTps ? 0 : 1);
            try
            {
                Graphics.Blit(target, curved, material);
                RenderTexture.active = curved;
                pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); pixels.Apply();
                File.WriteAllBytes(Folder + "/" + name + "-curved.png", pixels.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(material); RenderTexture.ReleaseTemporary(curved); }
        }
        finally { RenderTexture.active = prior; UnityEngine.Object.DestroyImmediate(pixels); }
    }

    static void Fail(Exception error)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/status.txt", "FAIL: " + error);
        Cleanup();
        if (Application.isBatchMode) EditorApplication.Exit(1);
    }

    static void Cleanup()
    {
        if (document != null)
        {
            document.enabled = false;
            document.panelSettings = null;
        }
        if (holder != null) UnityEngine.Object.DestroyImmediate(holder);
        if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        if (settings != null) UnityEngine.Object.DestroyImmediate(settings);
        if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
        holder = null; document = null; settings = null; target = null;
    }
}
