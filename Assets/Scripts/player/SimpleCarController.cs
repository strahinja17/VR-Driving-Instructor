using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleCarController : MonoBehaviour
{
    public WheelCollider[] wheels;
    public Transform[] wheelMeshes;
    public float motorTorque = 500f;
    public float maxSteerAngle = 30f;
    public float brakeTorque = 3000f;

    private float accelInput;
    private float steerInput;
    private bool brakeInput;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.7f, 0f);
    }

    void Update()
    {
        // Keyboard (WASD) fallback using the new InputSystem
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            accelInput = (keyboard.wKey.isPressed ? 1f : 0f) +
                         (keyboard.sKey.isPressed ? -1f : 0f);
            steerInput = (keyboard.aKey.isPressed ? -1f : 0f) +
                         (keyboard.dKey.isPressed ? 1f : 0f);
            brakeInput = keyboard.spaceKey.isPressed;
        }
    }

    void FixedUpdate()
    {
        // Steering
        wheels[0].steerAngle = steerInput * maxSteerAngle;
        wheels[1].steerAngle = steerInput * maxSteerAngle;

        // Motor torque
        wheels[2].motorTorque = accelInput * motorTorque;
        wheels[3].motorTorque = accelInput * motorTorque;

        // Brakes
        float appliedBrake = brakeInput ? brakeTorque : 0f;
        foreach (var w in wheels)
            w.brakeTorque = appliedBrake;

        // Sync visuals
        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
            wheelMeshes[i].SetPositionAndRotation(pos, rot);
        }
    }
}
