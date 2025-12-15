using UnityEngine;

public class TelemetryManager : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider rearLeft;
    public WheelCollider rearRight;

    [Header("Settings")]
    public float speedLimit = 50f; // km/h

    private Vector3 lastVelocity;
    private float lastTime;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        lastVelocity = rb.linearVelocity;
        lastTime = Time.time;
    }

    void Update()
    {
        // Speed in km/h
        float speed = rb.linearVelocity.magnitude * 3.6f;
        Debug.Log($"Speed: {speed:F1} km/h");

        // Acceleration (change in velocity per second)
        float dt = Time.time - lastTime;
        if (dt > 0)
        {
            float acceleration = (rb.linearVelocity.magnitude - lastVelocity.magnitude) / dt;
            Debug.Log($"Acceleration: {acceleration:F2} m/s²");
        }

        lastVelocity = rb.linearVelocity;
        lastTime = Time.time;

        // Check speed limit
        if (speed > speedLimit + 3f)
        {
            Debug.LogWarning($"⚠️ Speeding! Current: {speed:F1} km/h | Limit: {speedLimit}");

            DrivingAIInstructorHub.Instance.NotifyDrivingEvent(
            eventName: "SpeedingWarning",
            playerUtterance: null,
            extraInstruction: "Be very brief, < 2 sentences. Don't phrase it with you've revieved.. YOU are warning the player."
                                +  $"The speed was : {speed:F1} km/h | Limit: {speedLimit}"
        );
        }

        // Check slip for each wheel
        CheckWheelSlip(frontLeft, "Front Left");
        CheckWheelSlip(frontRight, "Front Right");
        CheckWheelSlip(rearLeft, "Rear Left");
        CheckWheelSlip(rearRight, "Rear Right");
    }

    void CheckWheelSlip(WheelCollider wheel, string name)
    {
        if (wheel == null) return;

        WheelHit hit;
        if (wheel.GetGroundHit(out hit))
        {
            float sideways = hit.sidewaysSlip;
            float forward = hit.forwardSlip;

            if (Mathf.Abs(sideways) > 0.3f)
                Debug.Log($"⚠️ {name} sideways slip: {sideways:F2}");

            if (Mathf.Abs(forward) > 0.3f)
                Debug.Log($"⚠️ {name} forward slip: {forward:F2}");
        }
    }
}
