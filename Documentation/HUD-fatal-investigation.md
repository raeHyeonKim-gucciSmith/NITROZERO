# HUD text bounds and fatal error investigation

## Observed crash evidence
- Latest crash: C:/Users/PC/AppData/Local/Temp/Unity/Editor/Crashes/Crash_2026-09-10_015959326/Editor.log
- `Could not allocate memory: System out of memory!`, allocation 1,048,576 bytes, MemoryLabel Texture.
- Graphics allocation at failure: 49,916,121,112 bytes (about 46.5 GiB).
- Stack includes TextCore FontAsset.SetupNewAtlasTexture, TryAddGlyphs, ATGTextJobSystem.PopulateGlyphs, VisualTreeAssetEditor.RenderStaticPreview.
- Later: Direct3D12 device failed 0x8007000e and unrecoverable GPU device error.

This identifies memory exhaustion during UI font preview rendering. It does not prove which engine bug, driver issue, or earlier allocation caused the growth. The graphics error may be a consequence of the exhausted memory.

## Applied mitigations
- Standard text generator on both HUD document roots, all HUD descendants, and custom digital labels, retaining DS-Digital.
- Windows graphics backend set to Direct3D11 by the Unity validation command.
- Numeric controls fit to their bounds in both documents; text overflow is clipped/ellipsized.
- TPS ranking content inset 6% horizontally and 8% vertically; smaller ranking text and bounded columns.
- Finite-size guard before measuring digital text.

Unity supports opting individual elements out of advanced text generation: https://docs.unity3d.com/6000.3/Documentation/Manual/ui-systems/enable-and-use-atg.html

See HUD-safety-validation.txt for the actual import/preview test result. A short successful preview test cannot guarantee that every long editing session is crash-free.

## Verification result
Both TPS and FPS passed Unity's actual UXML import and custom-element deserialization checks on Direct3D11. Import-worker preview generation completed without recorded errors. Batch mode did not return preview textures through AssetPreview or RenderStaticPreview, so visual bounds could not be inspected in this run. Both verification launches exited normally without a fatal crash. The first run's process private memory was observed externally around 2.3 GiB; Unity Profiler allocated memory in the direct-preview attempt was 282,831,416 bytes. These are different metrics from the original crash allocation report and are not a like-for-like benchmark.
