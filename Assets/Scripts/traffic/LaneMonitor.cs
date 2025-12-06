using UnityEngine;
using System.Collections.Generic;

public class LaneMonitor : MonoBehaviour
{
    [Header("References")]
    public CarBlinkers blinker;    // assign player blinker script here

    [Header("Runtime State")]
    public LaneSpline boundLane;           // currently assigned lane after entry
    public bool hasEnteredBoundLane = false;

    public float lateralOffset;

    public bool laneExcursion;             // entering oncoming lane
    public bool improperLaneChange;        // adjacent lane entered without blinker
    public bool wrongLaneAtEntry;
    public bool wrongLaneAtExit;

    public bool inExitRegion;

    private HashSet<LaneZone> activeZones = new HashSet<LaneZone>();

    void Update()
    {
        if (boundLane != null)
            lateralOffset = boundLane.GetLateralOffset(transform.position);
        else
            lateralOffset = 0f;

        EvaluateZones();
    }

    void OnTriggerEnter(Collider other)
    {
        LaneZone zone = other.GetComponent<LaneZone>();
        if (zone == null || zone.parentSpline == null)
            return;

        activeZones.Add(zone);

        // ---------------------------------------------------
        // (1) ENTRY LOGIC — when the player enters a lane segment
        // ---------------------------------------------------
        if (zone.isEntry)
        {
            if (!hasEnteredBoundLane)
            {
                // Bind to the first lane entered
                boundLane = zone.parentSpline;
                hasEnteredBoundLane = true;
                Debug.Log("[LaneMonitor] Bound to lane: " + boundLane.name);
            }
            else
            {
                // Entering an entry box from a different lane
                if (inExitRegion)
                {
                    // We're exiting the old lane and entering a new one:
                    // treat as lane handoff, NOT as wrong entry.
                    boundLane = zone.parentSpline;
                    inExitRegion = false;
                    Debug.Log("[LaneMonitor] Lane handoff at intersection. New bound lane: " + boundLane.name);
                }
                else
                {
                    // Not in exit region → this really is a wrong entry
                    wrongLaneAtEntry = true;
                    Debug.Log("[LaneMonitor] WRONG entry: entered lane " + zone.parentSpline.name);
                }
            }
        }
        else
        {
            // Entering a non-entry zone BEFORE hitting the real entry
            if (!hasEnteredBoundLane)
            {
                wrongLaneAtEntry = true;
                Debug.Log("[LaneMonitor] WRONG lane before entry: " + zone.parentSpline.name);
            }
        }

        // ---------------------------------------------------
        // (2) EXIT LOGIC — exiting via the wrong lane
        // ---------------------------------------------------
        if (zone.isExit)
        {
            if (zone.parentSpline != boundLane)
            {
                wrongLaneAtExit = true;
                Debug.Log("[LaneMonitor] WRONG EXIT: " + zone.parentSpline.name);
            }
            else
            {
                inExitRegion = true;
                Debug.Log("[LaneMonitor] Exit region of: " + boundLane.name);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        LaneZone zone = other.GetComponent<LaneZone>();
        if (zone == null) return;

        activeZones.Remove(zone);

        if (zone.isExit && zone.parentSpline == boundLane)
            inExitRegion = false;
    }

    void EvaluateZones()
    {
        HashSet<LaneSpline> overlapping = new HashSet<LaneSpline>();
        HashSet<LaneSpline> adjacentHits = new HashSet<LaneSpline>();
        ZoneType situation = ZoneType.None;

        foreach (var zone in activeZones)
        {
            if (zone.parentSpline != null)
                overlapping.Add(zone.parentSpline);

            if (zone.adjacentLane != null)
                adjacentHits.Add(zone.adjacentLane);
        }

        // ---------------------------------------------------
        // (3) LANE EXCURSION — overlapping different lanes mid-segment
        // ---------------------------------------------------
        laneExcursion = (overlapping.Count > 1 && !inExitRegion);

        if (laneExcursion)
            Debug.Log("[LaneMonitor] LANE EXCURSION!");

        // 3) ADJACENT LANE LOGIC — lane change detection
        improperLaneChange = false;

        foreach (var zone in activeZones)
        {
            if (zone.adjacentLane == null) continue;

            // We're touching a lane boundary that connects boundLane -> adjacentLane
            if (boundLane != null && zone.parentSpline == boundLane)
            {
                LaneSpline target = zone.adjacentLane;

                // Are we touching the adjacent lane?
                bool touchingAdjacent = false;
                foreach (var z in activeZones)
                {
                    if (z.parentSpline == target)
                    {
                        touchingAdjacent = true;
                        break;
                    }
                }

                if (touchingAdjacent)
                {
                    // Determine required blinker
                    bool requiredBlinker = zone.isLeftSideAdjacency ?
                                        (blinker && blinker.leftOn) :
                                        (blinker && blinker.rightOn);

                    if (!requiredBlinker)
                    {
                        improperLaneChange = true;
                        Debug.Log("[LaneMonitor] Improper lane change: no blinker");
                    }

                    // COMPLETED LANE CHANGE — we are fully in the adjacent lane
                    bool fullyLeftBoundLane = true;
                    foreach (var z in activeZones)
                    {
                        if (z.parentSpline == boundLane)
                        {
                            fullyLeftBoundLane = false;
                            break;
                        }
                    }

                    if (fullyLeftBoundLane)
                    {
                        // Rebind to new lane
                        boundLane = target;
                        Debug.Log("[LaneMonitor] Lane-change complete. New bound lane: " + boundLane.name);
                    }
                }
            }
        }

    }

    enum ZoneType
    {
        None,
        Home,
        Adjacent,
        Oncoming
    }
}
