using UnityEngine;

public class AICarScenarioStartTrigger : MonoBehaviour
{
    public AICarScenarioController scenario;
    public bool startOnce = true;

    bool didStart = false;

    private void OnTriggerEnter(Collider other)
    {
        var telemetry = other.transform.root.GetComponent<TelemetryManager>();
        if (telemetry == null) return;

        if (startOnce && didStart) return;
        didStart = true;

        if (scenario != null)
            scenario.StartScenario();
    }
}
