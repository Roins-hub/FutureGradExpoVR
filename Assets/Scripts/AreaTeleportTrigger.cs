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
