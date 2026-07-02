# Area Teleport Trigger Design

## Goal

Add a same-scene VR teleport trigger for the exhibition scene: when the player stands inside a configured trigger area for a short delay, the whole XR Origin is moved to a specified target point.

## Current Project Context

The project already contains scene-loading portal scripts:

- `Assets/Scripts/ScenePortal.cs`
- `Assets/Scripts/ScenePortalTrigger.cs`

Those scripts load another scene. This feature is different: it should keep the current scene loaded and move the player within the same scene.

The VR player is represented by `XR Origin (XR Rig)`, so teleporting should move the XR Origin root rather than moving the `Main Camera` directly.

## Recommended Approach

Create a focused runtime script:

- `Assets/Scripts/AreaTeleportTrigger.cs`

The script will be placed on a GameObject with a trigger collider. Designers can assign a target `Transform` in the Inspector.

## Scene Setup

A typical setup uses two objects:

```text
Teleport_Area_A
├─ Box Collider (Is Trigger = true)
└─ AreaTeleportTrigger

Teleport_Target_B
└─ Transform only
```

`Teleport_Area_A` is the region the player stands in. `Teleport_Target_B` is where the XR Origin will be moved.

## Behavior

1. Player enters the trigger area.
2. Script verifies the collider belongs to the XR player.
3. A delay timer starts, defaulting to `1.5` seconds.
4. If the player remains inside the area until the timer completes, the script moves `XR Origin (XR Rig)` to the target point.
5. If the player exits before the timer completes, the timer is cancelled.
6. After teleporting, a cooldown prevents immediate retriggering.

## Teleport Method

The script should move the XR Origin root. It should not directly move `Main Camera`, because the camera is offset by headset tracking inside the XR rig.

Target placement rules:

- Target position becomes the XR Origin root position.
- Target Y rotation becomes the XR Origin root Y rotation.
- X/Z rotation should be ignored to avoid tilting the player rig.

## Error Handling

The script should warn and do nothing if:

- No `targetPoint` is assigned.
- No XR Origin can be found.
- The trigger object has no Collider.
- The Collider is not marked `isTrigger`.

## Inspector Fields

Recommended fields:

- `Transform targetPoint`
- `float delaySeconds = 1.5f`
- `float cooldownSeconds = 1.0f`
- `bool alignYawToTarget = true`
- `string xrOriginNameHint = "XR Origin"`

## Testing

Editor test:

1. Add a cube or empty object named `Teleport_Area_A`.
2. Add a `Box Collider` and enable `Is Trigger`.
3. Add `AreaTeleportTrigger`.
4. Create an empty object named `Teleport_Target_B`.
5. Assign `Teleport_Target_B` to `targetPoint`.
6. Play with XR Device Simulator.
7. Move the XR Origin into the trigger area.
8. Confirm the player teleports after the configured delay.
9. Step out before the delay completes and confirm teleport is cancelled.
10. Confirm no repeated instant teleport happens during cooldown.

PICO test:

1. Build/run to PICO.
2. Walk into the configured trigger area.
3. Remain inside for the delay.
4. Confirm the XR Origin moves to the target point.

## Scope Boundaries

In scope:

- Same-scene area-to-point teleport.
- Delay before teleport.
- Cooldown after teleport.
- XR Origin-aware movement.

Out of scope for the first version:

- Scene loading.
- Fade-to-black transition.
- UI countdown.
- Audio prompts.
- Button-confirmed teleport.
