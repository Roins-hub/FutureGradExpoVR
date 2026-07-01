using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple VR-friendly scene portal.
/// Loads the target scene when the player's main camera gets close enough,
/// or when a child trigger volume reports that the XR rig entered it.
/// </summary>
public sealed class ScenePortal : MonoBehaviour
{
    [Header("Scene")]
    public string targetSceneName;

    [Header("Activation")]
    [Min(0.25f)]
    public float activationRadius = 1.6f;

    [Min(0f)]
    public float activationCooldown = 1f;

    [Header("Visual")]
    public Transform lookAtTarget;

    private Transform cachedCamera;
    private float nextActivationTime;
    private bool isLoading;

    private void Awake()
    {
        cachedCamera = FindPlayerCamera();
    }

    private void Update()
    {
        if (isLoading || string.IsNullOrWhiteSpace(targetSceneName))
            return;

        if (Time.unscaledTime < nextActivationTime)
            return;

        if (cachedCamera == null || !cachedCamera.gameObject.activeInHierarchy)
            cachedCamera = FindPlayerCamera();

        if (cachedCamera == null)
            return;

        if (lookAtTarget != null)
        {
            Vector3 direction = lookAtTarget.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        Vector3 portalPosition = transform.position;
        Vector3 playerPosition = cachedCamera.position;
        portalPosition.y = 0f;
        playerPosition.y = 0f;

        if (Vector3.Distance(portalPosition, playerPosition) <= activationRadius)
        {
            Activate();
        }
    }

    public void ActivateFromTrigger(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        Activate();
    }

    private void Activate()
    {
        if (isLoading || string.IsNullOrWhiteSpace(targetSceneName))
            return;

        if (Time.unscaledTime < nextActivationTime)
            return;

        nextActivationTime = Time.unscaledTime + activationCooldown;
        isLoading = true;
        SceneManager.LoadScene(targetSceneName);
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        if (cachedCamera == null || !cachedCamera.gameObject.activeInHierarchy)
            cachedCamera = FindPlayerCamera();

        if (cachedCamera == null)
            return false;

        Transform otherTransform = other.transform;
        if (otherTransform == cachedCamera || otherTransform.root == cachedCamera.root)
            return true;

        if (cachedCamera.IsChildOf(otherTransform))
            return true;

        string rootName = otherTransform.root.name;
        return rootName.Contains("XR Origin") || rootName.Contains("XR Rig") || rootName.Contains("Player");
    }

    private static Transform FindPlayerCamera()
    {
        if (Camera.main != null)
            return Camera.main.transform;

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Camera camera in cameras)
        {
            if (camera.enabled)
                return camera.transform;
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}
