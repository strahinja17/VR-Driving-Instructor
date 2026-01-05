using UnityEngine;

public class AICarScenarioReleaseTrigger : MonoBehaviour
{
    public AICarScenarioController scenario;
    public bool releaseOnce = true;

    bool didRelease = false;

    private void OnTriggerEnter(Collider other)
    {
        var telemetry = other.transform.root.GetComponent<TelemetryManager>();
        if (telemetry == null) return;

        if (releaseOnce && didRelease) return;
        didRelease = true;

        if (scenario != null)
            scenario.ReleaseFromStop();
    }
}
