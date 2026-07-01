using UnityEngine;

/// <summary>
/// Put this on a child trigger collider inside a portal gate.
/// It forwards trigger events to the nearest parent ScenePortal.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class ScenePortalTrigger : MonoBehaviour
{
    [SerializeField] private ScenePortal portal;

    private void Reset()
    {
        portal = GetComponentInParent<ScenePortal>();
        ConfigureCollider();
    }

    private void Awake()
    {
        if (portal == null)
            portal = GetComponentInParent<ScenePortal>();

        ConfigureCollider();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (portal != null)
            portal.ActivateFromTrigger(other);
    }

    private void ConfigureCollider()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }
}
