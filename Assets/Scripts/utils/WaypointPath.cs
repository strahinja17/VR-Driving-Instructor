using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    [Tooltip("Waypoints in order; if empty, children will be used automatically.")]
    public Transform[] waypoints;

    public Transform[] GetWaypoints()
    {
        if (waypoints != null && waypoints.Length > 0)
            return waypoints;

        // Auto-collect children if array not set
        int count = transform.childCount;
        Transform[] result = new Transform[count];
        for (int i = 0; i < count; i++)
            result[i] = transform.GetChild(i);
        return result;
    }

    private void OnDrawGizmos()
    {
        var pts = GetWaypoints();
        Gizmos.color = Color.yellow;

        for (int i = 0; i < pts.Length; i++)
        {
            if (pts[i] == null) continue;

            Gizmos.DrawSphere(pts[i].position, 0.1f);

            if (i + 1 < pts.Length && pts[i + 1] != null)
                Gizmos.DrawLine(pts[i].position, pts[i + 1].position);
        }
    }
}
