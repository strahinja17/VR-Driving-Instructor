using UnityEngine;

public class HandPlaneProjector : MonoBehaviour
{
    public Transform trackedHand;      // Reference to the real tracked OVRHand/Anchor
    public Transform steeringWheel;   // Reference to the in-game steering wheel center
    public float wheelRadius = 0.18f; // Limit hand to the rim

    void LateUpdate()
    {
        // 1. Get the hand's position relative to the wheel
        Vector3 directionToHand = trackedHand.position - steeringWheel.position;

        // 2. Project that position onto the wheel's local 2D plane (XY)
        Vector3 projectedPoint = Vector3.ProjectOnPlane(directionToHand, steeringWheel.forward);

        // 3. Optional: Snap the hand to the rim if it's too far from center
        if (projectedPoint.magnitude > wheelRadius)
        {
            projectedPoint = projectedPoint.normalized * wheelRadius;
        }

        // 4. Set the projected hand container's position
        transform.position = steeringWheel.position + projectedPoint;
        
        // 5. Keep hand rotation aligned with the wheel or the real hand
        transform.rotation = trackedHand.rotation; 
    }
}
