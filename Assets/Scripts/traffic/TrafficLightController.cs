using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    public enum LightState { Red, Yellow, Green, RedYellow }

    [Header("Cycle durations (seconds)")]
    public float greenDuration = 10f;
    public float yellowDuration = 3f;
    public float redDuration = 10f;

    [Header("Phase offset for this light (seconds)")]
    [Tooltip("Positive or negative. Lights with the same offset stay in sync.")]
    public float phaseOffset = 0f;

    [Header("Lamp renderers")]
    public Renderer redRenderer;
    public Renderer yellowRenderer;
    public Renderer greenRenderer;

    [Header("Lamp materials")]
    public Material redOn;
    public Material yellowOn;
    public Material greenOn;
    public Material offMaterial;

    public LightState CurrentState { get; private set; } = LightState.Red;

    private float TotalCycleDuration => greenDuration + 2 * yellowDuration + redDuration;

    private void Update()
    {
        UpdateStateFromTime();
        UpdateVisuals();
    }

    private void UpdateStateFromTime()
    {
        float t = (Time.time + phaseOffset) % TotalCycleDuration;
        if (t < 0f) t += TotalCycleDuration; // handle negative offsets

        if (t < greenDuration)
        {
            CurrentState = LightState.Green;
        }
        else if (t < greenDuration + yellowDuration)
        {
            CurrentState = LightState.Yellow;
        } else if (t < greenDuration + yellowDuration + redDuration)
        {
            CurrentState = LightState.Red;
        }
        else
        {
            CurrentState = LightState.RedYellow;
        }
    }

    private void UpdateVisuals()
    {
        if (redRenderer != null)
            redRenderer.sharedMaterial = (CurrentState == LightState.Red || CurrentState == LightState.RedYellow) ? redOn : offMaterial;

        if (yellowRenderer != null)
            yellowRenderer.sharedMaterial = (CurrentState == LightState.Yellow || CurrentState == LightState.RedYellow) ? yellowOn : offMaterial;

        if (greenRenderer != null)
            greenRenderer.sharedMaterial = (CurrentState == LightState.Green) ? greenOn : offMaterial;
    }
}
