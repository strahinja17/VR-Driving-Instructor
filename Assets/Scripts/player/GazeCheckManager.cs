using UnityEngine;

public class GazeCheckManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("XR HMD camera (XR Origin -> Camera).")]
    public Camera xrCamera;

    [Tooltip("Car transform for 'forward' reference. If null, uses this.transform.")]
    public Transform carReference;

    [Header("Mirror Colliders (put BoxCollider on the mirror surface)")]
    public Collider leftMirrorCollider;
    public Collider rightMirrorCollider;

    [Header("Mirror check")]
    [Tooltip("How long the gaze ray must stay on the mirror to count as a check.")]
    public float mirrorDwellSeconds = 0.2f;

    [Tooltip("Max distance for mirror gaze raycasts.")]
    public float mirrorMaxDistance = 3.0f;

    [Header("Shoulder check (head yaw)")]
    [Tooltip("Degrees yaw past which we count a shoulder check.")]
    public float shoulderYawThresholdDegrees = 60f;

    [Tooltip("How long the head must stay past the yaw threshold.")]
    public float shoulderDwellSeconds = 0.12f;

    // ---- Outputs (timestamps) ----
    public float LastLeftMirrorTime { get; private set; } = -999f;
    public float LastRightMirrorTime { get; private set; } = -999f;
    public float LastLeftShoulderTime { get; private set; } = -999f;
    public float LastRightShoulderTime { get; private set; } = -999f;

    // ---- Outputs (edge events, true only on the frame the check completes) ----
    public bool LeftMirrorCheckedThisFrame { get; private set; }
    public bool RightMirrorCheckedThisFrame { get; private set; }
    public bool LeftShoulderCheckedThisFrame { get; private set; }
    public bool RightShoulderCheckedThisFrame { get; private set; }

    // ---- Internal dwell counters ----
    private float _leftMirrorDwell, _rightMirrorDwell;
    private float _leftShoulderDwell, _rightShoulderDwell;

    private void Awake()
    {
        if (carReference == null) carReference = transform;
    }

    private void Update()
    {
        // reset one-frame flags
        LeftMirrorCheckedThisFrame = RightMirrorCheckedThisFrame = false;
        LeftShoulderCheckedThisFrame = RightShoulderCheckedThisFrame = false;

        if (xrCamera == null) return;

        float dt = Time.deltaTime;

        // 1) Mirror checks via gaze ray
        Ray gazeRay = new Ray(xrCamera.transform.position, xrCamera.transform.forward);

        if (leftMirrorCollider != null && MirrorDwellHit(gazeRay, leftMirrorCollider, ref _leftMirrorDwell, dt))
        {
            LastLeftMirrorTime = Time.time;
            LeftMirrorCheckedThisFrame = true;
        }

        if (rightMirrorCollider != null && MirrorDwellHit(gazeRay, rightMirrorCollider, ref _rightMirrorDwell, dt))
        {
            LastRightMirrorTime = Time.time;
            RightMirrorCheckedThisFrame = true;
        }

        // 2) Shoulder checks via head yaw relative to car forward
        float signedYaw = GetSignedYawFromCarForward();

        // Left shoulder: negative yaw beyond threshold
        if (signedYaw < -shoulderYawThresholdDegrees)
        {
            _leftShoulderDwell += dt;
            if (_leftShoulderDwell >= shoulderDwellSeconds)
            {
                _leftShoulderDwell = 0f;
                LastLeftShoulderTime = Time.time;
                LeftShoulderCheckedThisFrame = true;
            }
        }
        else
        {
            _leftShoulderDwell = 0f;
        }

        // Right shoulder: positive yaw beyond threshold
        if (signedYaw > shoulderYawThresholdDegrees)
        {
            _rightShoulderDwell += dt;
            if (_rightShoulderDwell >= shoulderDwellSeconds)
            {
                _rightShoulderDwell = 0f;
                LastRightShoulderTime = Time.time;
                RightShoulderCheckedThisFrame = true;
            }
        }
        else
        {
            _rightShoulderDwell = 0f;
        }
    }

    /// <summary>
    /// Use this for your blinker-armed logic.
    /// toLeft = true => left mirror/shoulder; false => right.
    /// </summary>
    public bool DidSideMirrorCheckThisFrame(bool toLeft)
        => toLeft ? LeftMirrorCheckedThisFrame : RightMirrorCheckedThisFrame;

    public bool DidShoulderCheckThisFrame(bool toLeft)
        => toLeft ? LeftShoulderCheckedThisFrame : RightShoulderCheckedThisFrame;

    /// <summary>
    /// Use this at lane-change initiation time.
    /// Returns whether checks happened within a time window.
    /// </summary>
    public (bool mirrorOk, bool shoulderOk) ChecksWithinWindow(bool toLeft, float windowSeconds)
    {
        float now = Time.time;

        float mirrorAge;
        if (toLeft)
            mirrorAge = now -  LastLeftMirrorTime;
        else
            mirrorAge = now - LastRightMirrorTime;
        float shoulderAge = toLeft ? (now - LastLeftShoulderTime) : (now - LastRightShoulderTime);

        bool mirrorOk = mirrorAge <= windowSeconds;
        bool shoulderOk = shoulderAge <= windowSeconds;

        return (mirrorOk, shoulderOk);
    }

    // ---------------- helpers ----------------

    private bool MirrorDwellHit(Ray gazeRay, Collider target, ref float dwell, float dt)
    {
        bool hit = Physics.Raycast(gazeRay, out RaycastHit rh, mirrorMaxDistance) && rh.collider == target;

        if (hit)
        {
            dwell += dt;
            if (dwell >= mirrorDwellSeconds)
            {
                dwell = 0f; // reset so repeated checks require re-dwell
                return true;
            }
        }
        else
        {
            dwell = 0f;
        }

        return false;
    }

    private float GetSignedYawFromCarForward()
    {
        Vector3 referenceForward = carReference != null ? carReference.forward : transform.forward;
        Vector3 headForward = xrCamera.transform.forward;

        referenceForward.y = 0f;
        headForward.y = 0f;

        if (referenceForward.sqrMagnitude < 1e-6f || headForward.sqrMagnitude < 1e-6f)
            return 0f;

        referenceForward.Normalize();
        headForward.Normalize();

        return Vector3.SignedAngle(referenceForward, headForward, Vector3.up);
    }
}
