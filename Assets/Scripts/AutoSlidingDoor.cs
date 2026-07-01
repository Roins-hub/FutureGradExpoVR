using UnityEngine;

public class AutoSlidingDoor : MonoBehaviour
{
    [Header("Door Leaves")]
    public Transform[] leftLeaves;
    public Transform[] rightLeaves;

    [Header("Player Detection")]
    public Transform playerHead;
    public float openDistance = 4f;
    public float closeDistance = 5.5f;
    public bool ignoreHeight = true;

    [Header("Sliding")]
    public float slideDistance = 1.35f;
    public float moveSpeed = 2.4f;

    Vector3[] _leftClosed;
    Vector3[] _rightClosed;
    bool _isOpen;

    void Awake()
    {
        CacheClosedPositions();
        ResolvePlayerHead();
    }

    void OnValidate()
    {
        closeDistance = Mathf.Max(closeDistance, openDistance + 0.1f);
        slideDistance = Mathf.Max(0f, slideDistance);
        moveSpeed = Mathf.Max(0.01f, moveSpeed);
    }

    void CacheClosedPositions()
    {
        _leftClosed = CachePositions(leftLeaves);
        _rightClosed = CachePositions(rightLeaves);
    }

    static Vector3[] CachePositions(Transform[] leaves)
    {
        if (leaves == null) return new Vector3[0];

        var positions = new Vector3[leaves.Length];
        for (int i = 0; i < leaves.Length; i++)
        {
            positions[i] = leaves[i] != null ? leaves[i].localPosition : Vector3.zero;
        }

        return positions;
    }

    void ResolvePlayerHead()
    {
        if (playerHead != null) return;

        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            playerHead = mainCamera.transform;
            return;
        }

        var xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin != null)
        {
            playerHead = xrOrigin.transform;
        }
    }

    void Update()
    {
        if (playerHead == null)
        {
            ResolvePlayerHead();
            if (playerHead == null) return;
        }

        float distance = DistanceToPlayer();

        if (!_isOpen && distance <= openDistance)
        {
            _isOpen = true;
        }
        else if (_isOpen && distance >= closeDistance)
        {
            _isOpen = false;
        }

        AnimateLeaves(leftLeaves, _leftClosed, Vector3.left, _isOpen);
        AnimateLeaves(rightLeaves, _rightClosed, Vector3.right, _isOpen);
    }

    float DistanceToPlayer()
    {
        Vector3 a = transform.position;
        Vector3 b = playerHead.position;

        if (ignoreHeight)
        {
            a.y = 0f;
            b.y = 0f;
        }

        return Vector3.Distance(a, b);
    }

    void AnimateLeaves(Transform[] leaves, Vector3[] closedPositions, Vector3 direction, bool open)
    {
        if (leaves == null || closedPositions == null) return;

        int count = Mathf.Min(leaves.Length, closedPositions.Length);
        for (int i = 0; i < count; i++)
        {
            if (leaves[i] == null) continue;

            Vector3 target = closedPositions[i];
            if (open)
            {
                target += direction * slideDistance;
            }

            leaves[i].localPosition = Vector3.Lerp(
                leaves[i].localPosition,
                target,
                1f - Mathf.Exp(-moveSpeed * Time.deltaTime)
            );
        }
    }

    public void RebindClosedPositions()
    {
        CacheClosedPositions();
    }
}
