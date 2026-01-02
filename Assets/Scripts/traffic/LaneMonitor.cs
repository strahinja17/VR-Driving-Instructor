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

    public bool laneChangeInProgress;

    public bool inExitRegion;

    public LaneChangeCheckArmer laneChangeChecks;

    private HashSet<LaneZone> activeZones = new HashSet<LaneZone>();

    private bool AIMode;

    public AudioClip laneExcur;

    void Start()
    {
        AIMode = StudyConditionManager.Instance.IsAIEnabled;
    }

    void Update()
    {
        EvaluateZones();
        // Debug.Log("[LaneMonitor] Active zones: " + activeZones.Count);
    }

    public void ProbeEnter(Collider other)
    {
        LaneZone zone = other.GetComponent<LaneZone>();
        if (zone == null) zone = other.GetComponentInParent<LaneZone>();
        if (zone == null || zone.parentSpline == null) return;

        activeZones.Add(zone);
        if (zone.isEntry)
        {
            boundLane = zone.parentSpline;
            inExitRegion = false;
            hasEnteredBoundLane = true;
            Debug.Log("[LaneMonitor] Bound to lane: " + boundLane.name);
        }
         if (zone.isExit)
        {
            inExitRegion = true;
            Debug.Log("[LaneMonitor] Exit region of: " + boundLane.name != null ? boundLane.name : "null");
        }
    }

    public void ProbeExit(Collider other)
    {
        LaneZone zone = other.GetComponent<LaneZone>();
        if (zone == null) zone = other.GetComponentInParent<LaneZone>();
        if (zone == null) return;

        activeZones.Remove(zone);

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


    // void OnTriggerEnter(Collider other)
    // {
    //     LaneZone zone = other.GetComponent<LaneZone>();
    //     if (zone == null || zone.parentSpline == null)
    //         return;

    //     activeZones.Add(zone);

    //     // ---------------------------------------------------
    //     // (1) ENTRY LOGIC — when the player enters a lane segment
    //     // ---------------------------------------------------
    //     if (zone.isEntry)
    //     {
    //         boundLane = zone.parentSpline;
    //         inExitRegion = false;
    //         hasEnteredBoundLane = true;
    //         Debug.Log("[LaneMonitor] Bound to lane: " + boundLane.name);

    //         // if (!hasEnteredBoundLane)
    //         // {
    //         //     // Bind to the first lane entered
    //         //     boundLane = zone.parentSpline;
    //         //     hasEnteredBoundLane = true;
    //         //     Debug.Log("[LaneMonitor] Bound to lane: " + boundLane.name);
    //         // }
    //         // else
    //         // {
    //             // // Entering an entry box from a different lane
    //             // if (inExitRegion)
    //             // {
    //             //     // We're exiting the old lane and entering a new one:
    //             //     // treat as lane handoff, NOT as wrong entry.
    //                 // boundLane = zone.parentSpline;
    //                 // inExitRegion = false;
    //                 // Debug.Log("[LaneMonitor] Lane handoff at intersection. New bound lane: " + boundLane.name);
    //             // }
    //             // else
    //             // {
    //             //     // Not in exit region → this really is a wrong entry
    //             //     wrongLaneAtEntry = true;
    //             //     Debug.Log("[LaneMonitor] WRONG entry: entered lane " + zone.parentSpline.name);
    //             // }
    //         // }
    //     }
    //     // else
    //     // {
    //     //     // Entering a non-entry zone BEFORE hitting the real entry
    //     //     if (!hasEnteredBoundLane)
    //     //     {
    //     //         wrongLaneAtEntry = true;
    //     //         Debug.Log("[LaneMonitor] WRONG lane before entry: " + zone.parentSpline.name);
    //     //         if(AIMode) {
    //     //             DrivingAIInstructorHub.Instance.NotifyDrivingEvent(
    //     //             eventName: "LaneWarning",
    //     //             playerUtterance: null,
    //     //             extraInstruction: "Tell the player to be careful when entering a new lane in the intersection, not to cut accross the opposing lane, few words!!");
    //     //         }
    //     //     }
    //     // }

    //     // ---------------------------------------------------
    //     // (2) EXIT LOGIC — exiting via the wrong lane
    //     // ---------------------------------------------------
    //     if (zone.isExit)
    //     {
    //         // if (zone.parentSpline != boundLane)
    //         // {
    //         //     wrongLaneAtExit = true;
    //         //     Debug.Log("[LaneMonitor] WRONG EXIT: " + zone.parentSpline.name);
    //         //     if (AIMode) {
    //         //         DrivingAIInstructorHub.Instance.NotifyDrivingEvent(
    //         //         eventName: "LaneWarning",
    //         //         playerUtterance: null,
    //         //         extraInstruction: "Tell the player to be careful when exiting a lane in the intersection, not to cut accross the opposing lane, few words!!");
    //         //     }
            
    //         // }
    //         // else
    //         // {
    //             inExitRegion = true;
    //             Debug.Log("[LaneMonitor] Exit region of: " + boundLane.name != null ? boundLane.name : "null");
    //         // }
    //     }
    // }

    // void OnTriggerExit(Collider other)
    // {
    //     LaneZone zone = other.GetComponent<LaneZone>();
    //     if (zone == null) return;

    //     activeZones.Remove(zone);

    //     // If this zone belongs to the current bound lane, check if we left it completely
    //     if (boundLane != null && zone.parentSpline == boundLane)
    //     {
    //         bool stillInBoundLane = false;

    //         foreach (var z in activeZones)
    //         {
    //             if (z.parentSpline == boundLane)
    //             {
    //                 stillInBoundLane = true;
    //                 break;
    //             }
    //         }

    //         // We've left the LAST zone of the bound lane
    //         if (!stillInBoundLane)
    //         {
    //             // And we were exiting that lane (either via an exit zone or exit region)
    //             if (zone.isExit || inExitRegion)
    //             {
    //                 Debug.Log("[LaneMonitor] Fully exited lane " + boundLane.name + " → unbinding.");
    //                 boundLane = null;
    //                 hasEnteredBoundLane = false;
    //                 inExitRegion = false;
    //             }
    //         }
    //     }

    //     // Keep this so inExitRegion only applies while you're actually inside the exit box
    //     if (zone.isExit && zone.parentSpline == boundLane)
    //     {
    //         inExitRegion = false;
    //     }
    // }


    void EvaluateZones()
    {
        HashSet<LaneSpline> overlapping = new HashSet<LaneSpline>();
        // ZoneType situation = ZoneType.None;

        foreach (var zone in activeZones)
        {
            if (zone.parentSpline != null)
                overlapping.Add(zone.parentSpline);
        }

        // ---------------------------------------------------
        // (3) LANE EXCURSION — overlapping different lanes mid-segment
        // ---------------------------------------------------
        // If NOT bound yet and overlapping multiple lanes,
        // choose the one that is NOT the wrongEntryLane.
        // if (!hasEnteredBoundLane && overlapping.Count > 1)
        // {
        //     foreach (var lane in overlapping)
        //     {
        //         if (lane != wrongLaneAtEntry)
        //         {
        //             boundLane = lane;
        //             hasEnteredBoundLane = true;
        //             wrongLaneAtEntry = false;
        //             Debug.Log("[LaneMonitor] Auto-bound to corrected lane: " + boundLane.name);
        //             break;
        //         }
        //     }
        // }

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
                        if (AIMode) {
                            DrivingAIInstructorHub.Instance.NotifyDrivingEvent(
                                eventName: "LaneWarning",
                                playerUtterance: null,
                                extraInstruction: "Tell the player to always use blinkers properlly when changing lanes, in very few words!!");
                        }

                    }
                    else
                    {

                        bool checksOk = true;
                        bool missingMirror = false;
                        bool missingShoulder = false;

                        if (laneChangeChecks != null)
                        {
                            var res = laneChangeChecks.EvaluateForLaneChange(blinker.leftOn);
                            checksOk = res.passed;
                            missingMirror = res.missingMirror;
                            missingShoulder = res.missingShoulder;
                        }

                        if (!checksOk)
                        {
                            improperLaneChange = true;
                            Debug.Log($"[LaneMonitor] Improper lane change: missing checks. mirrorMissing={missingMirror} shoulderMissing={missingShoulder}");

                            if (AIMode)
                            {
                                // Keep it short. You can branch messaging based on what was missing.
                                string instr =
                                    missingMirror && missingShoulder ? "Tell the driver to check the mirror AND shoulder before changing lanes, very briefly." :
                                    missingMirror ? "Tell the driver to check the mirror before changing lanes, very briefly." :
                                    "Tell the driver to do a shoulder check before changing lanes, very briefly.";

                                DrivingAIInstructorHub.Instance.NotifyDrivingEvent(
                                    eventName: "LaneChange",
                                    playerUtterance: null,
                                    extraInstruction: instr
                                );
                            }
                            else
                            {
                                // Optional: play scripted clip here if you have one
                                // GlobalInstructorAudio.Play(missingCheckClip);
                            }
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
            if (AIMode) {
                DrivingAIInstructorHub.Instance.NotifyDrivingEvent(
                                eventName: "LaneChange",
                                playerUtterance: null,
                                extraInstruction: "Acknowledge proper lane change, in very few words.");
            }
        }

        if (laneExcursion && !properLaneChange)
        {
            // Debug.Log("[LaneMonitor] LANE EXCURSION!");
            if (AIMode) {
                DrivingAIInstructorHub.Instance.NotifyDrivingEvent(
                                eventName: "LaneWarning",
                                playerUtterance: null,
                                extraInstruction: "Alert the player that they are stepping out of their lane, shortly and with authority");
            } else
            {
                GlobalInstructorAudio.Play(laneExcur);
            }

            Debug.Log("[LaneMonitor] Lane excursion!");
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
