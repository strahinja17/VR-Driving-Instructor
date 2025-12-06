using UnityEngine;
using UnityEngine.Splines;

public class LaneSpline : MonoBehaviour
{
    public SplineContainer spline;

    void Awake()
    {
        // Auto-assign if missing
        if (spline == null)
            spline = GetComponent<SplineContainer>();
    }

    //---------------------------------------------
    // Gets closest point on the spline (samples)
    //---------------------------------------------
    public Vector3 GetClosestPoint(Vector3 worldPos)
    {
        if (spline == null || spline.Spline == null)
            return worldPos;

        var s = spline.Spline;

        const int samples = 40; // higher = more accurate
        float minDist = float.MaxValue;
        Vector3 best = worldPos;

        for (int i = 0; i <= samples; i++)
        {
            float t = (float)i / samples;
            Vector3 p = SplineUtility.EvaluatePosition(s, t);

            float d = (p - worldPos).sqrMagnitude;
            if (d < minDist)
            {
                minDist = d;
                best = p;
            }
        }

        return best;
    }

    //---------------------------------------------
    // Lateral offset = distance to closest point
    //---------------------------------------------
    public float GetLateralOffset(Vector3 worldPos)
    {
        Vector3 c = GetClosestPoint(worldPos);
        Vector3 diff = worldPos - c;
        return new Vector2(diff.x, diff.z).magnitude;
    }
}
