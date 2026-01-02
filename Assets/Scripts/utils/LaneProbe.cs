using UnityEngine;

public class LaneProbe : MonoBehaviour
{
    public LaneMonitor monitor;

    void Awake()
    {
        if (!monitor)
            monitor = GetComponentInParent<LaneMonitor>();
    }

    void OnTriggerEnter(Collider other)
    {
        monitor?.ProbeEnter(other);
    }

    void OnTriggerExit(Collider other)
    {
        monitor?.ProbeExit(other);
    }
}
