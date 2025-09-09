using UnityEngine;
using UnityEngine.Splines;

public class LaneTracker : MonoBehaviour
{
    public SplineContainer laneCenter; // assign in Inspector
    public float laneHalfWidth = 1.75f; // meters (typical lane = 3.5m)
    public float departureTolerance = 0.2f; // slack before violation

    public static System.Action<float> OnLaneOffset; // signed meters
    public static System.Action<float> OnHeadingError; // degrees
    public static System.Action OnLaneDeparture;

    void Update()
    {
        if (!laneCenter) return;

        Vector3 pos = transform.position;

        // Find nearest point on the spline
        SplineUtility.GetNearestPoint(laneCenter.Splines[0], pos, out var nearest, out var t);
        Vector3 lanePos = laneCenter.transform.TransformPoint(nearest);
        Vector3 laneTangent = laneCenter.EvaluateTangent(0, t);
        Vector3 right = Vector3.Cross(Vector3.up, laneTangent).normalized;

        // Offset in meters (positive = right of center)
        float signedOffset = Vector3.Dot(pos - lanePos, right);

        // Heading error (angle between car forward and lane tangent)
        float headingError = Vector3.SignedAngle(transform.forward, laneTangent, Vector3.up);

        OnLaneOffset?.Invoke(signedOffset);
        OnHeadingError?.Invoke(headingError);

        Debug.Log($"Offset: {signedOffset:F2} m | Heading Error: {headingError:F1}°");


        if (Mathf.Abs(signedOffset) > laneHalfWidth + departureTolerance)
            OnLaneDeparture?.Invoke();
    }
}
