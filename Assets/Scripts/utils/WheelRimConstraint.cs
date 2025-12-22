using UnityEngine;

public class WheelRimConstraint : MonoBehaviour
{
    [Header("Wheel geometry")]
    public Transform wheelCenter;
    public Vector3 wheelAxisLocal = new(0, 0, 1); // try (1,0,0) if wrong
    public float rimRadius = 0.18f;
    public float rimBandHalfWidth = 0.02f;  // 0 = exact rim, >0 = ring
    public float surfaceOffset = 0.02f;     // push out to avoid intersection
    public bool offsetForward = true;

    [Header("Hands (Tracked sources)")]
    public Transform leftTracked;
    public Transform rightTracked;

    [Header("Hands (Visual targets you want to move)")]
    public Transform leftVisual;
    public Transform rightVisual;

    [Header("Mode")]
    public bool lockLeft = true;
    public bool lockRight = true;

    [Tooltip("If true, only lock while grabbing toggles are true.")]
    public bool requireGrab = false;

    [Header("Quick test grab toggles")]
    public bool leftGrabbing = true;
    public bool rightGrabbing = true;

    [Header("Smoothing")]
    public float positionLerp = 30f;
    public float rotationLerp = 30f;

    private void Awake()
    {
        if (wheelCenter == null) wheelCenter = transform;
    }

    private void Update()
    {
        // quick keyboard toggles for testing
        if (Input.GetKeyDown(KeyCode.Z)) leftGrabbing = !leftGrabbing;
        if (Input.GetKeyDown(KeyCode.X)) rightGrabbing = !rightGrabbing;
    }

    private void LateUpdate()
    {
        if (lockLeft)  ApplyLock(isLeft: true);
        if (lockRight) ApplyLock(isLeft: false);
    }

    private void ApplyLock(bool isLeft)
    {
        Transform tracked = isLeft ? leftTracked : rightTracked;
        Transform visual  = isLeft ? leftVisual  : rightVisual;

        if (tracked == null || visual == null) return;

        if (requireGrab)
        {
            bool grabbing = isLeft ? leftGrabbing : rightGrabbing;
            if (!grabbing) return;
        }

        Vector3 axisWorld = wheelCenter.TransformDirection(wheelAxisLocal.normalized);

        Vector3 centerToHand = tracked.position - wheelCenter.position;

        // 1) Project onto wheel plane
        Vector3 onPlane = Vector3.ProjectOnPlane(centerToHand, axisWorld);
        if (onPlane.sqrMagnitude < 1e-6f)
            onPlane = wheelCenter.right;

        // 2) Direction around wheel face
        Vector3 dir = onPlane.normalized;

        // 3) Radius clamp (exact rim or ring)
        float radial = onPlane.magnitude;
        float minR = Mathf.Max(0.001f, rimRadius - rimBandHalfWidth);
        float maxR = rimRadius + rimBandHalfWidth;
        float r = rimBandHalfWidth <= 0.0001f ? rimRadius : Mathf.Clamp(radial, minR, maxR);

        // 4) Position on rim + slight outward offset
        Vector3 rimPoint = wheelCenter.position + dir * r;
        Vector3 outward = (offsetForward ? wheelCenter.forward : -wheelCenter.forward).normalized;
        Vector3 targetPos = rimPoint + outward * surfaceOffset;

        // 5) Rotation: face the center; keep up aligned with wheel
        Vector3 forward = -dir;
        Vector3 up = Vector3.ProjectOnPlane(wheelCenter.up, axisWorld).normalized;
        if (up.sqrMagnitude < 1e-6f) up = wheelCenter.up;

        Quaternion targetRot = Quaternion.LookRotation(forward, up);

        // Apply smoothly (visual only)
        visual.position = Vector3.Lerp(visual.position, targetPos, Time.deltaTime * positionLerp);
        visual.rotation = Quaternion.Slerp(visual.rotation, targetRot, Time.deltaTime * rotationLerp);
    }
}
