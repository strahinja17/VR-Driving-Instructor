using UnityEngine;
using UnityEngine.XR;

public class XRRecenterPoll : MonoBehaviour
{
    [Header("Reference to the car's input hub")]
    public CarInputHub inputHub;

    [Header("Optional: snap origin to seat after recenter")]
    public Transform seatAnchor;
    public bool snapOriginToSeat = true;

    private void Update()
    {
        if (inputHub == null) return;

        if (inputHub.ConsumeRecenterPressed())
            RecenterNow();
    }

    private void RecenterNow()
    {
        InputTracking.Recenter();

        if (snapOriginToSeat && seatAnchor != null)
        {
            transform.position = seatAnchor.position;

            // yaw only
            Vector3 fwd = seatAnchor.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
        }

        Debug.Log("[XR] Recenter triggered.");
    }
}
