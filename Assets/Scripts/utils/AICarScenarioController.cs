using UnityEngine;

public class AICarScenarioController : MonoBehaviour
{
    [Header("References")]
    public AICarDriver_Scenario driver;

    [Header("Scenario")]
    [Tooltip("Teleport car to this waypoint when scenario starts.")]
    public int spawnAtWaypointIndex = 0;

    [Tooltip("Driver will stop and hold when it reaches this waypoint.")]
    public int stopAndWaitAtWaypointIndex = 1;

    [Tooltip("Optional: if true, hide car before start (disable renderers).")]
    public bool startHidden = true;

    bool hasStarted = false;

    void Reset()
    {
        driver = GetComponent<AICarDriver_Scenario>();
    }

    void Awake()
    {
        if (driver == null) driver = GetComponent<AICarDriver_Scenario>();

        if (startHidden)
            SetVisible(false);

        // keep driver paused until spawned
        if (driver != null)
            driver.SetPaused(true);
    }

    public void StartScenario()
    {
        if (hasStarted) return;
        if (driver == null) return;

        hasStarted = true;

        SetVisible(true);

        // Put on spawn waypoint and begin driving.
        driver.SnapToWaypoint(spawnAtWaypointIndex, faceNext: true);
        driver.SetStopAtWaypoint(stopAndWaitAtWaypointIndex);

        driver.SetPaused(false);
    }

    public void ReleaseFromStop()
    {
        if (driver == null) return;

        // Let it continue past the hold point.
        driver.ClearStopHold();
        driver.SetPaused(false);
    }

    public void ForceStopNow()
    {
        if (driver == null) return;
        driver.SetPaused(true);
    }

    void SetVisible(bool visible)
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
            r.enabled = visible;
    }
}
