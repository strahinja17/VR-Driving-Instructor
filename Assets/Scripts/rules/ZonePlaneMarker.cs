using UnityEngine;

public enum ZoneType
{
    Slowdown,
    ScenarioStart,
    ScenarioEnd
}

[RequireComponent(typeof(BoxCollider))]
public class ZoneMarker : MonoBehaviour
{
    public string markerId = "zone_1";
    public ZoneType type = ZoneType.Slowdown;
    public float advisorySpeedKmh = 30f;

    BoxCollider col;

    void Reset()
    {
        col = GetComponent<BoxCollider>();
        col.isTrigger = true; // must be trigger!
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        switch (type)
        {
            case ZoneType.Slowdown:
                DrivingEvents.RaiseAdvisorySlowdown(markerId);
                Debug.Log($"[ZoneMarker] Slowdown zone entered ({markerId}) {advisorySpeedKmh} km/h");
                break;
            case ZoneType.ScenarioStart:
                DrivingEvents.RaiseScenarioStart(markerId);
                Debug.Log($"[ZoneMarker] Scenario START ({markerId})");
                break;
            case ZoneType.ScenarioEnd:
                DrivingEvents.RaiseScenarioEnd(markerId);
                Debug.Log($"[ZoneMarker] Scenario END ({markerId})");
                break;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (type == ZoneType.Slowdown)
            Debug.Log($"[ZoneMarker] Exited slowdown zone ({markerId})");
    }
}
