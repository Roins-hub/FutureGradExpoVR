# Example_01 VR / PICO / Keyboard-Mouse Simulation Design

## Goal

Configure `Assets/Scenes/Example_01.unity` as a complete VR-ready exhibition scene that works in both target environments:

- Unity Editor Play Mode without a headset, using XR Device Simulator keyboard/mouse simulation.
- PICO headset runtime, using the project's existing OpenXR/PICO configuration.

The scene should support comfortable exhibition roaming, quick testing during development, and reliable final-device validation.

## Current Project Context

The project already contains the required XR packages and assets:

- Unity 6.4.11f1 project using URP.
- XR Interaction Toolkit 3.4.1.
- OpenXR 1.16.1.
- PICO OpenXR package.
- XR Management.
- XR Device Simulator sample assets.
- `Assets/Scenes/Example_01.unity` is already part of the project and has pending scene edits in the working tree.

The project also has existing portal scripts:

- `Assets/Scripts/ScenePortal.cs`
- `Assets/Scripts/ScenePortalTrigger.cs`

The implementation should preserve existing exhibition content, lighting, materials, portals, and collaborator edits. Scene-level XR setup should be preferred over unrelated global project setting changes.

## Recommended Approach

Use Unity's XR Interaction Toolkit starter prefabs and simulator assets as the shared interaction stack for both Editor simulation and PICO runtime:

- `Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab`
- `Assets/Samples/XR Interaction Toolkit/3.4.1/XR Device Simulator/XR Device Simulator.prefab`

This approach keeps movement, controller rays, teleportation, and UI interaction on standard XRI/OpenXR components, reducing device-specific scene logic.

## Scene Architecture

`Example_01.unity` should contain these functional objects:

```text
Example_01
├─ XR Interaction Manager
├─ EventSystem
├─ XR Origin (XR Rig)
│  ├─ Camera Offset
│  │  └─ Main Camera
│  ├─ Left Controller
│  └─ Right Controller
├─ XR Device Simulator
├─ Teleportation Areas / Anchors
├─ ScenePortal / ScenePortalTrigger objects
└─ Existing exhibition geometry, lights, materials, colliders, and content
```

Key rules:

- `XR Origin (XR Rig)` is the active player rig and the only active player camera source.
- Legacy non-XR cameras should be disabled if they would create duplicate active cameras or duplicate Audio Listeners.
- Existing `XR Interaction Manager` and `EventSystem` should be reused when present.
- The `EventSystem` should use an XR-compatible input module for XR UI interaction.
- Existing exhibition content should remain in place unless it directly blocks XR validation.

## Locomotion and Teleportation

The scene should enable both locomotion styles:

- Continuous movement for natural exhibition roaming.
- Ray-based teleportation for comfort, accessibility, and fast navigation.

Recommended components:

- `ContinuousMoveProvider` on the XR Origin.
- `SnapTurnProvider` on the XR Origin as the default turning mode, because snap turning is usually more comfortable in VR.
- `TeleportationProvider` on the XR Origin.
- `TeleportationArea` on valid walkable floor surfaces.
- `TeleportationAnchor` near important positions such as exhibit viewpoints, doors, or scene portals when precise placement is useful.

Walkable surfaces need colliders so controller rays and teleportation rays can hit them. If a floor mesh lacks a collider, add an appropriate collider before adding teleportation support.

## Editor Keyboard/Mouse Simulation

Add the XR Device Simulator prefab to `Example_01.unity` for headset-free development testing.

Expected Editor behavior:

1. Open `Assets/Scenes/Example_01.unity`.
2. Press Play without connecting a PICO headset.
3. Use XR Device Simulator controls to simulate headset/controller pose.
4. Use keyboard/mouse input to aim, select, move, turn, and teleport.
5. Confirm the simulator UI and input bindings are usable in Play Mode.

This lets scene layout, movement, teleportation, and portal logic be tested quickly before deploying to hardware.

## PICO Runtime Compatibility

The implementation should rely on standard XR Interaction Toolkit and OpenXR components so the same scene works on PICO.

Expected PICO behavior:

- The scene starts from the XR Origin camera.
- PICO headset pose drives the XR camera.
- PICO controllers drive XRI controller actions.
- Continuous movement, snap turning, ray interaction, and teleportation work through the same XRI action setup as Editor simulation.

Global OpenXR/PICO settings should not be overwritten as part of this scene task unless validation proves the scene cannot run without a specific setting change. Any required global setting change should be called out separately before modifying it.

## Portal Compatibility

Existing `ScenePortal` and `ScenePortalTrigger` behavior should continue to work with the XR Origin.

The current portal logic identifies the player through `Camera.main`, active cameras, XR Origin / XR Rig / Player root names, or trigger colliders. The VR scene setup should therefore:

- Keep the XR camera tagged or discoverable as the main player camera.
- Avoid duplicate active cameras that could confuse portal activation.
- Ensure the XR Origin's colliders or camera proximity can trigger scene portals as intended.

## Error Handling and Validation

The setup and validation flow should check for these issues:

- Missing `XR Origin (XR Rig)`.
- Missing `XR Device Simulator`.
- Missing or duplicated `XR Interaction Manager`.
- Missing `EventSystem` or non-XR UI input module.
- Duplicate active cameras or duplicate active Audio Listeners.
- Missing locomotion components.
- Missing teleportation provider or missing teleportation areas.
- Missing colliders on intended walkable surfaces.
- Broken prefab references or missing XRI sample assets.

## Testing Plan

### Editor tests

1. Open `Assets/Scenes/Example_01.unity`.
2. Press Play without a PICO headset.
3. Confirm no console errors related to missing XR references, input actions, duplicate cameras, or duplicate Audio Listeners.
4. Confirm XR Device Simulator can control headset and controller pose.
5. Confirm continuous movement works.
6. Confirm snap turning works.
7. Confirm ray teleportation works on configured walkable surfaces.
8. Confirm existing scene portals still load their target scenes when approached or triggered.

### PICO device tests

1. Confirm `Example_01.unity` is included in Build Settings.
2. Build/run to PICO using the existing OpenXR/PICO configuration.
3. Confirm the scene starts with the XR Origin camera.
4. Confirm headset tracking works.
5. Confirm PICO controllers can move, turn, raycast, and teleport.
6. Confirm no startup errors or controller binding failures appear.
7. Confirm performance is acceptable for exhibition navigation.

## Scope Boundaries

This design is limited to making `Assets/Scenes/Example_01.unity` VR-ready for Editor simulation and PICO runtime.

In scope:

- Add or repair XR Origin setup.
- Add XR Device Simulator for Editor testing.
- Configure continuous movement, snap turning, and teleportation.
- Ensure floor colliders/teleportation surfaces exist where needed.
- Preserve and verify existing scene portal behavior.
- Add focused Editor automation or validation if useful.

Out of scope unless separately requested:

- Redesigning all exhibition content.
- Rebuilding lighting or materials across the whole project.
- Replacing the global XR/OpenXR/PICO configuration.
- Optimizing every scene in the project.
- Creating new exhibit UI/interaction systems beyond what is required for VR movement and teleportation.
