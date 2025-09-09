using UnityEngine;

public enum LineType
{
    SpeedLimit,
    StopLine
}

public class LineMarker : MonoBehaviour
{
    public string markerId = "line_1";
    public LineType type = LineType.SpeedLimit;
    public float speedLimitKmh = 50f;
    public bool oneWay = true;

    public Plane GetPlane() => new Plane(transform.forward, transform.position);

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = (type == LineType.StopLine) ? Color.red : Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, new Vector3(6f, 0.05f, 0.1f));
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.white;
        Gizmos.DrawRay(transform.position, transform.forward * 0.6f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.2f, $"{type} ({markerId})");
#endif
    }
#endif
}
