using UnityEngine;

public class CrosswalkMonitor : MonoBehaviour
{
    [Tooltip("Speed (km/h) above which failing to yield is considered dangerous.")]
    public float dangerSpeedKmh = 12f;

    [Tooltip("If true, logs the events; your AI system can hook into this.")]
    public bool debugLogs = true;

    private int pedestriansInside = 0;

    private void OnTriggerEnter(Collider other)
    {
        // 1. Detect pedestrians
        if (other.CompareTag("Pedestrian"))
        {
            pedestriansInside++;
            return;
        }

        // 2. Detect player car
        var telemetry = other.transform.root.GetComponent<TelemetryManager>();
        if (telemetry == null) return;

        float kmh = telemetry.rb.linearVelocity.magnitude * 3.6f;

        if (pedestriansInside > 0 && kmh > dangerSpeedKmh)
        {
            SendAlert("Dangerous approach: pedestrian in crosswalk.");
        }

        if (pedestriansInside > 0 && kmh < 6f)
            SendAlert("Good job slowing for the pedestrian.");

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Pedestrian"))
        {
            pedestriansInside--;
        }
    }

    private void SendAlert(string msg)
    {
        if (debugLogs) Debug.Log($"[Crosswalk] {msg}");

        DrivingAIInstructorHub.Instance.NotifyDrivingEvent(
                            eventName: "CrosswalkZone",
                            playerUtterance: null,
                            extraInstruction: $"React to message regarding crosswalk adhereance accordingly, and shortly: {msg}");
        
        // forward to your AI or telemetry event system here
    }
}
