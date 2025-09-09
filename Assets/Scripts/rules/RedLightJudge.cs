using UnityEngine;

public class RedLightJudge : MonoBehaviour
{
    public string stopLineId = "stop_1";
    public TrafficLightController lightRef;

    void OnEnable()  => DrivingEvents.OnStopLineCrossed += Check;
    void OnDisable() => DrivingEvents.OnStopLineCrossed -= Check;

    void Check(string id)
    {
        if (id != stopLineId) return;
        if (!lightRef) return;

        if (lightRef.state == SignalState.Red || lightRef.state == SignalState.Yellow)
            Debug.LogWarning($"🚨 Red-light violation at {id}");
        else
            Debug.Log($"✅ Stop-line crossed on {lightRef.state}");
    }
}
