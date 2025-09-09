using UnityEngine;

/// Smooth NPC waypoint driver (physics-based, pure pursuit).
/// - Dynamic Rigidbody (collides with world), never sets isKinematic = true
/// - Pure pursuit look-ahead steering with low-pass smoothing
/// - Accel/brake smoothing & curvature-based speed limiting (no overshoot)
/// - Optional obstacle ray stop
/// - Stops cleanly at final waypoint or loops
/// - Keeps Y frozen for flat scenes (no grounding jitter)
[RequireComponent(typeof(Rigidbody))]
public class NPCPurePursuitDriver : MonoBehaviour
{
    [Header("Path (ordered)")]
    public Transform[] waypoints;
    public bool loop = false;
    public bool stopAtLast = true;
    public float switchDistance = 1.75f;   // reach radius for waypoints
    public float minAdvanceSpeed = 2f;     // avoid spin-in-place near a point

    [Header("Vehicle")]
    public float wheelbase = 2.6f;         // meters (front->rear axle)
    public float maxSteerDeg = 30f;        // front wheel max steer
    public float cruiseSpeed = 12f;        // m/s (~43 km/h)
    public float accel = 3.0f;             // m/s²
    public float brake = 6.0f;             // m/s²
    public float turnSlerp = 10f;          // body yaw response

    [Header("Pure Pursuit")]
    public float lookAheadMin = 3.0f;      // m at low speeds
    public float lookAheadMax = 12.0f;     // m at high speeds
    public float steerSmoothing = 0.15f;   // 0..1 low-pass on steer command

    [Header("Cornering comfort (speed cap)")]
    public bool useCurvatureSpeed = true;
    public float maxLateralAccel = 3.0f;   // m/s² (~0.3 g)
    public float minCornerSpeed = 6f;      // m/s lower bound in turns

    [Header("Obstacle (optional)")]
    public bool useObstacleSensor = false;
    public float sensorLength = 10f;
    public LayerMask obstacleMask = ~0;

    Rigidbody rb;
    int idx = 0;
    float v = 0f;                // current forward speed (m/s)
    bool finished = false;
    float steerDegSmoothed = 0f; // low-pass steering

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearDamping = 0.2f;
        rb.angularDamping = 0.3f;

        // Keep car upright & on a flat level (no vertical jitter)
        rb.constraints = RigidbodyConstraints.FreezePositionY
                       | RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        if (finished || waypoints == null || waypoints.Length == 0)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // --- Switch waypoint / finish ---
        Vector3 me = Flat(transform.position);
        Vector3 wp = Flat(waypoints[idx].position);

        if (Vector3.Distance(me, wp) < switchDistance)
        {
            if (idx < waypoints.Length - 1) idx++;
            else if (loop) idx = 0;
            else if (stopAtLast)
            {
                finished = true;
                v = 0f;
                rb.linearVelocity = Vector3.zero;
                return;
            }
            wp = Flat(waypoints[idx].position);
        }

        // --- Dynamic look-ahead based on speed ---
        float Ld = Mathf.Lerp(lookAheadMin, lookAheadMax, Mathf.InverseLerp(0f, cruiseSpeed, v));

        // Aim at a point Ld meters ahead along the polyline from current idx
        Vector3 lookPoint = FindLookAheadPoint(Ld, me);
        Vector3 toTarget = lookPoint - me;
        if (toTarget.sqrMagnitude < 1e-4f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // --- Pure pursuit steering geometry ---
        Vector3 fwd = FlatDir(transform.forward);
        Vector3 dir = toTarget.normalized;

        // alpha = signed angle between car forward and look direction (radians)
        float alpha = Mathf.Atan2(Vector3.Cross(fwd, dir).y, Vector3.Dot(fwd, dir));
        float steerDegCmd = Mathf.Clamp(Mathf.Atan2(2f * wheelbase * Mathf.Sin(alpha), Mathf.Max(0.1f, Ld)) * Mathf.Rad2Deg,
                                        -maxSteerDeg, maxSteerDeg);

        // Low-pass filter steering (reduce twitch/jolt)
        steerDegSmoothed = Mathf.Lerp(steerDegSmoothed, steerDegCmd, 1f - Mathf.Exp(-steerSmoothing / Mathf.Max(1e-3f, Time.fixedDeltaTime)));

        // Approximate yaw rate for curvature speed cap
        float yawRate = (v / Mathf.Max(0.1f, wheelbase)) * Mathf.Tan(steerDegSmoothed * Mathf.Deg2Rad); // rad/s

        // --- Desired speed (optionally limit on curvature & obstacle) ---
        float desired = cruiseSpeed;

        if (useCurvatureSpeed)
        {
            float absYaw = Mathf.Abs(yawRate);
            if (absYaw > 1e-3f)
            {
                // v_max ≈ maxLatAcc / |yawRate|
                float vCurve = Mathf.Clamp(maxLateralAccel / absYaw, minCornerSpeed, cruiseSpeed);
                desired = Mathf.Min(desired, vCurve);
            }
        }

        if (useObstacleSensor && Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out _, sensorLength, obstacleMask))
            desired = 0f;

        // Smooth accel/brake toward desired
        float rate = (desired > v) ? accel : brake;
        v = Mathf.MoveTowards(v, desired, rate * Time.fixedDeltaTime);

        // Ensure a tiny crawl speed to avoid spin at near-zero
        float driveSpeed = Mathf.Max(v, (Vector3.Distance(me, wp) < switchDistance * 2f) ? minAdvanceSpeed : 0f);

        // --- Apply yaw & velocity (physics) ---
        // Rotate body toward blended heading
        Quaternion lookRot = Quaternion.LookRotation(Vector3.Slerp(fwd, dir, turnSlerp * Time.fixedDeltaTime), Vector3.up);
        rb.MoveRotation(lookRot);

        // Advance with velocity along current forward (XZ only)
        Vector3 flatForward = FlatDir(transform.forward);
        rb.linearVelocity = flatForward * driveSpeed;
    }

    // Returns a point ~lookDist meters ahead along the path from current waypoint idx.
    Vector3 FindLookAheadPoint(float lookDist, Vector3 startPosFlat)
    {
        // Start from current position toward current waypoint, then along subsequent segments
        int i = idx;
        Vector3 from = startPosFlat;
        Vector3 to = Flat(waypoints[i].position);

        // If we are already very close to current wp, hop to next segment
        if (Vector3.Distance(from, to) < switchDistance && i < waypoints.Length - 1)
        {
            i++;
            to = Flat(waypoints[i].position);
        }

        float remaining = lookDist;

        while (true)
        {
            Vector3 seg = to - from;
            float len = seg.magnitude;
            if (len >= remaining && len > 1e-3f)
                return from + seg.normalized * remaining;

            remaining -= len;

            if (i >= waypoints.Length - 1)
                return loop ? Flat(waypoints[0].position) : Flat(waypoints[waypoints.Length - 1].position);

            from = to;
            i++;
            to = Flat(waypoints[i].position);
        }
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    static Vector3 FlatDir(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Obstacle sensor viz
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * sensorLength);

        // Path viz
        if (waypoints != null && waypoints.Length > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length - 1; i++)
                Gizmos.DrawLine(Flat(waypoints[i].position) + Vector3.up * 0.05f,
                                Flat(waypoints[i + 1].position) + Vector3.up * 0.05f);
            if (loop)
                Gizmos.DrawLine(Flat(waypoints[waypoints.Length - 1].position) + Vector3.up * 0.05f,
                                Flat(waypoints[0].position) + Vector3.up * 0.05f);
        }
    }
#endif
}
