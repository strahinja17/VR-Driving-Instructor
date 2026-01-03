using UnityEngine;

public class DashInstruments : MonoBehaviour
{
    public Transform speedNeedle;
    public Transform rpmNeedle;

    public CarAudioController carAudioController; 

    private float s0 = 0f;
    private float s10 = 10f;
    private float s80 = 80f;
    private float s300 = 300f;

    private float speedAngle0 = 0f; // 0
    private float speedAngle10 = -25f;  // 10
    private float speedAngle80 = -135f;   // 80
    private float speedAngle300 = -275f;  // 300

    private float maxRpm = 8000f;
    private float rpmMinAngle = 0f;
    private float rpmMaxAngle = -275f;

    private Rigidbody carRigidbody; // assign this from your car controller

    private CarInputHub inputHub;

    private float speedKmh;    // set this from your car controller
    private float rpm;         // we’ll compute this below
    private float throttle01;  // 0..1, from your input

    private float idleRpm = 900f;
    private float maxRpmValue = 8000f;
    private float maxSpeedKmh = 300f;
    private float rpmResponseSpeed = 5f;  // how fast needle reacts

    private float rpmCurrent;

    private Vector3 baseRotation; // original rotation from editor
    private Vector3 baseRotationRPM; // original rotation from editor

    void Awake()
    {
        if (speedNeedle != null)
            baseRotation = speedNeedle.localEulerAngles;
        if (rpmNeedle != null)
            baseRotationRPM = rpmNeedle.localEulerAngles;
                        
    }

    void Start()
    {
        if (carRigidbody == null) carRigidbody = GetComponent<Rigidbody>();
        
        if (inputHub == null)
            inputHub = GetComponent<CarInputHub>();
    }

    void Update()
    {
        speedKmh = carRigidbody.linearVelocity.magnitude * 3.6f;
        
        throttle01 = inputHub != null ? inputHub.Throttle : 0f;

        // Speed needle
        if (speedNeedle != null)
        {
            float speedAngle = GetSpeedAngle(speedKmh);
            speedNeedle.localEulerAngles =
            new Vector3(baseRotation.x, baseRotation.y, speedAngle);

        }

        // RPM needle
        if (rpmNeedle != null)
        {
            float rpmAngle = GetRpmAngle(rpm, rpmMinAngle, rpmMaxAngle, maxRpm);
            rpmNeedle.localEulerAngles = new Vector3(baseRotationRPM.x, baseRotationRPM.y, rpmAngle);
        }
    }

    float GetSpeedAngle(float v)
    {
        v = Mathf.Clamp(v, s0, s300);

        if (v <= s10)
        {
            float t = Mathf.InverseLerp(s0, s10, v);
            return Mathf.Lerp(speedAngle0, speedAngle10, t);
        }
        else if (v <= s80)
        {
            float t = Mathf.InverseLerp(s10, s80, v);
            return Mathf.Lerp(speedAngle10, speedAngle80, t);
        }
        else
        {
            float t = Mathf.InverseLerp(s80, s300, v);
            return Mathf.Lerp(speedAngle80, speedAngle300, t);
        }
    }

    float GetRpmAngle(float rpmValue, float minAngle, float maxAngle, float maxRpmValue)
    {
        float t = Mathf.InverseLerp(0f, maxRpmValue, rpmValue);
        return Mathf.Lerp(minAngle, maxAngle, t);
    }

    void LateUpdate()
    {
        // compute visual rpm
        rpmCurrent = ComputeFakeRpm(rpmCurrent, throttle01, speedKmh);

        // expose it for needle drawing
        rpm = rpmCurrent;

        if (carAudioController != null)
        {
            // normalize rpm between idle and max
            float rpmNorm = Mathf.InverseLerp(idleRpm, maxRpmValue, rpmCurrent);
            carAudioController.SetRpmNormalized(rpmNorm);

            // we already have throttle01
            carAudioController.SetThrottle(throttle01);
        }
    }

    float ComputeFakeRpm(float currentRpm, float throttle01, float speedKmh)
    {
        // 0..1 based on current speed vs max
        float speedNorm = Mathf.InverseLerp(0f, maxSpeedKmh, speedKmh);

        float targetRpm;

        if (throttle01 < 0.05f)
        {
            // Foot off the gas: idle + a bit from speed
            float cruiseRpm = idleRpm + speedNorm * 800f; // 900–1700-ish
            targetRpm = cruiseRpm;
        }
        else
        {
            // CVT-ish behaviour:
            // At low speed, even full throttle is only ~3000 rpm
            // At top speed, full throttle is ~6500 rpm
            float lowSpeedFullThrottleRpm  = 3000f;
            float highSpeedFullThrottleRpm = 6500f;

            float fullThrottleRpmAtThisSpeed =
                Mathf.Lerp(lowSpeedFullThrottleRpm, highSpeedFullThrottleRpm, speedNorm);

            // Blend between idle and that value based on throttle amount
            targetRpm = Mathf.Lerp(idleRpm, fullThrottleRpmAtThisSpeed, throttle01);
        }

        // Clamp to your gauge max (visual)
        targetRpm = Mathf.Clamp(targetRpm, idleRpm, maxRpmValue);

        // Smooth motion so it ramps instead of snapping
        return Mathf.Lerp(currentRpm, targetRpm, Time.deltaTime * rpmResponseSpeed);
    }



}
