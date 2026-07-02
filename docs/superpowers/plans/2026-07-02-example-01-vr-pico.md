# Example_01 VR / PICO / Keyboard-Mouse Simulation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Configure `Assets/Scenes/Example_01.unity` as a VR-ready exhibition scene that works in Unity Editor with keyboard/mouse XR Device Simulator and on PICO through the project's existing OpenXR/PICO setup.

**Architecture:** Add a focused Editor configurator that opens `Example_01.unity`, instantiates the existing XR Interaction Toolkit prefabs, preserves existing scene content, disables duplicate legacy cameras, and adds teleportation support to likely walkable floor geometry. Validate the result with an Editor validation method, package/build-setting checks, and manual Play Mode/PICO checks.

**Tech Stack:** Unity 6.4.11f1, Universal Render Pipeline 17.4.0, XR Interaction Toolkit 3.4.1, OpenXR 1.16.1, PICO OpenXR package, XR Management 4.5.4, Input System 1.19.0, Unity Editor scripting.

## Global Constraints

- Target scene: `Assets/Scenes/Example_01.unity`.
- Editor validation scene work must preserve existing exhibition content, lighting, materials, portals, and collaborator edits.
- Use `Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab` for the player rig.
- Use `Assets/Samples/XR Interaction Toolkit/3.4.1/XR Device Simulator/XR Device Simulator.prefab` for Editor keyboard/mouse testing.
- Reuse existing `XR Interaction Manager` and `EventSystem` when present.
- `XR Origin (XR Rig)` must be the active player rig and the only active player camera source.
- Keep the XR camera tagged or discoverable as the main player camera.
- Enable continuous movement, snap turning, and ray teleportation.
- Walkable teleportation surfaces must have colliders.
- Do not overwrite global OpenXR/PICO settings unless validation proves a specific setting change is required.
- Do not commit or push unless the user explicitly asks for git commits/pushes.

---

## File Structure

- Create: `Assets/Editor/Example01VRSceneConfigurator.cs`
  - One focused Editor-only automation class.
  - Opens and saves `Assets/Scenes/Example_01.unity`.
  - Provides `ConfigureExample01VR()` and `ValidateExample01VR()` static methods.
  - Adds Unity menu items for manual Editor use.
- Modify: `Assets/Scenes/Example_01.unity`
  - Adds or repairs `XR Origin (XR Rig)`.
  - Adds `XR Device Simulator`.
  - Ensures an `XR Interaction Manager` and XR-compatible `EventSystem` exist.
  - Disables legacy active cameras/audio listeners outside XR Origin.
  - Adds teleportation support to likely floor/walkable objects.
- Read/check: `Packages/manifest.json`
  - Confirms required XR/PICO dependencies remain present.
- Read/check: `ProjectSettings/EditorBuildSettings.asset`
  - Confirms `Example_01.unity` remains in Build Settings.
- Read/check only unless separately approved: `Assets/XR/Settings/OpenXRPackageSettings.asset`
  - Detects unintended global XR setting changes.

---

### Task 1: Add the Editor configurator and validator

**Files:**
- Create: `Assets/Editor/Example01VRSceneConfigurator.cs`

**Interfaces:**
- Consumes:
  - Scene path: `Assets/Scenes/Example_01.unity`.
  - XR Origin prefab path: `Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab`.
  - XR Device Simulator prefab path: `Assets/Samples/XR Interaction Toolkit/3.4.1/XR Device Simulator/XR Device Simulator.prefab`.
- Produces:
  - `Example01VRSceneConfigurator.ConfigureExample01VR()` static method.
  - `Example01VRSceneConfigurator.ValidateExample01VR()` static method.
  - Unity menu item `Tools/FutureGradExpoVR/Configure Example_01 VR`.
  - Unity menu item `Tools/FutureGradExpoVR/Validate Example_01 VR`.

- [ ] **Step 1: Create the configurator script**

Create `Assets/Editor/Example01VRSceneConfigurator.cs` with this complete content:

```csharp
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class Example01VRSceneConfigurator
{
    private const string ScenePath = "Assets/Scenes/Example_01.unity";
    private const string XrOriginPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
    private const string XrDeviceSimulatorPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/XR Device Simulator/XR Device Simulator.prefab";

    [MenuItem("Tools/FutureGradExpoVR/Configure Example_01 VR")]
    public static void ConfigureExample01VR()
    {
        if (!File.Exists(ScenePath))
            throw new FileNotFoundException($"Scene was not found: {ScenePath}", ScenePath);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        EnsureXRInteractionManager();
        EnsureEventSystem();
        var xrOrigin = EnsurePrefabInstance("XR Origin (XR Rig)", XrOriginPrefabPath);
        EnsurePrefabInstance("XR Device Simulator", XrDeviceSimulatorPrefabPath);
        EnsureXROriginCamera(xrOrigin);
        EnsureLocomotionProviders(xrOrigin);
        DisableLegacyCamerasOutsideXROrigin(xrOrigin);
        EnsureTeleportationAreas();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Configured Example_01 for XR Origin, PICO/OpenXR runtime use, teleportation, and XR Device Simulator testing.");
    }

    [MenuItem("Tools/FutureGradExpoVR/Validate Example_01 VR")]
    public static void ValidateExample01VR()
    {
        if (!File.Exists(ScenePath))
        {
            Debug.LogError($"Scene was not found: {ScenePath}");
            ExitIfBatchMode(1);
            return;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var errors = 0;

        errors += RequireObject("XR Origin (XR Rig)");
        errors += RequireObject("XR Device Simulator");
        errors += RequireComponentInScene("Unity.XR.CoreUtils.XROrigin");
        errors += RequireComponentInScene("UnityEngine.XR.Interaction.Toolkit.XRInteractionManager");
        errors += RequireComponentInScene("UnityEngine.EventSystems.EventSystem");
        errors += RequireComponentInScene("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule");
        errors += RequireComponentInScene("UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider");
        errors += RequireComponentInScene("UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.SnapTurnProvider");
        errors += RequireComponentInScene("UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider");
        errors += RequireComponentInScene("UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea");

        var activeCameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(camera => camera.enabled && camera.gameObject.activeInHierarchy)
            .ToArray();
        if (activeCameras.Length != 1)
        {
            Debug.LogError($"Example_01 has {activeCameras.Length} active enabled Cameras. Expected exactly one active XR camera.");
            errors++;
        }

        var activeAudioListeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(listener => listener.enabled && listener.gameObject.activeInHierarchy)
            .ToArray();
        if (activeAudioListeners.Length != 1)
        {
            Debug.LogError($"Example_01 has {activeAudioListeners.Length} active AudioListeners. Expected exactly one active AudioListener inside XR Origin.");
            errors++;
        }

        if (Camera.main == null)
        {
            Debug.LogError("Example_01 has no Camera.main. Expected the XR Origin camera to be tagged MainCamera.");
            errors++;
        }

        if (errors > 0)
        {
            Debug.LogError($"Example_01 VR validation failed with {errors} issue(s).");
            ExitIfBatchMode(1);
            return;
        }

        Debug.Log("Example_01 VR validation passed.");
        ExitIfBatchMode(0);
    }

    private static GameObject EnsurePrefabInstance(string expectedRootName, string prefabPath)
    {
        var existing = GameObject.Find(expectedRootName);
        if (existing != null)
            return existing;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            throw new InvalidOperationException($"Missing prefab: {prefabPath}");

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = expectedRootName;
        Undo.RegisterCreatedObjectUndo(instance, $"Create {expectedRootName}");
        return instance;
    }

    private static void EnsureXRInteractionManager()
    {
        var managerType = FindType("UnityEngine.XR.Interaction.Toolkit.XRInteractionManager");
        if (managerType == null)
            throw new InvalidOperationException("XRInteractionManager type was not found. Confirm XR Interaction Toolkit is installed.");

        if (UnityEngine.Object.FindFirstObjectByType(managerType) != null)
            return;

        var manager = new GameObject("XR Interaction Manager");
        manager.AddComponent(managerType);
        Undo.RegisterCreatedObjectUndo(manager, "Create XR Interaction Manager");
    }

    private static void EnsureEventSystem()
    {
        var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var go = new GameObject("EventSystem");
            eventSystem = go.AddComponent<EventSystem>();
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        }

        var xrUiInputModuleType = FindType("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule");
        if (xrUiInputModuleType == null)
            throw new InvalidOperationException("XRUIInputModule type was not found. Confirm XR Interaction Toolkit is installed.");

        var goWithEventSystem = eventSystem.gameObject;
        if (goWithEventSystem.GetComponent(xrUiInputModuleType) == null)
            goWithEventSystem.AddComponent(xrUiInputModuleType);

        foreach (var inputModule in goWithEventSystem.GetComponents<BaseInputModule>())
        {
            if (inputModule.GetType() != xrUiInputModuleType)
                inputModule.enabled = false;
        }
    }

    private static void EnsureXROriginCamera(GameObject xrOrigin)
    {
        var cameras = xrOrigin.GetComponentsInChildren<Camera>(true);
        if (cameras.Length == 0)
            throw new InvalidOperationException("XR Origin prefab instance does not contain a Camera.");

        var xrCamera = cameras.FirstOrDefault(camera => camera.name == "Main Camera") ?? cameras[0];
        xrCamera.enabled = true;
        xrCamera.tag = "MainCamera";

        var listener = xrCamera.GetComponent<AudioListener>();
        if (listener == null)
            listener = xrCamera.gameObject.AddComponent<AudioListener>();
        listener.enabled = true;
    }

    private static void EnsureLocomotionProviders(GameObject xrOrigin)
    {
        AddComponentIfMissing(xrOrigin, "UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider");
        AddComponentIfMissing(xrOrigin, "UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.SnapTurnProvider");
        AddComponentIfMissing(xrOrigin, "UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider");
    }

    private static void DisableLegacyCamerasOutsideXROrigin(GameObject xrOrigin)
    {
        foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (camera.transform.IsChildOf(xrOrigin.transform))
                continue;

            camera.enabled = false;
            if (camera.CompareTag("MainCamera"))
                camera.tag = "Untagged";

            var audioListener = camera.GetComponent<AudioListener>();
            if (audioListener != null)
                audioListener.enabled = false;
        }
    }

    private static void EnsureTeleportationAreas()
    {
        var teleportationAreaType = FindType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea");
        if (teleportationAreaType == null)
            throw new InvalidOperationException("TeleportationArea type was not found. Confirm XR Interaction Toolkit is installed.");

        var candidates = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Select(renderer => renderer.gameObject)
            .Where(go => IsLikelyWalkableSurface(go.name))
            .Distinct()
            .ToArray();

        if (candidates.Length == 0)
        {
            var fallback = GameObject.Find("Plane") ?? GameObject.Find("Floor") ?? GameObject.Find("Ground");
            if (fallback != null)
                candidates = new[] { fallback };
        }

        if (candidates.Length == 0)
            throw new InvalidOperationException("No likely walkable floor surface found in Example_01. Add a floor object named Plane, Floor, Ground, or containing floor/ground/plane/walk in its name.");

        foreach (var candidate in candidates)
        {
            if (candidate.GetComponent<Collider>() == null)
                candidate.AddComponent<BoxCollider>();

            AddComponentIfMissing(candidate, teleportationAreaType);
        }
    }

    private static bool IsLikelyWalkableSurface(string objectName)
    {
        var name = objectName.ToLowerInvariant();
        return name.Contains("floor") || name.Contains("ground") || name.Contains("plane") || name.Contains("walk") || name.Contains("地面");
    }

    private static void AddComponentIfMissing(GameObject target, string fullTypeName)
    {
        var type = FindType(fullTypeName);
        if (type == null)
            throw new InvalidOperationException($"Required type was not found: {fullTypeName}");

        AddComponentIfMissing(target, type);
    }

    private static void AddComponentIfMissing(GameObject target, Type type)
    {
        if (target.GetComponent(type) == null)
            target.AddComponent(type);
    }

    private static int RequireObject(string objectName)
    {
        if (GameObject.Find(objectName) != null)
            return 0;

        Debug.LogError($"Missing required GameObject: {objectName}");
        return 1;
    }

    private static int RequireComponentInScene(string fullTypeName)
    {
        var type = FindType(fullTypeName);
        if (type == null)
        {
            Debug.LogError($"Missing required type: {fullTypeName}");
            return 1;
        }

        var components = UnityEngine.Object.FindObjectsByType(type, FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (components.Length > 0)
            return 0;

        Debug.LogError($"Missing required active component in scene: {fullTypeName}");
        return 1;
    }

    private static Type FindType(string fullTypeName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullTypeName, false))
            .FirstOrDefault(type => type != null);
    }

    private static void ExitIfBatchMode(int exitCode)
    {
        if (Application.isBatchMode)
            EditorApplication.Exit(exitCode);
    }
}
#endif
```

- [ ] **Step 2: Import and compile the script**

Run from Git Bash at the repository root:

```bash
'/d/Software/unity/6000.4.11f1/Editor/Unity.exe' -batchmode -quit -projectPath "$(pwd)" -logFile Logs/example01-configurator-import.log
```

Expected:

```text
Unity exits with code 0.
Logs/example01-configurator-import.log contains no "error CS" compiler errors.
```

If that Unity executable path does not exist on this machine, use the installed Unity 6.4.11f1 Editor path shown by Unity Hub and keep the same `-batchmode -quit -projectPath "$(pwd)" -logFile ...` arguments.

- [ ] **Step 3: Review the script diff**

Run:

```bash
git diff -- Assets/Editor/Example01VRSceneConfigurator.cs
```

Expected: the diff contains only the new Editor configurator script.

---

### Task 2: Configure `Example_01.unity` for XR Origin, simulator, and teleportation

**Files:**
- Modify: `Assets/Scenes/Example_01.unity`
- May modify automatically if Unity requires it: `Assets/Scenes/Example_01.unity.meta`

**Interfaces:**
- Consumes:
  - `Example01VRSceneConfigurator.ConfigureExample01VR()` from Task 1.
  - `Example01VRSceneConfigurator.ValidateExample01VR()` from Task 1.
- Produces:
  - `Example_01.unity` containing `XR Origin (XR Rig)`.
  - `Example_01.unity` containing `XR Device Simulator`.
  - `Example_01.unity` with one active XR camera tagged `MainCamera`.
  - `Example_01.unity` with continuous movement, snap turn, teleport provider, and at least one teleportation area.

- [ ] **Step 1: Run the scene configurator**

Preferred manual path in Unity Editor:

```text
Tools / FutureGradExpoVR / Configure Example_01 VR
```

Batchmode alternative from Git Bash:

```bash
'/d/Software/unity/6000.4.11f1/Editor/Unity.exe' -batchmode -quit -projectPath "$(pwd)" -executeMethod Example01VRSceneConfigurator.ConfigureExample01VR -logFile Logs/example01-configure.log
```

Expected:

```text
Unity exits with code 0.
Assets/Scenes/Example_01.unity is modified.
Logs/example01-configure.log contains "Configured Example_01 for XR Origin".
```

- [ ] **Step 2: Validate the configured scene**

Preferred manual path in Unity Editor:

```text
Tools / FutureGradExpoVR / Validate Example_01 VR
```

Batchmode alternative from Git Bash:

```bash
'/d/Software/unity/6000.4.11f1/Editor/Unity.exe' -batchmode -quit -projectPath "$(pwd)" -executeMethod Example01VRSceneConfigurator.ValidateExample01VR -logFile Logs/example01-validate.log
```

Expected:

```text
Unity exits with code 0.
Logs/example01-validate.log contains "Example_01 VR validation passed.".
```

- [ ] **Step 3: Inspect scene and script diffs**

Run:

```bash
git diff --name-only -- Assets/Editor/Example01VRSceneConfigurator.cs Assets/Scenes/Example_01.unity Assets/Scenes/Example_01.unity.meta
```

Expected output includes:

```text
Assets/Editor/Example01VRSceneConfigurator.cs
Assets/Scenes/Example_01.unity
```

Run:

```bash
git diff -- Assets/Scenes/Example_01.unity | python -c "import sys; data=sys.stdin.read(); print(data[:12000]); print('...truncated...' if len(data)>12000 else '', end='')"
```

Expected: the visible diff includes additions or prefab modifications for `XR Origin (XR Rig)`, `XR Device Simulator`, locomotion providers, and teleportation areas. It should not include unrelated edits to other scenes.

---

### Task 3: Check PICO/OpenXR readiness without overwriting global settings

**Files:**
- Read: `Packages/manifest.json`
- Read: `ProjectSettings/EditorBuildSettings.asset`
- Read/check only: `Assets/XR/Settings/OpenXRPackageSettings.asset`
- Read/check only: `Packages/packages-lock.json`

**Interfaces:**
- Consumes:
  - Configured and validator-passing `Example_01.unity` from Task 2.
- Produces:
  - Confirmation that required XR/PICO packages are present.
  - Confirmation that `Example_01.unity` is in Build Settings.
  - Confirmation that this work did not introduce unintended global XR setting changes.

- [ ] **Step 1: Confirm required packages remain installed**

Run:

```bash
python - <<'PY'
import json
from pathlib import Path
manifest = json.loads(Path('Packages/manifest.json').read_text(encoding='utf-8'))
required = [
    'com.unity.xr.interaction.toolkit',
    'com.unity.xr.openxr',
    'com.unity.xr.openxr.picoxr',
    'com.unity.xr.management',
    'com.unity.inputsystem',
]
missing = [pkg for pkg in required if pkg not in manifest['dependencies']]
print('missing:', missing)
raise SystemExit(1 if missing else 0)
PY
```

Expected:

```text
missing: []
```

- [ ] **Step 2: Confirm `Example_01.unity` remains in Build Settings**

Run:

```bash
python - <<'PY'
from pathlib import Path
text = Path('ProjectSettings/EditorBuildSettings.asset').read_text(encoding='utf-8', errors='replace')
needle = 'path: Assets/Scenes/Example_01.unity'
print('Example_01 in build settings:', needle in text)
raise SystemExit(0 if needle in text else 1)
PY
```

Expected:

```text
Example_01 in build settings: True
```

- [ ] **Step 3: Check for unintended global XR/package/build-setting changes**

Run:

```bash
git diff --name-only -- Assets/XR/Settings/OpenXRPackageSettings.asset Packages/manifest.json Packages/packages-lock.json ProjectSettings/EditorBuildSettings.asset
```

Expected: no output caused by this task. If there is output, inspect it before deciding whether it belongs to the user's existing uncommitted work or a necessary change.

---

### Task 4: Manual Editor verification with keyboard and mouse simulation

**Files:**
- Read: `Assets/Scenes/Example_01.unity`
- Create for local notes only: `Logs/example01-playmode-check-notes.txt`

**Interfaces:**
- Consumes:
  - Configured scene from Task 2.
  - PICO/OpenXR readiness checks from Task 3.
- Produces:
  - Manual verification notes confirming Editor simulation behavior.

- [ ] **Step 1: Open the scene**

Open `Assets/Scenes/Example_01.unity` in Unity Editor.

Expected hierarchy entries:

```text
XR Interaction Manager
EventSystem
XR Origin (XR Rig)
XR Device Simulator
```

- [ ] **Step 2: Press Play and verify XR Device Simulator**

Press Play in Unity Editor.

Expected:

```text
XR Device Simulator UI appears or simulator controls respond.
No console errors about missing input actions, missing interaction manager, duplicate cameras, or duplicate AudioListener.
```

- [ ] **Step 3: Verify continuous movement and turning**

Use XR Device Simulator keyboard/mouse controls shown by the simulator UI to simulate headset/controller movement.

Expected:

```text
The XR Origin camera moves through the scene.
Snap turning changes view direction.
The scene remains rendered through the XR Origin camera.
```

- [ ] **Step 4: Verify ray teleportation**

Use the simulated controller ray to point at a configured floor/walkable surface and activate teleport.

Expected:

```text
Teleport ray can hit the walkable surface.
The XR Origin moves to the selected destination.
No console errors are emitted by TeleportationProvider or XRRayInteractor.
```

- [ ] **Step 5: Verify portal compatibility**

Move the XR Origin camera or XR rig collider into an existing portal activation area.

Expected:

```text
The existing ScenePortal / ScenePortalTrigger logic still recognizes the XR player.
The target scene loads when the portal condition is met.
No duplicate camera or Camera.main confusion prevents portal activation.
```

- [ ] **Step 6: Write local manual verification notes**

Create `Logs/example01-playmode-check-notes.txt` with this template and replace the bracketed values with observed results:

```text
Example_01 VR Play Mode Check

XR Device Simulator visible/responding: [PASS or FAIL]
Continuous movement: [PASS or FAIL]
Snap turning: [PASS or FAIL]
Ray teleportation: [PASS or FAIL]
Scene portal compatibility: [PASS or FAIL]
Duplicate camera errors: [NONE or DETAILS]
Duplicate AudioListener errors: [NONE or DETAILS]
XR input/interaction errors: [NONE or DETAILS]
Tester notes: [short note about any scene-specific issue observed]
```

Do not commit `Logs/example01-playmode-check-notes.txt` unless the user explicitly asks to track manual logs.

---

### Task 5: Optional PICO device verification

**Files:**
- Read: `Assets/Scenes/Example_01.unity`
- Read: `ProjectSettings/EditorBuildSettings.asset`
- Read: `Assets/XR/Settings/OpenXRPackageSettings.asset`

**Interfaces:**
- Consumes:
  - Editor-validated scene from Task 4.
- Produces:
  - Device-level confirmation that PICO headset/controller runtime works.

- [ ] **Step 1: Build/run to PICO from Unity Editor**

Use Unity Editor's Android/PICO build workflow already configured in the project. Build and run with `Assets/Scenes/Example_01.unity` included in Build Settings.

Expected:

```text
The build installs and launches on the PICO headset.
The initial view comes from XR Origin's Main Camera.
Head tracking moves the camera.
```

- [ ] **Step 2: Verify PICO controller locomotion**

On the PICO device, test controller input.

Expected:

```text
Left controller stick or configured movement input moves the player.
Right controller stick or configured turning input snap-turns the player.
Controller ray appears and can target teleportation surfaces.
Teleport action moves the player to the selected target.
```

- [ ] **Step 3: Record device notes locally**

Create `Logs/example01-pico-check-notes.txt` with this template and replace the bracketed values with observed results:

```text
Example_01 PICO Device Check

Build installs/launches: [PASS or FAIL]
XR Origin camera is active: [PASS or FAIL]
Head tracking: [PASS or FAIL]
Controller tracking: [PASS or FAIL]
Continuous movement: [PASS or FAIL]
Snap turning: [PASS or FAIL]
Ray teleportation: [PASS or FAIL]
Startup/runtime errors: [NONE or DETAILS]
Tester notes: [short note about any device-specific issue observed]
```

Do not commit `Logs/example01-pico-check-notes.txt` unless the user explicitly asks to track manual logs.

---

## Self-Review

Spec coverage:

- Editor keyboard/mouse simulation is covered by Task 2 adding XR Device Simulator and Task 4 manual verification.
- PICO/OpenXR compatibility is covered by Task 3 package/build-setting checks, preserving global settings, and Task 5 device verification.
- XR Origin as the single active player camera is covered by Task 1 configurator/validator and Task 2 validation.
- Continuous movement, snap turning, and ray teleportation are covered by Task 1 script, Task 2 scene configuration, and Task 4/5 verification.
- Walkable collider and teleportation requirements are covered by `EnsureTeleportationAreas()` in Task 1 and validation in Task 2.
- Existing portal compatibility is covered by Task 1 camera rules and Task 4 portal verification.
- Scope boundaries are enforced by global constraints and the Task 3 global settings diff check.

Placeholder scan:

- No `TBD`, `TODO`, `implement later`, or undefined implementation references remain.
- Bracketed `PASS or FAIL` markers appear only in manual local note templates that the tester fills with observed results.

Type consistency:

- `ConfigureExample01VR()` and `ValidateExample01VR()` are defined in Task 1 and consumed by Task 2.
- Unity type names match the installed XR Interaction Toolkit 3.4.1 package namespace checks.
- File paths match the approved design document and current project layout.
