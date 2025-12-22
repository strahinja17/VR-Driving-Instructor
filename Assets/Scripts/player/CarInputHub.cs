using UnityEngine;
using UnityEngine.InputSystem;

public class CarInputHub : MonoBehaviour
{
    [Header("Steering")]
    [Tooltip("Optional: scale steering if your wheel action isn't already -1..+1.")]
    public float steerScale = 1f;

    [Tooltip("Clamp steering to [-1, +1].")]
    public bool clampSteer = true;

    [Header("Pedals")]
    [Tooltip("Deadzone for pedals (0..1). Helps with noisy pedals.")]
    [Range(0f, 0.2f)] public float pedalDeadzone = 0.02f;

    [Header("Keyboard Fallback")]
    public bool keyboardFallback = true;

    public float Steer { get; private set; }      // -1..+1
    public float Throttle { get; private set; }   //  0..1
    public float Brake { get; private set; }      //  0..1
    public bool Reverse { get; private set; }     // toggle

    private bool _recenterPressed;

    private DrivingControlls controls;
    private CarBlinkers blinkers;

    private void Awake()
    {
        controls = new DrivingControlls();
        blinkers = GetComponent<CarBlinkers>();
    }

    private void OnEnable()
    {
        controls.Enable();
        controls.Driving.ReverseButton.performed += OnReversePerformed;
        controls.Driving.Recenter.performed += OnRecenterPerformed;
    }

    private void OnDisable()
    {
        controls.Driving.ReverseButton.performed -= OnReversePerformed;
        controls.Driving.Recenter.performed -= OnRecenterPerformed;
        controls.Disable();
    }

    private void OnReversePerformed(InputAction.CallbackContext ctx)
    {
        Reverse = !Reverse;
    }

    private void OnRecenterPerformed(InputAction.CallbackContext _)
    {
        _recenterPressed = true;
    }

    /// <summary>
    /// Returns true once per press. Clears the flag after read.
    /// Call this from your XR origin script.
    /// </summary>
    public bool ConsumeRecenterPressed()
    {
        if (!_recenterPressed) return false;
        _recenterPressed = false;
        return true;
    }

    private void Update()
    {
        // --- Read from wheel/pedals ---
        float steerRaw = controls.Driving.Steer.ReadValue<float>();  // ideally already -1..+1 after your deadzone fix
        float throttleRaw = controls.Driving.Throttle.ReadValue<float>();
        float brakeRaw = controls.Driving.Brake.ReadValue<float>();  // make sure this exists in your input actions

        Steer = steerRaw * steerScale;
        if (clampSteer) Steer = Mathf.Clamp(Steer, -1f, 1f);

        Throttle = Mathf.InverseLerp(-1f, 1f, throttleRaw);
        Brake = Mathf.InverseLerp(1f, -1f, brakeRaw);

        // Blinkers (use WasPressedThisFrame if your actions are Buttons)
        if (controls.Driving.BlinkerLeft.WasPressedThisFrame())
            blinkers?.ToggleLeft();

        if (controls.Driving.BlinkerRight.WasPressedThisFrame())
            blinkers?.ToggleRight();

        // --- Optional keyboard fallback ---
        if (keyboardFallback)
            ApplyKeyboardOverrideIfPressed();
    }

    private void ApplyKeyboardOverrideIfPressed()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        bool anyKeyPressed =
            kb.wKey.isPressed || kb.sKey.wasPressedThisFrame ||
            kb.aKey.isPressed || kb.dKey.isPressed ||
            kb.spaceKey.isPressed ||
            kb.qKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame;

        if (!anyKeyPressed) return;

        float accel = (kb.wKey.isPressed ? 1f : 0f);
        float steer = (kb.aKey.isPressed ? -1f : 0f) + (kb.dKey.isPressed ? 1f : 0f);
        float brake = kb.spaceKey.isPressed ? 1f : 0f;

        // If S is pressed, treat it as reverse throttle (or brake) depending on your design.
        // Here: W = throttle, S = brake unless Reverse is toggled.
        if (accel > 0f) { Throttle = 1f; Brake = 0f; }
        else { /* keep pedal values */ }

        Steer = Mathf.Clamp(steer, -1f, 1f);

        if (kb.qKey.wasPressedThisFrame) blinkers?.ToggleLeft();
        if (kb.eKey.wasPressedThisFrame) blinkers?.ToggleRight();
        if (kb.sKey.wasPressedThisFrame) Reverse = !Reverse;
    }

    /// <summary>
    /// Normalize pedal axis to 0..1.
    /// Supports:
    ///  - already 0..1 (common)
    ///  - -1..1 (common for some devices) -> map to 0..1
    /// </summary>
    private float NormalizePedal(float raw)
    {
        float v;

        // If it looks like it's in -1..1, map it to 0..1.
        // This heuristic works well in practice.
        if (raw < -0.001f || raw > 1.001f)
        {
            v = Mathf.InverseLerp(-1f, 1f, raw);
        }
        else
        {
            v = Mathf.Clamp01(raw);
        }

        // Deadzone
        if (v < pedalDeadzone) v = 0f;
        return v;
    }
}
