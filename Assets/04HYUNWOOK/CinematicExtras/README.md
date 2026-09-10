# Cinematic extra cars

Prefab: Prefabs/ExtraCar_Black_Wing.prefab

This is the purple member of the original three extra cars from 3DCarTest. Its historical filename, root IDs and prefab GUID are retained for scene compatibility. The classic car model has been replaced with Sport Car Free 4. The original rear wing and RacingThreatTarget are retained.

Paint: Materials/ExtraCar_Sport4_Purple_Paint.mat (Base Map color).
Model: SportCar_4 child, uniform scale 1.6, raised to place the source tire mesh on the ground.
Wing: Customization/Rear_Wing, adjusted for the new rear deck.

Drag the prefab into the desired scene. No extra copy was placed in the player's Test scene automatically. The shared SportCar mesh/texture assets already in NITROZERO are reused. The source 3DCarTest project is unchanged.

## White and green cars

- White: Prefabs/ExtraCar_White_Splitter.prefab — original Sport Car Free model, white paint and existing customization.
- Green: Prefabs/ExtraCar_Lime_ExtendedExhaust.prefab — RMCar26 model with green paint. The historical prefab filename is retained.

Both were copied from 3DCarTest without changing their prefab contents or GUIDs. Required dedicated materials and their meta files are included. Existing NITROZERO model, texture and shared prefab dependencies are reused. All three extra-car prefabs are now available in the Prefabs folder; they have not been placed automatically into the Test scene.
