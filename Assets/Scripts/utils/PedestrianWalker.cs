using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class PedestrianWalker : MonoBehaviour
{
    [Header("Path & movement")]
    public WaypointPath path;
    public float speed = 1.4f; // m/s – normal walking
    public float waypointRadius = 0.2f;

    [Header("Behaviour at end of path")]
    [Tooltip("If true, the pedestrian loops back to the first waypoint and keeps walking.")]
    public bool loopContinuously = true;

    [Tooltip("If not looping: destroy this pedestrian when reaching the last waypoint.")]
    public bool destroyAtEnd = true;

    private Transform[] waypoints;
    private int currentIndex = 0;
    private Animator anim;
    private Rigidbody rb;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // we move him manually
    }

    private void Start()
    {
        if (path == null)
        {
            Debug.LogWarning($"{name}: No WaypointPath assigned.");
            enabled = false;
            return;
        }

        waypoints = path.GetWaypoints();
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning($"{name}: WaypointPath has no waypoints.");
            enabled = false;
            return;
        }

        // Start at first waypoint
        rb.position = waypoints[0].position;

        // Face towards second waypoint if exists
        if (waypoints.Length > 1 && waypoints[1] != null)
        {
            Vector3 dir = waypoints[1].position - waypoints[0].position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                rb.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        currentIndex = Mathf.Min(1, waypoints.Length - 1);
    }

    private void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Transform target = waypoints[currentIndex];
        if (target == null)
            return;

        Vector3 pos = rb.position;
        Vector3 toTarget = target.position - pos;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance < waypointRadius)
        {
            // Reached waypoint
            currentIndex++;

            if (currentIndex >= waypoints.Length)
            {
                if (loopContinuously)
                {
                    currentIndex = 0;
                }
                else
                {
                    if (destroyAtEnd)
                        Destroy(gameObject);
                    else
                    {
                        // Stop moving & freeze animation
                        anim.speed = 0f;
                        enabled = false;
                    }
                    return;
                }
            }
            return;
        }

        Vector3 direction = toTarget.normalized;
        Vector3 move = direction * speed * Time.fixedDeltaTime;
        rb.MovePosition(pos + move);

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 10f * Time.fixedDeltaTime));
        }

        // If you want to scale animation speed, you can:
        // anim.speed = speed / 1.4f;
    }
}
