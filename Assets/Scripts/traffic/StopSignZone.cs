using UnityEngine;

public class StopSignZone : MonoBehaviour
{
    [Header("Drag the player's TelemetryManager here")]
    public TelemetryManager telemetry;

    [Tooltip("Below this speed (m/s) counts as a full stop.")]
    public float stopThreshold = 0.5f; // ~1.8 km/h

    [Header("Messages")]
    public string successMessage = "You came to a complete stop at the stop sign.";
    public string failureMessage = "You did not fully stop at the stop sign.";

    private bool tracking = false;
    private float minSpeedInside = float.MaxValue;
    private Transform playerRoot;

    private void Awake()
    {
        if (telemetry != null)
            playerRoot = telemetry.transform.root;
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

        if (minSpeedInside <= stopThreshold)
        {
            // telemetry.SendInstructorAlert(successMessage);
            Debug.Log($"[StopSignZone] Alert: {successMessage}");
        }
        else
        {
            // telemetry.SendInstructorAlert(failureMessage);
            Debug.Log($"[StopSignZone] Alert: {failureMessage}");
        }

        minSpeedInside = float.MaxValue;
    }
}
