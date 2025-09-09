using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NPCPurePursuitDriver_Min : MonoBehaviour
{
    [Header("Path (in order)")]
    public Transform[] waypoints;
    public bool loop = false;
    public bool stopAtLast = true;

    [Header("Motion")]
    public float cruiseSpeed = 12f;   // m/s (~43 km/h)
    public float accel = 4f;          // m/s^2
    public float brake = 6f;          // m/s^2
    public float turnSlerp = 12f;     // higher = turns faster
    public float stopDistance = 1.5f; // distance to switch waypoint
    public float lookAhead = 3f;      // aim a bit past the waypoint

    Rigidbody rb;
    int idx = 0;
    float v = 0f;
    bool finished = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // DYNAMIC rigidbody (collides with world)
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.linearDamping = 0.2f;
        rb.angularDamping = 0.3f;

        // Keep car upright and on flat ground without touching rb.position
        rb.constraints = RigidbodyConstraints.FreezePositionY
                       | RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        if (finished || waypoints == null || waypoints.Length == 0) { rb.linearVelocity = Vector3.zero; return; }

        // ---- Waypoint switching / finish
        Vector3 me = Flat(transform.position);
        Vector3 wp = Flat(waypoints[idx].position);

        if (Vector3.Distance(me, wp) < stopDistance)
        {
            if (idx < waypoints.Length - 1) idx++;
            else if (loop) idx = 0;
            else if (stopAtLast) { finished = true; rb.linearVelocity = Vector3.zero; v = 0f; return; }
        }

        // ---- Aim at a point slightly past the waypoint (look-ahead)
        Vector3 target = Flat(waypoints[idx].position);
        Vector3 toWp = (target - me);
        Vector3 aimPoint = target + toWp.normalized * lookAhead;
        Vector3 toAim = (aimPoint - me);
        if (toAim.sqrMagnitude < 0.001f) { rb.linearVelocity = Vector3.zero; return; }

        // ---- Steering (XZ only)
        Vector3 fwd = FlatDir(transform.forward);
        Vector3 desiredDir = toAim.normalized;
        Quaternion lookRot = Quaternion.LookRotation(desiredDir, Vector3.up);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRot, turnSlerp * Time.fixedDeltaTime));

        // ---- Speed control (no obstacles/curvature for now)
        float desired = cruiseSpeed;
        float rate = (desired > v) ? accel : brake;
        v = Mathf.MoveTowards(v, desired, rate * Time.fixedDeltaTime);

        // ---- Apply velocity (don’t touch rb.position)
        rb.linearVelocity = FlatDir(transform.forward) * v;

        // DEBUG (optional): uncomment to verify numbers
        Debug.Log($"v={v:F1} m/s (~{v*3.6f:F0} km/h) | vel={rb.linearVelocity}");
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    static Vector3 FlatDir(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
    }
}
