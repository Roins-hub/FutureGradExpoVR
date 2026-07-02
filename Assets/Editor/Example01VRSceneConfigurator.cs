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
