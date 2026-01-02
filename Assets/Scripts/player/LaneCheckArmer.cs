using UnityEngine;

public class LaneChangeCheckArmer : MonoBehaviour
{
    [Header("References")]
    public CarBlinkers blinkers;        // has public bool leftOn, rightOn
    public GazeCheckManager gaze;       // your new gaze script

    [Header("Arming / timing")]
    [Tooltip("How long we keep the 'armed' session alive after blinker turns on.")]
    public float maxArmedSeconds = 6f;

    [Tooltip("How long checks remain valid once performed (used at evaluation time).")]
    public float validCheckWindowSeconds = 2.0f;

    [Header("Requirements")]
    public bool requireMirror = true;
    public bool requireShoulder = true;

    [Header("Debug")]
    public bool debugLogs = false;

    private enum ArmState { None, LeftArmed, RightArmed }
    private ArmState _state = ArmState.None;

    private float _armedAt = -999f;

    // flags within current armed session
    private bool _mirrorDone;
    private bool _shoulderDone;

    // prevents multiple scoring within one blinker session (optional)
    private bool _evaluatedThisSession;

    private void Awake()
    {
        if (blinkers == null) blinkers = GetComponent<CarBlinkers>();
        if (gaze == null) gaze = FindObjectOfType<GazeCheckManager>();
    }

    private void Update()
    {
        if (blinkers == null || gaze == null) return;

        // Decide desired state from blinkers
        ArmState desired =
            blinkers.leftOn ? ArmState.LeftArmed :
            blinkers.rightOn ? ArmState.RightArmed :
            ArmState.None;

        // If state changed, reset session
        if (desired != _state)
        {
            _state = desired;
            _armedAt = Time.time;
            _mirrorDone = false;
            _shoulderDone = false;
            _evaluatedThisSession = false;

            if (debugLogs) Debug.Log($"[LaneChangeCheckArmer] State => {_state}");
        }

        if (_state == ArmState.None) return;

        // Optional timeout: after X sec, stop listening (but keep state if blinker still on)
        if (Time.time - _armedAt > maxArmedSeconds)
            return;

        bool toLeft = (_state == ArmState.LeftArmed);

        // Flip flags when checks occur
        if (!_mirrorDone && gaze.DidSideMirrorCheckThisFrame(toLeft))
        {
            _mirrorDone = true;
            if (debugLogs) Debug.Log("[LaneChangeCheckArmer] Mirror check done.");
        }

        if (!_shoulderDone && gaze.DidShoulderCheckThisFrame(toLeft))
        {
            _shoulderDone = true;
            if (debugLogs) Debug.Log("[LaneChangeCheckArmer] Shoulder check done.");
        }
    }

    /// <summary>
    /// Call this at lane-change initiation time to evaluate if checks were done.
    /// Returns: (passed, missingMirror, missingShoulder)
    /// </summary>
    public (bool passed, bool missingMirror, bool missingShoulder) EvaluateForLaneChange(bool toLeft)
    {
        ArmState expected = toLeft ? ArmState.LeftArmed : ArmState.RightArmed;

        // If they didn't have the correct blinker on, the lane monitor already handles "no blinker" violation.
        if (_state != expected)
        {
            return (false, requireMirror, requireShoulder);
        }

        // Use either session flags OR timestamp window (more robust)
        var (mirrorOkByTime, shoulderOkByTime) = gaze.ChecksWithinWindow(
            toLeft,
            windowSeconds: validCheckWindowSeconds);

        bool mirrorOk = !requireMirror || _mirrorDone || mirrorOkByTime;
        bool shoulderOk = !requireShoulder || _shoulderDone || shoulderOkByTime;

        bool passed = mirrorOk && shoulderOk;

        // Mark evaluated so you don’t spam-check multiple frames in same lane-change contact
        _evaluatedThisSession = true;

        return (passed, !mirrorOk, !shoulderOk);
    }

    /// <summary>Optional: reset when lane change completes.</summary>
    public void ResetAfterLaneChange()
    {
        // Keep blinkers as the truth; we reset flags so the next lane change needs checks again
        _mirrorDone = false;
        _shoulderDone = false;
        _evaluatedThisSession = false;
        _armedAt = Time.time;
    }
}
