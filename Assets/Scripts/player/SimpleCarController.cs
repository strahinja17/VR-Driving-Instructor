using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleCarController : MonoBehaviour
{
    public WheelCollider[] wheels;
    public Transform[] wheelMeshes;
    public float motorTorque = 500f;
    public float maxSteerAngle = 30f;
    public float brakeTorque = 3000f;

    private float accelInput = 0f;
    private float steerInput;
    private float brakeInput;
    private bool isReversing = false;

    private Rigidbody rb;
    private DrivingControlls drivingControls;

    private CarBlinkers blinkers;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, 0.5f, 0.1f);

        // Initialize and enable DrivingControls
        drivingControls = new DrivingControlls();
        drivingControls.Enable();

        // Bind the ReverseButton action
        drivingControls.Driving.ReverseButton.performed += ctx => ToggleReverse();

        blinkers = GetComponent<CarBlinkers>();
    }

    void ToggleReverse()
    {
        isReversing = !isReversing;
    }

    const float wheelMaxAbs = 0.69f;  // from your observation in Input Debugger

    void Update()
    {
        // Read input from DrivingControls
        // --- Steering from wheel (stick.x) ---
        Vector2 steerVec = drivingControls.Driving.Steer_small.ReadValue<Vector2>();
        float rawStickX = steerVec.x;          // this reacts instantly with small movements

        // Normalize from [-0.69 .. +0.69] to [-1 .. +1]
        float normalized = 0f;
        if (Mathf.Abs(rawStickX) > 0.0001f)
            normalized = Mathf.Clamp(rawStickX / wheelMaxAbs, -1f, 1f);

        steerInput = normalized;
        float rawThrottle = drivingControls.Driving.Throttle.ReadValue<float>();
        accelInput = Mathf.InverseLerp(1f, -1f, rawThrottle); 
        brakeInput = Mathf.Abs(drivingControls.Driving.Brake.ReadValue<float>() - 1);

        // Keyboard fallback (if no joystick input is detected)
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            // Only overwrite inputs if relevant keyboard keys are pressed
            bool anyKeyPressed = keyboard.wKey.isPressed || keyboard.sKey.isPressed ||
                                 keyboard.aKey.isPressed || keyboard.dKey.isPressed ||
                                 keyboard.spaceKey.isPressed;

            if (anyKeyPressed)
            {
                accelInput = (keyboard.wKey.isPressed ? 1f : 0f) +
                             (keyboard.sKey.isPressed ? -1f : 0f);

                steerInput = (keyboard.aKey.isPressed ? -1f : 0f) +
                             (keyboard.dKey.isPressed ? 1f : 0f);

                brakeInput = keyboard.spaceKey.isPressed ? 1f : 0f;
            }

            if (keyboard.qKey.isPressed)
            {
                blinkers.ToggleLeft();
            }

            if (keyboard.eKey.isPressed)
            {
                blinkers.ToggleRight();
            }

        }

        Debug.Log("Brake Input: " + brakeInput);

        // Apply a dead zone to throttle input
        if (Mathf.Abs(accelInput) < 0.1f)
        {
            accelInput = 0f;
        }
    }

    void FixedUpdate()
    {
        // Steering
        float speedFactor = Mathf.Clamp01(rb.linearVelocity.magnitude / 50f); // Scale steering based on speed
        float adjustedSteerAngle = steerInput * maxSteerAngle * (1f - speedFactor * 0.5f); // Reduce steering at high speeds
        wheels[0].steerAngle = adjustedSteerAngle;
        wheels[1].steerAngle = adjustedSteerAngle;

        // Motor torque and braking logic
        if (brakeInput != 0f)
        {
            // Apply brake torque to all wheels and disable motor torque
            foreach (var w in wheels)
            {
                w.motorTorque = 0f;
                w.brakeTorque = brakeTorque * brakeInput;
            }
        }
        else if (Mathf.Abs(accelInput) > 0.1f)
        {
            // Apply motor torque based on throttle and reverse mode
            float appliedTorque = accelInput * motorTorque * (isReversing ? -1f : 1f);
            foreach (var w in wheels)
            {
                w.motorTorque = appliedTorque;
                w.brakeTorque = 0f; // Release brakes
            }
        }
        else
        {
            // No throttle or brake: release motor torque and brakes
            foreach (var w in wheels)
            {
                w.motorTorque = 0f;
                w.brakeTorque = 0f;
            }
        }

        // Sync visuals
        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
            wheelMeshes[i].SetPositionAndRotation(pos, rot);
        }
    }
}
