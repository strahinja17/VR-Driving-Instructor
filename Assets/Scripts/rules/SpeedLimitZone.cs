using UnityEngine;

public class SpeedLimitZone : MonoBehaviour
{
    [Header("Drag your TelemetryManager here")]
    public TelemetryManager telemetryManager;

    [Header("New speed limit applied when EXITING the box")]
    public float newSpeedLimit = 50f;

    [Header("Warning message when ENTERING the box")]
    public string alertMessage = "Watch the changed speed limit ahead.";

    private void OnTriggerEnter(Collider other)
    {
        if (telemetryManager == null) return;

        // Send warning BEFORE the change
        // telemetryManager.SendInstructorAlert(alertMessage);
        Debug.Log($"[SpeedLimitZone] Alert: {alertMessage}");

        DrivingAIInstructorHub.Instance.NotifyDrivingEvent(
            eventName: "SpeedLimitChange",
            playerUtterance: null,
            extraInstruction: "Use only a few words, to remind the player of the impending speed limit change");
    }

    private void OnTriggerExit(Collider other)
    {
        if (telemetryManager == null) return;

        // Apply the speed limit AFTER the buffer zone
        telemetryManager.speedLimit = newSpeedLimit;
    }
}
