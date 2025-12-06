using UnityEngine;

public class CarBlinkers : MonoBehaviour
{
    [Header("Blinker Meshes (world lights)")]
    public GameObject[] leftBlinkerMeshes;
    public GameObject[] rightBlinkerMeshes;

    [Header("Dashboard Arrows (player only)")]
    public GameObject leftDashArrow;
    public GameObject rightDashArrow;

    [Header("Blink Settings")]
    public float blinkInterval = 0.5f; // how long each ON/OFF state lasts

    [Header("Audio (long blinker clips supported)")]
    public AudioSource blinkerSource; 
    public AudioClip blinkerLoop;      // long full blinker sound

    public bool leftOn = false;
    public bool rightOn = false;

    private float blinkTimer = 0f;
    private bool blinkState = false; // toggles ON/OFF

    // -------------------------------
    // PUBLIC TOGGLE METHODS
    // -------------------------------

    public void ToggleLeft()
    {
        leftOn = !leftOn;
        if (leftOn) rightOn = false;
    }

    public void ToggleRight()
    {
        rightOn = !rightOn;
        if (rightOn) leftOn = false;
    }

    public void SetLeftBlinker(bool state)
    {
        leftOn = state;
        if (state) rightOn = false;
    }

    public void SetRightBlinker(bool state)
    {
        rightOn = state;
        if (state) leftOn = false;
    }

    public void TurnOffAll()
    {
        leftOn = false;
        rightOn = false;
        blinkState = false;
        ApplyBlinkState(false);
        StopAudio();
    }

    // -------------------------------
    // MAIN UPDATE LOGIC
    // -------------------------------

    void Update()
    {
        bool active = leftOn || rightOn;

        if (!active)
        {
            // If neither blinker is enabled, force everything OFF
            blinkState = false;
            ApplyBlinkState(false);
            StopAudio();
            return;
        }

        // Handle the timer for blinking
        blinkTimer += Time.deltaTime;
        if (blinkTimer >= blinkInterval)
        {
            blinkTimer = 0f;
            blinkState = !blinkState; // flip ON/OFF
            ApplyBlinkState(blinkState);

            if (blinkState)
                StartAudio();
        }
    }

    // -------------------------------
    // HELPER FUNCTIONS
    // -------------------------------

    private void ApplyBlinkState(bool state)
    {
        // WORLD LIGHTS
        foreach (var m in leftBlinkerMeshes)
            if (m) m.SetActive(leftOn && state);

        foreach (var m in rightBlinkerMeshes)
            if (m) m.SetActive(rightOn && state);

        // DASH ARROWS
        if (leftDashArrow)
            leftDashArrow.SetActive(leftOn && state);

        if (rightDashArrow)
            rightDashArrow.SetActive(rightOn && state);
    }

    private void StartAudio()
    {
        if (!blinkerSource || !blinkerLoop) return;

        if (!blinkerSource.isPlaying)
        {
            blinkerSource.clip = blinkerLoop;
            blinkerSource.loop = true;
            blinkerSource.Play();
        }
    }

    private void StopAudio()
    {
        if (!blinkerSource) return;
        blinkerSource.Stop();
    }
}
