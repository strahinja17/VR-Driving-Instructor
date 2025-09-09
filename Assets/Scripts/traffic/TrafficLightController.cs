using UnityEngine;

public enum SignalState { Green, Yellow, Red }

public class TrafficLightController : MonoBehaviour
{
    public SignalState state = SignalState.Red;
    public float greenTime = 6f, yellowTime = 2f, redTime = 6f;
    float t;

    void Update()
    {
        t += Time.deltaTime;
        switch (state)
        {
            case SignalState.Green: if (t >= greenTime)  { state = SignalState.Yellow; t = 0f; } break;
            case SignalState.Yellow: if (t >= yellowTime) { state = SignalState.Red;    t = 0f; } break;
            case SignalState.Red:   if (t >= redTime)    { state = SignalState.Green;  t = 0f; } break;
        }
    }
}
