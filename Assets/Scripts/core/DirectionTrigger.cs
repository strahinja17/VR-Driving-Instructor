using UnityEngine;

public class DirectionTrigger : MonoBehaviour
{
    [Header("Trigger")]
    public bool triggerOnce = true;
    private bool hasFired;

    [Header("Detect player by script presence")]
    [Tooltip("If true, we require a TelemetryManager somewhere on the entering object or its parents.")]
    public bool requireTelemetryManager = true;

    public AudioClip noAiDirectionClip;

    [TextArea(3, 8)]
    public string aiContext =
        "This intersection has limited sightlines and frequent pedestrians. Emphasize scanning and yielding.";

    public string aiDirection = "";

    [Header("LLM Hub reference")]
    public DrivingAIInstructorHub instructorHub; // drag in, or auto-find

    [Header("Rate limiting")]
    [Tooltip("Minimum seconds between firings even if player re-enters quickly.")]
    public float minSecondsBetween = 2f;
    private float lastFireTime = -999f;

    private void Awake()
    {
        if (instructorHub == null)
            instructorHub = FindFirstObjectByType<DrivingAIInstructorHub>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasFired) return;
        if (Time.time - lastFireTime < minSecondsBetween) return;

        // Detect player by TelemetryManager
        if (requireTelemetryManager)
        {
            var tm = other.GetComponentInParent<TelemetryManager>();
            if (tm == null) return;
        }

        lastFireTime = Time.time;
        hasFired = true;

        bool aiMode = StudyConditionManager.Instance != null && StudyConditionManager.Instance.IsAIEnabled;

        if (!aiMode)
        {
            if (noAiDirectionClip != null)
            {
                GlobalInstructorAudio.Play(noAiDirectionClip);
            }
        }
        else
        {
            if (instructorHub != null && instructorHub.enabled)
            {
                string prompt =
                    $"Give the driver navigation directions.\n" +
                    $"Directions: {aiDirection} \n" +
                    $"Add brief context specific to this spot:\n{aiContext}\n" +
                    $"Constraints: 1 sentences, calm coaching tone, DON'T MAKE UP DIRECTIONS! STRICTLY ADHERE TO 'Directions: {aiDirection}' DON'T MAKE UP DISTANCES!";

                
                DrivingAIInstructorHub.Instance.NotifyDrivingEvent(
                eventName: "Directions",
                playerUtterance: null,
                extraInstruction: prompt);
            }
        }
    }

    // Optional: call this between runs if you don't reload scene
    public void ResetTrigger()
    {
        hasFired = false;
        lastFireTime = -999f;
    }
}
