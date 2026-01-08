using UnityEngine;

// Put this on a small trigger/collider at the stop line in front of a light
public class AITrafficLightStop : MonoBehaviour
{
    public new TrafficLightController light;  // your existing script

    public bool IsRed()
    {
        // adapt this to your actual API
        // e.g. if you have an enum:
        // return light.currentState == TrafficLightState.Red;
        return light != null && light.IsRed(); 
    }
}
