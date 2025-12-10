using System.Collections;
using UnityEngine;

public class PedestrianSpawnTrigger : MonoBehaviour
{
    [Header("Pedestrian settings")]
    public GameObject pedestrianPrefab;
    public WaypointPath path;

    [Tooltip("Delay before the pedestrian actually appears (for timing with crosswalk / lights).")]
    public float spawnDelay = 0f;

    [Tooltip("Only spawn once, then disable this trigger.")]
    public bool spawnOnce = true;

    private bool hasSpawned = false;

    private void OnTriggerEnter(Collider other)
    {
        // Find the root object of whatever hit this trigger
        Transform root = other.transform.root;
        var telemetry = root.GetComponent<TelemetryManager>();

        if (telemetry == null)
            return;

        if (spawnOnce && hasSpawned)
            return;

        if (pedestrianPrefab == null || path == null)
            return;

        StartCoroutine(SpawnAfterDelay());
    }

    private IEnumerator SpawnAfterDelay()
    {
        hasSpawned = true;

        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        Transform[] wps = path.GetWaypoints();
        if (wps == null || wps.Length == 0 || wps[0] == null)
        {
            Debug.LogWarning($"{name}: Path has no valid waypoints.");
            yield break;
        }

        Vector3 pos = wps[0].position;
        Quaternion rot = Quaternion.identity;

        // Face towards 2nd waypoint if any
        if (wps.Length > 1 && wps[1] != null)
        {
            Vector3 dir = wps[1].position - wps[0].position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        GameObject ped = Instantiate(pedestrianPrefab, pos, rot);

        // Hook up the path on the walker
        PedestrianWalker walker = ped.GetComponent<PedestrianWalker>();
        if (walker != null)
        {
            walker.path = path;
            walker.loopContinuously = false;
            walker.destroyAtEnd = true;
        }
    }
}
