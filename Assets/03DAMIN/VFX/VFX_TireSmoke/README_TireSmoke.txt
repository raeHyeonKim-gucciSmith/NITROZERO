TIRE SMOKE — stationary-wheel VFX prototype

Open Assets/03DAMIN/VFX.unity, select TireSmoke_Test_ROOT, and press F in Scene view.
For the dedicated Game view, enable the Camera component on
06_PreviewCamera_ENABLE_TO_VIEW, then press Play. Disable that Camera to return
to the existing booster view. The camera is disabled in the saved prefab.

PF_TireSmoke.prefab is the reusable smoke-only prefab.
PF_TireSmoke_TestRig.prefab includes a cube-tread test wheel, floor, preview
lights, disabled camera, and a looping Animator clip. There is no runtime
script controlling the smoke and no dependency on the completed boosters.

Four editable native VFX Graphs:
01 ContactSmoke: low contact patch smoke.
02 WheelWrapSmoke: partial wheel orbit followed by backward release.
03 TrailingSmoke: expanding rear plume.
04 FineWisps: lighter edge detail.

Blackboard controls: EmissionRate, WheelRadius, WheelWidth, WrapAngularSpeed,
WakeSpeed, Opacity, SmokeColor, SizeMultiplier. Defaults: radius 0.65 m,
width 0.34 m, angular speed 5.7 rad/s. Local axle X; forward +Z; wake -Z;
ground Y=0; wheel center Y=WheelRadius+0.005 (0.655 m).

IMPORTANT: This is a stationary-wheel, local-space visual prototype.
Actual vehicle grounding, slip detection, emission on/off, variable wheel
rotation and world-space smoke left behind a moving car are NOT implemented.
They should be connected separately when the visual design is approved.
There is no simulated fluid, tire collision or physical airflow solver.

Test lights affect only Layer 30; existing lights/settings are not changed.
The smoke uses alpha-blended lit animated quads, not additive booster fire.

Texture provenance: installed Unity Visual Effect Graph 17.3 package,
Samples~/VFXGraphAdditions/Textures/Smoke/WispySmoke03b_8x8.png and its
_N normal map, copied to this independent folder under new asset GUIDs.
These Unity sample assets remain subject to their applicable Unity license.
