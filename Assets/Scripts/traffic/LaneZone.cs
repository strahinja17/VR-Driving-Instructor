using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class LaneZone : MonoBehaviour
{
    public LaneSpline parentSpline;

    [Tooltip("If this zone borders another lane, set its spline here.")]
    public LaneSpline adjacentLane;

    [Tooltip("True if this border represents the LEFT-side lane-change direction.")]
    public bool isLeftSideAdjacency = false;

    public bool isEntry = false;
    public bool isExit = false;

    void Awake()
    {
        var bc = GetComponent<BoxCollider>();
        bc.isTrigger = true;
    }
}
