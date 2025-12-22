using UnityEngine;

public class WheelHandConstraint : MonoBehaviour
{
    [Header("Wheel Geometry")]
    public Transform wheelCenter;                 // center pivot of wheel
    public Vector3 wheelAxisLocal = new(0, 0, 1); // axis the wheel rotates around (local to wheelCenter)
    public float rimRadius = 0.18f;               // meters (tune to your wheel)
    public float surfaceOffset = 0.02f;           // push hand slightly toward player to avoid intersection
    public float rimThickness = 0.03f;            // allowed radial band around rim (prevents "inside wheel")

    [Header("Runtime snap targets")]
    public Transform leftSnapTarget;
    public Transform rightSnapTarget;

    private void Awake()
    {
        if (wheelCenter == null) wheelCenter = transform;

        leftSnapTarget ??= CreateTarget("WheelSnap_Left_Runtime");
        rightSnapTarget ??= CreateTarget("WheelSnap_Right_Runtime");
    }

    private Transform CreateTarget(string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(wheelCenter, worldPositionStays: true);
        return t;
    }

    /// <summary>
    /// Updates the snap target pose from a tracked hand pose. This projects onto the wheel plane,
    /// clamps to the rim, and offsets outward to prevent intersection.
    /// </summary>
    public void UpdateSnapFromHand(bool isLeftHand, Transform trackedHand)
    {
        if (trackedHand == null) return;

        Transform snap = isLeftHand ? leftSnapTarget : rightSnapTarget;

        Vector3 axisWorld = wheelCenter.TransformDirection(wheelAxisLocal.normalized);

        // 1) Project hand position onto the wheel plane
        Vector3 centerToHand = trackedHand.position - wheelCenter.position;
        Vector3 onPlane = Vector3.ProjectOnPlane(centerToHand, axisWorld);

        if (onPlane.sqrMagnitude < 1e-6f)
            onPlane = wheelCenter.right; // fallback

        Vector3 dir = onPlane.normalized;

        // 2) Clamp radius to stay around the rim (avoid being inside/outside wheel too much)
        float radial = onPlane.magnitude;
        float minR = Mathf.Max(0.001f, rimRadius - rimThickness * 0.5f);
        float maxR = rimRadius + rimThickness * 0.5f;
        float clampedR = Mathf.Clamp(radial, minR, maxR);

        Vector3 rimPoint = wheelCenter.position + dir * clampedR;

        // 3) Offset slightly toward player / away from wheel plane to avoid intersection
        // Choose a consistent "outward" direction. Often wheelCenter.forward points toward driver.
        Vector3 outward = wheelCenter.forward.normalized;
        Vector3 finalPos = rimPoint + outward * surfaceOffset;

        // 4) Set rotation so the hand looks like it grips the rim
        // forward points toward wheel center, up tries to align with wheel up but stays in plane
        Vector3 forward = -dir;
        Vector3 up = Vector3.ProjectOnPlane(wheelCenter.up, axisWorld).normalized;
        if (up.sqrMagnitude < 1e-6f) up = wheelCenter.up;

        snap.position = finalPos;
        snap.rotation = Quaternion.LookRotation(forward, up);
    }
}
