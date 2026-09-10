# Automatic-clutch manual transmission

Controls: W = forward accelerator; S = brake then reverse; Space = brake only; A/D = steering; E/Q = up/down within forward gears 1 through 6. W while reversing first brakes, then returns to first gear near standstill.
Q/E never select R or neutral. S automatically selects R below 1 km/h forward speed. Space takes priority over throttle/reverse requests. Starting gear is constrained to 1 through 6.

Automatic clutch interrupts wheel drive during shiftCooldown and synchronizes engine RPM to the new gear. Low gears produce more wheel torque; releasing the accelerator in gear adds engine braking. Reverse engagement while moving and downshifts beyond redline are rejected. Space never changes direction; holding S changes to reverse only after slowing down.

The engine torque limiter replaces direct per-gear rigidbody speed clamping. Each gear reaches redlineRpm at its configured gearSpeedLimits entry. Both HUDs and engine audio use engine RPM. The gauge fills at the redline region; early shifts are permitted and do not fake a full gauge.

Tune ArcadeCarController on the Car prefab: gearSpeedLimits, idleRpm, redlineRpm, launchRpm, rpmResponse, engineBraking and shiftCooldown. The current player has a 300 km/h upper setting and gear limits 50/100/150/200/250/300 km/h.

Validation: controller/HUD/audio compile passed; transmission-rule checks passed, including pedal direction changes, Q/E forward-only shifts and neutral rejection. Full Unity driving and WheelCollider handling require play testing. This is a simplified automatic-clutch driving model, not a full combustion-engine or H-pattern gearbox simulation.
