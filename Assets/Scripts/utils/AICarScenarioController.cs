using UnityEngine;

public class AICarScenarioController : MonoBehaviour
{
    [Header("References")]
    public AICarDriver_Scenario driver;

    [Header("Scenario")]
    public int spawnAtWaypointIndex = 0;
    public int stopAndWaitAtWaypointIndex = 1;
    public int stopAndWaitAtWaypointSecondIndex = 1;
    public bool startHidden = true;

    bool hasStarted = false;
    public bool stopTwice = false;

    void Reset()
    {
        driver = GetComponent<AICarDriver_Scenario>();
    }

    void Awake()
    {
        if (driver == null) driver = GetComponent<AICarDriver_Scenario>();
        if (driver == null)
        {
            Debug.LogError($"[{name}] ScenarioController has no driver reference.");
            enabled = false;
            return;
        }

        if (startHidden) SetVisible(false);
    }

    void Start()
    {
        // IMPORTANT: do this in Start so driver.Awake has definitely run
        if (driver != null)
            driver.SetPaused(true);
    }

    public void StartScenario()
    {
        if (hasStarted) return;
        if (driver == null) return;

        hasStarted = true;

        Debug.Log($"[SCENARIO] StartScenario on {name} | spawn={spawnAtWaypointIndex} stop={stopAndWaitAtWaypointIndex}");

        SetVisible(true);

        driver.SnapToWaypoint(spawnAtWaypointIndex, faceNext: true);
        driver.SetStopAtWaypoint(stopAndWaitAtWaypointIndex);
        driver.SetPaused(false);
    }

    public void ReleaseFromStop()
    {
        if (driver == null) return;

        Debug.Log($"[SCENARIO] ReleaseFromStop on {name} | holding={driver.IsHoldingAtStopPoint()} idx={driver.CurrentWaypointIndex} paused={driver.Paused}");

        // This is the critical call (consumes stop waypoint if we were holding)
        driver.ClearStopHoldConsumeAndGo();

        driver.SetPaused(false);

        // Optional: if player is near bumper / intersection proxy, grace period
        driver.IgnoreSensorsFor(0.75f);

        Debug.Log($"[SCENARIO] After release | holding={driver.IsHoldingAtStopPoint()} idx={driver.CurrentWaypointIndex} paused={driver.Paused}");
        
        if (stopTwice)
            driver.SetStopAtWaypoint(stopAndWaitAtWaypointIndex);
    }

    void SetVisible(bool visible)
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
            r.enabled = visible;
    }
}
