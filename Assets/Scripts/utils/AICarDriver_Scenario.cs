using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AICarDriver_Scenario : MonoBehaviour
{
    [Header("Path (in order)")]
    public Transform[] waypoints;
    public bool loop = false;
    public bool destroyAtEnd = false;

    [Header("Start behaviour")]
    public bool snapToFirstWaypoint = true;

    [Header("Motion")]
    public float cruiseSpeed = 12f;
    public float accel = 4f;
    public float brake = 8f;
    public float turnSlerp = 12f;
    public float stopDistance = 1.5f;
    public float lookAhead = 3f;

    [Header("Cornering")]
    public float gentleTurnAngle = 15f;
    public float minCornerSpeed = 3f;
    public float maxTurnAngle = 90f;

    [Header("Sensors (forward raycast)")]
    public Transform sensorOrigin;
    public float sensorLength = 15f;
    public float stopForObstacleDistance = 6f;
    public LayerMask obstacleLayers = ~0;
    public bool debugRays = false;

    Rigidbody rb;
    int idx = 0;
    float v = 0f;
    bool finished = false;
    bool hasSnapped = false;

    // Scenario controls
    public bool Paused { get; private set; } = false;

    int? stopAtWaypointIndex = null;
    bool holdingAtStopIndex = false;

    // Sensor grace window
    float ignoreSensorsUntil = -1f;

    public int CurrentWaypointIndex => idx;
    public bool IsHoldingAtStopPoint() => holdingAtStopIndex;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"[{name}] Missing Rigidbody on same GameObject as driver.");
            enabled = false;
            return;
        }

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.linearDamping = 0.2f;
        rb.angularDamping = 0.3f;

        rb.constraints =
            RigidbodyConstraints.FreezePositionY |
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (Paused)
        {
            HoldStill();
            return;
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            HoldStill();
            return;
        }

        // One-time snap (only if you want it)
        if (snapToFirstWaypoint && !hasSnapped)
        {
            SnapToWaypoint(0, faceNext: true);
            hasSnapped = true;
        }

        if (finished)
        {
            HoldStill();
            return;
        }

        if (holdingAtStopIndex)
        {
            HoldStill();
            return;
        }

        Vector3 me = Flat(rb.position);
        Vector3 wp = Flat(waypoints[idx].position);

        // Waypoint switching
        if (Vector3.Distance(me, wp) < stopDistance)
        {
            // Scenario stop point?
            if (stopAtWaypointIndex.HasValue && idx == stopAtWaypointIndex.Value)
            {
                holdingAtStopIndex = true;
                HoldStill();
                return;
            }

            AdvanceWaypointIndex(); // normal advance/loop/finish
            if (finished) return;

            wp = Flat(waypoints[idx].position);
        }

        // Aim slightly past waypoint
        Vector3 toWp = (wp - me);
        if (toWp.sqrMagnitude < 0.001f)
        {
            // If you get here, your waypoint positions are too close / identical
            HoldStill();
            return;
        }

        Vector3 aimPoint = wp + toWp.normalized * lookAhead;
        Vector3 toAim = (aimPoint - me);

        if (toAim.sqrMagnitude < 0.001f)
        {
            HoldStill();
            return;
        }

        Vector3 desiredDir = FlatDir(toAim);
        Quaternion lookRot = Quaternion.LookRotation(desiredDir, Vector3.up);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRot, turnSlerp * Time.fixedDeltaTime));

        float desired = cruiseSpeed;

        // Corner speed reduction
        float angle = Vector3.Angle(FlatDir(transform.forward), desiredDir);
        if (angle > gentleTurnAngle)
        {
            float t = Mathf.InverseLerp(gentleTurnAngle, maxTurnAngle, angle);
            float cornerSpeed = Mathf.Lerp(cruiseSpeed, minCornerSpeed, t);
            desired = Mathf.Min(desired, cornerSpeed);
        }

        // Sensor limiting (unless in grace period)
        if (Time.time >= ignoreSensorsUntil)
            desired = Mathf.Min(desired, GetSensorLimitedSpeed());

        // Speed control
        float rate = (desired > v) ? accel : brake;
        v = Mathf.MoveTowards(v, desired, rate * Time.fixedDeltaTime);

        rb.linearVelocity = FlatDir(transform.forward) * v;
    }

    void AdvanceWaypointIndex()
    {
        if (idx < waypoints.Length - 1)
        {
            idx++;
        }
        else if (loop)
        {
            idx = 0;
        }
        else
        {
            finished = true;
            HoldStill();
            if (destroyAtEnd) Destroy(gameObject);
        }
    }

    // ===== Public API =====

    public void SnapToWaypoint(int waypointIndex, bool faceNext)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        if (waypoints == null || waypoints.Length == 0) return;

        waypointIndex = Mathf.Clamp(waypointIndex, 0, waypoints.Length - 1);
        idx = waypointIndex;

        Vector3 wp0 = waypoints[idx].position;
        wp0.y = rb.position.y;

        rb.position = wp0;

        if (faceNext)
        {
            int next = Mathf.Clamp(idx + 1, 0, waypoints.Length - 1);
            Vector3 wp1 = waypoints[next].position;
            Vector3 dir = wp1 - wp0;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                rb.MoveRotation(Quaternion.LookRotation(dir.normalized, Vector3.up));
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        v = 0f;
        finished = false;

        // Important: snapping counts as "snapped" so FixedUpdate doesn't re-snap unexpectedly
        hasSnapped = true;

        // Clear any old holding state
        holdingAtStopIndex = false;
    }

    public void SetStopAtWaypoint(int waypointIndex)
    {
        if (waypoints == null || waypoints.Length == 0) return;
        stopAtWaypointIndex = Mathf.Clamp(waypointIndex, 0, waypoints.Length - 1);
        holdingAtStopIndex = false;
    }

    /// Release from hold, and (critical) consume the stop waypoint by advancing idx once.
    public void ClearStopHoldConsumeAndGo()
    {
        if (holdingAtStopIndex)
        {
            // Consume the stop waypoint so we don't re-trigger it while still inside stopDistance
            AdvanceWaypointIndex();
        }

        stopAtWaypointIndex = null;
        holdingAtStopIndex = false;
    }

    public void SetPaused(bool paused)
    {
        Paused = paused;
        if (Paused) HoldStill();
    }

    public void IgnoreSensorsFor(float seconds)
    {
        ignoreSensorsUntil = Time.time + Mathf.Max(0f, seconds);
    }

    void HoldStill()
    {
        if (rb == null) return;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        v = 0f;
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
