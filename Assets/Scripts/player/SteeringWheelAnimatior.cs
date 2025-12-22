using UnityEngine;

public class SteeringWheelAnimator : MonoBehaviour
{
    public Transform steeringWheel;
    public CarInputHub input;

    public float maxSteeringAngle = 450f;
    public float rotationSmoothing = 12f;

    public enum Axis { X, Y, Z }
    public Axis rotateAxis = Axis.Z;

    private float currentAngle;
    private Quaternion baseLocalRotation;

    private void Awake()
    {
        if (input == null)
            input = GetComponentInParent<CarInputHub>();

        if (steeringWheel == null)
            Debug.LogError("[SteeringWheelAnimator] steeringWheel is NOT assigned.");
        else
            baseLocalRotation = steeringWheel.localRotation; // ✅ store the tilt/original pose
    }

    private void LateUpdate()
    {
        if (steeringWheel == null || input == null) return;

        float targetAngle = input.Steer * maxSteeringAngle;
        currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * rotationSmoothing);

        Quaternion steerRot = rotateAxis switch
        {
            Axis.X => Quaternion.AngleAxis(-currentAngle, Vector3.right),
            Axis.Y => Quaternion.AngleAxis(-currentAngle, Vector3.up),
            _      => Quaternion.AngleAxis(-currentAngle, Vector3.forward),
        };

        // ✅ Preserve original tilt/orientation, apply steering on top
        steeringWheel.localRotation = baseLocalRotation * steerRot;
    }

    // Optional: if some other script changes the wheel pose at runtime,
    // call this to "re-capture" the base rotation.
    public void RecalibrateBaseRotation()
    {
        if (steeringWheel != null)
            baseLocalRotation = steeringWheel.localRotation;
    }
}
