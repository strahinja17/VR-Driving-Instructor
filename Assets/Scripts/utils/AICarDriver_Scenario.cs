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

    // ===== Scenario controls =====
    public bool Paused { get; private set; } = false;

    int? stopAtWaypointIndex = null;     // if set, car will stop when it reaches this waypoint index
    bool holdingAtStopIndex = false;     // true once it arrived and is holding

    public int CurrentWaypointIndex => idx;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

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
        if (Paused)
        {
            HoldStill();
            return;
        }

        if (snapToFirstWaypoint && !hasSnapped && waypoints != null && waypoints.Length >= 1)
        {
            SnapToWaypoint(0, faceNext: true);
            hasSnapped = true;
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            HoldStill();
            return;
        }

        if (finished)
        {
            HoldStill();
            return;
        }

        // If we are holding at a stop index, just stay still.
        if (holdingAtStopIndex)
        {
            HoldStill();
            return;
        }

        Vector3 me = Flat(rb.position);
        Vector3 wp = Flat(waypoints[idx].position);

        // --- Waypoint switching / finish ---
        if (Vector3.Distance(me, wp) < stopDistance)
        {
            // If this waypoint is our "stopAt", then stop and hold.
            if (stopAtWaypointIndex.HasValue && idx == stopAtWaypointIndex.Value)
            {
                holdingAtStopIndex = true;
                HoldStill();
                return;
            }

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
                finished = true;
                HoldStill();

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
            HoldStill();
            return;
        }

        Vector3 desiredDir = FlatDir(toAim);
        Quaternion lookRot = Quaternion.LookRotation(desiredDir, Vector3.up);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRot, turnSlerp * Time.fixedDeltaTime));

        float desired = cruiseSpeed;

        // --- Corner speed reduction ---
        float angle = Vector3.Angle(FlatDir(transform.forward), desiredDir);

        if (angle > gentleTurnAngle)
        {
            float t = Mathf.InverseLerp(gentleTurnAngle, maxTurnAngle, angle);
            float cornerSpeed = Mathf.Lerp(cruiseSpeed, minCornerSpeed, t);
            desired = Mathf.Min(desired, cornerSpeed);
        }

        // --- Sensor limiting ---
        desired = Mathf.Min(desired, GetSensorLimitedSpeed());

        // --- Speed control ---
        float rate = (desired > v) ? accel : brake;
        v = Mathf.MoveTowards(v, desired, rate * Time.fixedDeltaTime);

        rb.linearVelocity = FlatDir(transform.forward) * v;
    }

    // ===== Public API for scenario control =====

    /// Teleport to a waypoint index and optionally face toward the next waypoint.
    public void SnapToWaypoint(int waypointIndex, bool faceNext)
    {
        if (waypoints == null || waypoints.Length == 0) return;
        waypointIndex = Mathf.Clamp(waypointIndex, 0, waypoints.Length - 1);

        idx = waypointIndex;

        Vector3 wp0 = waypoints[idx].position;
        Vector3 pos = rb.position;
        wp0.y = pos.y;

        rb.position = wp0;

        if (faceNext && waypoints.Length >= 2)
        {
            int next = Mathf.Clamp(idx + 1, 0, waypoints.Length - 1);
            Vector3 wp1 = waypoints[next].position;
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
        v = 0f;
        finished = false;
        holdingAtStopIndex = false;
    }

    /// Tell the driver: "Stop and hold when you reach waypointIndex".
    public void SetStopAtWaypoint(int waypointIndex)
    {
        stopAtWaypointIndex = Mathf.Clamp(waypointIndex, 0, (waypoints?.Length ?? 1) - 1);
        holdingAtStopIndex = false;
    }

    /// Clears any stop-hold instruction and allows movement again.
    public void ClearStopHold()
    {
        stopAtWaypointIndex = null;
        holdingAtStopIndex = false;
    }

    /// Hard pause (freezes motion regardless of waypoint logic).
    public void SetPaused(bool paused)
    {
        Paused = paused;
        if (Paused) HoldStill();
    }

    public bool IsHoldingAtStopPoint() => holdingAtStopIndex;

    void HoldStill()
    {
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
            float dist = hit.distance;

            var tl = hit.collider.GetComponentInParent<AITrafficLightStop>();
            if (tl != null && tl.IsRed())
            {
                if (dist < stopForObstacleDistance * 1.5f)
                    return 0f;
            }

            var telemetry = hit.collider.transform.root.GetComponent<TelemetryManager>();
            if (telemetry != null)
            {
                if (dist < stopForObstacleDistance)
                    return 0f;
            }

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
