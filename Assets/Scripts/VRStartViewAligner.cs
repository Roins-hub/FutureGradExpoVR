using System.Collections;
using UnityEngine;

/// <summary>
/// Places the XR rig at a marker and rotates it so the initial view faces a target.
/// Useful for VR scenes where the headset/simulator can otherwise start facing away.
/// </summary>
public sealed class VRStartViewAligner : MonoBehaviour
{
    [Header("Start View")]
    public Transform startPoint;
    public Transform lookAtTarget;

    [Header("Options")]
    public bool alignOnStart = true;
    public bool alsoAlignMarkerRotation = true;

    private IEnumerator Start()
    {
        if (!alignOnStart)
            yield break;

        // Let XR / simulator pose setup finish first, then apply the scene yaw.
        yield return null;
        AlignNow();
    }

    [ContextMenu("Align Now")]
    public void AlignNow()
    {
        Transform origin = transform;
        if (!origin.name.Contains("XR Origin") && !origin.name.Contains("XR Rig"))
        {
            GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
            if (xrOrigin != null)
                origin = xrOrigin.transform;
        }

        if (startPoint == null)
        {
            GameObject marker = GameObject.Find("VR Start Point");
            if (marker != null)
                startPoint = marker.transform;
        }

        if (lookAtTarget == null)
        {
            GameObject target = GameObject.Find("Auto Center Entrance Door");
            if (target == null)
                target = GameObject.Find("uploads_files_4690857_Suzhou+Museum+Ensemble+entrance+hall+solo+VF");

            if (target != null)
                lookAtTarget = target.transform;
        }

        if (startPoint != null)
            origin.position = startPoint.position;

        if (lookAtTarget == null)
            return;

        Vector3 direction = lookAtTarget.position - origin.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        origin.rotation = targetRotation;

        if (alsoAlignMarkerRotation && startPoint != null)
            startPoint.rotation = targetRotation;
    }
}
