using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AICarDriver : MonoBehaviour
{
    [Header("Path (in order)")]
    public Transform[] waypoints;
    public bool loop = false;
    public bool destroyAtEnd = false;  // if true and !loop, destroy after final stop

    [Header("Start behaviour")]
    [Tooltip("If true, car will be teleported to first waypoint and aligned with second at start.")]
    public bool snapToFirstWaypoint = true;

    [Header("Motion")]
    [Tooltip("Cruise speed in m/s (10 m/s ≈ 36 km/h).")]
    public float cruiseSpeed = 12f;   // m/s
    public float accel = 4f;          // m/s^2
    public float brake = 12f;          // m/s^2
    public float turnSlerp = 12f;     // higher = faster turning
    public float stopDistance = 1.5f; // distance to switch waypoint
    public float lookAhead = 4f;      // aim a bit past the waypoint

    [Header("Cornering")]
    [Tooltip("Below this angle (deg), car uses full cruiseSpeed.")]
    public float gentleTurnAngle = 15f;

    [Tooltip("Above this angle (deg), car slows toward this speed (m/s).")]
    public float minCornerSpeed = 3f; // ~11 km/h

    [Tooltip("Angle (deg) at which we reach minCornerSpeed.")]
    public float maxTurnAngle = 90f; // hard turn


    [Header("Sensors (forward raycast)")]
    [Tooltip("Where the forward ray starts (e.g. an empty at the front bumper). If null, uses transform.")]
    public Transform sensorOrigin;
    [Tooltip("How far ahead the car looks for obstacles / lights.")]
    public float sensorLength = 20f;
    [Tooltip("Distance at which we commit to a full stop in front of an obstacle.")]
    public float stopForObstacleDistance = 6f;
    [Tooltip("Which layers should be considered obstacles (player, other cars, stop proxies, etc).")]
    public LayerMask obstacleLayers = ~0;
    public bool debugRays = false;

    Rigidbody rb;
    int idx = 0;
    float v = 0f;          // current speed (m/s)
    bool finished = false;
    bool hasSnapped = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Dynamic rigidbody, but we control movement.
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.linearDamping = 0.2f;
        rb.angularDamping = 0.3f;

        // Keep car flat and at fixed height.
        rb.constraints =
            RigidbodyConstraints.FreezePositionY |
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        // --- One-time snap to first waypoint + align to second ---
        if (snapToFirstWaypoint && !hasSnapped && waypoints != null && waypoints.Length >= 1)
        {
            Vector3 wp0 = waypoints[0].position;
            Vector3 pos = rb.position;
            wp0.y = pos.y; // keep current height (Y is frozen anyway)

            rb.position = wp0;

            if (waypoints.Length >= 2)
            {
                Vector3 wp1 = waypoints[1].position;
                Vector3 dir = wp1 - wp0;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    rb.MoveRotation(rot);
                }
            }

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            hasSnapped = true;
            // continue into normal logic this frame or return; both are fine.
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (finished)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 me = Flat(rb.position);
        Vector3 wp = Flat(waypoints[idx].position);

        // --- Waypoint switching / finish ---
        if (Vector3.Distance(me, wp) < stopDistance)
        {
            if (idx < waypoints.Length - 1)
            {
                idx++;
                wp = Flat(waypoints[idx].position);
            }
            else if (loop)
            {
                idx = 0;
                wp = Flat(waypoints[idx].position);
            }
            else
            {
                // Final waypoint reached: come to a stop
                finished = true;
                rb.linearVelocity = Vector3.zero;
                v = 0f;

                if (destroyAtEnd)
                    Destroy(gameObject);

                return;
            }
        }

        // --- Aim slightly past the waypoint (look-ahead) ---
        Vector3 target = wp;
        Vector3 toWp = (target - me);
        Vector3 aimPoint = target + toWp.normalized * lookAhead;
        Vector3 toAim = (aimPoint - me);

        if (toAim.sqrMagnitude < 0.001f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 desiredDir = FlatDir(toAim);
        Quaternion lookRot = Quaternion.LookRotation(desiredDir, Vector3.up);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRot, turnSlerp * Time.fixedDeltaTime));

        // --- Base desired speed from cruise ---
        float desired = cruiseSpeed;

        // --- Corner-based speed reduction ---
        float angle = Vector3.Angle(FlatDir(transform.forward), desiredDir); // 0..180 deg

        if (angle > gentleTurnAngle)
        {
            // Map angle from gentleTurnAngle..maxTurnAngle to 0..1
            float t = Mathf.InverseLerp(gentleTurnAngle, maxTurnAngle, angle);
            // Lerp down from cruiseSpeed to minCornerSpeed
            float cornerSpeed = Mathf.Lerp(cruiseSpeed, minCornerSpeed, t);
            desired = Mathf.Min(desired, cornerSpeed);
        }

        // --- Sensor logic: limit desired speed if obstacle/light ahead ---
        desired = Mathf.Min(desired, GetSensorLimitedSpeed());


        // --- Speed control (accel/brake) ---
        float rate = (desired > v) ? accel : brake;
        v = Mathf.MoveTowards(v, desired, rate * Time.fixedDeltaTime);

        // --- Apply velocity (don’t touch rb.position directly) ---
        rb.linearVelocity = FlatDir(transform.forward) * v;
    }

    float GetSensorLimitedSpeed()
    {
        Transform origin = sensorOrigin != null ? sensorOrigin : transform;
        Vector3 start = origin.position;
        Vector3 dir = origin.forward;

        if (debugRays)
            Debug.DrawRay(start, dir * sensorLength, Color.cyan);

        if (Physics.Raycast(start, dir, out RaycastHit hit, sensorLength, obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            // Ignore self hits
            if (hit.transform.root == transform.root)
                return cruiseSpeed;

            float dist = hit.distance;

            // 1) Traffic stop sensor (AI_StopSensor box)
            var tlStop = hit.collider.GetComponentInParent<AITrafficLightStop>();
            if (tlStop != null)
            {
                // DEBUG (optional)
                // Debug.Log($"[AI] Hit StopSensor {hit.collider.name}, red={tlStop.IsRed()}, dist={dist:0.00}");

                if (tlStop.IsRed())
                {
                    if (dist < stopForObstacleDistance * 1.5f)
                        return 0f;
                }

                // Not red -> ignore this collider entirely (do NOT stop for it)
                return cruiseSpeed;
            }

            // 2) Player car (TelemetryManager on root)
            var telemetry = hit.collider.GetComponentInParent<TelemetryManager>();
            if (telemetry != null)
            {
                if (dist < stopForObstacleDistance)
                    return 0f;
            }

            // 3) Generic obstacle (walls, props, etc.)
            if (dist < stopForObstacleDistance)
                return 0f;
        }

        return cruiseSpeed;
    }



    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    static Vector3 FlatDir(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
    }
}
