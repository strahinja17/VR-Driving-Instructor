using UnityEngine;

public class SimpleCarController : MonoBehaviour
{
    [Header("Wheel Colliders / Meshes")]
    public WheelCollider[] wheels;     // 0,1 = front
    public Transform[] wheelMeshes;

    [Header("Tuning")]
    public float motorTorque = 500f;
    public float maxSteerAngle = 30f;
    public float brakeTorque = 3000f;

    [Header("Steering Assist")]
    public float steeringHighSpeedReduction = 0.5f; // reduces steer at high speed
    public float highSpeedReference = 50f;          // m/s-ish reference

    private Rigidbody rb;
    private CarInputHub input;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, 0.5f, 0.1f);

        input = GetComponent<CarInputHub>();
        if (input == null)
        {
            Debug.LogError("Missing CarInputHub on the car. Add it to the same GameObject.");
        }
    }

    private void FixedUpdate()
    {
        if (input == null) return;

        // --- Steering ---
        float speed = rb.linearVelocity.magnitude;
        float speedFactor = Mathf.Clamp01(speed / highSpeedReference);
        float steerReduction = 1f - speedFactor * steeringHighSpeedReduction;

        float steerAngle = input.Steer * maxSteerAngle * steerReduction;
        wheels[0].steerAngle = steerAngle;
        wheels[1].steerAngle = steerAngle;

        // --- Motor + Brakes ---
        float brake01 = input.Brake;
        float throttle01 = input.Throttle;

        // If you want brake to override throttle:
        if (brake01 > 0.001f)
        {
            foreach (var w in wheels)
            {
                w.motorTorque = 0f;
                w.brakeTorque = brakeTorque * brake01;
            }
        }
        else if (throttle01 > 0.001f)
        {
            float direction = input.Reverse ? -1f : 1f;
            float torque = throttle01 * motorTorque * direction;

            foreach (var w in wheels)
            {
                w.brakeTorque = 0f;
                w.motorTorque = torque;
            }
        }
        else
        {
            foreach (var w in wheels)
            {
                w.motorTorque = 0f;
                w.brakeTorque = 0f;
            }
        }

        // --- Sync visuals ---
        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
            wheelMeshes[i].SetPositionAndRotation(pos, rot);
        }
    }
}
