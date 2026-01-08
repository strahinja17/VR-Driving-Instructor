using UnityEngine;

public class InstructorResetInvoker : MonoBehaviour
{
    public DrivingAIInstructorHub hub;

    [Header("Trigger mode (optional)")]
    public bool useAsTrigger = false;
    public bool triggerOnce = true;
    private bool _didTrigger = false;

    [Tooltip("If set, only objects with TelemetryManager in parents can trigger reset.")]
    public bool requireTelemetryManager = true;

    private void Awake()
    {
        if (hub == null)
            hub = DrivingAIInstructorHub.Instance ?? FindFirstObjectByType<DrivingAIInstructorHub>();
    }

    // ---- UI Button calls this ----
    public void ResetInstructorNow()
    {
        if (hub == null)
        {
            Debug.LogError("[InstructorResetInvoker] Hub not found.");
            return;
        }

        hub.ResetInstructorHard(resendDirections: true);
    }

    // ---- Trigger box mode ----
    private void OnTriggerEnter(Collider other)
    {
        if (!useAsTrigger) return;
        if (triggerOnce && _didTrigger) return;

        if (requireTelemetryManager)
        {
            var telemetry = other.GetComponentInParent<TelemetryManager>();
            if (telemetry == null) return;
        }

        _didTrigger = true;
        ResetInstructorNow();
    }
}
