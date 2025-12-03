using UnityEngine;
using UnityEngine.InputSystem;

public class SteeringWheelAnimator : MonoBehaviour
{
    [Header("References")]
    public Transform steeringWheel;       // Assign in Inspector
    public float maxSteeringAngle = 450f; // 900° wheels = 450° each side
    public float rotationSmoothing = 12f; // Higher = snappier, lower = smoother

    private float lastSteerAngle = 0f;

    public float steeringDelta = 0.05f;

    private DrivingControlls controls;
    private float steerInput;             // -1 to +1
    private float currentAngle;           // Smoothed wheel angle

    private void Awake()
    {
        controls = new DrivingControlls();
        controls.Enable();
    }

    // private void Update()
    // {
    //     // Read steer input from your steering wheel profile
    //     steerInput = controls.Driving.Steer.ReadValue<float>();
    //     // steerInput should be -1 (full left) to +1 (full right)

    //     // Convert to degrees
    //     float targetAngle = steerInput * maxSteeringAngle;

    //     // Smooth rotation
    //     currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * rotationSmoothing);

    //     // Apply rotation — adjust axis if needed depending on model orientation
    //     steeringWheel.localRotation = Quaternion.Euler(0f, 0f, -targetAngle);
    // }

    void LateUpdate()
{
    if (controls == null || steeringWheel == null)
        return;

    // RAW value from -1 .. +1
    Vector2 steerVec = controls.Driving.Steer_small.ReadValue<Vector2>();
    float rawStickX = steerVec.x;          // this reacts instantly with small movements

    float angle = 0f;   
    if (Mathf.Abs(rawStickX) > 0.42f)
    {
        rawStickX = controls.Driving.Steer_big.ReadValue<float>();
    }
    else
    {
        float breaking = controls.Driving.Brake.ReadValue<float>();
        
        if (breaking != 1f)
        {
            if (Mathf.Abs(rawStickX * maxSteeringAngle - lastSteerAngle) < steeringDelta)
            {
                angle = rawStickX * maxSteeringAngle;
            }
            else
            {
                angle = lastSteerAngle;
            }
        }
        else
        {
            float steer = rawStickX;
            angle = steer * maxSteeringAngle;
            lastSteerAngle = angle;
        }
    }

    steeringWheel.localRotation = Quaternion.Euler(0f, 0f, -angle);
}

}
