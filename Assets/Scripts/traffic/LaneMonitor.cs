using UnityEngine;
using System.Collections.Generic;

public class LaneMonitor : MonoBehaviour
{
    [Header("References")]
    public CarBlinkers blinker;    // assign player blinker script here

    [Header("Runtime State")]
    public LaneSpline boundLane;           // currently assigned lane after entry
    public bool hasEnteredBoundLane = false;

    public bool laneExcursion;             // entering oncoming lane
    public bool improperLaneChange;        // adjacent lane entered without blinker
    public bool wrongLaneAtEntry;
    public bool wrongLaneAtExit;

    public bool laneChangeInProgress;

    public bool inExitRegion;

    private HashSet<LaneZone> activeZones = new HashSet<LaneZone>();

    void Update()
    {
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
                // // Entering an entry box from a different lane
                // if (inExitRegion)
                // {
                //     // We're exiting the old lane and entering a new one:
                //     // treat as lane handoff, NOT as wrong entry.
                    boundLane = zone.parentSpline;
                    inExitRegion = false;
                    Debug.Log("[LaneMonitor] Lane handoff at intersection. New bound lane: " + boundLane.name);
                // }
                // else
                // {
                //     // Not in exit region → this really is a wrong entry
                //     wrongLaneAtEntry = true;
                //     Debug.Log("[LaneMonitor] WRONG entry: entered lane " + zone.parentSpline.name);
                // }
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

        // If this zone belongs to the current bound lane, check if we left it completely
        if (boundLane != null && zone.parentSpline == boundLane)
        {
            bool stillInBoundLane = false;

            foreach (var z in activeZones)
            {
                if (z.parentSpline == boundLane)
                {
                    stillInBoundLane = true;
                    break;
                }
            }

            // We've left the LAST zone of the bound lane
            if (!stillInBoundLane)
            {
                // And we were exiting that lane (either via an exit zone or exit region)
                if (zone.isExit || inExitRegion)
                {
                    Debug.Log("[LaneMonitor] Fully exited lane " + boundLane.name + " → unbinding.");
                    boundLane = null;
                    hasEnteredBoundLane = false;
                    inExitRegion = false;
                }
            }
        }

        // Keep this so inExitRegion only applies while you're actually inside the exit box
        if (zone.isExit && zone.parentSpline == boundLane)
        {
            inExitRegion = false;
        }
    }


    void EvaluateZones()
    {
        HashSet<LaneSpline> overlapping = new HashSet<LaneSpline>();
        HashSet<LaneSpline> adjacentHits = new HashSet<LaneSpline>();
        // ZoneType situation = ZoneType.None;

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
        // If NOT bound yet and overlapping multiple lanes,
        // choose the one that is NOT the wrongEntryLane.
        if (!hasEnteredBoundLane && overlapping.Count > 1)
        {
            foreach (var lane in overlapping)
            {
                if (lane != wrongLaneAtEntry)
                {
                    boundLane = lane;
                    hasEnteredBoundLane = true;
                    wrongLaneAtEntry = false;
                    Debug.Log("[LaneMonitor] Auto-bound to corrected lane: " + boundLane.name);
                    break;
                }
            }
        }

        // Now re-evaluate excursion only AFTER binding
        laneExcursion = (hasEnteredBoundLane && overlapping.Count > 1 && !inExitRegion);

        // 3) ADJACENT LANE LOGIC — lane change detection
        improperLaneChange = false;
        bool properLaneChange = false;

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
                                        (blinker && blinker.rightOn) :
                                        (blinker && blinker.leftOn);

                    if (!requiredBlinker)
                    {
                        improperLaneChange = true;
                        Debug.Log("[LaneMonitor] Improper lane change: no blinker");
                    }
                    else
                    {
                        properLaneChange = true;
                        laneChangeInProgress = true;
                        Debug.Log("[LaneMonitor] Proper lane change initiated towards " + target.name);
                    }
                }
            }
        }

        if (laneChangeInProgress && activeZones.Count == 1)
        {
            // Completed lane change
            laneChangeInProgress = false;
            hasEnteredBoundLane = true;
            foreach (var zone in activeZones)
            {
                boundLane = zone.parentSpline;
                break; // Exit after the first element
            }            
            Debug.Log("[LaneMonitor] Lane change complete. New bound lane: " + boundLane.name);
        }

        if (laneExcursion && !properLaneChange)
            Debug.Log("[LaneMonitor] LANE EXCURSION!");

    }

    enum ZoneType
    {
        None,
        Home,
        Adjacent,
        Oncoming
    }
}
