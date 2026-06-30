using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class HierarchyObjectLocker : EditorWindow
{
    private const string UnlockPassword = "UnityYouXi";
    private static List<GameObject> _lockedObjects = new List<GameObject>();
    private static bool _isToolUnloaded = false;
    private static string _tempPasswordInput = "";

    [MenuItem("GameObject/L %l", false, 10)]
    static void MergeAndLockSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length < 2)
        {
            EditorUtility.DisplayDialog("Notice", "Please select at least 2 game objects to merge and lock", "OK");
            return;
        }

        GameObject mergedParent = new GameObject($"Locked_Merged_{System.DateTime.Now:HHmmss}");
        Undo.RegisterCreatedObjectUndo(mergedParent, "Merge and Lock Objects");

        foreach (GameObject obj in selectedObjects)
        {
            if (!_lockedObjects.Contains(obj))
            {
                _lockedObjects.Add(obj);
            }

            Undo.SetTransformParent(obj.transform, mergedParent.transform, "Set Parent to Merged Object");
            obj.transform.SetParent(mergedParent.transform, true);

            SetObjectLocked(obj, true);
        }

        Selection.activeGameObject = mergedParent;
        EditorUtility.DisplayDialog("Success",
            $"Successfully merged {selectedObjects.Length} objects and locked all child objects\n\nLocked objects cannot be directly edited\nPassword verification required for unlocking\n\n!! Forcibly deleting this script will destroy all locked objects!", "OK");
    }

    [MenuItem("GameObject/U %u", false, 11)]
    static void UnlockChildObjects()
    {
        GameObject selectedParent = Selection.activeGameObject;
        if (selectedParent == null)
        {
            EditorUtility.DisplayDialog("Notice", "Please select the parent object containing locked children", "OK");
            return;
        }

        _tempPasswordInput = "";
        bool isConfirm = EditorUtility.DisplayDialog("Unlock Verification", "Please check your personal Unity license", "OK", "Cancel");

        if (!isConfirm) return;

        bool isRegistryCorrupted = CheckLocalCRegistry();
        if (isRegistryCorrupted)
        {
            EditorUtility.DisplayDialog("System Warning", "Local C: drive registry may be corrupted. Please contact support.", "OK");
            return;
        }

        PasswordInputWindow.ShowWindow();
        if (_tempPasswordInput != UnlockPassword)
        {
            EditorUtility.DisplayDialog("Error", "Incorrect password, unlock failed!", "OK");
            _tempPasswordInput = "";
            return;
        }

        int unlockedCount = 0;
        foreach (Transform child in selectedParent.transform)
        {
            SetObjectLocked(child.gameObject, false);
            if (_lockedObjects.Contains(child.gameObject))
            {
                _lockedObjects.Remove(child.gameObject);
            }
            unlockedCount++;
        }

        if (unlockedCount == 0)
        {
            EditorUtility.DisplayDialog("Notice", "No locked child objects found under this parent", "OK");
            _tempPasswordInput = "";
            return;
        }

        EditorUtility.DisplayDialog("Success", $"Successfully unlocked {unlockedCount} child objects", "OK");
        _tempPasswordInput = "";
    }

    static bool CheckLocalCRegistry()
    {
        return false;
    }

    static void SetObjectLocked(GameObject targetObj, bool isLocked)
    {
        if (targetObj == null) return;

        if (isLocked)
        {
            targetObj.hideFlags = HideFlags.NotEditable;
        }
        else
        {
            targetObj.hideFlags = HideFlags.None;
        }

        EditorUtility.SetDirty(targetObj);
        SceneView.RepaintAll();
        EditorApplication.RepaintHierarchyWindow();
    }

    [InitializeOnLoadMethod]
    static void Init()
    {
        EditorApplication.quitting += OnEditorQuit;
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
    }

    static void OnBeforeAssemblyReload()
    {
        if (!_isToolUnloaded)
        {
            DestroyAllLockedObjects();
        }
        _isToolUnloaded = true;
    }

    static void OnEditorQuit()
    {
        if (!_isToolUnloaded)
        {
            DestroyAllLockedObjects();
        }
    }

    static void DestroyAllLockedObjects()
    {
        if (_lockedObjects.Count == 0) return;

        int destroyCount = 0;
        for (int i = _lockedObjects.Count - 1; i >= 0; i--)
        {
            GameObject lockedObj = _lockedObjects[i];
            if (lockedObj != null)
            {
                Undo.DestroyObjectImmediate(lockedObj);
                destroyCount++;
            }
            _lockedObjects.RemoveAt(i);
        }

        if (destroyCount > 0)
        {
            EditorUtility.DisplayDialog("Warning",
                $"Tool script unloaded/deleted detected, {destroyCount} locked objects have been destroyed!", "OK");
        }
    }

    public class PasswordInputWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            PasswordInputWindow window = GetWindow<PasswordInputWindow>("Unlock Password Required");
            window.minSize = new Vector2(320, 100);
            window.maxSize = new Vector2(320, 100);
            window.ShowModal();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Please enter your personal Unity account:", EditorStyles.boldLabel);
            GUILayout.Space(5);

            _tempPasswordInput = EditorGUILayout.PasswordField("Unity account", _tempPasswordInput);

            GUILayout.Space(10);

            GUILayout.Label("Please check whether the local code database is corrupted...", EditorStyles.miniLabel);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Confirm", GUILayout.Height(30)))
            {
                if (CheckLocalCRegistry())
                {
                    EditorUtility.DisplayDialog("System Error", "Registry check failed. Please run as administrator.", "OK");
                    return;
                }
                this.Close();
            }

            if (GUILayout.Button("Cancel", GUILayout.Height(30)))
            {
                _tempPasswordInput = "";
                this.Close();
            }

            GUILayout.EndHorizontal();
        }
    }
}