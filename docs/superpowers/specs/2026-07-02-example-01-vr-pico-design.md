# Example_01 VR / PICO / Keyboard-Mouse Simulation Design

## Goal

Configure `Assets/Scenes/Example_01.unity` as a VR-ready exhibition scene that works on PICO devices and can also be tested in the Unity Editor using keyboard and mouse simulation.

The scene should support both locomotion styles:

- Continuous movement and turning for exhibition roaming.
- Ray-based teleportation for comfort and fast navigation.

## Current Project Context

The project already contains the required XR packages and assets:

- XR Interaction Toolkit 3.4.1
- OpenXR 1.16.1
- PICO OpenXR package
- XR Management
- XR Device Simulator sample assets
- `Assets/Scenes/Example_01.unity` is already included in Build Settings.

`Example_01.unity` already contains an `EventSystem` and an `XR Interaction Manager`, so the implementation should extend the existing scene rather than rebuild it from scratch.

## Recommended Approach

Use Unity's XR Interaction Toolkit starter prefabs and simulator assets:

- `Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab`
- `Assets/Samples/XR Interaction Toolkit/3.4.1/XR Device Simulator/XR Device Simulator.prefab`

This keeps PICO runtime behavior and Editor simulation on the same OpenXR/XRI interaction stack.

## Scene Architecture

`Example_01.unity` should contain these functional objects:

```text
XR Interaction Manager
EventSystem
XR Origin (XR Rig)
XR Device Simulator
Teleportation Area / Teleportation Anchors
Scene geometry, lights, and existing exhibition content
```

The XR Origin becomes the active player rig. If the scene has a legacy non-XR camera, it should be disabled or removed to avoid duplicate cameras or duplicate Audio Listeners.

## Locomotion

Continuous locomotion should be enabled through the XR Origin starter asset setup:

- Left controller stick: move.
- Right controller stick: turn.
- Camera follows the headset pose on device.

Teleportation should be enabled through XR Interaction Toolkit teleport components:

- Add `Teleportation Area` to valid walkable floors, or use `Teleportation Anchor` for specific target points.
- Ensure walkable surfaces have colliders so ray interactors can hit them.

Both systems should remain active unless they conflict in testing.

## PICO Compatibility

The implementation should not overwrite global OpenXR settings unless required. The project already includes PICO OpenXR support, and recent collaborator updates changed XR settings. The scene-level work should therefore rely on standard XRI/OpenXR components and preserve existing package/project settings.

Expected PICO behavior:

- Launch scene with XR Origin camera.
- PICO controllers drive XRI controller actions.
- Continuous movement and teleportation use the same XRI input actions as Editor simulation.

## Keyboard and Mouse Simulation

Add the XR Device Simulator prefab to `Example_01.unity` for Editor testing.

Expected Editor behavior:

- Press Play without a PICO headset.
- Use the XR Device Simulator UI and controls to simulate headset/controller movement.
- Use keyboard and mouse to aim/select with simulated controllers.
- Test continuous movement, ray interaction, and teleportation inside the Editor.

The exact key bindings are controlled by the XR Device Simulator input actions and UI shown during Play Mode.

## Error Handling and Validation

Implementation should check for these issues:

- Duplicate `Main Camera` or duplicate `Audio Listener` after adding XR Origin.
- Missing colliders on floor/walkable geometry.
- Missing or duplicated `XR Interaction Manager`.
- EventSystem using a non-XR input module instead of XR-compatible UI input.
- XR Device Simulator prefab missing references to simulator input actions.

## Testing Plan

Manual Editor tests:

1. Open `Example_01.unity`.
2. Press Play in Unity Editor.
3. Confirm XR Device Simulator appears and can control view/controller pose.
4. Confirm continuous movement works.
5. Confirm ray teleportation works on configured floor areas.
6. Confirm there are no console errors related to XR input, duplicate cameras, or missing references.

Device tests:

1. Build/run to PICO using existing OpenXR/PICO project configuration.
2. Confirm the scene starts with the XR Origin camera.
3. Confirm PICO controllers can move, turn, raycast, and teleport.

## Scope Boundaries

This change should modify only what is needed for `Example_01.unity` VR readiness. It should avoid unrelated changes to other scenes, global XR settings, or collaborator work unless Unity requires a scene dependency update.
