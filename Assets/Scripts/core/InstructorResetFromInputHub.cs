using UnityEngine;

public class InstructorResetFromInputHub : MonoBehaviour
{
    public CarInputHub inputHub;
    public DrivingAIInstructorHub hub;

    [Tooltip("Safety cooldown so you can't accidentally reset multiple times.")]
    public float cooldownSeconds = 1.0f;

    private float _lastResetTime = -999f;

    private void Awake()
    {
        if (inputHub == null)
            inputHub = FindFirstObjectByType<CarInputHub>();

        if (hub == null)
            hub = DrivingAIInstructorHub.Instance ?? FindFirstObjectByType<DrivingAIInstructorHub>();
    }

    private void Update()
    {
        if (inputHub == null || hub == null)
            return;

        // THIS is the key line
        if (!inputHub.ConsumeRecenterPressed())
            return;

        float now = Time.unscaledTime;
        if (now - _lastResetTime < cooldownSeconds)
            return;

        _lastResetTime = now;

        hub.ResetInstructorHard(resendDirections: true);

        Debug.Log("[InstructorReset] Instructor hard reset triggered via wheel button (Recenter).");
    }
}
