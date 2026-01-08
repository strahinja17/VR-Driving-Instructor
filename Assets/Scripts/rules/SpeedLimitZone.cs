using UnityEngine;

public class SpeedLimitZone : MonoBehaviour
{
    [Header("Drag your TelemetryManager here")]
    public TelemetryManager telemetryManager;

    [Header("New speed limit applied when EXITING the box")]
    public float newSpeedLimit = 50f;

    [Header("Warning message when ENTERING the box")]
    public string alertMessage = "Watch the changed speed limit ahead.";

    private bool AIMode;

    public AudioClip limitChange;

    private Transform playerRoot;

    void Start()
    {
        AIMode = StudyConditionManager.Instance.IsAIEnabled;

        if (telemetryManager != null)
            playerRoot = telemetryManager.transform.root;
    }

    private bool IsPlayer(Collider other)
    {
        if (telemetryManager == null || playerRoot == null)
            return false;

        return other.transform.root == playerRoot;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (telemetryManager == null) return;

        if (!IsPlayer(other)) return;

        // Send warning BEFORE the change
        // telemetryManager.SendInstructorAlert(alertMessage);
        Debug.Log($"[SpeedLimitZone] Alert: {alertMessage}");

        if (AIMode) {
        DrivingAIInstructorHub.Instance.NotifyDrivingEvent(
            eventName: "SpeedLimitChange",
            playerUtterance: null,
            extraInstruction: "Use only a few words, to remind the player of the impending speed limit change");
        } else
        {
            GlobalInstructorAudio.Play(limitChange);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (telemetryManager == null) return;

        if (!IsPlayer(other)) return;

        // Apply the speed limit AFTER the buffer zone
        telemetryManager.speedLimit = newSpeedLimit;
    }
}
