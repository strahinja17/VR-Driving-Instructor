using UnityEngine;

public class RedLightEntryZone : MonoBehaviour
{
    [Header("Drag the player TelemetryManager here")]
    public TelemetryManager telemetry;

    [Header("Drag the corresponding TrafficLightController here")]
    public TrafficLightController trafficLight;

    [Header("Message sent when player enters on red")]
    public string redLightMessage = "You entered the intersection on a red light.";

    private Transform _playerRoot;

    private bool AIMode;

    public AudioClip redLightVio;

    private void Awake()
    {
        if (telemetry != null)
            _playerRoot = telemetry.transform.root;
    }

    void Start()
    {
        AIMode = StudyConditionManager.Instance.IsAIEnabled;
    }

    private bool IsPlayer(Collider other)
    {
        if (telemetry == null || _playerRoot == null)
            return false;

        // Compare roots so it still works if the collider is on a child object
        return other.transform.root == _playerRoot;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        if (telemetry == null || trafficLight == null) return;

        var state = trafficLight.CurrentState;

        // Treat these as "not allowed to enter"
        bool isProhibited =
            state == TrafficLightController.LightState.Red ||
            state == TrafficLightController.LightState.RedYellow;

        if (isProhibited)
        {
            // telemetry.SendInstructorAlert(redLightMessage);
            Debug.Log($"[RedLightEntryZone] Alert: {redLightMessage}");
            if (AIMode) {
            DrivingAIInstructorHub.Instance.NotifyDrivingEvent(
                            eventName: "RedLightViolation",
                            playerUtterance: null,
                            extraInstruction: "Tell the player in a few words, but with intensity, that they've entered an intersection on a red light.");
            } else
            {
                GlobalInstructorAudio.Play(redLightVio);
            }
            StudySessionManager.Instance.RegisterWarning("RedLight");
        }
    }
}
