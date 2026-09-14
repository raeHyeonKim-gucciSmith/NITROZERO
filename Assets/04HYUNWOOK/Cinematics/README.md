# Dome cinematic: cameras 1, 3 and 4 (5 seconds)

Scene: Assets/04HYUNWOOK/Scenes/HW_domeInTheMoon.unity
Hierarchy: HW_Dome_Shots_1_3_4_5s
Timeline: HW_Dome_Shots_1_3_4_5s.playable

| Camera | Time |
|---|---|
| 1: Dome establishing | 0–1.5 s |
| 3: Dome top, fixed downward view | 1.5–3 s |
| 4: Manual ground position, tracking red | 3–5 s |

Camera 2 and its spline are inactive. Existing camera coordinates, rotations, lens settings, and vehicle speed settings are preserved. The scene currently has a base speed of 600 km/h and last-shot speed of 500 km/h; adjust them on Dome Cinematic Sequence. Auto-exit Play Mode at Timeline end remains enabled.

Cameras 1, 3 and 4 use their Transform positions directly. Camera 1 also uses its Transform rotation as the starting direction and applies Right Pan Degrees only to the output. Camera 3 has fixed downward aim. Camera 4 automatically aims at the red vehicle from its manually edited position. For camera 3, 카메라 이동 is the single position-mode switch: on enables Dolly/Timeline movement and disables direct position; off disables Dolly and enables direct Transform editing. Fixed downward aim is unchanged.

Dome Dolly Aim exposes Inspector controls for manual position/rotation, pan angle/curve, target offsets and Timeline progress. Motion and camera tracks must be edited together to keep cut timings aligned. Normal Unity Play Mode persistence rules apply.

No Play Mode, test rendering, or tests were run for this change. No frame images or video are used at runtime. Earlier Timeline assets and scene backups remain available under Documentation/DomeCameraPreview.

