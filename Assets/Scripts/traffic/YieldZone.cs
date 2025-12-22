using UnityEngine;

public class YieldZone : MonoBehaviour
{
    [Header("Drag the player's TelemetryManager here")]
    public TelemetryManager telemetry;

    [Tooltip("Player must drop below this speed (km/h) inside the zone.")]
    public float yieldThresholdKmh = 10f; // 10 km/h default

    [Header("Messages")]
    public string successMessage = "You slowed appropriately at the yield sign.";
    public string failureMessage = "You did not slow sufficiently at the yield sign.";

    private bool tracking = false;
    private float minSpeedInside = float.MaxValue;
    private Transform playerRoot;

    private bool AIMode;

    public AudioClip yieldVio;

    private void Awake()
    {
        if (telemetry != null)
            playerRoot = telemetry.transform.root;

    }

    void Start()
    {
        AIMode = StudyConditionManager.Instance.IsAIEnabled;
    }


    private bool IsPlayer(Collider other)
    {
        if (telemetry == null || playerRoot == null)
            return false;

        return other.transform.root == playerRoot;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        tracking = true;
        minSpeedInside = float.MaxValue;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!tracking || !IsPlayer(other)) return;

        float speed = telemetry.rb.linearVelocity.magnitude; // << USING RB VELOCITY HERE

        if (speed < minSpeedInside)
            minSpeedInside = speed;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!tracking || !IsPlayer(other)) return;

        tracking = false;

        // Convert to m/s: km/h * (1000/3600)
        float thresholdMs = yieldThresholdKmh * (1000f / 3600f);

        if (minSpeedInside <= thresholdMs)
        {
            // telemetry.SendInstructorAlert(successMessage);
            Debug.Log($"[YieldZone] Alert: {successMessage}");
            if (AIMode) {
                DrivingAIInstructorHub.Instance.NotifyDrivingEvent(
                                eventName: "YieldSignZone",
                                playerUtterance: null,
                                extraInstruction: "Acknowledge properly yielding for a yield sign, in very few words.");
            }
        }
        else
        {
            // telemetry.SendInstructorAlert(failureMessage);
            Debug.Log($"[YieldZone] Alert: {failureMessage}");
            if (AIMode) {
                DrivingAIInstructorHub.Instance.NotifyDrivingEvent(
                                eventName: "YieldSignZone",
                                playerUtterance: null,
                                extraInstruction: "Point out the player failed to yield propely for a yield sign, in very few words.");
            } else
            {
                GlobalInstructorAudio.Play(yieldVio);
            }
        }

        minSpeedInside = float.MaxValue;
    }
}
