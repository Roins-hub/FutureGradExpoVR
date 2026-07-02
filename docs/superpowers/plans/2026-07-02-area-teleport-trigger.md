# Area Teleport Trigger Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a same-scene VR area teleport trigger that moves the whole `XR Origin (XR Rig)` to a configured target point after the player stands in a trigger area for a short delay.

**Architecture:** Create one focused runtime MonoBehaviour, `AreaTeleportTrigger`, placed on a trigger collider GameObject. The script detects XR player colliders, starts a delay timer while the player remains inside, moves the XR Origin root to the target transform, and enforces a cooldown to prevent immediate retriggering.

**Tech Stack:** Unity 6.4.11f1, C#, MonoBehaviour, PhysX trigger colliders, XR Interaction Toolkit scene setup with `XR Origin (XR Rig)`.

## Global Constraints

- Target script path: `Assets/Scripts/AreaTeleportTrigger.cs`.
- Same-scene teleport only; do not load another scene.
- Move the XR Origin root, not `Main Camera` directly.
- Default delay before teleport: `1.5` seconds.
- Default cooldown after teleport: `1.0` seconds.
- Target Y rotation may align the XR Origin yaw; X/Z rotation must not tilt the player rig.
- Warn and do nothing if `targetPoint` is missing.
- Warn and do nothing if no XR Origin can be found.
- Warn if the trigger object has no `Collider` or its collider is not marked `isTrigger`.
- Do not commit or push unless the user explicitly asks for git commits/pushes.

---

## File Structure

- Create: `Assets/Scripts/AreaTeleportTrigger.cs`
  - Runtime component for same-scene trigger-area teleport.
  - Designer-facing fields for target point, delay, cooldown, yaw alignment, and XR Origin name hint.
  - Uses trigger enter/exit to start/cancel delayed teleport.
- No scene file needs to be edited by the plan itself.
  - Scene setup is manual in Unity Editor after the script compiles.

---

### Task 1: Add the runtime area teleport trigger script

**Files:**
- Create: `Assets/Scripts/AreaTeleportTrigger.cs`

**Interfaces:**
- Consumes:
  - A trigger collider on the same GameObject as `AreaTeleportTrigger`.
  - `Transform targetPoint` assigned in Inspector.
  - An XR Origin GameObject named with `xrOriginNameHint`, default `XR Origin`.
- Produces:
  - Public MonoBehaviour class `AreaTeleportTrigger`.
  - Inspector fields:
    - `Transform targetPoint`
    - `float delaySeconds`
    - `float cooldownSeconds`
    - `bool alignYawToTarget`
    - `string xrOriginNameHint`

- [ ] **Step 1: Create the script**

Create `Assets/Scripts/AreaTeleportTrigger.cs` with this complete content:

```csharp
using System.Collections;
using UnityEngine;

/// <summary>
/// Same-scene VR teleport trigger.
/// Put this on a trigger collider; when the XR player remains inside long enough,
/// the whole XR Origin is moved to the configured target point.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class AreaTeleportTrigger : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform targetPoint;

    [Header("Timing")]
    [Min(0f)]
    [SerializeField] private float delaySeconds = 1.5f;

    [Min(0f)]
    [SerializeField] private float cooldownSeconds = 1f;

    [Header("XR Origin")]
    [SerializeField] private bool alignYawToTarget = true;
    [SerializeField] private string xrOriginNameHint = "XR Origin";

    private Transform xrOrigin;
    private Coroutine pendingTeleport;
    private float nextAllowedTeleportTime;
    private int playerColliderCount;

    private void Reset()
    {
        ConfigureTriggerCollider();
    }

    private void Awake()
    {
        ConfigureTriggerCollider();
        xrOrigin = FindXROrigin();
    }

    private void OnValidate()
    {
        if (delaySeconds < 0f)
            delaySeconds = 0f;

        if (cooldownSeconds < 0f)
            cooldownSeconds = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerColliderCount++;

        if (pendingTeleport != null)
            return;

        if (Time.unscaledTime < nextAllowedTeleportTime)
            return;

        pendingTeleport = StartCoroutine(TeleportAfterDelay());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerColliderCount = Mathf.Max(0, playerColliderCount - 1);

        if (playerColliderCount == 0 && pendingTeleport != null)
        {
            StopCoroutine(pendingTeleport);
            pendingTeleport = null;
        }
    }

    private IEnumerator TeleportAfterDelay()
    {
        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);

        pendingTeleport = null;

        if (playerColliderCount <= 0)
            yield break;

        TeleportNow();
    }

    private void TeleportNow()
    {
        if (targetPoint == null)
        {
            Debug.LogWarning($"[{nameof(AreaTeleportTrigger)}] No target point assigned on {name}.", this);
            return;
        }

        if (xrOrigin == null)
            xrOrigin = FindXROrigin();

        if (xrOrigin == null)
        {
            Debug.LogWarning($"[{nameof(AreaTeleportTrigger)}] Could not find XR Origin for {name}.", this);
            return;
        }

        xrOrigin.position = targetPoint.position;

        if (alignYawToTarget)
        {
            Vector3 euler = xrOrigin.eulerAngles;
            euler.y = targetPoint.eulerAngles.y;
            xrOrigin.eulerAngles = euler;
        }

        nextAllowedTeleportTime = Time.unscaledTime + cooldownSeconds;
        playerColliderCount = 0;
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        if (xrOrigin == null)
            xrOrigin = FindXROrigin();

        Transform otherTransform = other.transform;

        if (xrOrigin != null)
        {
            if (otherTransform == xrOrigin || otherTransform.IsChildOf(xrOrigin))
                return true;

            if (xrOrigin.IsChildOf(otherTransform))
                return true;
        }

        if (Camera.main != null)
        {
            Transform cameraTransform = Camera.main.transform;
            if (otherTransform == cameraTransform || otherTransform.IsChildOf(cameraTransform.root))
                return true;
        }

        string rootName = otherTransform.root.name;
        return rootName.Contains("XR Origin") || rootName.Contains("XR Rig") || rootName.Contains("Player");
    }

    private Transform FindXROrigin()
    {
        if (!string.IsNullOrWhiteSpace(xrOriginNameHint))
        {
            GameObject byName = GameObject.Find(xrOriginNameHint);
            if (byName != null)
                return byName.transform;

            Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Transform candidate in allTransforms)
            {
                if (candidate.name.Contains(xrOriginNameHint))
                    return candidate;
            }
        }

        if (Camera.main != null)
            return Camera.main.transform.root;

        return null;
    }

    private void ConfigureTriggerCollider()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            Debug.LogWarning($"[{nameof(AreaTeleportTrigger)}] {name} needs a Collider.", this);
            return;
        }

        if (!triggerCollider.isTrigger)
        {
            triggerCollider.isTrigger = true;
            Debug.Log($"[{nameof(AreaTeleportTrigger)}] Enabled Is Trigger on {name}.", this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            Gizmos.DrawWireCube(triggerCollider.bounds.center, triggerCollider.bounds.size);

        if (targetPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(targetPoint.position, 0.25f);
            Gizmos.DrawLine(transform.position, targetPoint.position);
        }
    }
}
```

- [ ] **Step 2: Let Unity compile the script**

Because the Unity Editor is already open, use the Editor instead of batchmode:

```text
Assets / Refresh
```

Then wait for script compilation to finish.

Expected:

```text
Unity Console has no red C# compilation errors for AreaTeleportTrigger.cs.
```

- [ ] **Step 3: If Console shows a compile error, copy it exactly**

Expected if there is an error:

```text
The Console message includes a file path, line number, and error code such as CSxxxx.
```

Do not continue scene setup until the script compiles.

---

### Task 2: Set up one area-to-target teleport pair in the Unity Editor

**Files:**
- Modify manually in Unity Editor: `Assets/Scenes/Example_01.unity`

**Interfaces:**
- Consumes:
  - `AreaTeleportTrigger` from Task 1.
- Produces:
  - Scene object `Teleport_Area_A` with trigger collider and `AreaTeleportTrigger`.
  - Scene object `Teleport_Target_B` assigned as the trigger target.

- [ ] **Step 1: Create the source area object**

In Unity Hierarchy, create a cube:

```text
GameObject / 3D Object / Cube
```

Rename it:

```text
Teleport_Area_A
```

Expected: `Teleport_Area_A` appears in the Hierarchy.

- [ ] **Step 2: Position and scale the trigger area**

Set `Teleport_Area_A` Transform in the Inspector to a visible test location near the current player start. Example values:

```text
Position: X = 0, Y = 0.05, Z = 2
Rotation: X = 0, Y = 0, Z = 0
Scale:    X = 2, Y = 0.1, Z = 2
```

Expected: a flat square area appears on the floor.

- [ ] **Step 3: Configure collider as trigger**

On `Teleport_Area_A`, in the `Box Collider` component, enable:

```text
Is Trigger = true
```

Expected: the collider is a trigger and will not block player movement.

- [ ] **Step 4: Optional visual material**

If the cube blocks the scene visually, disable its `Mesh Renderer` component.

Expected: the trigger area can still work even if the mesh is hidden.

- [ ] **Step 5: Create the target point**

Create an empty GameObject:

```text
GameObject / Create Empty
```

Rename it:

```text
Teleport_Target_B
```

Set an example Transform:

```text
Position: X = 0, Y = 0, Z = 8
Rotation: X = 0, Y = 180, Z = 0
Scale:    X = 1, Y = 1, Z = 1
```

Expected: `Teleport_Target_B` marks where the XR Origin should appear.

- [ ] **Step 6: Add AreaTeleportTrigger to the source area**

Select `Teleport_Area_A`, click:

```text
Add Component / Area Teleport Trigger
```

Expected: `Area Teleport Trigger` appears in the Inspector.

- [ ] **Step 7: Assign target and timing**

Drag `Teleport_Target_B` from the Hierarchy into `Teleport_Area_A`'s `Target Point` field.

Set:

```text
Delay Seconds = 1.5
Cooldown Seconds = 1
Align Yaw To Target = enabled
XR Origin Name Hint = XR Origin
```

Expected: the component has a valid target point and timing values.

---

### Task 3: Verify the area teleport in Play Mode

**Files:**
- Read/modify manually in Unity Editor: `Assets/Scenes/Example_01.unity`

**Interfaces:**
- Consumes:
  - Configured `Teleport_Area_A` and `Teleport_Target_B` from Task 2.
- Produces:
  - Manual confirmation that standing in the trigger area teleports the XR Origin after the delay.

- [ ] **Step 1: Enter Play Mode in Game view**

Click the `Game / 游戏` tab, then press Play.

Expected:

```text
The XR Device Simulator overlay appears in Game view.
Keyboard/mouse input controls the simulator when Game view is focused.
```

- [ ] **Step 2: Move the XR player into Teleport_Area_A**

Use the XR Device Simulator controls that are now working in Game view to move the player into `Teleport_Area_A`.

Expected:

```text
The player remains inside the trigger area without being physically blocked.
```

- [ ] **Step 3: Remain inside for the delay**

Stay inside the trigger area for at least:

```text
1.5 seconds
```

Expected:

```text
The XR Origin moves to Teleport_Target_B.
The view changes to the target position.
```

- [ ] **Step 4: Test cancellation**

Move into `Teleport_Area_A`, then leave before `1.5` seconds.

Expected:

```text
No teleport occurs when the player leaves early.
```

- [ ] **Step 5: Test cooldown**

After a successful teleport, immediately re-enter or remain near a trigger.

Expected:

```text
The trigger does not repeatedly teleport every frame.
```

- [ ] **Step 6: Save the scene if behavior is correct**

If the test works, save the scene:

```text
File / Save
```

Expected:

```text
Assets/Scenes/Example_01.unity contains the configured trigger and target objects.
```

---

## Self-Review

Spec coverage:

- Same-scene area-to-point teleport is implemented by `AreaTeleportTrigger.TeleportNow()`.
- Delay before teleport is implemented by `TeleportAfterDelay()` and `delaySeconds`.
- Cooldown after teleport is implemented by `nextAllowedTeleportTime` and `cooldownSeconds`.
- XR Origin-aware movement is implemented by `FindXROrigin()` and moving `xrOrigin.position`.
- Target yaw alignment is implemented by assigning only `euler.y` from the target point.
- Missing target, missing XR Origin, and collider/trigger warnings are covered by `TeleportNow()` and `ConfigureTriggerCollider()`.
- Scene setup and Editor testing are covered by Tasks 2 and 3.

Placeholder scan:

- No `TBD`, `TODO`, `implement later`, or undefined implementation references remain.
- All code steps include complete code.

Type consistency:

- Public class name `AreaTeleportTrigger` matches file name `AreaTeleportTrigger.cs`.
- Inspector fields match the approved design names and defaults.
- Unity APIs used are available in Unity 6.4.11f1.
