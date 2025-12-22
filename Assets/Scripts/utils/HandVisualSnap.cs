using UnityEngine;

/// <summary>
/// Controls a visual hand model that normally follows a tracked pose,
/// but can be snapped to a target transform (e.g. steering wheel).
/// </summary>
[DisallowMultipleComponent]
public class HandVisualSnap : MonoBehaviour
{
    [Header("Tracked source")]
    [Tooltip("The real tracked hand/controller transform")]
    public Transform trackedPose;

    [Header("Snapping")]
    public bool snapped;
    public Transform snapTarget;

    [Header("Smoothing")]
    public float positionLerp = 30f;
    public float rotationLerp = 30f;

    void LateUpdate()
    {
        if (trackedPose == null)
            return;

        Transform target = snapped && snapTarget != null
            ? snapTarget
            : trackedPose;

        // Smooth follow to avoid jitter
        transform.position = Vector3.Lerp(
            transform.position,
            target.position,
            Time.deltaTime * positionLerp
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            target.rotation,
            Time.deltaTime * rotationLerp
        );
    }

    /// <summary>
    /// Snap the visual hand to a target pose.
    /// </summary>
    public void SnapTo(Transform target)
    {
        snapTarget = target;
        snapped = true;
    }

    /// <summary>
    /// Return the visual hand to following the tracked pose.
    /// </summary>
    public void Unsnap()
    {
        snapped = false;
        snapTarget = null;
    }
}
