# Example_01 VR / PICO / Keyboard-Mouse Simulation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Configure `Assets/Scenes/Example_01.unity` as a VR-ready scene for PICO devices with continuous movement, ray teleportation, and keyboard/mouse simulation in the Unity Editor.

**Architecture:** Add a focused Editor configurator that opens `Example_01.unity`, instantiates the existing XRI prefabs, preserves existing scene content, disables legacy non-XR cameras, and adds teleportation support to walkable floor geometry. Validate the result with a companion Editor validation method before manual Play Mode testing.

**Tech Stack:** Unity 6.4.11f1, XR Interaction Toolkit 3.4.1, OpenXR 1.16.1, PICO OpenXR package, XR Device Simulator, Unity Editor scripting.

## Global Constraints

- Only configure `Assets/Scenes/Example_01.unity` unless Unity requires dependency metadata updates.
- Do not overwrite global OpenXR/PICO settings unless validation proves the scene cannot work without it.
- Reuse existing XRI sample assets from `Assets/Samples/XR Interaction Toolkit/3.4.1/`.
- Preserve existing `EventSystem` and `XR Interaction Manager` when present.
- Support both continuous movement/turning and ray-based teleportation.
- Support Editor keyboard/mouse simulation through the XR Device Simulator prefab.
- Avoid unrelated scene, package, or project setting changes.

---

## File Structure

- Create `Assets/Editor/Example01VRSceneConfigurator.cs`
  - One-off Editor automation for configuring and validating `Example_01.unity`.
  - Provides `ConfigureExample01VR()` and `ValidateExample01VR()` static methods for Unity batchmode or menu execution.
- Modify `Assets/Scenes/Example_01.unity`
  - Add `XR Origin (XR Rig)` prefab instance when missing.
  - Add `XR Device Simulator` prefab instance when missing.
  - Ensure one `XR Interaction Manager` and one XR-compatible `EventSystem` exist.
  - Disable legacy scene cameras/audio listeners that are not inside XR Origin.
  - Add teleportation components/colliders to likely floor objects.
- No intended changes to `Assets/XR/Settings/OpenXRPackageSettings.asset`, `Packages/manifest.json`, or other scenes.

---

### Task 1: Add an Editor configurator and validator

**Files:**
- Create: `Assets/Editor/Example01VRSceneConfigurator.cs`

**Interfaces:**
- Consumes:
  - Scene path: `Assets/Scenes/Example_01.unity`
  - XR Origin prefab path: `Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab`
  - XR Device Simulator prefab path: `Assets/Samples/XR Interaction Toolkit/3.4.1/XR Device Simulator/XR Device Simulator.prefab`
- Produces:
  - `Example01VRSceneConfigurator.ConfigureExample01VR()` static method.
  - `Example01VRSceneConfigurator.ValidateExample01VR()` static method.
  - Unity menu items:
    - `Tools/FutureGradExpoVR/Configure Example_01 VR`
    - `Tools/FutureGradExpoVR/Validate Example_01 VR`

- [ ] **Step 1: Write the configurator script**

Create `Assets/Editor/Example01VRSceneConfigurator.cs` with this complete content:

```csharp
#if UNITY_EDITOR
using System;
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
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        EnsureXRInteractionManager();
        EnsureEventSystem();
        var xrOrigin = EnsurePrefabInstance("XR Origin (XR Rig)", XrOriginPrefabPath);
        EnsurePrefabInstance("XR Device Simulator", XrDeviceSimulatorPrefabPath);
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
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var errors = 0;

        errors += RequireObject("XR Origin (XR Rig)");
        errors += RequireObject("XR Device Simulator");
        errors += RequireObject("XR Interaction Manager");
        errors += RequireObject("EventSystem");

        if (FindType("Unity.XR.CoreUtils.XROrigin") != null)
            errors += RequireComponentInScene("Unity.XR.CoreUtils.XROrigin");

        if (FindType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider") != null)
            errors += RequireComponentInScene("UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider");

        if (FindType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.SnapTurnProvider") != null)
            errors += RequireComponentInScene("UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.SnapTurnProvider");

        if (FindType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider") != null)
            errors += RequireComponentInScene("UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider");

        if (FindType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea") != null)
            errors += RequireComponentInScene("UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea");

        var duplicateActiveAudioListeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
        if (duplicateActiveAudioListeners > 1)
        {
            Debug.LogError($"Example_01 has {duplicateActiveAudioListeners} active AudioListeners. Expected exactly one active AudioListener inside XR Origin.");
            errors++;
        }

        if (errors > 0)
        {
            Debug.LogError($"Example_01 VR validation failed with {errors} issue(s).");
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
            return;
        }

        Debug.Log("Example_01 VR validation passed.");
        if (Application.isBatchMode)
            EditorApplication.Exit(0);
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
        if (GameObject.Find("XR Interaction Manager") != null)
            return;

        var type = FindType("UnityEngine.XR.Interaction.Toolkit.XRInteractionManager");
        if (type == null)
            throw new InvalidOperationException("XRInteractionManager type was not found. Confirm XR Interaction Toolkit is installed.");

        var manager = new GameObject("XR Interaction Manager");
        manager.AddComponent(type);
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

    private static void EnsureLocomotionProviders(GameObject xrOrigin)
    {
        AddComponentIfMissing(xrOrigin, "UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider");
        AddComponentIfMissing(xrOrigin, "UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.SnapTurnProvider");
        AddComponentIfMissing(xrOrigin, "UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider");
    }

    private static void DisableLegacyCamerasOutsideXROrigin(GameObject xrOrigin)
    {
        foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (camera.transform.IsChildOf(xrOrigin.transform))
                continue;

            camera.enabled = false;
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
            throw new InvalidOperationException("No likely walkable floor surface found in Example_01. Add a floor object named Plane, Floor, Ground, or containing floor/ground/plane in its name.");

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
        return name.Contains("floor") || name.Contains("ground") || name.Contains("plane") || name.Contains("walk");
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
}
#endif
```

- [ ] **Step 2: Import and compile the script**

Run this in Unity Editor by focusing the editor window and waiting for compilation to finish. If using batchmode from Git Bash, run:

```bash
"/c/Program Files/Unity/Hub/Editor/6000.4.11f1/Editor/Unity.exe" -batchmode -quit -projectPath "$(pwd)" -logFile Logs/example01-configurator-import.log
```

Expected: command exits `0` and `Logs/example01-configurator-import.log` does not contain `error CS`.

- [ ] **Step 3: Commit the configurator script**

```bash
git add Assets/Editor/Example01VRSceneConfigurator.cs
git commit -m "Add Example_01 VR scene configurator"
```

Expected: commit succeeds with only the configurator script staged.

---

### Task 2: Configure `Example_01.unity` for XR Origin, simulator, and teleportation

**Files:**
- Modify: `Assets/Scenes/Example_01.unity`
- May modify automatically: `Assets/Scenes/Example_01.unity.meta` only if Unity changes metadata during save.

**Interfaces:**
- Consumes:
  - `Example01VRSceneConfigurator.ConfigureExample01VR()` from Task 1.
- Produces:
  - `Example_01.unity` containing `XR Origin (XR Rig)`.
  - `Example_01.unity` containing `XR Device Simulator`.
  - `Example_01.unity` containing active continuous movement, snap turn, teleport provider, and at least one teleportation area.

- [ ] **Step 1: Run the scene configurator**

Preferred: in Unity Editor, click:

```text
Tools / FutureGradExpoVR / Configure Example_01 VR
```

Batchmode alternative from Git Bash:

```bash
"/c/Program Files/Unity/Hub/Editor/6000.4.11f1/Editor/Unity.exe" -batchmode -quit -projectPath "$(pwd)" -executeMethod Example01VRSceneConfigurator.ConfigureExample01VR -logFile Logs/example01-configure.log
```

Expected: command exits `0`, `Assets/Scenes/Example_01.unity` is modified, and the log contains `Configured Example_01 for XR Origin`.

- [ ] **Step 2: Validate the configured scene**

Preferred: in Unity Editor, click:

```text
Tools / FutureGradExpoVR / Validate Example_01 VR
```

Batchmode alternative from Git Bash:

```bash
"/c/Program Files/Unity/Hub/Editor/6000.4.11f1/Editor/Unity.exe" -batchmode -quit -projectPath "$(pwd)" -executeMethod Example01VRSceneConfigurator.ValidateExample01VR -logFile Logs/example01-validate.log
```

Expected: command exits `0` and the log contains `Example_01 VR validation passed.`

- [ ] **Step 3: Inspect the scene diff**

```bash
git diff -- Assets/Scenes/Example_01.unity | python -c "import sys; data=sys.stdin.read(); print(data[:12000]); print('...truncated...' if len(data)>12000 else '', end='')"
```

Expected: diff includes additions or prefab modifications for `XR Origin (XR Rig)`, `XR Device Simulator`, locomotion providers, and teleportation areas. Diff should not include unrelated changes to other scenes.

- [ ] **Step 4: Commit the scene configuration**

```bash
git add Assets/Scenes/Example_01.unity
git commit -m "Configure Example_01 for VR locomotion"
```

Expected: commit succeeds. If Unity also changed scene template dependencies for `Example_01`, review those changes and include only the dependency file that Unity updated for `Example_01`.

---

### Task 3: Manual Editor verification with keyboard and mouse simulation

**Files:**
- Read only: `Assets/Scenes/Example_01.unity`
- Read only: `Logs/example01-playmode-check-notes.txt`
- Create: `Logs/example01-playmode-check-notes.txt`

**Interfaces:**
- Consumes:
  - Configured scene from Task 2.
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
No console errors about missing input actions, missing interaction manager, or duplicate AudioListener.
```

- [ ] **Step 3: Verify continuous movement and turning**

Use XR Device Simulator keyboard/mouse controls shown by the simulator UI to simulate headset/controller movement.

Expected:

```text
The XR Origin camera moves through the scene.
Turning changes view direction.
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

- [ ] **Step 5: Write manual verification notes**

Create `Logs/example01-playmode-check-notes.txt` with this content, replacing only the bracketed pass/fail words with the actual result:

```text
Example_01 VR Play Mode Check

XR Device Simulator visible/responding: [PASS or FAIL]
Continuous movement: [PASS or FAIL]
Turning: [PASS or FAIL]
Ray teleportation: [PASS or FAIL]
Duplicate AudioListener errors: [NONE or DETAILS]
XR input/interaction errors: [NONE or DETAILS]
Tester notes: [short note about any scene-specific issue observed]
```

- [ ] **Step 6: Commit verification notes only if the team wants logs tracked**

Do not commit `Logs/example01-playmode-check-notes.txt` unless the project intentionally tracks manual verification notes. The Unity `.gitignore` normally ignores `/Logs/`, so the expected default is no commit.

---

### Task 4: PICO readiness check and final sync

**Files:**
- Read: `Packages/manifest.json`
- Read: `ProjectSettings/EditorBuildSettings.asset`
- Read: `Assets/XR/Settings/OpenXRPackageSettings.asset`
- Modify only if missing required scene entry: `ProjectSettings/EditorBuildSettings.asset`

**Interfaces:**
- Consumes:
  - Configured and Editor-validated `Example_01.unity` from Task 2.
- Produces:
  - Confirmation that existing PICO/OpenXR packages and Build Settings are intact.
  - Final commit if Build Settings needed a correction.

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

- [ ] **Step 3: Check for unintended global XR setting changes**

Run:

```bash
git diff --name-only -- Assets/XR/Settings/OpenXRPackageSettings.asset Packages/manifest.json Packages/packages-lock.json ProjectSettings/EditorBuildSettings.asset
```

Expected:

```text
```

No output is expected unless Unity made a necessary Build Settings update. If output includes `Assets/XR/Settings/OpenXRPackageSettings.asset`, inspect it and revert unless the change is required for PICO launch.

- [ ] **Step 4: Final status check**

Run:

```bash
git status --short --branch
```

Expected:

```text
## main...origin/main
```

If there are committed local changes not yet pushed, push using:

```bash
git push origin main
```

Expected:

```text
main -> main
```

---

## Self-Review

Spec coverage:

- PICO/OpenXR compatibility is covered by preserving global settings and package checks in Task 4.
- Continuous movement and turning are covered by Task 2 adding locomotion providers and Task 3 manual verification.
- Ray teleportation is covered by Task 2 adding `TeleportationProvider`/`TeleportationArea` and Task 3 manual verification.
- Keyboard/mouse simulation is covered by Task 2 adding the XR Device Simulator and Task 3 manual verification.
- Existing `EventSystem` and `XR Interaction Manager` preservation is covered by Task 1 configurator behavior.
- Duplicate camera/audio listener validation is covered by Task 1 validation and Task 3 manual verification.

Placeholder scan:

- No placeholder markers or undefined implementation steps remain.
- Bracketed `PASS or FAIL` values appear only in a manual verification note template where the tester records observed results.

Type consistency:

- `ConfigureExample01VR()` and `ValidateExample01VR()` are defined in Task 1 and used by Tasks 2 and 4.
- File paths match the approved design document and current project layout.
