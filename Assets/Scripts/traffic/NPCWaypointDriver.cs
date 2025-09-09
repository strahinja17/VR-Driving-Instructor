using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NPCWaypointDriver : MonoBehaviour
{
    [Header("Path")]
    public Transform[] waypoints;
    public bool loop = false;

    [Header("Driving")]
    public float cruiseSpeed = 10f;   // m/s (~36 km/h)
    public float accel = 3f;
    public float brake = 6f;
    public float stopDistance = 1.5f; // how close to waypoint before switching
    public float lookAhead = 2f;      // meters to look ahead past the waypoint
    public float turnSpeed = 3f;

    private Rigidbody rb;
    private int currentIndex = 0;
    private float currentSpeed = 0f;
    private bool finished = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // we’ll move manually
    }

    void Update()
    {
        if (waypoints.Length == 0 || finished) return;

        Transform target = waypoints[currentIndex];
        Vector3 targetPos = target.position;
        targetPos.y = transform.position.y; // 🚫 keep car flat

        // === Waypoint reached? ===
        if (Vector3.Distance(transform.position, targetPos) < stopDistance)
        {
            if (currentIndex < waypoints.Length - 1)
            {
                currentIndex++;
                target = waypoints[currentIndex];
                targetPos = target.position;
            }
            else if (loop)
            {
                currentIndex = 0;
                target = waypoints[currentIndex];
                targetPos = target.position;
            }
            else
            {
                // ✅ Final stop
                finished = true;
                currentSpeed = 0f;
                return; // stop both movement and rotation
            }
        }

        // === Look-ahead point ===
        Vector3 dirToTarget = (targetPos - transform.position).normalized;
        Vector3 aimPoint = targetPos + dirToTarget * lookAhead;
        Vector3 dir = (aimPoint - transform.position).normalized;

        // === Steering ===
        if (dir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, turnSpeed * Time.deltaTime);
        }

        // === Smooth speed ===
        currentSpeed = Mathf.MoveTowards(currentSpeed, cruiseSpeed, accel * Time.deltaTime);

        // === Movement ===
        Vector3 move = transform.forward * currentSpeed * Time.deltaTime;
        rb.MovePosition(rb.position + move);
    }
}
